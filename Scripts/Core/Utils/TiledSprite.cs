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
        public GameObject EntryObject;
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
                if (entry.ConnectivityMask.Equals(eightConnectivitiy))
                {
                    SetEntry(entry);
                }
                else
                {
                    UnsetEntry(entry);
                }
            }
            //
            // //Set as default
            // SpriteRenderer.sprite = DefaultSprite;
        }

        private void SetEntry(TiledSpriteEntry entry)
        {
            if (SpriteRenderer != null && entry.Sprite != null)
            {
                SpriteRenderer.sprite = entry.Sprite;
            }
            
            if (entry.EntryObject != null)
            {
                entry.EntryObject.SetActive(true);
            }
            transform.localRotation = Quaternion.AngleAxis(entry.Angle, UpVector);

        }

        private void UnsetEntry(TiledSpriteEntry entry)
        {
            if (entry.EntryObject != null)
            {
                entry.EntryObject.SetActive(false);
            }
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