using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kuantech.Merge
{
    /// <summary>
    /// Class that is an IDraggable. Useful for cases where dragging methods are needed out of the box
    /// </summary>
    public class Draggable : MonoBehaviour, IDraggable
    {
        [SerializeField] protected IDropZone DropZone;
        private Vector3 _positionBeforeDrag;

        protected IDropZone CurrentDropZone;
        
        public virtual bool DragStart()
        {
            _positionBeforeDrag = transform.position;
            return true;
        }

        [Header("Offset")]
        [SerializeField] private float OffsetDistance = 5f;
        public virtual void Drag(Vector3 cursorPosition)
        {
            IDropZone newZone = CheckForDragBench();
            if(_receivedHitThisFrame)
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
            LandedOnDropZone(DropZone);
        }
        
        protected virtual void LandedOnDropZone(IDropZone dropZone)
        {
            if(CurrentDropZone != null && CurrentDropZone != dropZone) 
            {
                CurrentDropZone.ClearSlot(0,0);
                CurrentDropZone = dropZone;
            }
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
                pointerEventData.position = Input.mousePosition;

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
            Ray ray = dm.MainCamera.ScreenPointToRay(Input.mousePosition);

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
            transform.position = _positionBeforeDrag;
        }

    }
}