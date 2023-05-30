using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public abstract class LevelElement : MonoBehaviour
    {
        public Vector3 InitialPosition;
        public Quaternion InitialRotaiton;
        public Level ParentLevel;
        public virtual void OnLevelCreated(){}
        public abstract void OnPrepareLevel();
        public abstract void OnLeaveLevel();
        public abstract void OnPlayLevel();
        public abstract void OnPlayerEntered();
        public abstract void OnPlayerExited();
    }
}