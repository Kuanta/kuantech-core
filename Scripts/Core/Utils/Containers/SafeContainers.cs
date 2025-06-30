using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Kuantech.Core.Utils
{
    public class SafeCollection<TCollection, T> : ISafeCollection<T>
        where TCollection : ICollection<T>, IEnumerable<T>, new()
    {
        protected readonly TCollection items = new();
        protected readonly HashSet<T> toRemove = new();

        public virtual void Add(T item)
        {
            if (!items.Contains(item))
                items.Add(item);
        }

        public virtual void Remove(T item)
        {
            if (items.Contains(item))
                toRemove.Add(item);
        }

        public virtual void FlushRemovals()
        {
            foreach (var item in toRemove)
            {
                items.Remove(item);
            }

            toRemove.Clear();
        }

        public virtual void Clear()
        {
            items.Clear();
            toRemove.Clear();
        }

        public virtual int Count => items.Count - toRemove.Count;

        public IEnumerator<T> GetEnumerator() => items.ToList().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    public class SafeList<T> : SafeCollection<List<T>, T> { }
    
    public class SafeHashSet<T> : SafeCollection<HashSet<T>, T> { }

}