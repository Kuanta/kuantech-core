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
    }
    public class GridTile : MonoBehaviour {
        [NonSerialized] public GridBoard ParentBoard;
        public int Row;
        public int Column;
        public GameObject CurrentVisual;
        public bool LockVisual = false;

        /// <summary>
        /// Called when spawned from the grid board
        /// </summary>
        public virtual void Spawn()
        {

        }
        public virtual void SetRowCol(int row, int col)
        {
            Row = row;
            Column = col;
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
            CurrentVisual.transform.SetParent(transform);
            CurrentVisual.transform.localPosition = Vector3.zero;
            CurrentVisual.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// This is called for tiles that exists on the board
        /// </summary>
        public virtual void OnCreateExisting()
        {

        }

        public virtual void OnDespawn()
        {

        }
    }
}