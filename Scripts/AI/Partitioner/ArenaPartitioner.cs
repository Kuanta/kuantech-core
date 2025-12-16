using System.Collections.Generic;
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
        // Artık origin'i LOCAL olarak saklıyoruz. Obje nereye giderse gitsin grid onunla gelir.
        public Vector3 localGridOrigin; 
        public Vector2Int gridSize;    
        public List<GridCell> validCells = new List<GridCell>();

        // Lookup optimizasyonu için grid haritası (Runtime'da doldurulacak)
        private Dictionary<int, int> _gridLookup; // Key: Hash(x,z), Value: ListIndex
        private int[] _fastLookupTable;
        public void InitializeFastLookup()
        {
            // Toplam olası hücre sayısı (Boşluklar dahil tüm dikdörtgen alan)
            int totalSlots = gridSize.x * gridSize.y;
            _fastLookupTable = new int[totalSlots];

            // 1. Önce içini -1 (Geçersiz) ile doldur
            for (int i = 0; i < totalSlots; i++) _fastLookupTable[i] = -1;

            // 2. Geçerli hücrelerin ID'lerini doğru slotlara yerleştir
            foreach (var cell in validCells)
            {
                // 2D koordinatı 1D Array indeksine çevirme formülü:
                // index = (row * width) + col
                int flatIndex = (cell.zIndex * gridSize.x) + cell.xIndex;
            
                // Güvenlik kontrolü (Bake hatası varsa taşmasın)
                if (flatIndex >= 0 && flatIndex < totalSlots)
                {
                    _fastLookupTable[flatIndex] = cell.id;
                }
            }
        }
        
        [System.Serializable]
        public struct GridCell
        {
            public int id;          // Unique ID
            public int xIndex;      // Grid X (Col)
            public int zIndex;      // Grid Z (Row)
            public Vector3 localCenter; // Objenin merkezine göre konumu
        }

       [Button("Bake Arena Grid")]
        public void BakeGrid()
        {
            validCells.Clear();

            // 1. WORLD BOUNDS HESAPLA
            Bounds worldBounds = new Bounds(transform.position, Vector3.zero);
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            if (renderers.Length == 0)
            {
                Debug.LogError("ArenaPartitioner: Renderer bulunamadı!");
                return;
            }

            foreach (Renderer r in renderers)
            {
                worldBounds.Encapsulate(r.bounds);
            }

            // 2. LOCAL BOUNDS HESAPLA (Düzeltilen Kısım)
            // World Bounds'un 4 alt köşesini alıp locale çeviriyoruz.
            // Böylece objenin dönüşü ne olursa olsun en küçük x ve z'yi buluruz.
            
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;

            // Kutunun tabanındaki 4 köşe
            Vector3[] worldCorners = new Vector3[]
            {
                new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z), // Min-Min
                new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z), // Max-Min
                new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z), // Min-Max
                new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z)  // Max-Max
            };

            // Şimdi bu köşeleri locale çevirip en küçükleri (Min) ve en büyükleri (Max) bulalım
            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;

            foreach (var corner in worldCorners)
            {
                Vector3 localPoint = transform.InverseTransformPoint(corner);
                if (localPoint.x < minX) minX = localPoint.x;
                if (localPoint.z < minZ) minZ = localPoint.z;
                if (localPoint.x > maxX) maxX = localPoint.x;
                if (localPoint.z > maxZ) maxZ = localPoint.z;
            }

            // Artık gerçek local origin'i biliyoruz
            localGridOrigin = new Vector3(minX, 0, minZ); // Y'yi sıfır kabul edebiliriz, ray yukarıdan inecek
            
            // Grid boyutlarını hesapla
            float width = maxX - minX;
            float length = maxZ - minZ;

            int cols = Mathf.CeilToInt(width / cellSize);
            int rows = Mathf.CeilToInt(length / cellSize);
            gridSize = new Vector2Int(cols, rows);

            Debug.Log($"Rotasyon: {transform.rotation.eulerAngles}. Local Origin: {localGridOrigin}. Grid: {cols}x{rows}");

            // 3. TARAMA (THE RAIN)
            int cellIDCounter = 0;
            float halfCell = cellSize * 0.5f;
            
            // Raycast başlangıç Y yüksekliği (World Space)
            float rayStartY = worldBounds.max.y + 2f; 

            for (int x = 0; x < cols; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    // Local koordinatı hesapla
                    Vector3 localTarget = new Vector3(
                        localGridOrigin.x + (x * cellSize) + halfCell,
                        0, 
                        localGridOrigin.z + (z * cellSize) + halfCell
                    );

                    // World Space'e çevirip ışın at
                    Vector3 worldRayOrigin = transform.TransformPoint(localTarget);
                    // Ray yüksekliğini world bounds'a göre sabitle (Local Y kullanma çünkü zemin eğimli olabilir)
                    worldRayOrigin.y = rayStartY;

                    // Işın at
                    if (UnityEngine.Physics.Raycast(worldRayOrigin, Vector3.down, out RaycastHit hit, 100f, groundLayer))
                    {
                        if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope) continue;

                        Vector3 boxCenter = hit.point + Vector3.up * (agentHeight * 0.5f);
                        Vector3 boxHalfExtents = new Vector3(halfCell * 0.9f, agentHeight * 0.45f, halfCell * 0.9f);

                        if (UnityEngine.Physics.OverlapBox(boxCenter, boxHalfExtents, Quaternion.identity, obstacleLayer).Length > 0)
                            continue;

                        // Kayıt
                        GridCell newCell = new GridCell
                        {
                            id = cellIDCounter++,
                            xIndex = x,
                            zIndex = z,
                            localCenter = transform.InverseTransformPoint(hit.point)
                        };

                        validCells.Add(newCell);
                    }
                }
            }
            Debug.Log($"Bake Tamamlandı! {validCells.Count} hücre.");
        }

        // --- RUNTIME METOTLAR ---

        /// <summary>
        /// ID'si bilinen bir hücrenin GÜNCEL dünya pozisyonunu verir.
        /// Arena hareket etse bile doğru çalışır.
        /// </summary>
        public Vector3 GetCellWorldPosition(int cellID)
        {
            if (cellID >= 0 && cellID < validCells.Count)
            {
                // Local -> World dönüşümü
                return transform.TransformPoint(validCells[cellID].localCenter);
            }
            return Vector3.zero;
        }

        public int GetClosestCellIndex(Vector3 worldPosition)
        {
            // Eğer lookup henüz oluşmadıysa (Editör modunda vs.) oluştur
            if (_fastLookupTable == null || _fastLookupTable.Length == 0) InitializeFastLookup();

            // 1. Local Uzaya Çevir
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);

            // 2. Farkı Bul
            Vector3 diff = localPos - localGridOrigin;

            // 3. İndeksleri Hesapla
            int x = Mathf.FloorToInt(diff.x / cellSize);
            int z = Mathf.FloorToInt(diff.z / cellSize);

            // 4. Sınır Kontrolü (Gridin tamamen dışında mı?)
            if (x < 0 || z < 0 || x >= gridSize.x || z >= gridSize.y)
                return -1; 

            // 5. O(1) LOOKUP (BÜYÜK DEĞİŞİM BURASI)
            // Listeyi gezmek yerine, matematiği konuşturuyoruz.
            int flatIndex = (z * gridSize.x) + x;

            if (_fastLookupTable == null)
            {
                InitializeFastLookup();
            }
            
            return _fastLookupTable[flatIndex];
        }
        
        public GridCell GetCellByID(int cellID)
        {
            if (cellID >= 0 && cellID < validCells.Count) return validCells[cellID];
            return new GridCell(); // Boş struct
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (validCells == null || validCells.Count == 0) return;

            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Vector3 size = new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f);

            foreach (var cell in validCells)
            {
                // Çizerken Local'i World'e çeviriyoruz
                Vector3 worldPos = transform.TransformPoint(cell.localCenter);
                Gizmos.DrawCube(worldPos, size);
            }
            
            // Bounds çizimi (opsiyonel, hesaplaması biraz uzun olduğu için atladım)
        }
#endif
    }
}