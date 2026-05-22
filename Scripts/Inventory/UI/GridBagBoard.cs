using System;
using System.Collections.Generic;
using Kuantech.Puzzle;
using UnityEngine;

namespace Kuantech.Inventory.UI
{
    public class GridBagBoard : GridBoard
    {
        [SerializeField] private ItemTile _itemTilePrefab;
        [SerializeField] private string _boardId = "bag";

        public string BoardId => _boardId;

        public event Action OnInventoryChanged;

        public override void CreateBoard()
        {
            base.CreateBoard();
            var dropZone = GetComponent<GridBoardDropZone>();
            if (dropZone != null)
                dropZone.OnTileDropped += OnItemDropped;
        }

        // ── Populate ──────────────────────────────────────────────────────────

        public void Populate(Inventory inventory)
        {
            ClearBoard();
            if (inventory == null) return;

            foreach (var (item, comp) in inventory.GetItemsWithComponent<GridBoardComponent>())
            {
                if (!comp.IsPlaced || comp.BoardId != _boardId || comp.PlacedAt == null) continue;

                ItemTile tile = Instantiate(_itemTilePrefab, transform);
                tile.SetItem(item);

                if (!CanTileBePlaced(tile, comp.PlacedAt.Row, comp.PlacedAt.Column))
                {
                    Destroy(tile.gameObject);
                    continue;
                }

                SetTile(tile, comp.PlacedAt.Row, comp.PlacedAt.Column, 0);
            }
        }

        // ── Place ─────────────────────────────────────────────────────────────

        public bool PlaceItem(Item item, GridTileCoordinate coord)
        {
            if (item == null || _itemTilePrefab == null || coord == null) return false;

            ItemTile tile = Instantiate(_itemTilePrefab, transform);
            tile.SetItem(item);

            if (!CanTileBePlaced(tile, coord.Row, coord.Column))
            {
                Destroy(tile.gameObject);
                return false;
            }

            SetTile(tile, coord.Row, coord.Column, 0);
            item.GetItemComponent<GridBoardComponent>()?.SetPlacement(coord, _boardId);
            OnInventoryChanged?.Invoke();
            return true;
        }

        // ── Space checking ────────────────────────────────────────────────────

        public bool CanPlaceItem(Item item)
        {
            return FindSpaceForItem(item) != null;
        }

        public GridTileCoordinate FindSpaceForItem(Item item)
        {
            if (item == null) return null;
            var comp = item.GetItemComponent<GridBoardComponent>();
            if (comp == null) return null;

            List<GridTileCoordinate> shape = comp.GetShape();

            for (int r = 0; r < RowCount; r++)
            {
                for (int c = 0; c < ColumnCount; c++)
                {
                    if (CanShapeBePlaced(shape, r, c))
                        return new GridTileCoordinate(r, c);
                }
            }

            return null;
        }

        private bool CanShapeBePlaced(List<GridTileCoordinate> shape, int row, int col)
        {
            foreach (var offset in shape)
            {
                if (!IsTileValidAndEmpty(offset.Row + row, offset.Column + col)) return false;
            }
            return true;
        }

        // ── New item from asset ───────────────────────────────────────────────

        public bool PlaceNewItem(ItemData asset, GridTileCoordinate coord, Inventory inventory)
        {
            if (asset == null || coord == null || inventory == null) return false;
            Item item = inventory.AddItem(asset);
            if (item == null) return false;
            return PlaceItem(item, coord);
        }

        // ── Drop handler ──────────────────────────────────────────────────────

        private void OnItemDropped((GridTileCoordinate coord, GridTileGroup group) data)
        {
            if (GetTile(data.coord.Row, data.coord.Column, 0) is not ItemTile tile) return;
            if (tile.AssignedItem == null) return;

            tile.AssignedItem.GetItemComponent<GridBoardComponent>()?.SetPlacement(data.coord, _boardId);
            OnInventoryChanged?.Invoke();
        }
    }
}
