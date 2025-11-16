using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Utils
{
    public class TransparentCameraCapture : MonoBehaviour
{
    public Camera captureCamera;         // Şeffaf arka planlı kamera
    public int width = 1024;
    public int height = 1024;
    [Tooltip("Assets klasörü dışına yazacaksanız tam yol verin; yoksa Application.persistentDataPath kullanın.")]
    public string fileName = "capture.png";
    public bool autoTrimTransparentBorder = true; // Kenarlardaki boş alfa piksellerini kırp

    [Button("Capture Now")]
    public void CaptureNow()
    {
        if (!captureCamera)
        {
            Debug.LogError("Capture camera not set.");
            return;
        }

        // 1) RenderTexture (alfa destekli)
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 1; // MSAA kapalı (ReadPixels için güvenli)
        rt.Create();

        // 2) Render
        var prevTarget = captureCamera.targetTexture;
        var prevActive = RenderTexture.active;
        captureCamera.targetTexture = rt;
        captureCamera.Render();
        RenderTexture.active = rt;

        // 3) Readback
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // 4) İsteğe bağlı: transparan kenarları kırp
        if (autoTrimTransparentBorder)
            tex = TrimAlpha(tex);

        // 5) PNG yaz
        var bytes = tex.EncodeToPNG();
        var path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, bytes);
        Debug.Log($"PNG saved: {path}");

        // 6) Temizlik
        captureCamera.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        rt.Release();
        Destroy(rt);
        Destroy(tex);
    }

    // Şeffaf kenarları kırpan basit yardımcı
    Texture2D TrimAlpha(Texture2D src)
    {
        var pixels = src.GetPixels32(); // Tam dokuyu Color32[] olarak okuyalım
        int w = src.width, h = src.height;

        int minX = w, minY = h, maxX = -1, maxY = -1;

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[row + x].a != 0)
                {
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
        }

        // Tamamen boşsa geri dön
        if (maxX < minX || maxY < minY)
            return src;

        int tw = maxX - minX + 1;
        int th = maxY - minY + 1;

        // DİKKAT: Blok almak için GetPixels kullan (Color[] döner)
        Color[] block = src.GetPixels(minX, minY, tw, th);

        var trimmed = new Texture2D(tw, th, TextureFormat.RGBA32, false, false);
        trimmed.SetPixels(block);
        trimmed.Apply();
        return trimmed;
    }
}

}
