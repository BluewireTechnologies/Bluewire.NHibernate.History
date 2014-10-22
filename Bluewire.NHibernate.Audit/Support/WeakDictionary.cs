using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bluewire.NHibernate.Audit.Support
{
    /// <summary>
    /// A limited dictionary implementation which does not prevent GC of its keys.
    /// General design taken from Lucene.NET's WeakDictionary class.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class WeakDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<WeakKey, TValue> inner = new ConcurrentDictionary<WeakKey, TValue>(new WeakKey.Comparer());
        private int gcCount;
        private readonly object cleanLock = new object();

        struct WeakKey
        {
            public WeakKey(TKey obj)
            {
                if (ReferenceEquals(obj, null)) throw new ArgumentNullException("obj");
                hashcode = obj.GetHashCode();
                reference = new WeakReference(obj);
            }

            private readonly WeakReference reference;
            private readonly int hashcode;

            public bool IsAlive { get { return reference.IsAlive; } }

            public class Comparer : IEqualityComparer<WeakKey>
            {
                public bool Equals(WeakKey x, WeakKey y)
                {
                    if (x.hashcode != y.hashcode) return false;

                    var xObj = x.reference.Target;
                    if (!x.IsAlive) return false;
                    var yObj = y.reference.Target;
                    if (!y.IsAlive) return false;

                    return xObj.Equals(yObj);
                }

                public int GetHashCode(WeakKey obj)
                {
                    return obj.hashcode;
                }
            }
        }

        public TValue GetOrAdd(TKey key, Func<TValue> getValue)
        {
            MaybeClean();
            return inner.GetOrAdd(new WeakKey(key), f => getValue());
        }

        public TValue AddOrUpdate(TKey key, TValue value)
        {
            MaybeClean();
            return inner.AddOrUpdate(new WeakKey(key), value, (k, v) => value);
        }

        public void Clean()
        {
            if (inner.Count == 0) return;
            var locked = false;
            try
            {
                Monitor.TryEnter(cleanLock, ref locked);
                if (!locked) return;

                var deadKeys = inner.Keys.Where(k => !k.IsAlive);
                foreach (var k in deadKeys)
                {
                    TValue v;
                    inner.TryRemove(k, out v);
                }
                gcCount = GC.CollectionCount(0);
            }
            finally
            {
                if (locked) Monitor.Exit(cleanLock);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return inner.TryGetValue(new WeakKey(key), out value);
        }

        private void MaybeClean()
        {
            if (gcCount < GC.CollectionCount(0))
            {
                Clean();
            }
        }

        public bool ContainsKey(TKey key)
        {
            return inner.ContainsKey(new WeakKey(key));
        }

        public bool Remove(TKey key)
        {
            MaybeClean();
            TValue v;
            return inner.TryRemove(new WeakKey(key), out v);
        }

        public void Clear()
        {
            inner.Clear();
        }

        public int Count
        {
            get { return inner.Count; }
        }
    }
}
