using UnityEngine;

namespace Kuantech.EndlessRunner
{
    public abstract class LevelElement : MonoBehaviour
    {
        public Vector3 InitialPosition;
        public Quaternion InitialRotaiton;
        
        public abstract void OnPrepareLevel();
        public abstract void OnLeaveLevel();
        public abstract void OnPlayLevel();
        public abstract void OnPlayerEntered();
        public abstract void OnPlayerExited();
    }
}