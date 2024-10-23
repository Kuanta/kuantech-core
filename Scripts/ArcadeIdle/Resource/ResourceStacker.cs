using Kuantech.Utils;
using UnityEditor;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ResourceStacker : MonoBehaviour
    {
        [Header("Properties")] 
        [SerializeField] private Vector3 CellSize;
        [SerializeField] private Vector3 Paddings;
        [SerializeField] private Vector2 AnchorPoints = new Vector2(0, 0);
        [SerializeField] private int ColumnCount;
        [SerializeField] private int RowCount;
        [SerializeField] protected Transform AnchorPoint;
        public void SetRowCount(int rowCount)
        {
            RowCount = rowCount;
        }
        
        public void SetColumnCount(int colCount)
        {
            ColumnCount = colCount;
        }

        public void StackObject(IResourcePositioner visual, int index, bool flyToPosition = false)
        {
            WorldPoint point = GetLocalPosition(index);
            Transform parent = AnchorPoint != null ? AnchorPoint : transform;
            point.Target = parent;
            if (flyToPosition)
            {
                visual.GoToTarget(point);
            }else{
                visual.WarpToPoint(point);
            }
        }

        public WorldPoint GetWorldPosition(int index)
        {
            WorldPoint localPoint = GetLocalPosition(index);
            Vector3 worldPoint = AnchorPoint.transform.TransformPoint(localPoint.LocalPosition);
            Quaternion worldRotation = AnchorPoint.rotation * localPoint.LocalRotation;

            return new WorldPoint
            {
                Position = worldPoint,
                Rotation = worldRotation,
            };
        }

        public WorldPoint GetLocalPosition(int index)
        {
            int currentIndex = index;
            int heightIndex = Mathf.FloorToInt(currentIndex / (float)(ColumnCount * RowCount));

            int rowColFlatIndex = currentIndex - heightIndex * ColumnCount * RowCount;

            int rowIndex = Mathf.FloorToInt(rowColFlatIndex / (float)ColumnCount);
            int columnIndex = rowColFlatIndex - rowIndex * ColumnCount;

            // Incorporate the AnchorPoints to center the grid and apply padding to move cells outward
            Vector3 colPos = new Vector3(1, 0, 0) *
                             (CellSize.x * 0.5f + columnIndex * (CellSize.x + Paddings.x) - AnchorPoints.x * (CellSize.x) * ColumnCount - Mathf.Max(ColumnCount-1,0) * Paddings.x * AnchorPoints.x);

            Vector3 rowPos = new Vector3(0, 0, 1) *
                             (CellSize.z * 0.5f + rowIndex * (CellSize.z + Paddings.z) - AnchorPoints.y * (CellSize.z) * RowCount  - Mathf.Max(RowCount-1,0) * Paddings.z * AnchorPoints.y);

            Vector3 heightPos = new Vector3(0, 1, 0) *
                                (heightIndex * (CellSize.y + Paddings.y) + (Mathf.Max(heightIndex-1, 0)) * Paddings.y); // Apply padding to the height as usual

            return new WorldPoint
            {
                LocalPosition = colPos + rowPos + heightPos,
                LocalRotation = Quaternion.identity,
            };
        }

#if UNITY_EDITOR
        public bool AlwaysShow = false;
        void OnDrawGizmos()
        {
            if(AnchorPoint == null && !AlwaysShow) return;
            if(Selection.activeGameObject != gameObject) return;
            Gizmos.color = Color.yellow; // Set the color of the gizmos
            ColumnCount = Mathf.Max(ColumnCount, 1);
            RowCount = Mathf.Max(RowCount, 1);
            Vector3 size = new Vector3(CellSize.x, CellSize.y, CellSize.z);
            // Loop through each cell and draw it
            for (int i = 0; i < ColumnCount * RowCount; i++)
            {
                WorldPoint point = GetLocalPosition(i);
                // Get the rotated size
                Vector3 rotatedSize = AnchorPoint.rotation * size;

                // Draw a wire cube at each cell position
                Gizmos.DrawWireCube(AnchorPoint.TransformPoint(point.LocalPosition), rotatedSize);
            }
        }
#endif
    }
}