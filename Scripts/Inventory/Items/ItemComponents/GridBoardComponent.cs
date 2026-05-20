using System;
using System.Collections.Generic;
using Kuantech.Puzzle;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class GridBoardComponent : ItemComponent
    {
        private readonly GridBoardComponentData _data;

        public bool IsPlaced { get; private set; }
        public GridTileCoordinate PlacedAt { get; private set; }
        public string BoardId { get; private set; }

        // (oldBoardId, newBoardId) — newBoardId is null when cleared
        public event Action<string, string> OnBoardChanged;

        public GridBoardComponent(GridBoardComponentData data) => _data = data;

        public List<GridTileCoordinate> GetShape() => _data.GetCoordinates();

        public void SetPlacement(GridTileCoordinate coord, string boardId)
        {
            string oldBoardId = BoardId;
            PlacedAt = coord;
            BoardId = boardId;
            IsPlaced = true;
            if (oldBoardId != boardId)
                OnBoardChanged?.Invoke(oldBoardId, boardId);
        }

        public void ClearPlacement()
        {
            if (!IsPlaced) return;
            string oldBoardId = BoardId;
            IsPlaced = false;
            PlacedAt = null;
            BoardId = null;
            OnBoardChanged?.Invoke(oldBoardId, null);
        }

        public override void OnItemAdded(Item item) { }
        public override void OnItemRemoved(Item item) => ClearPlacement();
        public override void OnItemUsed(Item item) { }
        public override void OnItemEquipped(Item item, EquipmentSlotType slotType) { }
        public override void OnItemUnequipped(Item item) => ClearPlacement();

        [Serializable]
        private struct PlacementState
        {
            public int Row;
            public int Column;
            public int Layer;
            public bool IsPlaced;
            public string BoardId;
        }

        public override string SerializeState()
        {
            return JsonUtility.ToJson(new PlacementState
            {
                Row = PlacedAt?.Row ?? 0,
                Column = PlacedAt?.Column ?? 0,
                Layer = PlacedAt?.Layer ?? 0,
                IsPlaced = IsPlaced,
                BoardId = BoardId,
            });
        }

        public override void DeserializeState(string data)
        {
            var s = JsonUtility.FromJson<PlacementState>(data);
            IsPlaced = s.IsPlaced;
            BoardId = s.BoardId;
            PlacedAt = IsPlaced ? new GridTileCoordinate(s.Row, s.Column, s.Layer) : null;
        }
    }
}
