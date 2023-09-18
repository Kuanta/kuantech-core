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
        private IDropZone _dropZone;
        private Vector3 _positionBeforeDrag;
        
        public virtual bool DragStart()
        {
            _positionBeforeDrag = transform.position;
            return true;
        }

        public virtual void Drag(Vector3 position)
        {
            transform.position = position;
            IDropZone newZone = CheckForDragBench();
            if (_dropZone != null && newZone == null)
            {
                //_dropZone.CancelHighlight();
                //_lastRowCol = Vector2Int.one * -1;
            }
            _dropZone = newZone;
            if (_dropZone == null)
            {
                return;
            }
        }

        public virtual void DragEnd()
        {
            
        }
        
        [Header("Ground Checking")] 
        [SerializeField] private LayerMask GroundRayMask;
        
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
            Ray ray = GameManager.Instance.MainCamera.ScreenPointToRay(Input.mousePosition);

            // Cast the ray and get the first object hit
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(ray, out hit, Mathf.Infinity, GroundRayMask))
            {
                // If it hits a DragBench, return it
                IDropZone hitMergableDropBench = hit.collider.gameObject.GetComponent<IDropZone>();
                if (hitMergableDropBench != null)
                {
                    return hitMergableDropBench;
                }
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