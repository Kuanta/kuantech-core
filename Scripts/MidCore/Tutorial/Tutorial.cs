using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Midcore.Tutorial
{
    public class Tutorial : MonoBehaviour
    {
        public string TutorialId;
        public List<GameObject> TutorialTasks;

        public void SetTasks(GameTaskManager taskManager)
        {
        }
    }
}