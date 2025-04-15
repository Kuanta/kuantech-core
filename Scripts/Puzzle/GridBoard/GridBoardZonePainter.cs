using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class BoardZoneData
    {
        public BoardSubZone SubZone;
        public Color ZoneColor;
        public string ZoneName;
    }
    
    [ExecuteInEditMode]
    public class GridBoardZonePainter : MonoBehaviour
    {
        public GridBoard Board;
        
        public List<BoardZoneData> Zones = new List<BoardZoneData>();

        public Color DarkZoneColor;
        public Color LighterZoneColor;

        private void OnDrawGizmos()
        {
            if (!Application.isEditor) return;
            if (Zones.IsNullOrEmpty()) return;

            foreach (var zone in Zones)
            {
                if(zone.SubZone == null || zone.SubZone.Coordinates.IsNullOrEmpty()) continue;
                Gizmos.color = zone.ZoneColor;
                foreach (var tile in zone.SubZone.Coordinates)
                {
                    Vector3 tilePos = Board.GetGlobalPosition(tile.y, tile.x);
                    Gizmos.DrawCube(tilePos, new Vector3(Board.CellWidth, 0.01f, Board.CellHeight));
                }
                
                Gizmos.color = zone.SubZone.BoardSubZoneColorId == 0 ? DarkZoneColor : LighterZoneColor;
                foreach (var tile in zone.SubZone.Coordinates)
                {
                    Vector3 tilePos = Board.GetGlobalPosition(tile.y, tile.x);
                    Gizmos.DrawCube(tilePos+Vector3.up*0.1f, new Vector3(0.8f * Board.CellWidth, 0.02f, 0.8f * Board.CellHeight));
                }

              
            }
        }

        public void PaintTileToZone(BoardZoneData zoneData, int row, int col)
        {
            Vector2Int rc = new Vector2Int(col, row);
            foreach (var zone in Zones)
            {
                if (zone.SubZone.Coordinates.Contains(rc))
                {
                    EraseTileFromZone(zone, row, col);
                }
            }

            if (!zoneData.SubZone.Coordinates.Contains(rc))
            {
                zoneData.SubZone.Coordinates.Add(rc);
            }
        }
        
        public void EraseTileFromZone(BoardZoneData zoneData, int row, int col)
        {
            Vector2Int rc = new Vector2Int(col, row);
            if (zoneData.SubZone.Coordinates.Contains(rc))
            {
                zoneData.SubZone.Coordinates.Remove(rc);
            }
        }
    }
}