using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.Utils
{
    public class GridBoardScreenSizeFitter : ScreenSizeAdjuster
    {
        [Header("Paddings")]
        public float TopPadding;
        public float BottomPadding;
        public float LeftPadding;
        public float RightPadding;
        public Vector3 UpVector = new Vector3(0,1,0);

        [Header("Board")]
        public GridBoard GridBoard;
        protected override void Update()
        {
            if(!FitOnUpdate) return;
            AdjustAnchorPositions();
            FitCameraToAnchors();
            
        }

        private void AdjustAnchorPositions()
        {
            if(GridBoard == null) return;
            Vector3 center = GridBoard.transform.position;
            TopAnchor.transform.position = center + (GridBoard.GetDepth()*0.5f + TopPadding) * UpVector;
            BottomAnchor.transform.position = center - (GridBoard.GetDepth() * 0.5f + BottomPadding) * UpVector;
            LeftAnchor.transform.position = center - (GridBoard.GetWidth() * 0.5f + LeftPadding) * Vector3.right;
            RightAnchor.transform.position = center + (GridBoard.GetWidth() * 0.5f + RightPadding) * Vector3.right;
        }
    }
}