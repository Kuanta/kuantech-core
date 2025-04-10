using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Puzzle
{
    
    public class ModularTileVisual : MonoBehaviour
    {
        public GridTile AttachedTile;

        public List<ModularTileVisualPiece> ModularTileVisualPiece;

        public void SetVisual(bool[] eightConnectivity)
        {
            foreach (var piece in ModularTileVisualPiece)
            {
                piece.Toggle(eightConnectivity);
            }
        }
        public void SetVisual(GridTile parentTile)
        {
            AttachedTile = parentTile;
            bool[] eightConnectivity = new bool[8];
            eightConnectivity[(int)GridBoard.Directions.TopLeft] = HasSameTileAtDirection(GridBoard.Directions.TopLeft);
            eightConnectivity[(int)GridBoard.Directions.Top] = HasSameTileAtDirection(GridBoard.Directions.Top);
            eightConnectivity[(int)GridBoard.Directions.TopRight] = HasSameTileAtDirection(GridBoard.Directions.TopRight);
            eightConnectivity[(int)GridBoard.Directions.Left] = HasSameTileAtDirection(GridBoard.Directions.Left);
            eightConnectivity[(int)GridBoard.Directions.Right] = HasSameTileAtDirection(GridBoard.Directions.Right);
            eightConnectivity[(int)GridBoard.Directions.BottomLeft] = HasSameTileAtDirection(GridBoard.Directions.BottomLeft);
            eightConnectivity[(int)GridBoard.Directions.Bottom] = HasSameTileAtDirection(GridBoard.Directions.Bottom);
            eightConnectivity[(int)GridBoard.Directions.BottomRight] = HasSameTileAtDirection(GridBoard.Directions.BottomRight);

            foreach (var piece in ModularTileVisualPiece)
            {
                piece.Toggle(eightConnectivity);
            }
        }
        
        private bool HasSameTileAtDirection(GridBoard.Directions direction)
        {
            GridTile tileAtDirection = AttachedTile.ParentBoard.GetTileAtDirection(direction, AttachedTile);
            if (tileAtDirection == null) return false;
            if (tileAtDirection.TryGetComponent(out ModularTileVisual modularTileVisual))
            {
                return true;
            }
            return false;
        }
            
        [Button("Detect Modular Pieces")]
        private void DetectModularPieces()
        {
            ModularTileVisualPiece = GetComponentsInChildren<ModularTileVisualPiece>().ToList();
        }

        [SerializeField] private bool Top;
        [SerializeField] private bool Left;
        [SerializeField] private bool Right;
        [SerializeField] private bool Bottom;
        [SerializeField] private bool TopRight;
        [SerializeField] private bool TopLeft;
        [SerializeField] private bool BottomRight;
        [SerializeField] private bool BottomLeft;
        [Button("Test Piece")]
        private void TestPieces()
        {
            bool[] eightConnectivity = new bool[8];
            eightConnectivity[(int)GridBoard.Directions.TopLeft] = TopLeft;
            eightConnectivity[(int)GridBoard.Directions.Top] = Top;
            eightConnectivity[(int)GridBoard.Directions.TopRight] = TopRight;
            eightConnectivity[(int)GridBoard.Directions.Left] = Left;
            eightConnectivity[(int)GridBoard.Directions.Right] = Right;
            eightConnectivity[(int)GridBoard.Directions.BottomLeft] = BottomLeft;
            eightConnectivity[(int)GridBoard.Directions.Bottom] = Bottom;
            eightConnectivity[(int)GridBoard.Directions.Right] = Right;
            eightConnectivity[(int)GridBoard.Directions.BottomRight] = BottomRight;
            foreach (var piece in ModularTileVisualPiece)
            {
                piece.Toggle(eightConnectivity);
            }
        }

        [Button("Create Pieces")]
        private void CreatePieces(ModularTilePieceCollection collection)
        {
            foreach (var piece in ModularTileVisualPiece)
            {
                piece.CreatePiece(collection);
            }
        }
    }
}