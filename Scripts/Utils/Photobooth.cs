using System;
using System.Collections.Generic;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Makeshift.Utils
{
    public class Photobooth : MonoBehaviour
    {
#if UNITY_EDITOR
        public string FolderPath = "Assets/Kuantech/Art/Sprite";
        public Transform PartParent;
        public List<GameObject> Parts;
        public int _currentIndex;
        public GameObject _currentPart;
        private Quaternion OldRotation;
        
        [Header("Zoom")]

        public float MaxSize = 10;
        public float MinSize = 1;
        public float MinZDist = 2f; //Min z dist at min size
        public float MaxZDist = 10f; //Max z dist at max size
        
        [Header("Screenshot")]
        public int captureWidth = 1024;
        public int captureHeight = 1024;
        public Color TransparencyColor;
        public bool SwapTransparent = true;
        
        private void Start()
        {
            _currentIndex = 0;
            SetNextPart();
        }

        public void SetNextPart()
        {
            OldRotation = PartParent.transform.rotation;
            if(_currentPart != null) Destroy(_currentPart);
            _currentPart = Instantiate(Parts[_currentIndex]);
            _currentPart.transform.SetParent(PartParent);
            _currentPart.transform.localPosition = Vector3.zero;
            _currentPart.transform.localRotation = Quaternion.Euler(new Vector3(0,180,0));
            _currentPart.transform.localScale = Vector3.one;
            _currentPart.name = Parts[_currentIndex].name;
            Helpers.ChangeLayerRecursively(_currentPart.transform, 12);
            PartParent.transform.rotation = Quaternion.identity;
            Invoke(nameof(PlaceGameObject), 0.01f);
            Invoke(nameof(RevertRotation), 0.01f);

        }

        private void RevertRotation()
        {
            PartParent.transform.rotation = OldRotation;
        }
        private void PlaceGameObject()
        {
            //Find bounding box    
            Bounds bounds = new Bounds( _currentPart.transform.position, Vector3.zero);

            Renderer[] renderers =  _currentPart.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 boundsCenter = bounds.center;
            Vector3 offset = PartParent.transform.position - boundsCenter;
            _currentPart.transform.localPosition += offset;

            //Get bounds
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y);
            Debug.LogError($"MaxSize:{maxSize}");
            maxSize = Mathf.Clamp(maxSize, MinSize, MaxSize);
            float zPos = (maxSize - MinSize) / (MaxSize - MinSize) * (MaxZDist - MinZDist) + MinZDist;
        }
        
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _currentIndex--;
                if (_currentIndex < 0)
                {
                    _currentIndex = Parts.Count - 1;
                }
                SetNextPart();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                _currentIndex++;

                if (_currentIndex >= Parts.Count)
                {
                    _currentIndex = 0;
                }
                SetNextPart();
            }
        }
        
        [Button("Set Index")]
        public void SetIndex(int index)
        {
            _currentIndex = index;
            if (_currentIndex < 0)
            {
                _currentIndex = Parts.Count - 1;
            }
            if (_currentIndex >= Parts.Count)
            {
                _currentIndex = 0;
            }
            SetNextPart();
        }

        #region Screenshot
        
        [Button("Take Screenshot")]
        private async void TakeScreenshot()
        {
            await _TakeScreenshot();
        }
        
        public async UniTask _TakeScreenshot()
        {
#if UNITY_EDITOR
            await UniTask.DelayFrame(1);

            RenderTexture rt = new RenderTexture(captureWidth, captureHeight, 24);
            Camera.main.targetTexture = rt;
            Camera.main.Render();

            // // If there's an additional camera (like one for outlines), render its view.
            // // Make sure this camera doesn't clear the color/depth.
            // Camera outlineCamera = Camera.main; // Replace with your outline camera if you have one.
            // if(outlineCamera != null)
            // {
            //     outlineCamera.targetTexture = rt;
            //     outlineCamera.Render();
            // }

            RenderTexture.active = rt;
            Texture2D screenshot = new Texture2D(captureWidth, captureHeight, TextureFormat.ARGB32, false);
            screenshot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);

            if (SwapTransparent)
            {
                for (int x = 0; x <  screenshot.width; x++)
                {
                    for (int y = 0; y <  screenshot.height; y++)
                    {
                        Color pixelColor =  screenshot.GetPixel(x, y);
                        if (Math.Abs(pixelColor.r - TransparencyColor.r) < 0.01f && Math.Abs(pixelColor.g - TransparencyColor.g) < 0.01f 
                            && Math.Abs(pixelColor.b - TransparencyColor.b) < 0.01f)  // You might want a small threshold here due to potential color variations
                        {
                            screenshot.SetPixel(x, y, Color.clear);
                        }
                        else
                        {
                            screenshot.SetPixel(x, y, pixelColor);
                        }
                    }
                }
                screenshot.Apply();

            }
 
            Camera.main.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            byte[] bytes = screenshot.EncodeToPNG();
            string filepath = $"{FolderPath}/{_currentPart.name}.png";
            await System.IO.File.WriteAllBytesAsync(filepath, bytes);
            
         
            
#endif
        }
        #endregion
#endif
    }
}