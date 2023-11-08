using DG.Tweening;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class RectangularFormation : CrowdFormation
    {
        public RectangularFormation(CrowdFormationData data) : base(data)
        {
        }
        
        private int ColCount;
        private int RowCount;
        private float Depth;
        public override void SetCrowdFormation(Transform parent)
        {
            int count = parent.childCount;
            ColCount = Mathf.Max(FormationData.MaxColumn, 1);
            RowCount = Mathf.Max(Mathf.CeilToInt(count / ColCount), 1);

            Depth = (RowCount-1) * FormationData.RowDelta;

            base.SetCrowdFormation(parent);
        }

        protected override Vector3 GetLocalPositionForChild(int childIndex, int workerCount)
        {
            int rowIndex = Mathf.FloorToInt(childIndex / ColCount);
            int colIndex = childIndex - rowIndex * ColCount;

            //Find the worker count in this row
            int childCountAtRow = workerCount - rowIndex * ColCount;
            childCountAtRow = Mathf.Min(ColCount, childCountAtRow);
            float width = (Mathf.Max(childCountAtRow, 1) - 1) * FormationData.ColumnDelta;
            return new Vector3(
                colIndex * FormationData.ColumnDelta - width * 0.5f,
                0f,
                rowIndex * FormationData.RowDelta - Depth * 0.5f
            );
        }
    }
}