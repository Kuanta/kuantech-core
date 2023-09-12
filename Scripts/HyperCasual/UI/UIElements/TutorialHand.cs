using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class TutorialHand : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private float SwayMovementDistance = 100f;
        [SerializeField] private float SwayMovementSpeed = 1;
        [SerializeField] private AnimationCurve SpeedCurve;
        private float _swayMovementTimer = 0f;

        private void OnEnable()
        {
            _swayMovementTimer = 0f;
        }

        private void Update()
        {
            SwayMovement();
        }

        private void SwayMovement()
        {
            Vector3 localPos = rectTransform.transform.localPosition;
            float sinValue = Mathf.Sin(_swayMovementTimer * SwayMovementSpeed);
            rectTransform.transform.localPosition = new Vector3(sinValue * SwayMovementDistance,
                localPos.y, localPos.z);
            _swayMovementTimer += Time.deltaTime;
        }
    }
    
    
}