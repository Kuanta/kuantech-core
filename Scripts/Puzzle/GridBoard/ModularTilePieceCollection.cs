using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [CreateAssetMenu(fileName = "Modular Tile Visual Collection", menuName = "Kuantech/Utils/ModularTileVisualPiecesCollection")]
    public class ModularTilePieceCollection : ScriptableObject
    {
        [Serializable]
        public struct ModularTileVisualPieceData
        {
            public ModularTileVisualType PieceType;
            public GameObject Prefab;
        }
        
        public enum ModularTileVisualType
        {
            Center,
            ConnectedEdge,
            ClosedEdge,
            ClosedCorner,
            HorizontalCorner,
            VerticalCorner,
            AllConnectedCorner,
            BothConnectedCorner,
        }
        
        public List<ModularTileVisualPieceData> ModularTileVisualPieces;
        [SerializeField] private Dictionary<ModularTileVisualType, GameObject> _pieces;

        public GameObject GetPrefab(ModularTileVisualType pieceType)
        {
            if (_pieces == null)
            {
                _pieces = new Dictionary<ModularTileVisualType, GameObject>();
                foreach (var piece in ModularTileVisualPieces)
                {
                    _pieces[piece.PieceType] = piece.Prefab;
                }
            }

            if (!_pieces.ContainsKey(pieceType))
            {
                return null;
            }

            return _pieces[pieceType];
        }
    }
}