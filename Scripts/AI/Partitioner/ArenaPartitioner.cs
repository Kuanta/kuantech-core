using System.Collections.Generic;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kuantech.AI
{
    public class ArenaPartitioner : MonoBehaviour
    {
        [Header("Grid Settings")]
        public float cellSize = 1.0f;     
        public LayerMask groundLayer;    
        public LayerMask obstacleLayer;   
        public float maxSlope = 45f;       
        public float agentHeight = 2.0f;  

        [Header("Bake Info (Read Only)")]
        public Vector3 gridOrigin; 
        public Vector2Int gridSize;    
        public List<GridCell> validCells = new List<GridCell>();

        [System.Serializable]
        public struct GridCell
        {
            public int id;          // Unique ID
            public int xIndex;      // X koordinatı
            public int zIndex;      // Z koordinatı
            public Vector3 worldCenter; // Dünyadaki merkezi
        }

        // Editörde butona basınca çalışacak
        [Button("Bake Arena Grid")]
        public void BakeGrid()
        {
            validCells.Clear();

            // 1. BOUNDING BOX HESAPLA
            // Bu objenin ve çocuklarının kapladığı alanı bul
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            if (renderers.Length == 0)
            {
                Debug.LogError("ArenaPartitioner: Hiçbir Renderer bulunamadı! Lütfen arena objelerini child olarak ekle.");
                return;
            }

            foreach (Renderer r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            // Grid sınırlarını belirle
            gridOrigin = bounds.min;
            Vector3 gridMax = bounds.max;

            // Kaç tane hücre sığar?
            int cols = Mathf.CeilToInt((gridMax.x - gridOrigin.x) / cellSize);
            int rows = Mathf.CeilToInt((gridMax.z - gridOrigin.z) / cellSize);
            gridSize = new Vector2Int(cols, rows);

            Debug.Log($"Arena Bounds: {bounds}. Grid Size: {cols}x{rows}");

            // 2. RAYCAST YAĞMURU (THE RAIN)
            int cellIDCounter = 0;
            float halfCell = cellSize * 0.5f;

            // Yukarıdan tarama yapacağımız yükseklik (Tavanın biraz üstü)
            float rayOriginY = bounds.max.y + 1f; 
            float rayDistance = 100f;

            for (int x = 0; x < cols; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    // Hücrenin merkez X ve Z koordinatını bul
                    float worldX = gridOrigin.x + (x * cellSize) + halfCell;
                    float worldZ = gridOrigin.z + (z * cellSize) + halfCell;
                    Debug.Log("X:"+worldX +" Z:"+worldZ);
                    Vector3 rayStart = new Vector3(worldX, rayOriginY, worldZ);

                    // Aşağı ışın at
                    if (UnityEngine.Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
                    {
                        // Zemin bulduk! Peki burası yürünebilir mi?
                        
                        // A. Eğim Kontrolü
                        if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope)
                            continue; // Çok dik

                        // B. Engel Kontrolü (OverlapBox)
                        // Hücrenin merkezinde, hücre boyutunda bir kutu oluşturup içinde engel var mı diye bakıyoruz.
                        // Yükseklik olarak agentHeight kullanıyoruz.
                        Vector3 boxCenter = hit.point + Vector3.up * (agentHeight * 0.5f);
                        Vector3 boxHalfExtents = new Vector3(halfCell * 0.9f, agentHeight * 0.45f, halfCell * 0.9f); // 0.9 ile biraz pay bırakıyoruz (padding)

                        if (UnityEngine.Physics.OverlapBox(boxCenter, boxHalfExtents, Quaternion.identity, obstacleLayer).Length > 0)
                        {
                            // Engel var, bu hücreyi alma!
                            // (Veya senin dediğin gibi "Kısmen engelse al" mantığı için burayı gevşetebilirsin
                            // ama botların duvara sıkışmaması için almamak daha iyidir)
                            continue; 
                        }

                        // C. Her şey temiz, listeye ekle
                        GridCell newCell = new GridCell
                        {
                            id = cellIDCounter++,
                            xIndex = x,
                            zIndex = z,
                            worldCenter = hit.point // Zemindeki nokta
                        };

                        validCells.Add(newCell);
                    }
                }
            }

            Debug.Log($"Bake Tamamlandı! Toplam {validCells.Count} geçerli hücre bulundu.");
        }

        // GIZMOS: Editörde gridi görmek için
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (validCells == null || validCells.Count == 0) return;

            Gizmos.color = new Color(0, 1, 0, 0.3f); // Yarı saydam yeşil
            Vector3 size = new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f);

            foreach (var cell in validCells)
            {
                Gizmos.DrawCube(cell.worldCenter, size);
            }
            
            // Grid sınırlarını çiz
            Gizmos.color = Color.yellow;
            Vector3 center = gridOrigin + new Vector3(gridSize.x * cellSize * 0.5f, 0, gridSize.y * cellSize * 0.5f);
            Vector3 boundsSize = new Vector3(gridSize.x * cellSize, 1, gridSize.y * cellSize);
            Gizmos.DrawWireCube(center, boundsSize);
        }
#endif

        
       /// <summary>
       /// Detect cell index from global position
       /// </summary>
       /// <param name="position"></param>
       /// <returns></returns>
       [Button("Get Closest Cell Index")]
        public int GetClosestCellIndex(Vector3 position)
        {
            // Bu en basit (ve yavaş) yöntemdir. Grid matematiksel olduğu için
            // aslında O(1) ile bulunabilir ama şimdilik en yakın valid cell'i bulalım.
            
            float minDst = float.MaxValue;
            int bestID = -1;

            foreach (var cell in validCells)
            {
                float dst = Vector3.SqrMagnitude(cell.worldCenter - position);
                if (dst < minDst)
                {
                    minDst = dst;
                    bestID = cell.id;
                }
            }
            return bestID;
        }

       public GridCell GetCellByID(int cellID)
       {
           if (validCells.IsValidIndex(cellID)) return validCells[cellID];
           return new GridCell();
       }
       
        // Matematiksel O(1) bulma yöntemi (Eğer Grid düzgünse bunu kullan)
        public int GetCellIDFast(Vector3 position)
        {
            // Bu metod valid olup olmadığını kontrol etmez, matematiksel index döner.
            // Lookup table'da bu index -1 ise geçersizdir.
            // İleride burayı implement ederiz.
            return -1;
        }

        [Button("Test raycast")]
        public void TestRaycast(Vector3 rayPos)
        {

            if (UnityEngine.Physics.Raycast(rayPos, Vector3.down, out RaycastHit hit,100, groundLayer))
            {
                Debug.Log("Hit!"+hit.point);
            }
        }
        
    }
}