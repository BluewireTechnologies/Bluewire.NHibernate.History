param(
    [Parameter(Mandatory=$True)]
    [string]$rootPath
)



function Make-RelativeTo {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)]
        [string]$referencePath,
        
        [Parameter(Mandatory=$True, ValueFromPipeline=$True)]
        [string[]]$pipePaths,
        
        [Parameter(ValueFromRemainingArguments=$true)]
        [string[]]$paths
    )
    BEGIN {
        function _GetAncestry($p, [switch]$skipFile)
        {
            $p = Get-Item $p;
            if(!$p) { return; }
            function _List($i)
            {
                if(!$i.PSIsContainer)
                {
                    if(!$skipFile) { $i; }
                    $i = $i.Directory;
                }
                while($i) { $i; $i = $i.Parent; }
            }
            $l = _List $p;
            return $l[$l.Count..0];
        }
        
        $referencePathParts = _GetAncestry $referencePath -skipFile;
    }
    PROCESS {
        function _Relativise($path)
        {
            $pathParts = _GetAncestry $path;
            if($pathParts[0].Name -ne $referencePathParts[0].Name)
            {
                # Absolute.
                return (Get-Item $path).FullName.TrimEnd('\');
            }
            $i = 0;
            while($pathParts[$i].Name -eq $referencePathParts[$i].Name)
            {   
                $i++;
                if($i -gt $pathParts.Length -and $i -gt $referencePathParts.Length) 
                {
                    return "";
                }
            }
            
            $downPath = @($pathParts[$i..$pathParts.Length] | %{$_.Name}) -join '\';
            
            $upDistance = $referencePathParts.Length - $i;
            if($upDistance -gt 0)
            {
                $upPath = @(1..$upDistance | %{ '..' }) -join '\';
                if($downPath) { return $upPath + '\' + $downPath; }
                return $upPath;
            }
            return $downPath;
        }
        
        if($pipePaths.Length -gt 0)
        {
            $paths = $pipePaths;
        }
        foreach($_ in $paths)
        {
            if($_)
            {
                _Relativise $_;
            }
        }
    }
}

function Find-ProjectFiles() # $containers...
{
    $containers = $args;
    foreach($container in $args)
    {
        foreach($containerPath in @(Resolve-Path "${rootPath}\${container}"))
        {
            Get-ChildItem -recurse -filter *.csproj $containerPath | %{ $_.FullName };
        }
    }
}

function Interpret-PathRelativeToFile {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)]
        [string]$referenceFilePath,
        
        [Parameter(ValueFromRemainingArguments=$true)]
        [string[]]$paths
    )
    BEGIN {
        $file = Get-Item $referenceFilePath;
        $referenceDir = $file.Directory;
    }
    PROCESS {
        foreach($_ in $paths)
        {
            
            $p = "${referenceDir}\$_";
            if(Test-Path $p) {
                (Get-Item $p).FullName;
            }
            else { $p; } # Don't canonicalise if nonexistent.
        }
    }
    END {}
}

filter Fixup-ProjectFileImports([string]$globalPropsFile, [string]$globalTargetsFile)
{
    $projectFile = Get-Item $_;
    if(!$projectFile) { return; } # Not present?!
   
    $projectFileXml = [xml](Get-Content $projectFile);
    
    $imports = @($projectFileXml.Project.Import);
    $projectFileXmlDocument = $projectFileXml.Project.OwnerDocument;
    
    $unconditionalImports = $imports | where { -not $_.Condition; };
    $msImports = @($unconditionalImports | where { $_.Project -match "\bMicrosoft\b"; });
    $xamarinImports = @($unconditionalImports | where { $_.Project -match "\bXamarin\b"; });
    $defaultImport = ($msImports + $xamarinImports)[0];
    if(!$defaultImport -and !$projectFileXml.Project.Sdk)
    {
        Write-Host -ForegroundColor Yellow "WARNING: Unable to find default import or Sdk attribute in $(${projectFile}.FullName)";
        return;
    }
    
    Write-Host "Processing $(${projectFile}.FullName)...";
    
    function _FindImport($fullPath)
    {
        @($unconditionalImports | where { $fullPath -eq $(Interpret-PathRelativeToFile $projectFile $_.Project) })[0];
    }

    $projectFileChanged = $false;

    function Insert-AroundDefaultImport([string]$before, [string]$after, [ref]$changed)
    {
        function _CreateImport($path)
        {
            $importNode = $projectFileXmlDocument.CreateElement("Import", $projectFileXmlDocument.DocumentElement.NamespaceURI);
            $importNode.SetAttribute("Project", $path);
            return $importNode;
        }
    
        if($before -and (Test-Path $before))
        {
            $existingBefore = _FindImport $before;
            if(!$existingBefore)
            {
                $relativeBefore = Make-RelativeTo $projectFile $before;
                $importNode = _CreateImport $relativeBefore;
                if ($defaultImport)
                {
                    $defaultImport.ParentNode.InsertBefore($importNode, $defaultImport) | Out-Null;
                }
                else
                {
                    $projectFileXml.Project.AppendChild($importNode) | Out-Null;
                }
                Write-Host "   Added $relativeBefore";
                $changed.Value = $true;
            }
        }
        
        if($after -and (Test-Path $after))
        {
            $existingAfter = _FindImport $after;
            if(!$existingAfter)
            {
                $relativeAfter = Make-RelativeTo $projectFile $after;
                $importNode = _CreateImport $relativeAfter;
                if ($defaultImport)
                {
                    $defaultImport.ParentNode.InsertAfter($importNode, $defaultImport) | Out-Null;
                }
                else
                {
                    $projectFileXml.Project.AppendChild($importNode) | Out-Null;
                }
                Write-Host "   Added $relativeAfter";
                $changed.Value = $true;
            }
        }
    }

    Insert-AroundDefaultImport $globalPropsFile $globalTargetsFile ([ref]$projectFileChanged);
    
    foreach($import in $imports)
    {
        if($import.Project -match "\$\(") { continue; }
        if(Test-Path (Interpret-PathRelativeToFile $projectFile $import.Project)) { continue; }
        Write-Host -ForegroundColor Yellow "   Missing file: $(${import}.Project)";
    }

    if ($projectFileChanged)
    {
        $projectFileXml.Save($projectFile.FullName);
    }
}

if(!$rootPath) 
{
    throw "No root path specified.";
}
if(-not (Test-Path $rootPath))
{
    throw "Root path does not exist: $rootPath";
}
$rootPath = $(Get-Item $rootPath).FullName;

$allProjects = Find-ProjectFiles .;
$allProjects | Fixup-ProjectFileImports "${rootPath}\StyleCopAnalyzers.props";
