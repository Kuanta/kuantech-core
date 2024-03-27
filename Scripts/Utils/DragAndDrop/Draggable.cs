using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kuantech.Utils
{
    /// <summary>
    /// Class that is an IDraggable. Useful for cases where dragging methods are needed out of the box
    /// </summary>
    public class Draggable : MonoBehaviour, IDraggable
    {
        [SerializeField] protected IDropZone DropZone;
        [Tooltip("IF set to true, position will be set using the ground ray")]
        [SerializeField] private bool PositionWithGroundRay = false;
        public Vector3 DragPositionOffset;

        private Vector3 _positionBeforeDrag;
        private Transform _parentBeforeDrag;

        protected IDropZone CurrentDropZone;
        
        //Events
        public Action OnDrop;

        public virtual bool DragStart()
        {
            if (!CanBeDragged()) return false;
            _positionBeforeDrag = transform.position;
            _parentBeforeDrag = transform.parent;
            transform.SetParent(null);
            transform.localScale = Vector3.one;
            return true;
        }
        [Tooltip("If set to false, Draggable can't be dragged")]
        public bool DragToggle = true;

        [Header("Offset")]
        [SerializeField] private float OffsetDistance = 5f;
        public virtual void Drag(Vector3 cursorPosition)
        {
            cursorPosition += DragPositionOffset;
            if(!CanBeDragged()) return;
            IDropZone newZone = CheckForDragBench();
            if(_receivedHitThisFrame && PositionWithGroundRay)
            {
                Transform cameraTransform = DragManager.GetContext<DragManager>().MainCamera.transform;
                Vector3 diff = cameraTransform.position - _lastHit.point;
                diff.Normalize();
                SetPosition(_lastHit.point + diff * OffsetDistance);
            }
            else{
                SetPosition(cursorPosition);
            }

            if (DropZone != null && newZone == null)
            {
                //_dropZone.CancelHighlight();
                //_lastRowCol = Vector2Int.one * -1;
            }
            DropZone = newZone;
            if (DropZone == null)
            {
                return;
            }
        }

        protected virtual void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
        public virtual void DragEnd()
        {
            if(DropZone == null || DropZone == CurrentDropZone || !DropZone.OnDrop(this)) 
            {
                ReturnToPreviousPosition();
                return;
            }
            if(!LandedOnDropZone(DropZone))
            {
                ReturnToPreviousPosition();
            }else{
                //Dropped
                OnDrop?.Invoke();
            }
        }
        
        protected virtual bool LandedOnDropZone(IDropZone dropZone)
        {
            if(CurrentDropZone != null && CurrentDropZone != dropZone) 
            {
                CurrentDropZone.ClearSlot(0,0);
                CurrentDropZone = dropZone;
            }
            return true;
        }

        [Header("Ground Checking")] 
        [SerializeField] private LayerMask GroundRayMask;
        private RaycastHit _lastHit;
        private bool _receivedHitThisFrame;
        public IDropZone CheckForDragBench()
        {
            //Check UI elements first
           
            // Raycast using the Graphics Raycaster and mouse click position
            DragManager dm = GameManager.Instance.GetSubManagerByType<DragManager>() as DragManager;
            if (dm.GraphicsRaycaster != null)
            {
                // Set up the PointerEventData based on the current mouse position
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
                pointerEventData.position = Input.mousePosition + DragPositionOffset;

                // Create a list of Raycast Results
                List<RaycastResult> results = new List<RaycastResult>();
                dm.GraphicsRaycaster.Raycast(pointerEventData, results);

                // Check each hit
                foreach (RaycastResult result in results)
                {
                    IDropZone dropZone = result.gameObject.GetComponent<IDropZone>();
                    if (dropZone != null)
                    {
                        return dropZone;
                    }
                }
            }
            
            // Create a ray from the camera through the cursor position
            Ray ray = dm.MainCamera.ScreenPointToRay(Input.mousePosition + DragPositionOffset);

            // Cast the ray and get the first object hit
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, GroundRayMask))
            {
                _receivedHitThisFrame = true;
                _lastHit = hit;
                // If it hits a DragBench, return it
                IDropZone hitMergableDropBench = hit.collider.gameObject.GetComponent<IDropZone>();
                if (hitMergableDropBench != null)
                {
                    return hitMergableDropBench;
                }
            }else{
                _receivedHitThisFrame = false;
            }
            

            // If no DragBench was hit, return null
            return null;
        }
        
        public virtual void ReturnToPreviousPosition()
        {
            transform.SetParent(_parentBeforeDrag, true);
            transform.position = _positionBeforeDrag;
            transform.localScale = Vector3.one;
        }

        public virtual bool CanBeDragged()
        {
            return DragToggle;
        }

        public Vector3 GetCursorPosition()
        {
            return Input.mousePosition + DragPositionOffset;
        }
    }
}