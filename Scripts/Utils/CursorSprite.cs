using System.Collections;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class CursorSprite : MonoBehaviour
    {
        public Camera Camera;
        public bool AlwaysShow = false;
        [SerializeField]
        private float distanceFromCamera = 10f;
        public float FollowLerpFactor = 10f;
        private Vector3 _targetPosition;
        public GameObject Visual;

        public float TapDistanceThresh = 10;
        public float TapTime = 0.1f;
        private float _tapStartTime;
        private Vector3 _tapStartPosition;
        
        [Header("Animations")]
        public Animator Animator;

        public float TapAnimationTime = 1;
        private static readonly int Tap = Animator.StringToHash("Tap");

        private void Enable()
        {
            transform.position = GetCursorPosition();
            Visual.SetActive(AlwaysShow);
        }
        private void Update()
        {
            _targetPosition = GetCursorPosition();
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * FollowLerpFactor);
            if(Input.GetMouseButtonDown(0))
            {
                if (!Helpers.IsCursorOnUI())
                {
                    transform.position = _targetPosition;
                    _tapStartPosition = transform.position;
                    _tapStartTime = Time.time;
                    if (Animator != null)
                    {
                        Animator.SetBool(Tap, false);
                        Animator.Rebind();
                    }
                    Visual.SetActive(true);
                }
               
            }
            else if(Input.GetMouseButtonUp(0))
            {
                //Is this a tap?

                if (Vector3.Distance(transform.position, _tapStartPosition) <= TapDistanceThresh &&
                    (Time.time - _tapStartTime) <= TapTime)
                {
                    if(Animator != null) Animator.SetBool(Tap, true);
                    StartCoroutine(TapAnimateEndRoutine());
                }
                else
                {
                   if(!AlwaysShow) Visual.SetActive(false);
                }
            }
        }

        private IEnumerator TapAnimateEndRoutine()
        {
            yield return new WaitForSeconds(TapAnimationTime);
            if(!AlwaysShow)  Visual.SetActive(false);
        }
        
        private Vector3 GetCursorPosition()
        {
            Vector3 mousePosition = Input.mousePosition;

            if (Camera == null)
            {
                //In overlay ui
                return mousePosition;
            }
            mousePosition.z = distanceFromCamera; // Set the distance from the camera
            return Camera.ScreenToWorldPoint(mousePosition);
        }

        private void OnMouseDown() {

        }

        private void OnMouseUp() {

        }
    }
}