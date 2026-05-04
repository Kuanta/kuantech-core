using System.Collections.Generic;
using Kuantech.Core.Utils;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class LockModule : ActorModule
    {
        //Events
        public UnityAction<string> OnLocked;
        public UnityAction<string> OnUnlocked;
        
        //Runtime
        private Dictionary<string, LockVariable> _locks = new Dictionary<string, LockVariable>();
        public override void Initialize()
        {
            base.Initialize();
            _locks = new Dictionary<string, LockVariable>();
        }

        public void RegisterLock(string lockKey)
        {
            _locks[lockKey] = new LockVariable();
        }

        public bool IsLocked(LockKey lockKey)
        {
            if(lockKey == null) return false;
            return IsLocked(lockKey.LockId);
        }

        public bool IsLocked(string lockKey)
        {
            if(!_locks.ContainsKey(lockKey)) return false;
            if(_locks[lockKey] == null) return false;
            return _locks[lockKey].IsLocked();
        }

        public LockVariable GetLock(LockKey lockKey)
        {
            return GetLock(lockKey.LockId);
        }

        public LockVariable GetLock(string lockKey)
        {
            if (!_locks.ContainsKey(lockKey)) return null;
            return _locks[lockKey];
        }
        
        public void Lock(LockKey lockKey, object locker)
        {
            Lock(lockKey.LockId, locker);
        }

        public void Lock(string lockKey, object locker)
        {
            bool alreadyLocked = IsLocked(lockKey);
            var lockVariable = GetLock(lockKey);
            if (lockVariable == null)
            {
                RegisterLock(lockKey);
                lockVariable = GetLock(lockKey);
            }
            lockVariable.Lock(locker);

            //Send events if from unlocked to locked
            if (!alreadyLocked)
            {
                OnLocked?.Invoke(lockKey);
            }
        }

        public void Unlock(LockKey lockKey, object unlocker)
        {
            Unlock(lockKey.LockId, unlocker);
        }
        
        public void Unlock(string lockKey, object unlocker)
        {
            bool alreadyLocked = IsLocked(lockKey);

            var lockVariable = GetLock(lockKey);
            if (lockVariable == null) return;
            lockVariable.Unlock(unlocker);

            bool unlockedNow = IsLocked(lockKey);

            //Send event if from locked to unlocked
            if(unlockedNow && alreadyLocked)
            {
                OnUnlocked?.Invoke(lockKey);
            }
        }
        
        public override void ResetModule()
        {
            if(_locks == null) return;
            foreach(var lockVar in _locks.Values)
            {
                lockVar.Reset();
            }
        }
    }
}