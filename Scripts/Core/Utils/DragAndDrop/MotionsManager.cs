using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kuantech.Utils
{
    public class MotionsManager : MonoBehaviour
    {
        [Serializable]
        public struct MotionData
        {
            public Vector3 StartCursorPosition;
            public Vector3 EndCursorPosition;
            public float MotionStartTime;
            public float MotionEndTime;
            public float Duration => MotionEndTime - MotionStartTime;
            public Vector2 Delta => (Vector2)(EndCursorPosition - StartCursorPosition);
            public float Distance => Delta.magnitude;
            public Vector2 Direction => Distance > 0.0001f ? Delta / Distance : Vector2.zero;
        }

        [Serializable]
        public struct DrawData
        {
            public Vector3 StartPosition;
            public Vector3 PreviousPosition;
            public Vector3 CurrentPosition;
            public float StartTime;
            public float CurrentTime;
            public float TotalDuration => CurrentTime - StartTime;
            public Vector2 Delta => (Vector2)(CurrentPosition - PreviousPosition);
            public float DeltaMagnitude => Delta.magnitude;
        }

        // ============================
        // Settings
        // ============================
        [Header("General")]
        [SerializeField] private bool ignoreWhenPointerOverUI = true;

        [Header("Tap / Swipe Settings")]
        [SerializeField] private float tapMaxDuration = 0.25f;
        [SerializeField] private float tapMaxMovement = 15f;
        [SerializeField] private float swipeMinDistance = 40f;

        [Header("Draw Settings")]
        [SerializeField] private bool alwaysUpdateWhileHeld = false;
        [SerializeField] private float minPixelMoveForUpdate = 0.75f;
        [SerializeField] private float tickIntervalSeconds = 0.12f;

        // ============================
        // Events
        // ============================
        public event EventHandler<MotionData> OnTap;
        public event EventHandler<MotionData> OnSwipe;

        public event EventHandler<DrawData> OnDrawStart;
        public event EventHandler<DrawData> OnDrawUpdate;
        public event EventHandler<DrawData> OnDrawTick;
        public event EventHandler<DrawData> OnDrawEnd;

        // ============================
        // Internal state
        // ============================
        private bool _isHeld;
        private int _activeFingerId = -1;
        private Vector3 _startPos, _prevPos, _currPos;
        private float _startTime, _nextTickTime;

        private void Update()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            HandleTouch();
#else
            HandleMouse();
#endif

            if (_isHeld)
                HandleDrawWhileHeld();
        }

        // ============================
        // Mouse
        // ============================
        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (ignoreWhenPointerOverUI && IsPointerOverUIStandalone()) return;
                BeginHold(Input.mousePosition);
            }

            if (_isHeld && Input.GetMouseButtonUp(0))
            {
                EndHold(Input.mousePosition);
            }
        }

        // ============================
        // Touch
        // ============================
        private void HandleTouch()
        {
            if (!_isHeld)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase != TouchPhase.Began) continue;

                    if (ignoreWhenPointerOverUI && IsPointerOverUITouch(t)) return;
                    _activeFingerId = t.fingerId;
                    BeginHold(t.position);
                    return;
                }
            }

            if (_isHeld && _activeFingerId != -1)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.fingerId != _activeFingerId) continue;

                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        EndHold(t.position);
                        _activeFingerId = -1;
                    }
                    break;
                }
            }
        }

        // ============================
        // Core flow
        // ============================
        private void BeginHold(Vector3 screenPos)
        {
            _isHeld = true;
            _startPos = _prevPos = _currPos = screenPos;
            _startTime = Time.time;
            _nextTickTime = _startTime + tickIntervalSeconds;

            var data = new DrawData
            {
                StartPosition = _startPos,
                PreviousPosition = _prevPos,
                CurrentPosition = _currPos,
                StartTime = _startTime,
                CurrentTime = _startTime
            };
            OnDrawStart?.Invoke(this, data);
        }

        private void HandleDrawWhileHeld()
        {
            _currPos = GetCurrentPointerScreenPosition();

            var drawData = new DrawData
            {
                StartPosition = _startPos,
                PreviousPosition = _prevPos,
                CurrentPosition = _currPos,
                StartTime = _startTime,
                CurrentTime = Time.time
            };

            bool movedEnough = drawData.DeltaMagnitude >= minPixelMoveForUpdate;
            if (alwaysUpdateWhileHeld || movedEnough)
            {
                OnDrawUpdate?.Invoke(this, drawData);
                _prevPos = _currPos;
            }

            if (Time.time >= _nextTickTime)
            {
                OnDrawTick?.Invoke(this, drawData);
                _nextTickTime += tickIntervalSeconds;
            }
        }

        private void EndHold(Vector3 screenPos)
        {
            if (!_isHeld) return;
            _isHeld = false;

            var motion = new MotionData
            {
                StartCursorPosition = _startPos,
                EndCursorPosition = screenPos,
                MotionStartTime = _startTime,
                MotionEndTime = Time.time
            };

            // 1️⃣ Tap / Swipe tespiti
            bool isTap = (motion.Duration <= tapMaxDuration && motion.Distance <= tapMaxMovement);
            if (isTap)
            {
                OnTap?.Invoke(this, motion);
            }
            else if (motion.Distance >= swipeMinDistance)
            {
                OnSwipe?.Invoke(this, motion);
            }

            // 2️⃣ Draw end
            var drawData = new DrawData
            {
                StartPosition = _startPos,
                PreviousPosition = _prevPos,
                CurrentPosition = screenPos,
                StartTime = _startTime,
                CurrentTime = Time.time
            };
            OnDrawEnd?.Invoke(this, drawData);
        }

        private Vector3 GetCurrentPointerScreenPosition()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_activeFingerId != -1)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.fingerId == _activeFingerId)
                        return t.position;
                }
            }
            return _currPos;
#else
            return Input.mousePosition;
#endif
        }

        // ============================
        // UI helpers
        // ============================
        private static bool IsPointerOverUIStandalone()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsPointerOverUITouch(Touch t)
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject(t.fingerId);
        }
    }
}
