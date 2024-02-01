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

        //State
        public bool CanBeMoved;
        [NonSerialized] public bool ToBeDestroyed;
        private MatchThreeBoard _parentMatchThreeBoard;

        private bool _initialized = false;
    
        public void SetInitialized()
        {
            _initialized = true;
        }
        
        public void SetBoard(MatchThreeBoard board, int row, int col)
        {
            _parentMatchThreeBoard = board;
            ParentBoard = board;
            SetRowCol(row, col);
        }
        public void SetElementData(MatchThreeElementData data)
        {
            _initialized = true;
            CurrentData = data;
            if(CurrentVisual != null)
            {
                GameManager.Instance.Pool.PoolObject(CurrentVisual.gameObject);
            }
            CurrentVisual = GameManager.Instance.Pool.GetObject(CurrentData.VisualPrefab.gameObject);
            CurrentVisual.transform.SetParent(transform);
            CurrentVisual.transform.localPosition = Vector3.zero;
            CurrentVisual.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Moves a tile towards a row - col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void MoveToRowCol(int row, int col)
        {
            SetRowCol(row, col);
            UpdateTargetPosition();
        }

        #region Input
        private Vector3 _firstTouchPoint;
        private Vector3 _releasePoint;
        private bool _mousePressed = false;

        private void OnMouseDown()
        {
            if (!CanBeMoved) return;
            _mousePressed = true;
            _firstTouchPoint = GetMainCameraPos();
        }

        private void OnMouseUp()
        {
            if (!CanBeMoved) return;
            _releasePoint = GetMainCameraPos();
            //Movement Angle
            float angle = Mathf.Atan2(_releasePoint.y - _firstTouchPoint.y, _releasePoint.x - _firstTouchPoint.x);
            angle = angle * 180.0f / Mathf.PI;
            if(Vector3.SqrMagnitude(_releasePoint - _firstTouchPoint) > .25f)
            {
                CheckMovement(angle);
            }
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
            if(!CanBeMoved) return;
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
            if(otherElement != null && otherElement.CanBeMoved)
            {
                (ParentBoard as MatchThreeBoard).MakeAMove(this, otherElement);
            }
        }
        public void UpdateTargetPosition()
        {
            if(WaypointFollower == null)
            {
                WaypointFollower = gameObject.AddComponent<WaypointFollower>();
            }
            if(_parentMatchThreeBoard == null) return;
            WaypointFollower.SetSpeed(_parentMatchThreeBoard.TileSpeed);
        
            WaypointFollower.Waypoint newWaypoint = new WaypointFollower.Waypoint{
                Position = ParentBoard.GetLocalPosition(Row, Column),
                IsLocal = true,
            };
            WaypointFollower.AddWaypoint(newWaypoint);
        }
        #endregion

        public bool IsSameType(MatchThreeElement element)
        {
            if(element == null || CurrentData == null || element.CurrentData == null) return false;
            return CurrentData.IsSameType(element.CurrentData);
        }

        public void Despawn()
        {
            _initialized = false;
            Destroy(gameObject);
        }
    }
}