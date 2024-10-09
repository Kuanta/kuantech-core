using System;
using Kuantech.UI;
using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    /// <summary>
    /// Tutorail hand is a ui element that handles shows a gesture to the player
    /// </summary>
    public class TutorialHand : MonoBehaviour
    {
        public enum Motions
        {
            None,
            Swipe,
            Tap,
        }
        
        public UICanvas ParentCanvas;
        [SerializeField] private RectTransform ParentRectTransform;

        [Header("Swipe Motion")] 
        public float SwipeMotionSpeed = 10;
        public float ReachedThresh = 1;

        [Header("Animator")] 
        [SerializeField] private Animator Animator;
        
        [NonSerialized] public Motions CurrentMotion;
        
        private Vector2 _startSwipePosition;
        private Vector2 _endSwipePosition;
        private static readonly int TapHash = Animator.StringToHash("Tap");

        private void Update()
        {
            if (CurrentMotion == Motions.None) return;

            if (CurrentMotion == Motions.Swipe)
            {
                SwipeUpdate();
            }
        }

     
        #region Motions

        public void DoTapMotion(Vector3 position)
        {
            Vector2 screenPos = ParentCanvas.GlobalToScreenPosition(position, ParentCanvas.GetGameCamera());
            ParentRectTransform.anchoredPosition = ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, screenPos);
            CurrentMotion = Motions.Tap;
            Animator.SetBool(TapHash, true);
        }
        
        /// <summary>
        /// Moves the hand from a world object to a ui object
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void DoSwipeMotionWorldToUI(Vector3 from, Vector3 to)
        {
            Vector2 fromPos = ParentCanvas.GlobalToScreenPosition(from, ParentCanvas.GetGameCamera());
            Vector2 toPos = ParentCanvas.GlobalToScreenPosition(to, ParentCanvas.GetCanvasCamera());
            StartSwipe(ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, fromPos), 
                ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, toPos));
        }
        
        /// <summary>
        /// Moves the hand from a ui object to a world object
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void DoSwipeMotionUIToWorld(Vector3 from, Vector3 to)
        {
            
        }
        
        /// <summary>
        /// Swipes the hand from a world object to another world object
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void DoSwipeMotionWorldToWorld(Vector3 from, Vector3 to)
        {
            Vector2 fromPos = ParentCanvas.GlobalToScreenPosition(from, ParentCanvas.GetGameCamera());
            Vector2 toPos = ParentCanvas.GlobalToScreenPosition(to, ParentCanvas.GetGameCamera());
            StartSwipe(ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, fromPos), 
                ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, toPos));
        }
        private void StartSwipe(Vector2 startAnchoredPosition, Vector2 endAnchoredPosition)
        {
            ParentRectTransform.anchoredPosition = startAnchoredPosition;
            _startSwipePosition = startAnchoredPosition;
            _endSwipePosition = endAnchoredPosition;
            CurrentMotion = Motions.Swipe;
        }

        private void SwipeUpdate()
        {
            Vector2 error = _endSwipePosition - ParentRectTransform.anchoredPosition;
            float errorMag = error.magnitude;
            errorMag = Mathf.Max(errorMag, 0.001f);
            //error /= errorMag;
            //error.Normalize();
            if (errorMag < ReachedThresh)
            {
                ParentRectTransform.anchoredPosition = _startSwipePosition;
            }
            else
            {
                ParentRectTransform.anchoredPosition += error * Time.deltaTime * SwipeMotionSpeed;

            }
        }
        public void StopMotions()
        {
            CurrentMotion = Motions.None;
            Animator.SetBool(TapHash, false);
        }
        #endregion
    }
}