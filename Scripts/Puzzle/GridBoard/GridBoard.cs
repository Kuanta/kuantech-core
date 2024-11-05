using System;
using System.Collections.Generic;
using Kuantech.Core.FX;
using Kuantech.Puzzle.Pathfinding;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class ExistingTileInfo
    {
        public GameObject Prefab;
        public int Row;
        public int Col;
    }

    public class GridBoard : MonoBehaviour
    {
        public enum Directions : uint
        {
            Top = 0, Right = 1, Bottom = 2, Left = 3,
        }
        
        [Header("Board Size")]
        public int RowCount = 5;
        public int ColumnCount = 5;
        public Vector3 ForwardVector = Vector3.up;
        public Vector3 RightVector = Vector3.right;

        [Header("Cell Size")]
        public float CellWidth = 1f;
        public float CellHeight = 1f;

        [Header("Origin Offset")] 
        public Vector2 OriginOffset = new Vector2(-0.5f, -0.5f);

        [Header("Existing Tiles")] 
        public List<GameObject> ExistingLayers;
        
        [Header("BackgroundTile object")] 
        public GridTileBackground BackgroundGameObjectPrefab;

        [Header("Pathfinding")] 
        public GridBoardPathTree GridBoardPathTree;
        
        [Header("Editor Background")] [SerializeField]
        private GameObject Editorbackground;
        
        public List<GridTile[,]> Tiles; //A list of list to represent layered tiles
        //public GridTile[,] Tiles;
        public GridTileBackground[,] BackgroundObjects;
        public bool[,] BackgroundMask; //A background object can't be placed if its masked here

        protected Dictionary<GridTileCoordinate, GridTile> _existingTilesDict;
        protected HashSet<GridTile> _existingTilesSet = new HashSet<GridTile>();

        //Masked tiles are tiles that are empty but 'blocked' tiles. 
        public HashSet<GridTileCoordinate> MaskedTiles = new HashSet<GridTileCoordinate>();

        public delegate void TileOperation(GridTile tile);
        public virtual void CreateBoard()
        {
            Tiles = new List<GridTile[,]>();
            BackgroundMask = new bool[RowCount,ColumnCount];
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    BackgroundMask[r, c] = false;
                }
            }
            
            AddLayer(); //Add at least a single layer
            FindExistingTiles();
            SetBackgroundTiles();
            UpdateDirectionalTiles();
            if (Editorbackground != null)
            {
                Editorbackground.gameObject.SetActive(false);
            }

            if (GridBoardPathTree != null)
            {
                GridBoardPathTree.CreateNodes(this);
            }
        }
        
        /// <summary>
        /// Finds existing tiles on the board
        /// </summary>
        private void FindExistingTiles()
        {
            _existingTilesDict = new Dictionary<GridTileCoordinate, GridTile>();
            if (ExistingLayers == null)
            {
                //Single layer
                FindExistingTilesUnderLayer(gameObject, 0);
            }
            else
            {
                for (int i = 0; i < ExistingLayers.Count; ++i)
                {
                    //Add non base layers
                    if (i > 0)
                    {
                        AddLayer();
                    }
                    FindExistingTilesUnderLayer(ExistingLayers[i], i);
                }
            }
        }

        private void AddLayer()
        {
            Tiles.Add(new GridTile[RowCount, ColumnCount]);
        }
        private void FindExistingTilesUnderLayer(GameObject parent, int layer)
        {
            GridTile[] existingTiles = parent.transform.GetComponentsInChildren<GridTile>();
            foreach (var tile in existingTiles)
            {
                GridTileCoordinate coord = GetRowColFromPosition(tile.transform.position);
                if (!IsCoordinateValid(coord))
                {
                    continue;
                }
                coord.Layer = layer;
                if (_existingTilesDict.ContainsKey(coord))
                {
                    Destroy(tile.gameObject);
                    continue;
                }

                tile.DestroyOnDespawn = false; //Existing tiles should be deactivated on despawn, not destroyed
                _existingTilesSet.Add(tile);
                _existingTilesDict[coord] = tile;
                if (!CanTileBePlaced(tile, coord.Row, coord.Column, coord.Layer))
                {
                    tile.Despawn(false);
                    continue;
                }
                SpawnExistingTile(tile, coord);

                if (tile is GridBoardUnpassableTile unpassableTile)
                {
                    BackgroundMask[coord.Row, coord.Column] = true;
                }else if (tile.MaskBackground)
                {
                    BackgroundMask[coord.Row, coord.Column] = true;
                }
            }
        }

        protected virtual void SpawnExistingTile(GridTile tile, GridTileCoordinate coord)
        {
            tile.gameObject.SetActive(true);
            SetTile(tile, coord.Row, coord.Column, coord.Layer);
            tile.Spawn(true);
        }
        
        /// <summary>
        /// Creates the background tiles
        /// </summary>
        private void SetBackgroundTiles()
        {
            BackgroundObjects = new GridTileBackground[RowCount, ColumnCount];
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    if (BackgroundMask[r, c]) continue;
                    AddBackgroundObject(r,c);
                }
            }
        }
        
        /// <summary>
        /// Resets the existing tiles
        /// </summary>
        private void RespawnExistingTiles()
        {
            foreach (var pair in _existingTilesDict)
            {
                GridTileCoordinate coord = pair.Key;
                GridTile existing = pair.Value;
                if (existing == null) continue;
                SpawnExistingTile(existing, coord);
            }
        }
        
        public virtual void AddBackgroundObject(int row, int col)
        {
            if (BackgroundGameObjectPrefab != null)
            {
                GridTileBackground bgObj = Instantiate(BackgroundGameObjectPrefab);
                bgObj.transform.parent = transform;
                bgObj.transform.localPosition = GetLocalPosition(row, col);
                bgObj.transform.localRotation = Quaternion.identity;
                BackgroundObjects[row, col] = bgObj;
            }
        }
        public virtual void RestartBoard()
        {
            ClearBoard();
            RespawnExistingTiles();
        }
        
        #region Move
        public virtual bool MoveTile(GridTile gridTile, int row, int col, int layer=0, bool setPosition = true)
        {
            if(!IsCoordinateValid(row, col)) return false;
            if(IsTileOccupied(row, col)) return false;
            SetTile(gridTile, row, col, layer,setPosition);
            return true;
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Masks a tile
        /// </summary>
        /// <param name="coord"></param>
        public void MaskTile(GridTileCoordinate coord)
        {
            MaskedTiles ??= new HashSet<GridTileCoordinate>();
            MaskedTiles.Add(coord);
        }
        
        /// <summary>
        /// Removes the mask from the tile
        /// </summary>
        /// <param name="coord"></param>
        public void RemoveTileMask(GridTileCoordinate coord)
        {
            if(MaskedTiles == null) return;
            MaskedTiles.Remove(coord);
        }

        public void ClearTileMasks()
        {
            MaskedTiles.Clear();
        }
        
        public bool IsCoordinateValid(GridTileCoordinate coordinate)
        {
            return IsCoordinateValid(coordinate.Row, coordinate.Column);
        }
        
        public bool IsCoordinateValid(int row, int col)
        {
            if(row < 0 || col < 0 || row >= RowCount || col >= ColumnCount) return false;
            return true;
        }

        public bool IsLayerValid(int layer)
        {
            if (Tiles.Count <= layer) return false;
            return true;
        }
        
        public bool IsTileValidAndEmpty(GridTileCoordinate coordinate)
        {
            return IsTileValidAndEmpty(coordinate.Row, coordinate.Column);
        }
        public bool IsTileValidAndEmpty(int row, int col, int layer=0)
        {
            return !IsTileOccupied(row, col, layer) && IsCoordinateValid(row, col);
        }

        /// <summary>
        /// Sets the tile for a grid tile
        /// </summary>
        /// <param name="gridTile">Grid Tile to set</param>
        /// <param name="row">Desired row</param>
        /// <param name="col">Desired col</param>
        /// <param name="layer"></param>
        /// <param name="setPosition">If flag is set to true, the position will be set</param>
        public virtual void SetTile(GridTile gridTile, int row, int col, int layer=0, bool setPosition = true)
        {
            if (!IsCoordinateValid(row, col) || !IsLayerValid(layer) || !CanTileBePlaced(gridTile, row, col,layer)) return;
            gridTile.ParentBoard = this;
            gridTile.SetRowCol(row, col, layer);
            SetTileArrayForTile(gridTile, row, col, layer);
            if(setPosition)
            {
                PositionTileAtCoordinate(gridTile, row, col, layer);
            }
        }

        private void UpdateDirectionalTiles()
        {
            for(int r=0;r<RowCount;++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    GridTile tile = GetTile(r, c, 0);
                    if(tile == null) continue;
                    if (tile.TryGetComponent(out DirectionalVisualSelector dvs))
                    {
                        dvs.SetVisual();
                    }
                }
            }
        }
        /// <summary>
        /// Just sets the position of a tile without setting its coordinates
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="layer"></param>
        public void PositionTileAtCoordinate(GridTile tile, int row, int col, int layer)
        {
            tile.transform.SetParent(transform);
            tile.SetLocalPosition(GetLocalPosition(row, col));
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localScale = Vector3.one;
        }
        /// <summary>
        /// Simply fills the tile array.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="anchorRow"></param>
        /// <param name="anchorColumn"></param>
        /// <param name="anchorLayer"></param>
        private void SetTileArrayForTile(GridTile tile, int anchorRow, int anchorColumn, int anchorLayer)
        {
            Tiles[anchorLayer][anchorRow , anchorColumn] = tile;
            foreach (var localCoordinte in tile.Coordinates)
            {
                Tiles[anchorLayer][anchorRow + localCoordinte.Row, anchorColumn + localCoordinte.Column] = tile;
            }
            
        }

        private void ClearTileArrayForTile(GridTile tile,int anchorRow, int anchorCol, int anchorLayer=0)
        {
            Tiles[anchorLayer][anchorRow, anchorCol] = null;
            foreach (var localCoord in tile.Coordinates)
            {
                int row = anchorRow + localCoord.Row;
                int col = anchorCol + localCoord.Column;
                int layer = anchorLayer + localCoord.Layer;
                if (Tiles[layer][row, col] == tile)
                {
                    Tiles[layer][row, col] = null;
                }
            }
        }
        public bool CanTileBePlaced(GridTile tile, int anchorRow, int anchorCol, int anchorLayer = 0)
        {
            foreach (var localCoord in tile.Coordinates)
            {
                int row = anchorRow + localCoord.Row;
                int col = anchorCol + localCoord.Column;
                int layer = anchorLayer + localCoord.Layer;
                GridTile tileAtCoord = GetTile(row, col, layer);
                if (tileAtCoord == tile)
                {
                    //Self occupation is ok
                    continue;
                }
                if (IsTileOccupied(row, col, layer)) return false;
            }

            return true;
        }
        
        public void ClearTile(int row, int col, int layer=0)
        {
            if (!IsCoordinateValid(row, col) || IsLayerValid(layer)) return;
            GridTile tile = GetTile(row, col, layer);
            UnsetTile(tile);
        }

        /// <summary>
        /// Unsets the tile at given row, col. Doesn't despawn the tile.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public void UnsetTile(int anchorRow, int anchorCol, int anchorLayer=0)
        {
            GridTile tile = GetTile(anchorRow, anchorCol, anchorLayer);
            if (tile == null) return;
            ClearTileArrayForTile(tile, anchorRow, anchorCol, anchorLayer);
            tile.ParentBoard = null;
        }
        
        public void UnsetTiles(List<GridTile> tiles)
        {
            foreach(var tile in tiles)
            {
                UnsetTile(tile);
            }
        }
        
        public void UnsetTile(GridTile tile)
        {
            UnsetTile(tile.AnchorRow, tile.AnchorColumn, tile.AnchorLayer);
        }
        
        /// <summary>
        /// Unsets and despawns the tile
        /// </summary>
        /// <param name="tile"></param>
        public void DespawnTile(GridTile tile)
        {
            UnsetTile(tile);
            tile.Despawn(false);
        }

        /// <summary>
        /// Gets the tile at given row col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public GridTile GetTile(int row, int col, int layer)
        {
            //Check if coordinates and the layer is valid
            if (Tiles == null || !IsCoordinateValid(row, col) || !IsLayerValid(layer))
            {
                return null;
            }
            //todo: Do a safety check here maybe?
            return Tiles[layer][row, col];
        }
        
        /// <summary>
        /// Gets the tile at relative direction of given tile
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public GridTile GetTileAtDirection(Directions direction, GridTile tile)
        {
            int row = tile.AnchorRow;
            int col = tile.AnchorColumn;
            int layer = tile.AnchorLayer;
            switch (direction)
            {
                case Directions.Bottom:
                    row -= 1;
                    break;
                case Directions.Left:
                    col -= 1;
                    break;
                case Directions.Top:
                    row += 1;
                    break;
                case Directions.Right:
                    col += 1;
                    break;
            }
            return GetTile(row, col, layer);
        }
        
        public bool IsTileOccupied(int row, int col, int layer=0)
        {
            if(!IsCoordinateValid(row, col)) return false;

            if (MaskedTiles != null && MaskedTiles.Contains(new GridTileCoordinate()
                {
                    Row = row, Column = col, Layer = layer,
                }))
            {
                return true;
            }
            return GetTile(row, col, layer) != null;
        }
   
        public int GetEmptyTileCount(int layer=0)
        {
            int emptyCount = 0;
            for(int r=0;r<RowCount;++r)
            {
                for(int c=0;c<ColumnCount;++c)
                {
                    if(GetTile(r,c, layer) == null) emptyCount++;
                }
            }
            return emptyCount;
        }
        
        /// <summary>
        /// Finds the largest square window with empty tiles on the board
        /// </summary>
        /// <returns></returns>
        public int GetLargestEmptyTileWindow()
        {
            if (RowCount > 10 && ColumnCount > 10) return 10; //todo: For cpi videos, this freezes the game
            int largestSquare = 1;
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    int windowSize = FindEmptyWindowSizeAtRowCol(r, c);
                    largestSquare = Mathf.Max(largestSquare, windowSize);
                }
            }

            return largestSquare;
        }
        
        /// <summary>
        /// Finds the empty window size at given row and col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        [Button("Find window size")]
        private int FindEmptyWindowSizeAtRowCol(int row, int col)
        {
            int windowSize = 0;
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    bool occupied = IsTileOccupied(r + row, c + col);
                    if (occupied) return windowSize;
                    if (c == r && c > 0 && r > 0)
                    {
                        windowSize = r;
                        break; //Continue from row
                    }
                }
            }
            return windowSize;
        }
        /// <summary>
        /// Returns the first empty tile starting from R=0, C=0
        /// </summary>
        /// <returns>Vector2 in the form of (row, col) </returns>
        public Vector2Int GetEmptyRowCol(int layer=0)
        {
            Vector2Int emptyTileCoords = Vector2Int.one * -1; // Start as invalid
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    if (GetTile(r, c, layer) == null)
                    {
                        emptyTileCoords.x = r;
                        emptyTileCoords.y = c;
                        return emptyTileCoords;
                    }
                }
            }
            return emptyTileCoords;
        }

        /// <summary>
        /// Gets a list of empty row columns
        /// </summary>
        /// <returns></returns>
        public List<Vector2Int> GetEmptyTiles()
        {
            List<Vector2Int> emptyTiles = new List<Vector2Int>();
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    if (IsTileOccupied(r,c))
                    {
                        continue;
                    }
                    emptyTiles.Add(new Vector2Int(r,c));
                }
            }
            return emptyTiles;
        }

        public List<GridTile> Get4Neighs(int row, int col, int layer=0)
        {
            List<GridTile> neighs = new List<GridTile>();
            for (int i = -1; i < 2; ++i)
            {
                for (int j = -1; j < 2; ++j)
                {
                    if(i==j || (i != 0 && j != 0)) continue; //4 neighs condition
                    GridTile tile = GetTile(row + i, col + j, layer);
                    if(tile == null) continue;
                    neighs.Add(tile);
                }
            }

            return neighs;
        }

        public List<GridBoardPathNode> Get4NeighsPathNodes(int row, int col, int layer=0)
        {
            List<GridBoardPathNode> neighs = new List<GridBoardPathNode>();
            for (int i = -1; i < 2; ++i)
            {
                for (int j = -1; j < 2; ++j)
                {
                    if(i==j || (i != 0 && j != 0)) continue; //4 neighs condition
                    GridTileCoordinate coordinate = new GridTileCoordinate()
                    {
                        Row = row+i,
                        Column = col+j,
                        Layer = layer,
                    };
                    GridBoardPathNode pathNode = GetPathNodeAtCoordinate(coordinate);
                    if(pathNode == null) continue;
                    neighs.Add(pathNode);
                }
            }

            return neighs;
        }
        /// <summary>
        /// Returns the background object for given row and col
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public GridTileBackground GetBackground(GridTileCoordinate coord)
        {
            if (!IsCoordinateValid(coord)) return null;
            return BackgroundObjects[coord.Row, coord.Column];
        }
        #endregion

        public void ClearBoard()
        {
            for (int layer = 0; layer < Tiles.Count; ++layer)
            {
                for (int r = 0; r < RowCount; ++r)
                {
                    for (int c = 0; c < ColumnCount; ++c)
                    {
                        GridTile tile = GetTile(r, c, layer);
                        if(tile != null)
                        {
                            UnsetTile(r,c);
                            tile.Despawn(true);
                        }
                    }
                }
            }
            
        }
        
        #region Utility Methods
        /// <summary>
        /// Returns the corresponding row and col from a world position. The world position is first projected onto the board
        /// </summary>
        /// <param name="position">World position</param>
        /// <returns></returns>
        public GridTileCoordinate GetRowColFromPosition(Vector3 position)
        {
            Vector3 pointOnBoard = GetPointOnPlane(position);
            Vector3 boardNormal = GetBoardNormal();
            Vector3 diff = position - transform.position;
            return GetRowColFromPointOnBoard(pointOnBoard);
        }
        /// <summary>
        /// Returns row and col 
        /// </summary>
        /// <param name="pointOnGrid"></param>
        public GridTileCoordinate GetRowColFromPointOnBoard(Vector3 pointOnGrid)
        {
            GridTileCoordinate coord = new GridTileCoordinate();
            Vector3 localBotLeft = ForwardVector * GetDepth() * OriginOffset.y + RightVector * GetWidth() * OriginOffset.x;
            Vector3 botLeftPoint = transform.TransformPoint(localBotLeft);
            Vector3 diff = pointOnGrid - botLeftPoint;
            Vector3 localDiff = transform.InverseTransformDirection(diff);
            float horDist = Kuantech.Utils.Helpers.DotProjection(localDiff, RightVector);
            float depthDist = Kuantech.Utils.Helpers.DotProjection(localDiff, ForwardVector);
            coord.Column = Mathf.FloorToInt(horDist / CellWidth);
            coord.Row = Mathf.FloorToInt(depthDist / CellHeight);
            return coord;
        }
        
        public void ApplyOperationToTiles(TileOperation operation, int layer=0)
        {
            for(int r=0;r<RowCount;++r)
            {
                for(int c=0;c<ColumnCount;++c)
                {
                    if(GetTile(r,c,layer) == null) continue;
                    operation.Invoke(GetTile(r,c,layer));
                }
            }
        }

        /// <summary>
        /// Returns the flattened coordinates from row and col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public int GetFlattenCoordinates(int row, int col)
        {
            return row*ColumnCount + col;
        }

        /// <summary>
        /// Returns the row and col indices from a flattened coordinate
        /// </summary>
        /// <param name="flat"></param>
        /// <returns>(row, col)</returns>
        public Vector2 GetRowColFromFlattened(int flat)
        {
            int rowCount = Mathf.FloorToInt(flat / ColumnCount);
            int colCount = flat - rowCount * ColumnCount;
            return new Vector2Int(rowCount, colCount);
        }
        public Vector3 GetBoardNormal()
        {
            return Vector3.Cross(RightVector, ForwardVector);
        }
        
        public Vector3 GetPointOnPlane(Vector3 globalPosition)
        {
            
            Vector3 diff = globalPosition - transform.position;
            Vector3 projectedOntoNormal = Helpers.ProjectVector(diff, GetBoardNormal());
            if (Mathf.Approximately(projectedOntoNormal.sqrMagnitude, 0f))
            {
                return globalPosition;
            }
            Ray ray = new Ray(globalPosition, GetBoardNormal());
            return GetPointOnPlane(ray);
        }
        
        public Vector3 GetPointOnPlane(Ray ray)
        {
            //todo: Fix this to comply with rotated boards
            float rayDistance;
            Vector3 groundPlaneNormal = Vector3.Cross(transform.rotation * ForwardVector, transform.rotation * RightVector).normalized;
            Plane groundPlane = new Plane(groundPlaneNormal, transform.position);
            if (groundPlane.Raycast(ray, out rayDistance))
            {
                return ray.GetPoint(rayDistance);
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Returns the local position from row and col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public Vector3 GetLocalPosition(float row, float col, Vector2 originOffset)
        {
            Vector3 horizontalPosition = RightVector * (col * CellWidth + CellWidth * ColumnCount * originOffset.x + CellWidth * 0.5f);
            Vector3 depthPosition = ForwardVector * (row * CellHeight + CellHeight * RowCount * originOffset.y + CellHeight * 0.5f);
            return horizontalPosition + depthPosition;
        }
        
        public Vector3 GetLocalPosition(float row, float col)
        {
            return GetLocalPosition(row, col, OriginOffset);
        }
        
        public Vector3 GetGlobalPosition(float row, float col)
        {
            Vector3 localPos = GetLocalPosition(row, col);
            return transform.TransformPoint(localPos);
        }
        public float GetWidth()
        {
            return (ColumnCount) * CellWidth;
        }

        public float GetDepth()
        {
            return (RowCount) * CellHeight;
        }

        #endregion

        #region PathFinding

        public GridBoardPathNode GetPathNodeAtCoordinate(GridTileCoordinate coordinate)
        {
            if (GridBoardPathTree == null) return null;
            return GridBoardPathTree.GetPathNodeAtCoordinate(coordinate);
        }
        #endregion
        
        #if UNITY_EDITOR
        [Button("Select Tile")]
        public void SelectTile(int row, int col, int layer=0)
        {
            GridTile tile = GetTile(row, col, layer);
            if(tile == null)
            {
                Debug.LogError("Null tile!");
                return;
            }
            Selection.activeGameObject = tile.gameObject;
        }
        #endif
        
        #region Effects
        public void PlayEffect(Effect effect, int row, int col)
        {
            //Boom
        }
        #endregion

        #region Tile Highlighting

        private HashSet<GridTileBackground> _highlightedTiles;
        
        /// <summary>
        /// Clears the highlighted tile backgrounds
        /// </summary>
        public virtual void ClearHighlightedTiles()
        {
            if (_highlightedTiles == null) return;
            foreach (var bg in _highlightedTiles)
            {
                bg.ClearHighlight();
            }
        }
        
        /// <summary>
        /// Highlights the selected tiles
        /// </summary>
        /// <param name="coordinates"></param>
        public void HighlightTiles(List<GridTileCoordinate> coordinates)
        {
            ClearHighlightedTiles();
            if(_highlightedTiles == null) _highlightedTiles = new HashSet<GridTileBackground>();
            foreach (var coord in coordinates)
            {
                HighlightTile(coord);
            }
        }

        public virtual void HighlightTile(GridTileCoordinate coord)
        {
            GridTileBackground bgObj = GetBackground(coord);
            if (bgObj == null) return;
            bgObj.Highlight();
            _highlightedTiles.Add(bgObj);
        }
        #endregion
    }
}