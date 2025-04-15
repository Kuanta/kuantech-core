using System;
using System.Collections.Generic;
using Kuantech.Puzzle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Utils
{
    [Serializable]
    public class TiledSpriteEntry
    {
        public Sprite Sprite;
        public float Angle;
        public ConnectivityMask ConnectivityMask;
        //public List<ModularTileVisualPiece.DirectionalityConditionEntry> DirectionalityConditions;
    }
    
    public class TiledSprite : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer SpriteRenderer;
        [SerializeField] private List<TiledSpriteEntry> SpriteEntries;
        [SerializeField] private Sprite DefaultSprite;
        
        /// <summary>
        /// Sets the sprite
        /// </summary>
        /// <param name="eightConnectivitiy"></param>
        public void SetSprite(bool[] eightConnectivitiy)
        {
            foreach (var entry in SpriteEntries)
            {
                bool satisfied = true;
                if (entry.ConnectivityMask.Equals(eightConnectivitiy))
                {
                    SpriteRenderer.sprite = entry.Sprite;
                    transform.localRotation = Quaternion.Euler(0, entry.Angle, 0);
                    return;
                }
            }
            
            //Set as default
            SpriteRenderer.sprite = DefaultSprite;
        }

        public void SetColor(Color color)
        {
            SpriteRenderer.color = color;
        }
        [Button("Test Sprite")]
        private void TestSprite(ConnectivityMask mask)
        {
            bool[] eightConnectivity = new bool[8];
            eightConnectivity[(int)GridBoard.Directions.TopLeft] = mask.Get(GridBoard.Directions.TopLeft) > 0;
            eightConnectivity[(int)GridBoard.Directions.Top] = mask.Get(GridBoard.Directions.Top) > 0;
            eightConnectivity[(int)GridBoard.Directions.TopRight] = mask.Get(GridBoard.Directions.TopRight) > 0;
            eightConnectivity[(int)GridBoard.Directions.Left] = mask.Get(GridBoard.Directions.Left) > 0;
            eightConnectivity[(int)GridBoard.Directions.Right] = mask.Get(GridBoard.Directions.Right) > 0;
            eightConnectivity[(int)GridBoard.Directions.BottomLeft] = mask.Get(GridBoard.Directions.BottomLeft) > 0;
            eightConnectivity[(int)GridBoard.Directions.Bottom] = mask.Get(GridBoard.Directions.Bottom) > 0;
            eightConnectivity[(int)GridBoard.Directions.BottomRight] = mask.Get(GridBoard.Directions.BottomRight) > 0;

            SetSprite(eightConnectivity);
        }
   
    }
}