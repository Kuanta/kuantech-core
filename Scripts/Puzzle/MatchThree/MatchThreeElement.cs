using System;
using System.Collections.Generic;
using DG.Tweening;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    [Serializable]
    public struct MatchThreeElementData
    {
        [KTTag("GemType")]
        public int Type;
        public GameObject Visual;

    }
    public class MatchThreeElement : GridTile
    {
        [Header("Element Types")]
        public List<MatchThreeElementData> Datas;
        private int _currentDataIndex;

        [Header("Properties")]
        [SerializeField] private float TweenDuration = 0.1f; 

        [HideInInspector] public int Type;

        //State
        [NonSerialized] public bool ToBeDestroyed;
  
        public void SetBoard(MatchThreeBoard board, int row, int col)
        {
            ParentBoard = board;
            Row = row;
            Column = col;
        }
        public void SetElement(int id)
        {
            Type = id;
            _currentDataIndex = -1;
            for(int i=0;i<Datas.Count;++i)
            {
                if(Datas[i].Type == id)
                {
                    _currentDataIndex = i;
                }
                Datas[i].Visual.SetActive(Datas[i].Type == id);
            }
        }

        /// <summary>
        /// Changes the type. Used to prevent initial mathces
        /// </summary>
        public void ChangeType()
        {
            _currentDataIndex++;
            if(_currentDataIndex >= Datas.Count || _currentDataIndex < 0)
            {
                _currentDataIndex = 0;
            }
            SetElement(Datas[_currentDataIndex].Type);
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
        private bool _moving = false;
        private Tween _moveTween = null;
        private void CheckMovement(float angle)
        {
            if(_moving) return;
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
                Debug.LogError("Other element:"+otherElement.name+" - Color:"+otherElement.Type);
                (ParentBoard as MatchThreeBoard).MakeAMove(this, otherElement);
            }
        }

        public void MoveTile(Vector3 newLocalPosition)
        {
            if(_moveTween != null)
            {
                _moveTween.Kill();
            }
            _moveTween = transform.DOLocalMove(newLocalPosition, TweenDuration).OnComplete(()=>{
                _moveTween = null;
            });
        }
        #endregion

        public bool IsSameType(MatchThreeElement element)
        {
            return Type == element.Type;
        }

        public void Despawn()
        {
            GameManager.Instance.Pool.PoolObject(gameObject);
        }
    }
}