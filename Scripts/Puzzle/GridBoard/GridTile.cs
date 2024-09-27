using System;
using UnityEngine;

namespace Kuantech.Puzzle
{   

    [Serializable]
    public struct GridTileData
    {
        public string Id;
        public GridTile Prefab;
    }
    
    [Serializable]
    public struct GridTileCoordinate
    {
        public int Row;
        public int Column;
        public int Layer;
    }
    public class GridTile : MonoBehaviour
    {
        public bool DestroyOnDespawn = true;
        public bool StayOnBoardAfterDespawn = false;
        [NonSerialized] public GridBoard ParentBoard;
        [NonSerialized] public int Row;
        [NonSerialized] public int Column;
        [NonSerialized] public int Layer;
        
        [Header("Visual")] 
        public Transform VisualParent;
        [NonSerialized] public GameObject CurrentVisual;
        public bool LockVisual = false;
        
        /// <summary>
        /// Called when spawned from the grid board
        /// </summary>
        public virtual void Spawn()
        {
        
        }

        public virtual void Despawn(bool clearingBoard)
        {
            if (!clearingBoard && StayOnBoardAfterDespawn)
            {
                return;
            }
            if (ParentBoard != null)
            {
                ParentBoard.UnsetTile(this);
            }
            if (DestroyOnDespawn)
            {
                Destroy(gameObject);
            }
            else if(!StayOnBoardAfterDespawn)
            {
                gameObject.SetActive(false);
            }
        }
        
        public virtual void SetRowCol(int row, int col, int layer)
        {
            Row = row;
            Column = col;
            Layer = layer;
        }
        
        public void SetLocalPosition(Vector3 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public virtual void SetVisual(GameObject visual)
        {
            if(CurrentVisual != null && LockVisual) return;
            if(CurrentVisual != null)
            {
                Destroy(CurrentVisual);
            }
            CurrentVisual = Instantiate(visual);
            Transform visualParent = VisualParent != null ? VisualParent : transform;
            CurrentVisual.transform.SetParent(visualParent);
            CurrentVisual.transform.localPosition = Vector3.zero;
            CurrentVisual.transform.localRotation = Quaternion.identity;
        }
    }
}