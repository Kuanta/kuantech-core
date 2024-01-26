using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{

    public class MatchThreeElement : GridTile
    {
        [NonSerialized] public MatchThreeElementData CurrentData;
        [NonSerialized] public MatchThreeElementVisual CurrentVisual;

        //State
        [NonSerialized] public bool ToBeDestroyed;
        private MatchThreeBoard _parentMatchThreeBoard;

        private bool _initialized = false;
        private void Update()
        {
            if(!_initialized) return;
            transform.localPosition = Vector3.Lerp(transform.position, _targetLocalPosition, _parentMatchThreeBoard.TileSpeed * Time.deltaTime);
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
            CurrentVisual = GameManager.Instance.Pool.GetObject(CurrentData.VisualPrefab.gameObject)
            .GetComponent<MatchThreeElementVisual>();

            CurrentVisual.transform.SetParent(transform);
            CurrentVisual.transform.localPosition = Vector3.zero;
            CurrentVisual.transform.localRotation = Quaternion.identity;
        }

        public override void SetRowCol(int row, int col)
        {
            base.SetRowCol(row, col);
            UpdateTargetPosition();
        }

        #region Input
        private Vector3 _firstTouchPoint;
        private Vector3 _releasePoint;
        private bool _mousePressed = false;

        private void OnMouseDown()
        {
            _mousePressed = true;

            _firstTouchPoint = GetMainCameraPos();
        }

        private void OnMouseUp()
        {
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

        private void CheckMovement(float angle)
        {
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
            if(otherElement != null)
            {
                (ParentBoard as MatchThreeBoard).MakeAMove(this, otherElement);
            }
        }
        private Vector3 _targetLocalPosition;
        public void UpdateTargetPosition()
        {
            _targetLocalPosition = ParentBoard.GetLocalPosition(Row, Column);
        }
        #endregion

        public bool IsSameType(MatchThreeElement element)
        {
            return CurrentData.IsSameType(element.CurrentData);
        }

        public void Despawn()
        {
            _initialized = false;
            GameManager.Instance.Pool.PoolObject(gameObject);
        }
    }
}