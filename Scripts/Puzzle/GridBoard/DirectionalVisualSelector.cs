using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class DirectionalVisualSelector : MonoBehaviour
    {
        [Serializable]
        public struct PieceByDirectionalityData
        {
            public bool Top;
            public bool Bottom;
            public bool Left;
            public bool Right;
            public float Rotation;
            public GameObject Visual;
        }
        public List<PieceByDirectionalityData> PieceDatas;
        public int Tag = 0; //Tag is needed to compare 
        public GridTile AttachedTile;

        public void SetVisual()
        {
            if (AttachedTile == null || AttachedTile.ParentBoard == null) return;

            bool[] connectivitiy = new bool[4];
            for (int i = 0; i < 4; ++i)
            {
                GridBoard.Directions direction = (GridBoard.Directions) i;
                GridTile tile = AttachedTile.ParentBoard.GetTileAtDirection(direction, AttachedTile);
                if(tile == null) continue;
                if (tile.TryGetComponent(out DirectionalVisualSelector dvs) && dvs.Tag == Tag)
                {
                    connectivitiy[i] = true;
                }
                else
                {
                    connectivitiy[i] = false;
                }
            }

            GameObject activeGameObject = null;
            float angle = 0;
            foreach (var data in PieceDatas)
            {
                data.Visual.SetActive(false); //Just in case
                
                //is this the piece
                if (data.Top == connectivitiy[(int) GridBoard.Directions.Top] &&
                    data.Bottom == connectivitiy[(int) GridBoard.Directions.Bottom] &&
                    data.Left == connectivitiy[(int) GridBoard.Directions.Left] && 
                    data.Right == connectivitiy[(int)GridBoard.Directions.Right])
                {
                    activeGameObject = data.Visual;
                    angle = data.Rotation;
                }
            }

            if (activeGameObject != null)
            {
                activeGameObject.SetActive(true);
                activeGameObject.transform.localRotation = Quaternion.Euler(0,angle, 0);
            }
        }
    }
}