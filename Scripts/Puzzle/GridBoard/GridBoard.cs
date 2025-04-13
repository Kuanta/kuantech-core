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

    public class GridBoard : Board
    {
        public enum Directions : uint
        {
            Top = 0, Right = 1, Bottom = 2, Left = 3,
            TopRight=4,BottomRight=5,BottomLeft=6,TopLeft=7,
        }

        public struct ExistingTileData
        {
            public GridTileCoordinate Coordinate;
            public GridTile ExistingTile;
            public bool IsPrefab; //If prefab, this existing tile should be instantiated probably
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

        [Header("Tile Collection")] 
        public TileCollection TileCollection;
        
        [Header("BackgroundTile object")] 
        public GridTileBackground BackgroundGameObjectPrefab;

        [Header("Pathfinding")] 
        public GridBoardPathTree GridBoardPathTree;

        [Header("Sub Zones")] 
        public List<BoardSubZone> SubZones;
        
        public List<GridTile[,]> Tiles; //A list of list to represent layered tiles
        //public GridTile[,] Tiles;
        public GridTileBackground[,] BackgroundObjects;
        public bool[,] BackgroundMask; //A background object can't be placed if its masked here

        protected List<ExistingTileData> ExistingTiles;
        //protected HashSet<GridTile> _existingTilesSet = new HashSet<GridTile>();

        //Masked tiles are tiles that are empty but 'blocked' tiles. 
        public HashSet<GridTileCoordinate> MaskedTiles = new HashSet<GridTileCoordinate>();

        public delegate void TileOperation(GridTile tile);

        public override void CreateBoard()
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
            SpawnExistingTiles();
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
        protected virtual void FindExistingTiles()
        {
            ExistingTiles = new List<ExistingTileData>();
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

        public void AddExistingTileData(GridTileCoordinate tileCoordinate, GridTile existingTile, bool isPrefab)
        {
            if (ExistingTiles == null) ExistingTiles = new List<ExistingTileData>();
            ExistingTiles.Add(new ExistingTileData()
            {
                Coordinate = tileCoordinate,
                ExistingTile = existingTile,
                IsPrefab = isPrefab,
            });
        }
        /// <summary>
        /// Spawns the found existing tiles on the board
        /// </summary>
        protected virtual void SpawnExistingTiles()
        {
            foreach (var data in ExistingTiles)
            {
                SpawnExistingTile(data);
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
                GridTileCoordinate coord = GetRowColFromPosition(tile.GetTileAnchorPosition());
                if (!IsCoordinateValid(coord))
                {
                    continue;
                }
                coord.Layer = layer;
                tile.DestroyOnDespawn = false; //Existing tiles should be deactivated on despawn, not destroyed
                AddExistingTileData(coord, tile, false);
            }
        }

        protected virtual void SpawnExistingTile(ExistingTileData data)
        {
            GridTileCoordinate coord = data.Coordinate;
            GridTile tile = data.ExistingTile;
            if (data.IsPrefab)
            {
                tile = Instantiate(data.ExistingTile);
            }
            
            //Initialize Existing
            tile.InitializeExisting();

            if (!CanTileBePlaced(tile, coord.Row, coord.Column, coord.Layer))
            {
                tile.Despawn(false);
                return;
            }
            if (tile.MaskBackground)
            {
                BackgroundMask[coord.Row, coord.Column] = true;
            }
            tile.DestroyOnDespawn = false;
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
            SpawnExistingTiles();
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
            if (!IsCoordinateValid(row, col) || !IsLayerValid(layer) || !CanTileBePlaced(gridTile, row, col, layer))
            {
                Debug.LogError("Couldn't set tile!");
                return;
            }

            if (gridTile.ParentBoard != null)
            {
                gridTile.ParentBoard.UnsetTile(gridTile);
            }
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
                    if (tile.TryGetComponent(out ModularTileVisual modularTile))
                    {
                        modularTile.SetVisual(tile);
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
            Vector3 tileLocalPositionOffset = tile.GetTileLocalOffset();
            tile.SetLocalPosition(GetLocalPosition(row, col)-tileLocalPositionOffset); 
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
            if (tile.Coordinates.IsNullOrEmpty())
            {
                Tiles[anchorLayer][anchorRow, anchorColumn] = tile;
                return;
            }
            foreach (var localCoordinte in tile.Coordinates)
            {
                Tiles[anchorLayer][anchorRow + localCoordinte.Row, anchorColumn + localCoordinte.Column] = tile;
            }
            
        }

        private void ClearTileArrayForTile(GridTile tile,int anchorRow, int anchorCol, int anchorLayer=0)
        {
            if (tile.Coordinates.IsNullOrEmpty())
            {
                if (Tiles[anchorLayer][anchorRow, anchorCol] == tile)
                {
                    Tiles[anchorLayer][anchorRow, anchorCol] = null;
                }
                return;
            }
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
            if (tile.Coordinates.IsNullOrEmpty())
            {
                //Single tile
                return !IsTileOccupied(anchorRow, anchorCol, anchorLayer);
            }
            foreach (var localCoord in tile.Coordinates)
            {
                int row = anchorRow + localCoord.Row;
                int col = anchorCol + localCoord.Column;
                int layer = anchorLayer + localCoord.Layer;
                if (!IsCoordinateValid(row, col)) return false;
                GridTile tileAtCoord = GetTile(row, col, layer);
                if (tileAtCoord == tile)
                {
                    //Self occupation is ok
                    continue;
                }

                if (IsTileOccupied(row, col, layer))
                {
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Checks whether 
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool CanTileGroupBePlaced(GridTileGroup group)
        {
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    if (group.CanBePlacedToBoard(this, r, c)) return true; 
                }
            }

            return false;
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
        public void 
            UnsetTile(int anchorRow, int anchorCol, int anchorLayer=0)
        {
            GridTile tile = GetTile(anchorRow, anchorCol, anchorLayer);
            if (tile == null) return;
            UnsetTile(tile);
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
            // if (tile.Coordinates.IsNullOrEmpty())
            // {
            //     UnsetTile(tile.AnchorRow, tile.AnchorColumn, tile.AnchorLayer);
            // }
            // else
            // {
            //     int row = tile.AnchorRow + tile.Coordinates[0].Row;
            //     int col = tile.AnchorColumn + tile.Coordinates[0].Column;
            //     UnsetTile(row, col, tile.AnchorLayer);
            // }
            ClearTileArrayForTile(tile, tile.AnchorRow, tile.AnchorColumn,tile.AnchorLayer);
            tile.ParentBoard = null;
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
                case Directions.TopRight:
                    row += 1;
                    col += 1;
                    break;
                case Directions.BottomRight:
                    row -= 1;
                    col += 1;
                    break;
                case Directions.BottomLeft:
                    row -= 1;
                    col -= 1;
                    break;
                case Directions.TopLeft:
                    row += 1;
                    col -= 1;
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


        public override BoardTile GetTile(BoardTileCoordinate qbertTileCoordinate)
        {
            return GetTile(qbertTileCoordinate.Row, qbertTileCoordinate.Column, 0);
        }

        protected override void RegisterTile(BoardTile tile, BoardTileCoordinate coordinate)
        {
            //throw new NotImplementedException();
        }

        protected override void UnregisterTile(BoardTile tile)
        {
            //throw new NotImplementedException();
        }

        public override BoardTileCoordinate GetTileCoordinateFromWorldPosition(Vector3 worldPosition)
        {
            Vector3 pointOnBoard = GetPointOnPlane(worldPosition);
            return GetRowColFromPointOnBoard(pointOnBoard);
        }

        public override Vector3 GetLocalPosition(BoardTileCoordinate tileCoordinate)
        {
            return GetLocalPosition(tileCoordinate.Row, tileCoordinate.Column);
        }

        public override bool IsCoordinateValid(BoardTileCoordinate tileCoordinate)
        {
            return IsTileValidAndEmpty(tileCoordinate);
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
        /// <param name="originOffset"></param>
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