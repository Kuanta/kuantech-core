using UnityEngine;

namespace Kuantech.Puzzle
{
    public class LevelIntroPanel : MonoBehaviour
    { 
        [SerializeField] private Animator Animator;
        [SerializeField] private float HideAfterSeconds = 1.15f;
        public void PlayAnimation()
        {
            Animator.Rebind();
            Invoke(nameof(Hide), HideAfterSeconds);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}