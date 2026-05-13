using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] private Canvas _rootCanvas;
        
        [Header("Effects")]
        [SerializeField] private EffectPlayer DropEffect;
        
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
        public delegate bool DragCheckHandler(Draggable draggable);
        public List<DragCheckHandler> DragCheckHandlers;
        public bool DragToggle = true;
        
        [Tooltip("If set to false, can't be tapped")]
        public bool TapToggle;
        
        [Header("Offset")]
        public float OffsetDistance = 5f;
        public float SmoothDampTime = 0.1f;


        #region Ghost Indicator
        public void CheckProxyDraggable()
        {
            _usingExisting = false;
            _ghostInstance = GetGhostInstance();
            if (_ghostInstance == null) return;
            Draggable proxyDraggableCandidate = _ghostInstance.GetComponent<Draggable>();
            if (proxyDraggableCandidate != null)
            {
                OnProxyDraggableSet(proxyDraggableCandidate);
            }
        }
        
        /// <summary>
        /// Instantiates the ghost indicator prefab if the prefab is not null
        /// </summary>
        /// <returns></returns>
        public virtual GameObject GetGhostInstance()
        {
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
        
        protected virtual void OnProxyDraggableSet(Draggable draggable)
        {
            ProxyDraggable = draggable;
        }
        
        public void RemoveGhostIndicator(bool succesfullDrop)
        {
            if (_ghostInstance == null) return;
            if (_usingExisting)
            {
                _ghostInstance.gameObject.SetActive(false);
                _ghostInstance.transform.SetParent(transform);
                _ghostInstance.transform.localScale = Vector3.one;
            }else
            {
                //Is this draggable used to spawn another draggable
                if (succesfullDrop && ProxyDraggable != null)
                {
                    _ghostInstance = null;
                    ProxyDraggable = null;
                }
                else
                {
                    PoolManager.PoolObject(_ghostInstance);
                }
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
            _dragPositionOffset = IsCanvasElement() ? Vector3.zero : dragHitPoint - transform.position;
            
            //Check Proxy Draggable
            CheckProxyDraggable();
       
            if (_ghostInstance != null)
            {
                _ghostInstance.gameObject.SetActive(true);
                if (IsCanvasElement())
                {
                    var ghostRect = _ghostInstance.GetComponent<RectTransform>();
                    Canvas canvas = GetCanvas();
                    if (canvas != null && ghostRect != null)
                    {
                        ghostRect.SetParent(canvas.transform, false);
                        SetCanvasPosition(ghostRect, dragHitPoint);
                    }
                }
                else
                {
                    _ghostInstance.transform.SetParent(null);
                    _ghostInstance.transform.localScale = Vector3.one;
                    _ghostInstance.transform.position = GetWorldPositionFromCursor();
                }
            }
            else
            {
                if (IsCanvasElement())
                {
                    Canvas canvas = GetCanvas();
                    if (canvas != null)
                    {
                        transform.SetParent(canvas.transform, false);
                        if (RectTransform != null)
                            SetCanvasPosition(RectTransform, dragHitPoint);
                    }
                }
                else
                {
                    transform.SetParent(null);
                    transform.localScale = Vector3.one;
                }
            }
            
            
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
            if(!CanBeDragged()) return;
            if (ProxyDraggable != null)
            {
                ProxyDraggable.Drag(cursorPosition, cursorPositionChange);
                return;
            }

            if (IsCanvasElement())
            {
                IDropZone hoveredZone = CheckForDragBench();
                RectTransform target = _ghostInstance != null ? _ghostInstance.GetComponent<RectTransform>() : RectTransform;
                if (target == null) target = RectTransform;
                SetCanvasPosition(target, cursorPosition);
                _lastDragPosition = cursorPosition;
                if (DropZone != null && hoveredZone == null) OnLeftDropZone(DropZone);
                if (DropZone == null && hoveredZone != null) OnEnteredDropZone(hoveredZone);
                DropZone = hoveredZone;
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
            
            Draggable draggableToCheck = ProxyDraggable != null ? ProxyDraggable : this;
            if (ProxyDraggable != null)
            {
                ProxyDraggable.OnDragEndAsProxy();
            }
            bool sucessfulDrop = false;
            //Fail?
            if(!CanBeDropped() || draggableToCheck.RequireDropZone && draggableToCheck.DropZone == null || 
               draggableToCheck.DropZone != null && !draggableToCheck.DropZone.OnDrop(draggableToCheck))
            {
                sucessfulDrop = false;
                OnFailedDrop();
                OnDrop?.Invoke(false);
                ReturnToPreviousPosition();
            }
            else
            {
                sucessfulDrop = true;
                OnSuccesfullDrop();
            }
            OnDrop?.Invoke(true);
            
            //Clear ghost indicator
            RemoveGhostIndicator(sucessfulDrop);

        }

        public virtual void OnDragEndAsProxy()
        {
            
        }
        #endregion

        #region Queries
        private bool _isCanvas;
        private bool _isCanvasCached;
        public bool IsCanvasElement()
        {
            if (!_isCanvasCached)
            {
                _isCanvas = GetComponentInParent<Canvas>() != null;
                _isCanvasCached = true;
            }
            return _isCanvas;
        }

        protected Canvas GetCanvas() => _rootCanvas != null ? _rootCanvas : GetComponentInParent<Canvas>();

        protected void SetCanvasPosition(RectTransform rect, Vector2 screenPos)
        {
            Canvas canvas = GetCanvas();
            if (canvas == null || rect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPos);
            rect.anchoredPosition = localPos;
        }

        public bool IsDragged()
        {
            return _isDragged;
        }
        
        public virtual bool CanBeDragged()
        {

            if (!DragCheckHandlers.IsNullOrEmpty())
            {
                foreach(var handler in DragCheckHandlers)
                {
                    if (handler != null)
                    {
                        if (!handler.Invoke(this)) return false;
                    }
                }
            }
            return DragToggle;
        }
        
        /// <summary>
        /// A final check before drop
        /// </summary>
        /// <returns></returns>
        public virtual bool CanBeDropped()
        {
            return true;
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
            if (IsCanvasElement())
            {
                RectTransform r = _ghostInstance != null ? _ghostInstance.GetComponent<RectTransform>() : RectTransform;
                return r != null ? (Vector3)r.anchoredPosition : transform.position;
            }
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
            if (IsCanvasElement())
            {
                transform.SetParent(_parentBeforeDrag, false);
                if (RectTransform != null) RectTransform.anchoredPosition = _positionBeforeDrag;
                transform.localScale = Vector3.one;
                return;
            }
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
            DropInformation dropInformation = new DropInformation
            {
                DropZone = DropZone,
                DropPosition = _lastDragPosition
            };
            OnSuccesfullDropEvent?.Invoke(dropInformation);
            if (ProxyDraggable != null)
            {
                ProxyDraggable.PlayDroppedEffect();
                ProxyDraggable.OnSuccesfullDropAsProxyDraggable(dropInformation);
            }
            else
            {
                PlayDroppedEffect();
            }
        }

        protected virtual void PlayDroppedEffect()
        {
            if(gameObject.activeInHierarchy && DropEffect != null)
            {
                DropEffect.PlayEffectAtPosition(transform.position, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// Called on proxy draggable in case of succesfull drop
        /// </summary>
        public virtual void OnSuccesfullDropAsProxyDraggable(DropInformation dropInformation)
        {
            OnSuccesfullDropEvent?.Invoke(dropInformation);
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