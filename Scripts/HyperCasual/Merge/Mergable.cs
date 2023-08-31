using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kuantech.Merge
{
   
    
    [RequireComponent(typeof(Slottable))]
    public class Mergable : MonoBehaviour, IDraggable
    {
        public MergableTemplate MergableData;
        public int Level = 0;
        
        [Header("Slottable")] 
        public Slottable Slottable;
        
        [Header("Ground Checking")] 
        [SerializeField] private Vector3 GroundRayOffset = Vector3.zero;
        [SerializeField] private float GroundRayLength = 5f;
        [SerializeField] private LayerMask GroundRayMask;

        [Header("Visuals")] 
        [SerializeField] private MergeHeadUI HeadUI;
        
        private IDropZone _dropZone;
        private Vector3 _positionBeforeDrag;
        //private Vector2Int _lastRowCol;
        
        public virtual void Initialize(MergableTemplate mergableData)
        {
            Level = 1;
            MergableData = mergableData;
            UpdateHeadUI();
        }

        public void ToggleHeadUI(bool toggle)
        {
            if (HeadUI == null) return;
            HeadUI.gameObject.SetActive(toggle);
        }
        public virtual bool DragStart()
        {
            _positionBeforeDrag = transform.position;
            return true;
        }

        public void Drag(Vector3 position)
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
            //Vector2Int rowCol = _dropZone.GetRowColIndices(gameObject);
            //Check only if row and column beneath the object has been changed
            // if (rowCol.x != _lastRowCol.x || rowCol.y != _lastRowCol.y)
            // {
            //     _dropZone.Highlight(Slottable);
            // }
            //
            // _lastRowCol = rowCol;
        }

        public void DragEnd()
        {
            //if(_dropZone != null) _dropZone.CancelHighlight();
            if (!enabled) return;
            if (_dropZone == null || !_dropZone.OnDrop(this))
            {
                //ReturnToPreviousPosition();
            }
            
        }
        
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
        public void SetLevel(int level)
        {
            Level = level;
            UpdateHeadUI();
        }
        public void Upgrade()
        {
            Level++;
            UpdateHeadUI();
        }

        public bool CanBeMergedWith(Mergable other)
        {
            if (other.MergableData.Id == MergableData.Id && other.Level == Level) return true;
            return false;
        }

        #region Visuals

        protected virtual void UpdateHeadUI()
        {
            if (HeadUI == null) return;
            HeadUI.SetText($"{Level}");
        }
        #endregion
    }
}