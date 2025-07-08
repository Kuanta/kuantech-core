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
        [SerializeField] private Vector3 UpVector = Vector3.up;
        
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
                    if(SpriteRenderer != null) SpriteRenderer.sprite = entry.Sprite;
                    transform.localRotation = Quaternion.AngleAxis(entry.Angle, UpVector);
                    return;
                }
            }
            
            //Set as default
            SpriteRenderer.sprite = DefaultSprite;
        }

        public void SetSprite(ConnectivityMask mask)
        {
            SetSprite(mask.GetEightConnectivity());
        }
        
        public void SetColor(Color color)
        {
            SpriteRenderer.color = color;
        }
        
        [Button("Test Sprite")]
        private void TestSprite(ConnectivityMask mask)
        {
            SetSprite(mask);
        }
   
    }
}