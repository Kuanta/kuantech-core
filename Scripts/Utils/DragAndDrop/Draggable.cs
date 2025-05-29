using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kuantech.Utils
{
    public struct DropInformation
    {
        public IDropZone DropZone;
        public Vector3 DropPosition;
    }
    
    /// <summary>
    /// Class that is an IDraggable. Useful for cases where dragging methods are needed out of the box
    /// </summary>
    public class Draggable : MonoBehaviour, IDraggable
    {
        protected IDropZone DropZone;
        [Header("Positioning")]
        [Tooltip("IF set to true, position will be set using the ground ray")]
        [SerializeField] private bool PositionWithGroundRay = false;
        [SerializeField] private bool PositionWithPlane;
        [SerializeField] private Vector3 planeNormal = Vector3.up;
        [SerializeField] private Vector3 planePoint = Vector3.zero;
 
        [Header("Ghost Indicator")]
        [Tooltip("Existing Ghost Indicator")]
        [SerializeField] private GameObject ExistingGhostIndicator;
        [Tooltip(
            "If set to a non null value, the draggable wont be moved, instead a ghost indicator will be shown to represent the drag")]
        [SerializeField] private GameObject GhostIndicatorPrefab;
        private GameObject _ghostInstance;
        private bool _usingExisting;

        [Header("Ground Checking")] 
        [SerializeField] private LayerMask GroundRayMask;
        private RaycastHit _lastHit;
        private bool _receivedHitThisFrame;

        [Header("Drop Behaviour")]
        [Tooltip("If set to true, dropping must be done onto a dropping zone")]
        public bool RequireDropZone = true;
        
        [Header("UI Draggable")]
        [SerializeField] private RectTransform RectTransform;
        
        //Runtime
        [NonSerialized] public Draggable ProxyDraggable;
        private bool _isDragged = false;
        private Vector3 _positionBeforeDrag;
        private Transform _parentBeforeDrag;
        private Vector3 _lastDragPosition;
        protected Vector3 _dragPositionOffset;
        protected IDropZone CurrentDropZone;
        
        //Events
        public Action OnDragStartEvent; //Called when dragging starts
        public Action OnDragEndEvent; //Called when dragging ends somehow
        public Action<Vector3> OnTapped;
        public Action<bool> OnDrop;
        public Action<DropInformation> OnSuccesfullDropEvent;

        [Tooltip("If set to false, Draggable can't be dragged")]
        public bool DragToggle = true;
        
        [Tooltip("If set to false, can't be tapped")]
        public bool TapToggle;
        
        [Header("Offset")]
        public float OffsetDistance = 5f;
        public float SmoothDampTime = 0.1f;


        #region Ghost Indicator
        public GameObject GetGhostIndicator()
        {
            _usingExisting = false;
            if (ExistingGhostIndicator != null)
            {
                _usingExisting = true;
                return ExistingGhostIndicator;
            }
            if (GhostIndicatorPrefab != null)
            {
                return PoolManager.GetObjectFromPool(GhostIndicatorPrefab);
            }
            return null;
        }
        
        public void RemoveGhostIndicator()
        {
            if (_ghostInstance == null) return;
            if (_usingExisting)
            {
                _ghostInstance.gameObject.SetActive(false);
                _ghostInstance.transform.SetParent(transform);
                _ghostInstance.transform.localScale = Vector3.one;
            }else
            {
                PoolManager.PoolObject(_ghostInstance);
            }
        }
        #endregion

        #region Drag Lifecycle
        public virtual bool DragStart(Vector3 dragHitPoint)
        {
            if (!CanBeDragged()) return false;
            if (RectTransform != null)
            {
                _positionBeforeDrag = RectTransform.anchoredPosition;
            }
            else
            {
                _positionBeforeDrag = transform.position;
            }
            _parentBeforeDrag = transform.parent;
            _dragPositionOffset = dragHitPoint - transform.position;
            
            _ghostInstance = GetGhostIndicator();
            if (_ghostInstance != null)
            {
                _ghostInstance.gameObject.SetActive(true);
                _ghostInstance.transform.SetParent(null);
                _ghostInstance.transform.localScale = Vector3.one;
                
                //Position _ghostInstance 
                _ghostInstance.transform.position = GetWorldPositionFromCursor();
            }
            else
            {
                transform.SetParent(null);
                transform.localScale = Vector3.one;
            }

            ProxyDraggable = _ghostInstance.GetComponent<Draggable>();
            
            if (ProxyDraggable != null)
            {
                ProxyDraggable.OnDragStart();
            }


            OnDragStart();
            
            return true;
        }

        protected virtual void OnDragStart()
        {
            _isDragged = true;
            OnDragStartEvent?.Invoke();

            if (PositionWithPlane)
            {
                _dragPositionOffset = Helpers.ProjectVectorOnPlane(_dragPositionOffset, planeNormal, planePoint);
            }
        }

        public virtual void Drag(Vector3 cursorPosition, Vector3 cursorPositionChange)
        {
            //cursorPosition += GetDragPositionOffset();
            if(!CanBeDragged()) return;
            if (ProxyDraggable != null)
            {
                ProxyDraggable.Drag(cursorPosition, cursorPositionChange);
                return;
            }
            
            IDropZone newZone = CheckForDragBench();
            if (PositionWithPlane)
            {
                Vector3 point = HitAtPlane(planePoint, planeNormal);
                
                SmoothDampPosition(point + planeNormal* OffsetDistance - _dragPositionOffset);
                _lastDragPosition = point - _dragPositionOffset;
            }
            else if(_receivedHitThisFrame && PositionWithGroundRay)
            {
                Transform cameraTransform = DragManager.GetContext<DragManager>().GetMainCamera().transform;
                Vector3 diff = cameraTransform.position - _lastHit.point;
                diff.Normalize();
                SmoothDampPosition(_lastHit.point + diff * OffsetDistance - _dragPositionOffset);
                _lastDragPosition = _lastHit.point - _dragPositionOffset;

            }
            else{
                SmoothDampPosition(cursorPosition);
                _lastDragPosition = cursorPosition;
            }

            if (DropZone != null && newZone == null)
            {
                OnLeftDropZone(DropZone);
            }

            if (DropZone == null && newZone != null)
            {
                OnEnteredDropZone(newZone);
            }
            DropZone = newZone;
            if (DropZone == null)
            {
                return;
            }
        }
        
        public virtual void DragEnd()
        {
            OnDragEndEvent?.Invoke();
   
            _isDragged = false;
            
            //Fail?
            if(RequireDropZone && DropZone == null || DropZone != null && !DropZone.OnDrop(ProxyDraggable != null ? ProxyDraggable : this)) 
            {
                OnFailedDrop();
                OnDrop?.Invoke(false);
                ReturnToPreviousPosition();
                return;
            }
            OnSuccesfullDrop();
            OnDrop?.Invoke(true);
            
            //Clear ghost indicator
            RemoveGhostIndicator();

        }
        #endregion

        #region Queries
        public bool IsDragged()
        {
            return _isDragged;
        }
        
        public virtual bool CanBeDragged()
        {
            return DragToggle;
        }
        #endregion

        #region Position Setter/Getter
        private Vector3 _dampSpeed;
        protected virtual void SetPosition(Vector3 position)
        {
            if (RectTransform != null)
            {
                RectTransform.anchoredPosition = position;
            }
            else
            {
                transform.position = position;
            }
        }

        protected virtual Vector3 GetPosition()
        {
            if (RectTransform != null) return RectTransform.anchoredPosition;
            return transform.position;
        }
        public virtual Vector3 GetCurrentPosition()
        {
            if (_ghostInstance != null) return _ghostInstance.transform.position;
            return transform.position;
        }

        public Vector3 GetDropPosition()
        {
            return _lastDragPosition;
        }
        protected void SmoothDampPosition(Vector3 targetPos)
        {
            Vector3 currPos = GetCurrentPosition();
            
            Vector3 newPos = Vector3.SmoothDamp(currPos, targetPos, ref _dampSpeed, SmoothDampTime);
            if (_ghostInstance != null)
            {
                _ghostInstance.transform.position = newPos;
            }
            else
            {
                SetPosition(targetPos);
            }
        }

        #endregion
       

        public IDropZone CheckForDragBench()
        {
            //Check UI elements first
           
            // Raycast using the Graphics Raycaster and mouse click position
            DragManager dm = GameManager.Instance.GetSubManagerByType<DragManager>() as DragManager;
            if (dm.GraphicsRaycaster != null)
            {
                // Set up the PointerEventData based on the current mouse position
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
                //pointerEventData.position = Input.mousePosition + GetDragPositionOffset();
                pointerEventData.position = DragManager.GetCursorPosition(true);
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
            //Ray ray = dm.MainCamera.ScreenPointToRay(Input.mousePosition + GetDragPositionOffset());
            Ray ray = dm.GetMainCamera().ScreenPointToRay(DragManager.GetCursorPosition(true));

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

        public Vector3 GetWorldPositionFromCursor()
        {
            return DragManager.GetContext<DragManager>().GetMouseWorldPosition();
        }
        
        /// <summary>
        /// Cast a ray from cursor position to plane
        /// </summary>
        /// <param name="planePoint"></param>
        /// <param name="planeNormal"></param>
        /// <returns></returns>
        private Vector3 HitAtPlane(Vector3 planePoint, Vector3 planeNormal)
        {
            Vector3 cursorPosition = DragManager.GetCursorPosition(true);
            DragManager dm = GameManager.Instance.GetSubManagerByType<DragManager>() as DragManager;
            Ray ray = dm.GetMainCamera().ScreenPointToRay(cursorPosition);
            return Helpers.CheckRayAgainstPlane(ray, planeNormal, this.planePoint);
        }
        
        /// <summary>
        /// Called when draggable enters a drop zone
        /// </summary>
        /// <param name="dropZone"></param>
        public virtual void OnEnteredDropZone(IDropZone dropZone)
        {
            
        }
        
        /// <summary>
        /// Called when draggable leaves the dropzone
        /// </summary>
        public virtual void OnLeftDropZone(IDropZone dropZone)
        {
                
        }
        
        public virtual void ReturnToPreviousPosition()
        {
            transform.SetParent(_parentBeforeDrag, true);
            SetPosition(_positionBeforeDrag);
            transform.localScale = Vector3.one;
        }



        public virtual void OnTap(Vector3 hitPoint)
        {
            if (!TapToggle) return;
            OnTapped?.Invoke(hitPoint);
        }

        public GameObject GetDroppedObject()
        {
            //This is where we give this or object to spawn
            return gameObject;
        }

        public Vector3 GetCursorPosition()
        {
            return DragManager.GetCursorPosition(true); //Input.mousePosition + GetDragPositionOffset();
        }

        public virtual void OnSuccesfullDrop()
        {
            OnSuccesfullDropEvent?.Invoke(new DropInformation
            {
                DropZone = DropZone,
                DropPosition = _lastDragPosition
            });
        }

        public virtual void OnFailedDrop()
        {
            
        }
        
        #region Click
        public virtual void OnClickDown()
        {
            
        }

        public virtual void OnClickUp()
        {
            
        }
        #endregion

    }
}