using System;
using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Utils;
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
            DynamicSwipe, //Updates the position with transform
            Tap,
        }
        
        public UICanvas ParentCanvas;
        [SerializeField] private RectTransform ParentRectTransform;

        [Header("Swipe Motion")] 
        public float SwipeMotionSpeed = 10;
        public float ReachedThresh = 1;
        [SerializeField] float easeRadius = 120f;     // son hedefte yaklaşırken yumuşatma yarıçapı
        
        [Header("Animator")] 
        [SerializeField] private Animator Animator;
        
        [NonSerialized] public Motions CurrentMotion;
        
        private Vector2 _startSwipePosition;
        private Vector2 _endSwipePosition;
        
        private Transform _startSwipeTransform;
        private Transform _endSwipeTransform;
        
        private List<Vector2> _swipePoints;
        private int _currentSwipePointIndex;
        
        private static readonly int TapHash = Animator.StringToHash("Tap");

        private void Update()
        {
            if (CurrentMotion == Motions.None) return;

            if (CurrentMotion == Motions.Swipe)
            {
                SwipeUpdate();
            }else if (CurrentMotion == Motions.DynamicSwipe)
            {
                DynamicSwipeUpdate();
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
            Vector2 fromPos = ParentCanvas.GlobalToScreenPosition(from, ParentCanvas.GetGameCamera());
            Vector2 toPos = ParentCanvas.GlobalToScreenPosition(to, ParentCanvas.GetGameCamera());
            StartSwipe(ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, fromPos), 
                ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, toPos));
        }

        public void DoSwipeMotionUIToUI(Vector3 from, Vector3 to)
        {
            Vector2 fromPos = ParentCanvas.GlobalToScreenPosition(from, ParentCanvas.GetCanvasCamera());
            Vector2 toPos = ParentCanvas.GlobalToScreenPosition(to, ParentCanvas.GetCanvasCamera());
            StartSwipe(ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, fromPos), 
                ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, toPos));
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

        public void DoSwipeMotionWorldTransformToWorldTransform(Transform from, Transform to)
        {
            StartSwipe(from, to);
        }
        
        /// <summary>
        /// Does a start to end swipe motion between world points
        /// </summary>
        /// <param name="points"></param>
        public void DoSwipeMotionBetweenWorldPoints(List<Transform> points, bool pingPong = false)
        {
            if (points.Count < 2) return;
            _swipePoints = new List<Vector2>();
            foreach (var point in points)
            {
                _swipePoints.Add(GetAnhcoredPositionFromWorldPosition(point.position));
            }

            _startSwipePosition = _swipePoints[0];
            _endSwipePosition = _swipePoints[^1];
            ParentRectTransform.anchoredPosition = _startSwipePosition;
            CurrentMotion = Motions.Swipe;
            _currentSwipePointIndex = 1; //Always set 1 ahead
            _dir = 1;
            LoopPingPong = pingPong;
        }
        
        private void StartSwipe(Vector2 startAnchoredPosition, Vector2 endAnchoredPosition)
        {
            ParentRectTransform.anchoredPosition = startAnchoredPosition;
            _startSwipePosition = startAnchoredPosition;
            _endSwipePosition = endAnchoredPosition;
            _swipePoints = new List<Vector2> { startAnchoredPosition, endAnchoredPosition };
            _currentSwipePointIndex = 1;
            _dir = 1;
            LoopPingPong = false;
            CurrentMotion = Motions.Swipe;
        }

        public void SetStartSwipePositionFromWorldPosition(Vector3 worldPosition)
        {
            _startSwipePosition = GetAnhcoredPositionFromWorldPosition(worldPosition);
        }

        public void SetEndSwipePositionFromWorldPosition(Vector3 worldPosition)
        {
            _endSwipePosition = GetAnhcoredPositionFromWorldPosition(worldPosition);
        }
        
        private void StartSwipe(Transform from, Transform to)
        {
            _startSwipeTransform = from;
            _endSwipeTransform = to;
            CurrentMotion = Motions.DynamicSwipe;
        }

        private Vector2 GetAnhcoredPositionFromWorldPosition(Vector3 position)
        {
            Vector2 screenPos = ParentCanvas.GlobalToScreenPosition(position, ParentCanvas.GetGameCamera());
            Vector2 anchoredPos = ParentCanvas.ScreenPositionToAnchoredPosition(ParentRectTransform, screenPos);
            return anchoredPos;
        }

        int _dir = 1; // ping-pong yönü (+1 veya -1)
        [SerializeField] bool LoopPingPong = false; 
        
        void SwipeUpdate()
        {
            if (_swipePoints == null || _swipePoints.Count < 2) return;

            var targetIdx = _currentSwipePointIndex;
            var targetPos = _swipePoints[targetIdx];
            var curPos    = ParentRectTransform.anchoredPosition;

            float dist = Vector2.Distance(curPos, targetPos);

            // Ease only on last segment
            bool isLastSegment = IsOnLastSegment(targetIdx);
            float speedFactor = 1f;
            if (isLastSegment && dist < easeRadius)
            {
                float t = Mathf.Clamp01(dist / easeRadius);
                speedFactor = SmoothStep01(t); // 0..1
                speedFactor = Mathf.Max(0.15f, speedFactor);
            }

            // Constant Speed + Speed
            float step = SwipeMotionSpeed * speedFactor * Time.unscaledDeltaTime;

            var next = Vector2.MoveTowards(curPos, targetPos, step);

            // To avoid jitter
            if ((next - curPos).sqrMagnitude < 1e-7f) next = targetPos;

            ParentRectTransform.anchoredPosition = next;

            // If Reached, go to next point
            if ((targetPos - next).sqrMagnitude <= ReachedThresh *ReachedThresh)
                AdvanceWaypoint();
        }
        bool IsOnLastSegment(int targetIdx)
        {
            if (LoopPingPong)
                return _dir > 0 ? targetIdx == _swipePoints.Count - 1
                    : targetIdx == 0;
            else
                return targetIdx == _swipePoints.Count - 1;
        }
        static float SmoothStep01(float t) => t * t * (3f - 2f * t);
        void AdvanceWaypoint()
        {
            if (LoopPingPong)
            {
                // Ping-pong: 0..N-1..0.. 
                if (_currentSwipePointIndex == _swipePoints.Count - 1) _dir = -1;
                else if (_currentSwipePointIndex == 0) _dir = 1;
                _currentSwipePointIndex += _dir;
            }
            else
            {
                // Restart: 0 → 1 → ... → N-1 → 0 → ...
                _currentSwipePointIndex++;
                if (_currentSwipePointIndex >= _swipePoints.Count)
                {
                    _currentSwipePointIndex = 1;
                    ParentRectTransform.anchoredPosition = _swipePoints[0];
                }
            }
        }
        
        private void DynamicSwipeUpdate()
        {
            Vector2 error = GetTargetSwipePosition() - ParentRectTransform.anchoredPosition;
            float errorMag = error.magnitude;
            errorMag = Mathf.Max(errorMag, 0.001f);
            //error /= errorMag;
            //error.Normalize();
            if (errorMag < ReachedThresh)
            {
                OnReachedToSwipeTarget();
            }
            else
            {
                ParentRectTransform.anchoredPosition += error * Time.deltaTime * SwipeMotionSpeed;

            }
        }

        private void OnReachedToSwipeTarget()
        {
            _currentSwipePointIndex++;
            if (_currentSwipePointIndex >= _swipePoints.Count)
            {
                _currentSwipePointIndex = 1;
                ParentRectTransform.anchoredPosition = GetFirstSwipePosition();
            }
        }

        private Vector2 GetFirstSwipePosition()
        {
            return _startSwipePosition;
        }
        
        private Vector2 GetStartSwipePosition()
        {
            if (_swipePoints.IsNullOrEmpty()) return _startSwipePosition;
            return _swipePoints[_currentSwipePointIndex-1];
        }
        
        private Vector2 GetTargetSwipePosition()
        {
            if (_swipePoints.IsNullOrEmpty()) return _endSwipePosition;
            return _swipePoints[_currentSwipePointIndex];
        }
        
        public void StopMotions()
        {
            CurrentMotion = Motions.None;
            Animator.SetBool(TapHash, false);
        }
        #endregion
    }
}