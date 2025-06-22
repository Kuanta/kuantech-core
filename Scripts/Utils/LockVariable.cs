using System.Collections.Generic;

namespace Kuantech.Core.Utils
{
    public class LockVariable
    {
        private float _lastLockTime;
        
        private HashSet<string> _lockingSources = new HashSet<string>();
        
        public void Lock(string locker)
        {
            _lockingSources.Add(locker);
        }

        public void Unlock(string locker)
        {
            _lockingSources.Remove(locker);
        }
        public void Reset()
        {
            _lockingSources.Clear();
            ;
        }

        public bool IsLocked()
        {
            return _lockingSources.Count > 0;
        }
    }
    
}