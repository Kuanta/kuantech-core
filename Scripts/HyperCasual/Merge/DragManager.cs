using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Merge
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
        public LayerMask DraggableLayer;
        private Camera mainCamera;
        private Transform draggedObject;
        private Vector3 _offset;
        private IDraggable _draggedInterface;
        public LayerMask GroundLayer;
        public float DragCameraDistance = 10.0f;
        public GraphicRaycaster GraphicsRaycaster;
        
        //Events
        public EventHandler<IDraggable> OnDragStart;
        public EventHandler<IDraggable> OnDragEnd;
        
        private void Awake()
        {
            mainCamera = Camera.main;
        }
        public float cameraDepth = 10;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
         
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (UnityEngine.Physics.Raycast(ray, out hit, 100, DraggableLayer.value))
                {
                    if (hit.collider.gameObject.CompareTag("RaycastBlocker")) return;
                    // Check if the hit object implements the IDraggable interface
                    _draggedInterface = hit.collider.transform.gameObject.GetComponent<IDraggable>();
                    if (_draggedInterface != null)
                    {
                        draggedObject = hit.collider.transform;
                        OnDragStart?.Invoke(this, _draggedInterface); //Invoke event for subscribers
                        _draggedInterface.DragStart(); //Call drag start on the dragged object
                        _offset = draggedObject.position - hit.point;
                        _offset.y = 0;
                    }
                }
            }
            //Dragging
            else if (Input.GetMouseButton(0))
            {
                if (draggedObject != null && _draggedInterface != null)
                {
                    Vector3 mousePosition = Input.mousePosition;
                    mousePosition.z = DragCameraDistance;
                    Vector3 worldPosition = GameManager.Instance.MainCamera.ScreenToWorldPoint(mousePosition);
                    _draggedInterface.Drag( worldPosition );
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (draggedObject != null && _draggedInterface != null)
                {
                    OnDragEnd?.Invoke(this, _draggedInterface);
                    _draggedInterface.DragEnd();
                    draggedObject = null;
                    _draggedInterface = null;
                }
            }
        }

       
        
        private bool CheckGround(out Vector3 targetPosition)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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
        
    }
}