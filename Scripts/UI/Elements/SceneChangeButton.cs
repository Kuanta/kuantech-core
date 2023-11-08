using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    
    public class SceneChangeButton : MonoBehaviour {
        public string TransitioningSceneName;
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => {
                GameManager.Instance.ChangeScene(TransitioningSceneName);
            });
        }
    }
}