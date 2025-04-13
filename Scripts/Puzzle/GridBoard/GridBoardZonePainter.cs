using System;
using System.Collections.Generic;
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

        private void OnDrawGizmos()
        {
            if (!Application.isEditor) return;
            if (Zones.IsNullOrEmpty()) return;

            foreach (var zone in Zones)
            {
                Gizmos.color = zone.ZoneColor;
                if(zone.SubZone == null || zone.SubZone.Coordinates.IsNullOrEmpty()) continue;
                foreach (var tile in zone.SubZone.Coordinates)
                {
                    Vector3 tilePos = Board.GetGlobalPosition(tile.Item1, tile.Item2);
                    
                    Gizmos.DrawWireCube(tilePos, new Vector3(0.9f * Board.CellWidth, 0.01f, 0.9f * Board.CellHeight));
                }
            }
        }
    }
}