using System;
using System.Collections.Generic;
using Kuantech.Puzzle;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Inventory.UI
{
    public class ItemTile : GridTile
    {
        [Header("Item Tile")]
        public float CellSize;
        public Image ClickArea;
        public Image ColorImage;
        public Image Icon;

        [NonSerialized] public Item AssignedItem;

        public void SetItem(Item item)
        {
            AssignedItem = item;
            Coordinates = new List<BoardTileCoordinate>();
            AnchorOffset = Vector3.zero;

            if (item == null) return;

            ItemData asset = ItemsLibrary.GetItemData(item.GetId());
            GridBoardComponentData data = asset?.GetItemComponentData<GridBoardComponentData>();

            if (data != null)
            {
                List<GridTileCoordinate> shape = data.GetCoordinates();
                int minRow = int.MaxValue, minCol = int.MaxValue;
                int maxRow = int.MinValue, maxCol = int.MinValue;

                foreach (var coord in shape)
                {
                    Coordinates.Add(coord);
                    if (coord.Row < minRow) minRow = coord.Row;
                    if (coord.Column < minCol) minCol = coord.Column;
                    if (coord.Row > maxRow) maxRow = coord.Row;
                    if (coord.Column > maxCol) maxCol = coord.Column;
                }

                int rowSpan = maxRow - minRow;
                int colSpan = maxCol - minCol;

                if (ClickArea != null)
                    ClickArea.GetComponent<RectTransform>().sizeDelta = new Vector2((colSpan + 1) * CellSize, (rowSpan + 1) * CellSize);

                AnchorOffset = new Vector3(-CellSize * 0.5f * (colSpan - minCol), -CellSize * 0.5f * (rowSpan - minRow), 0);

                if (ColorImage != null) ColorImage.color = data.TileColor;
            }
            else
            {
                if (ClickArea != null)
                    ClickArea.GetComponent<RectTransform>().sizeDelta = new Vector2(CellSize, CellSize);
            }

            if (Icon != null) Icon.sprite = asset?.GetIcon();
        }

        public void ClearTile()
        {
            AssignedItem = null;
            Coordinates?.Clear();
            AnchorOffset = Vector3.zero;
            if (Icon != null) Icon.sprite = null;
            if (ColorImage != null) ColorImage.color = Color.white;
        }
    }
}
