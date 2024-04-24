using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.UI;


namespace Kuantech.Utils
{
    [Serializable]
    public class SlotState
    {
        public int SlotIndex;
        public string SlottableId;
        public int SlottableLevel;
        public bool Occupied;
    }

    [Serializable]
    public class SlottableZoneState
    {
        public Dictionary<int, SlotState> Slots ;

        public SlottableZoneState()
        {
            Slots = new Dictionary<int, SlotState>();
        }
        

        public void SetSlot(string slottableId, int slottableLevel, int index)
        {
            Slots ??= new Dictionary<int, SlotState>();

            Slots[index] = new SlotState()
            {
                SlottableId = slottableId,
                SlottableLevel = slottableLevel,
                SlotIndex = index,
                Occupied = true,
            };
        }

        public void UnSetSlot(int index)
        {
            if (Slots == null || !Slots.ContainsKey(index)) return;
            Slots[index].Occupied = false;
            Slots[index].SlottableId = "";
            Slots[index].SlottableLevel = 1;
        }
    }
    
    public class DragManager : SubManager
    {
        [Header("Properties")]
        public LayerMask DraggableLayer;
        private Transform draggedObject;
        //private Vector3 _offset;
        private IDraggable _draggedInterface;
        private IDraggable _draggedUnderCursor;
        public LayerMask GroundLayer;
        public float DragCameraDistanceOffset = 0f;
        private float _dragCameraDistance;
        public GraphicRaycaster GraphicsRaycaster;
        public Camera MainCamera;
        public float RaycastLength = 100;
        public float MaxTapTime = 0.25f; //Taps should be quicker than this
        public float TapDistanceThresh = 10f;
        public float DragStartTime = 0f; //A single touch should persist at least this amount
        public float DragRemainErrorThreshold = 1f;

        [Header("Offset")]
        public Vector2 OffsetPercentages;

        [Header("Follow Object")]
        public GameObject FollowObject; //If we have a gameobject that follows cursor smoothly, we can use this to get world position
        //Events
        public EventHandler<IDraggable> OnDragStart;
        public EventHandler<IDraggable> OnDragEnd;

        protected Vector3 _startPosition;
        protected float _startTime;
        protected bool _startedClick;
        protected bool _dragging = false;
        

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _startedClick = false;
                _dragging = false;
                if (Utils.Helpers.IsCursorOnUI()) return;
                _startedClick = true;
                _startPosition = GetCursorPosition(false);
                _startTime = Time.time;
                CheckWorld();
            }
            //Dragging
            else if (Input.GetMouseButton(0))
            {
                if(!_startedClick) return;
                //
                // if (IsPointerClick())
                // {
                //     return;
                // }
                Vector3 mousePosition = GetCursorPosition(true);
                if (Time.time - _startTime < DragStartTime)
                {
                    //Check if the finger is moved
                    if(Vector3.Distance(mousePosition, _startPosition) > DragRemainErrorThreshold)
                    {
                        Release();
                    }

                    return;
                }
                if (draggedObject != null && _draggedInterface != null)
                {
                    _dragging = true;
                    Vector3 worldPosition = GetMouseWorldPosition();
                    _draggedInterface.Drag(worldPosition);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (IsPointerClick())
                {
                    _draggedInterface.OnTap();
                }
                if(!_startedClick) return;
                OnCursorUp();
                Release();
            }
        }

        public static Vector3 GetCursorPosition(bool applyOffset)
        {
            Vector3 mousePos = Input.mousePosition;
            var dm = DragManager.GetContext<DragManager>();
            if(applyOffset) 
            {
               mousePos += dm.GetCursorOffset();
            }
            return mousePos;
        }

        public Vector3 GetCursorOffset()
        {
            return new Vector3(Screen.width * OffsetPercentages.x, Screen.height * OffsetPercentages.y, 0);
        }

        public Vector3 GetMouseWorldPosition()
        {
            if(FollowObject != null)
            {
                return FollowObject.transform.position;
            }
            Vector3 mousePosition = GetCursorPosition(true);
            mousePosition.z = _dragCameraDistance;
            return MainCamera.ScreenToWorldPoint(mousePosition);
        }

        /// <summary>
        /// Releases the dragging state
        /// </summary>
        protected virtual void Release()
        {
            _startedClick = false;
            _dragging = false;
        }

        /// <summary>
        /// Checks if the motion is a simple click
        /// </summary>
        /// <returns></returns>
        protected bool IsPointerClick()
        {
            Vector3 mousePosition = GetCursorPosition(false);
            float sqrMag = (mousePosition - _startPosition).sqrMagnitude;
            if (sqrMag <  TapDistanceThresh && Time.time - _startTime < MaxTapTime)
            {
                return true;
            }
            return false;
        }
        protected virtual void CheckWorld()
        {

            Ray ray = MainCamera.ScreenPointToRay(GetCursorPosition(false));
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(ray, out hit, RaycastLength, DraggableLayer.value))
            {
                HandleRaycastHit(hit);
            }
        }

        protected virtual void OnCursorUp()
        {
            if (_draggedInterface != null)
            {
                _draggedInterface.OnClickUp();
            }
            if (draggedObject != null && _draggedInterface != null)
            {
                OnDragEnd?.Invoke(this, _draggedInterface);
                _draggedInterface.DragEnd();
            }
            draggedObject = null;
            _draggedInterface = null;
        }
        
        /// <summary>
        /// Handles the raycast hit
        /// </summary>
        /// <param name="hit"></param>
        protected virtual void HandleRaycastHit(RaycastHit hit)
        {
            // Check if the hit object implements the IDraggable interface
            _draggedInterface = hit.collider.transform.gameObject.GetComponent<IDraggable>();
            if(_draggedInterface != null)
            {
                _draggedInterface.OnClickDown();
            }
            if (_draggedInterface != null && _draggedInterface.DragStart())
            {
                draggedObject = hit.collider.transform;
                _dragCameraDistance = Vector3.Distance(MainCamera.transform.position, draggedObject.position) + DragCameraDistanceOffset;

                //Does _draggedInterface wants to be dragged?
                bool canBeDragged = _draggedInterface.CanBeDragged();
                if(!canBeDragged)
                {
                    _draggedInterface = null;
                    return;
                }
                OnDragStart?.Invoke(this, _draggedInterface); //Invoke event for subscribers
            }
        }

        public void SetDraggable(IDraggable draggable)
        {
            _draggedInterface = draggable;
            OnDragStart?.Invoke(this, _draggedInterface); //Invoke event for subscribers
            _draggedInterface.DragStart(); //Call drag start on the dragged object
            draggedObject = (_draggedInterface as MonoBehaviour).gameObject.transform;
        }
        private bool CheckGround(out Vector3 targetPosition)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(GetCursorPosition(false));
            targetPosition = Vector3.zero;
            if (UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, GroundLayer))
            {
                Vector3 newPosition = hit.point;
                newPosition += hit.normal * 0.25f;
                targetPosition = newPosition;
                return true;
            }
            return false;
        }

        public bool IsDraggingObject()
        {
            return _dragging;
        }
    }
}