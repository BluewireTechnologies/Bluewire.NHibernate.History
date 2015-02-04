namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    public class ComponentType
    {
        protected bool Equals(ComponentType other)
        {
            return string.Equals(String, other.String) && Integer == other.Integer;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ComponentType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((String != null ? String.GetHashCode() : 0)*397) ^ Integer;
            }
        }

        public virtual string String {get; set;}
        public virtual int Integer {get; set;}
    }
}