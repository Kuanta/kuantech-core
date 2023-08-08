using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;


namespace Kuantech.Core.Utils
{
    public class PhotoshootCamera : MonoBehaviour
    {
        [SerializeField] private Camera Camera;
        [SerializeField] private int Width;
        [SerializeField] private int Height;
        //[SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private Image Image;
        
        [SerializeField] private RenderTexture _renderTexture;
        public string rootPath = "Kuantech/Art/Sprites";
        [Button("Take A Shot")]
        public async void SetIconForImage(string filename = "shot.png")
        {
            if (!Application.isPlaying)
            {
                _renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);

            }
            Sprite sprite = await SnapshotAndSave(filename);
            if(Image != null) Image.sprite = sprite;
            if (!Application.isPlaying)
            {
                
                DestroyImmediate(_renderTexture);
            }
        }

        private async UniTask<Sprite> SnapshotAndSave(string filename = "shot.png")
        {
            Sprite sprite = 
                CreateSprite(512, 512);
            await Snapshot(sprite.texture);
            SaveSprite(sprite, rootPath, filename);
            _renderTexture.Release();
            Camera.targetTexture = null;
            return sprite;
        }

        public void SaveSprite(Sprite sprite, string filePath,string filename)
        {
            byte[] pngBytes = sprite.texture.EncodeToPNG();
            string source = Path.Combine(Application.dataPath, filePath);
            File.WriteAllBytes(Path.Combine(source, filename), pngBytes);
        }
        
        private Sprite CreateSprite(int width, int height)
        {
            return Sprite.Create(new Texture2D(Width, Height, TextureFormat.RGBA32, false),  new Rect(0,0,Width, Height), Vector2.zero);
        }
        
        private async UniTask Snapshot(Texture2D texture)
        {
            // Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            Rect rect = new Rect(0,0,Width, Height);
            Camera.targetTexture = _renderTexture;
            _renderTexture.Release();
            Camera.Render();
            RenderTexture currentRenderTexture = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            texture.ReadPixels(rect, 0, 0);
            texture.Apply();
            
            //Re-apply default frame buffer
            RenderTexture.active = currentRenderTexture;
            await UniTask.DelayFrame(1);
        }
    }
}