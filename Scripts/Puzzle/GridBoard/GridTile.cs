using System;
using System.Collections.Generic;
using Kuantech.AI.Pathfinding;
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
        public List<GridTileCoordinate> Coordinates;
        [NonSerialized] public GridBoard ParentBoard;
        [NonSerialized] public int AnchorRow;
        [NonSerialized] public int AnchorColumn;
        [NonSerialized] public int AnchorLayer;

        [Header("Visual")] public bool MaskBackground = false;
        public Transform VisualParent;
        [NonSerialized] public GameObject CurrentVisual;
        public bool LockVisual = false;
        
        /// <summary>
        /// Called when spawned from the grid board
        /// </summary>
        public virtual void Spawn(bool isExisting=false)
        {
            
        }

        public virtual void Despawn(bool clearingBoard)
        {
            if (!clearingBoard && StayOnBoardAfterDespawn)
            {
                gameObject.SetActive(false);
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
            AnchorRow = row;
            AnchorColumn = col;
            AnchorLayer = layer;
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
        
        /// <summary>
        /// Returns the path node on currently standing tile
        /// </summary>
        /// <returns></returns>
        public virtual PathNode GetCurrentPathNode()
        {
            if (ParentBoard == null) return null;
            return ParentBoard.GetPathNodeAtCoordinate(new GridTileCoordinate()
            {
                Row = AnchorRow,
                Column = AnchorColumn,
                Layer = AnchorLayer,
            });
        }

        public void RemoveFromBoard()
        {
            if (ParentBoard == null) return;
            ParentBoard.UnsetTile(this);
        }
    }
}