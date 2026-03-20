using System.Collections.Generic;

namespace Kuantech.Core.Utils
{
    public class LockVariable
    {
        private float _lastLockTime;
        
        private HashSet<object> _lockingSources = new HashSet<object>();
        
        public void Lock(object locker)
        {
            if (_lockingSources.Contains(locker)) return;
            _lockingSources.Add(locker);
        }

        public void Unlock(object locker)
        {
            _lockingSources.Remove(locker);
        }
        public void Reset()
        {
            _lockingSources.Clear();
        }

        public bool IsLocked()
        {
            return _lockingSources.Count > 0;
        }
    }
    
}