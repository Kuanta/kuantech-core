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
            Vector3 boardForwardVector = GridBoard.transform.rotation * GridBoard.ForwardVector;
            Vector3 boardRightVector = GridBoard.transform.rotation * GridBoard.RightVector;
            TopAnchor.transform.localPosition = center + (GridBoard.GetDepth()*0.5f + TopPadding) * boardForwardVector;
            BottomAnchor.transform.localPosition = center - (GridBoard.GetDepth() * 0.5f + BottomPadding) * boardForwardVector ;
            LeftAnchor.transform.localPosition = center - (GridBoard.GetWidth() * 0.5f + LeftPadding) * boardRightVector;
            RightAnchor.transform.localPosition = center + (GridBoard.GetWidth() * 0.5f + RightPadding) * boardRightVector;
        }
    }
}