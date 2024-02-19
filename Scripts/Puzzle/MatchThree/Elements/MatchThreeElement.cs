using System;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchThreeElement : GridTile
    {
        public MatchThreeElementData CurrentData;

        [Header("Components")]
        public WaypointFollower WaypointFollower;
        public GameObject CurrentVisual;

        [Header("State")]
        [Tooltip("Non movable tiles are counted as obstacles")]
        [SerializeField] private bool _canBeMoved;
        [NonSerialized] public bool Frozen; //Froze tiles can't be moved. Used for tutorial

        [Tooltip("Interactable elements are interacted by creating matches around it, or moving it if it is movable")]
        public bool Interactable;
        [Tooltip("Indestructible tiles can't be destroyed with bombs")] 
        public bool Indestructible;
        [Tooltip("Destructible Tiles gets destroyed when there is a match near them")]
        public bool Destructible;
        [Tooltip("If set to true, its visual will be hidden on spawn")]
        public bool HideVisual;
        public int HitToDestroy = 1;
        private int _currentTakenHit = 0;

        [Header("Gestures")]
        [SerializeField] private float TapDistanceThresh = 0.1f;
        [NonSerialized] public bool ToBeDestroyed;
        protected MatchThreeBoard ParentMatchThreeBoard;

        public override void Spawn()
        {
            base.Spawn();
            _currentTakenHit = 0;
            if(CurrentVisual != null && HideVisual)
            {
                CurrentVisual.gameObject.SetActive(false);
            }
        }
 
        public void SetBoard(MatchThreeBoard board, int row, int col)
        {
            ParentMatchThreeBoard = board;
            ParentBoard = board;
            SetRowCol(row, col);
        }
        public virtual void SetElementData(MatchThreeElementData data)
        {
            CurrentData = data;
            if (Row == 2 && Column == 2)
            {
                Debug.LogError("Deb");
            }
            if(CurrentData == null || CurrentData.VisualPrefab == null) return;
            if (CurrentVisual != null)
            {
                GameManager.Instance.Pool.PoolObject(CurrentVisual.gameObject);
            }
            CurrentVisual = GameManager.Instance.Pool.GetObject(CurrentData.VisualPrefab.gameObject);
            CurrentVisual.transform.SetParent(transform);
            CurrentVisual.transform.localPosition = Vector3.zero;
            CurrentVisual.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Checks whether a tile can be moved or not
        /// </summary>
        /// <returns></returns>
        public bool CanBeMoved()
        {
            return _canBeMoved;
        }

        public bool IsFrozen()
        {
            return Frozen;
        }

        /// <summary>
        /// Moves a tile towards a row - col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void MoveToRowCol(int row, int col, float speed)
        {
            SetRowCol(row, col);
            UpdateTargetPosition(speed);
        }

        #region Input
        private Vector3 _firstTouchPoint;
        private Vector3 _releasePoint;
        private bool _mousePressed = false;

        private void OnMouseDown()
        {
            if(Kuantech.Utils.Helpers.IsCursorOnUI()) return;
            if (!_canBeMoved) return;
            _mousePressed = true;
            _firstTouchPoint = GetMainCameraPos();
        }

        private void OnMouseUp()
        {
            if (!_mousePressed) return;
            _mousePressed = false;
            _releasePoint = GetMainCameraPos();
            if (Kuantech.Utils.Helpers.IsCursorOnUI()) return;
            //Is this tap?
            if ((_releasePoint - _firstTouchPoint).sqrMagnitude <= TapDistanceThresh)
            {
                OnTap();
                return;
            }
            if (!_canBeMoved) return;
            //Movement Angle
            float angle = Mathf.Atan2(_releasePoint.y - _firstTouchPoint.y, _releasePoint.x - _firstTouchPoint.x);
            angle = angle * 180.0f / Mathf.PI;
            if(Vector3.SqrMagnitude(_releasePoint - _firstTouchPoint) > .25f)
            {
                CheckMovement(angle);
            }
        }

        protected virtual void OnTap()
        {
            if(!Interactable) return;
            Interact();
            ParentMatchThreeBoard.PostMove();
        }

        private Vector3 GetMainCameraPos()
        {
            Vector3 mousePos = Input.mousePosition;
            return mousePos;
        }
        #endregion

        #region Moving

        public bool IsMoving()
        {
            if(WaypointFollower == null) return false;
            return WaypointFollower.IsMoving();
        }
        private void CheckMovement(float angle)
        {
            if(!_canBeMoved) return;
            //if(_moving) return;
            Vector2Int direction = new Vector2Int();
            if(angle <= 45.0f && angle >= -45.0f)
            {
                direction.x = 1;
                direction.y = 0;
            }
            else if(angle > 45.0f && angle < 135.0f)
            {
                direction.x = 0;
                direction.y = 1;
            }
            else if(angle >=135.0f || angle <= -135.0f)
            {
                direction.x = -1;
                direction.y = 0;
            }else if(angle > -135.0f && angle < -45.0f)
            {
                direction.y = -1;
                direction.x = 0;
            }else{
                Debug.LogError("An edge case about angle:"+angle);
                return;
            }
            MatchThreeElement otherElement = ParentBoard.GetTile(Row + direction.y, Column+direction.x) as MatchThreeElement;
            if(otherElement != null && otherElement._canBeMoved)
            {
                (ParentBoard as MatchThreeBoard).MakeAMove(this, otherElement);
            }
        }
        public void UpdateTargetPosition(float speed)
        {
            if(WaypointFollower == null)
            {
                WaypointFollower = gameObject.AddComponent<WaypointFollower>();
            }
            if(ParentMatchThreeBoard == null) return;
            WaypointFollower.Waypoint newWaypoint = new WaypointFollower.Waypoint{
                Position = ParentBoard.GetLocalPosition(Row, Column),
                IsLocal = true,
                Speed = speed,
            };
            WaypointFollower.AddWaypoint(newWaypoint);
        }
        #endregion

        #region Interactable
        public virtual void Interact()
        {
            ParentMatchThreeBoard.DestroyElement(this); //Destroy after using
        }

        /// <summary>
        /// Destructibles tla
        /// </summary>
        public virtual void TakeDamage()
        {
            if(!Destructible) return;
            _currentTakenHit++;
            if(_currentTakenHit >= HitToDestroy)
            {
                _currentTakenHit = 0;
                ParentMatchThreeBoard.DestroyElement(this);
            }
        }
        #endregion

        public bool IsSameType(MatchThreeElement element)
        {
            if(element == null || CurrentData == null || element.CurrentData == null) return false;
            return CurrentData.IsSameType(element.CurrentData);
        }

        public void Despawn()
        {
            //todo: Play destroy effect 
            if(CurrentData != null && CurrentData.EffectPlayer != null)
            {
                CurrentData.EffectPlayer.PlayEffectAtPosition(ParentBoard.GetGlobalPosition(Row, Column), 
                Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}