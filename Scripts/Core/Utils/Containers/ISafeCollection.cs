using System.Collections.Generic;

namespace Kuantech.Core.Utils
{
    public interface ISafeCollection<T> : IEnumerable<T>
    {
        void Add(T item);
        void Remove(T item);
        void FlushRemovals();
        void Clear();
        int Count { get; }
    }
}