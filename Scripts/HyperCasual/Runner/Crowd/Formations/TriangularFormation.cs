using System;
using Kuantech.Core.HyperCasual.Runner;
using UnityEngine;

namespace Kuantech.DemolutionRunner
{
    public class TriangularFormation : CrowdFormation
    {
        private int TotalRowCount;
        public TriangularFormation(CrowdFormationData data) : base(data)
        {
        }

        public override void SetCrowdFormation(Transform parent)
        {
            // This is the quadratic formula to solve for the triangular number series
            // We use only the positive result of the quadratic formula since the row count cannot be negative
            float n = (Mathf.Sqrt(1 + 8 * (float)parent.childCount) - 1) / 2;

            // We take the floor of the result to ensure we don't count an incomplete row
            TotalRowCount =  Mathf.CeilToInt(n);

            base.SetCrowdFormation(parent);
        }
        protected override Vector3 GetLocalPositionForChild(int childIndex, int workerCount)
        {
            // Calculate which row and position in the row this child is in
            int row = 0;
            int rowStartIndex = 0; // Index where the current row starts

            // Find the row this child belongs to and the starting index of that row
            while (childIndex >= rowStartIndex + row)
            {
                rowStartIndex += row;
                row++;
            }

            // Calculate the position within the row
            int positionInRow = childIndex - rowStartIndex;

            // The X position is determined by the row and the position in the row
            // Adjust X position by ColumnDelta to space out the workers
            float posX = (positionInRow - (row - 1) * 0.5f) * FormationData.ColumnDelta;

            // The Z position (forward/backward in Unity's coordinate system) moves back for each row
            // Adjust Z position by RowDelta to space out the workers
            float posZ = -row * FormationData.RowDelta + (TotalRowCount- 1)*FormationData.RowDelta*0.5f;

            // Add some noise if needed to randomize positions slightly
            Vector3 noise = new Vector3(
                UnityEngine.Random.Range(-FormationData.NoiseMagnitude, FormationData.NoiseMagnitude),
                0f,
                UnityEngine.Random.Range(-FormationData.NoiseMagnitude, FormationData.NoiseMagnitude)
            );

            return new Vector3(posX, 0f, posZ) + noise;
        }
    }


}