using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Midcore.Tutorial
{
    public class ToggleGameObjectTask : GameTask
    {
        [SerializeField] private GameObject ObjectToToggle;
        [SerializeField] private bool Toggle;
        public override void StartTask()
        {
            base.StartTask();
            if(ObjectToToggle != null) ObjectToToggle.SetActive(Toggle);
            CompleteTask();
        }

    }
}