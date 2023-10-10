namespace Kuantech.Core.Utils
{
    public class LockVariable
    {
        private int _lockAmount;
        private float _lastLockTime;
        
        public void Lock()
        {
            _lockAmount++;
        }

        public void Unlock()
        {
            _lockAmount--;
        }
        public void Reset()
        {
            _lockAmount = 0;
        }

        public bool IsLocked()
        {
            return _lockAmount != 0;
        }
    }
    
}