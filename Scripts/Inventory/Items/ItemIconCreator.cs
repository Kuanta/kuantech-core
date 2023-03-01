using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Inventory.Items
{
    public class ItemIconCreator : MonoBehaviour
    {
        [SerializeField] private Camera Camera;
        [SerializeField] private int Width;
        [SerializeField] private int Height;
        private RenderTexture _renderTexture;
        private Transform _photoShootPoint;

        private Dictionary<GameObject, Sprite> _itemSprites = new Dictionary<GameObject, Sprite>();
        
        private void Start()
        {
            if (_renderTexture == null) _renderTexture = new RenderTexture(Width, Height, 24);
            Camera.targetTexture = _renderTexture;
        }
        
        private Sprite GetSpriteForItem(GameObject itemVisualPrefab)
        {
            if (_itemSprites != null && _itemSprites.ContainsKey(itemVisualPrefab)) return _itemSprites[itemVisualPrefab];
            
            //Position Object
            GameObject itemVisual = Instantiate(itemVisualPrefab, _photoShootPoint);
            itemVisual.transform.localPosition = Vector3.zero;
            itemVisual.transform.localRotation = Quaternion.identity;
            
            //Create Texture
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            Rect rect = new Rect(0,0,Width, Height);
            
            //Render the object
            Camera.Render();

            RenderTexture currentRenderTexture = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            texture.ReadPixels(rect, 0, 0);
            texture.Apply();
            
            //Re-apply default frame buffer
            RenderTexture.active = currentRenderTexture;
            
            //Destroy instantiated
            Destroy(itemVisual);
            
            //todo: Should we delete _renderTexture?
            Sprite sprite = Sprite.Create(texture, rect, Vector2.zero);
            _itemSprites[itemVisualPrefab] = sprite;
            
            return sprite;
        }
    }
}