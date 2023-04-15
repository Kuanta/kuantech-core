using UnityEngine;

namespace Kuantech.EndlessRunner
{
    public abstract class LevelElement : MonoBehaviour
    {
        public abstract void OnPrepareLevel();
        public abstract void OnLeaveLevel();
        public abstract void OnPlayLevel();
        public abstract void OnPlayerEntered();
        public abstract void OnPlayerExited();
    }
}