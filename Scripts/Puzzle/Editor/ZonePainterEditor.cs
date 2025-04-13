using System.Collections.Generic;
using Kuantech.Utils;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [CustomEditor(typeof(GridBoardZonePainter))]
    public class ZonePainterEditor : UnityEditor.Editor
    {
        private enum PaintMode {None, Paint, Erase}

        private PaintMode _currentMode = PaintMode.None;
        private int _activeZoneIndex = -1;

        public override void OnInspectorGUI()
        {
            GridBoardZonePainter zp = (GridBoardZonePainter) target;

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Zone Painter Tools", EditorStyles.boldLabel);
            
            //Show zones as list
            if(zp.Zones != null)
            {
                for (int i = 0; i < zp.Zones.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    zp.Zones[i].ZoneName = EditorGUILayout.TextField(zp.Zones[i].ZoneName);
                    zp.Zones[i].ZoneColor = EditorGUILayout.ColorField(zp.Zones[i].ZoneColor);
                    
                    if(GUILayout.Button("Select", GUILayout.Width(50)))
                    {
                        _activeZoneIndex = i;
                        _currentMode = PaintMode.None;
                    }

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        zp.Zones.RemoveAt(i);
                        if (_activeZoneIndex == i) _activeZoneIndex = -1;
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.Space();
            
            //Creating a new zone
            
            if (GUILayout.Button("Create New Zone"))
            {
                BoardZoneData zoneData = new BoardZoneData();
                zoneData.SubZone = new BoardSubZone();
                zoneData.SubZone.Coordinates = new List<(int, int)>();
                zoneData.ZoneName = "Zone " + (zp.Zones.Count + 1);
                zoneData.ZoneColor = Color.HSVToRGB(Random.value, 1f, 1f);
                zp.Zones.Add(zoneData);
                _activeZoneIndex = zp.Zones.Count - 1;
                _currentMode = PaintMode.None;
            }
            
            if (_activeZoneIndex >= 0 && _activeZoneIndex < zp.Zones.Count)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Active Zone: " + zp.Zones[_activeZoneIndex].ZoneName, 
                    EditorStyles.boldLabel);

                // Paint / Erase butonları
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_currentMode == PaintMode.Paint, "Paint", "Button"))
                {
                    _currentMode = PaintMode.Paint;
                }
                if (GUILayout.Toggle(_currentMode == PaintMode.Erase, "Erase", "Button"))
                {
                    _currentMode = PaintMode.Erase;
                }
                if (GUILayout.Toggle(_currentMode == PaintMode.None, "None", "Button"))
                {
                    _currentMode = PaintMode.None;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
         void OnSceneGUI()
        {
            GridBoardZonePainter zp = (GridBoardZonePainter)target;
            if (zp == null) return;
            if (zp.Zones == null) return;
            if (_activeZoneIndex < 0 || _activeZoneIndex >= zp.Zones.Count) return;

            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            
            if (_currentMode == PaintMode.Paint || _currentMode == PaintMode.Erase)
            {
                HandleUtility.AddDefaultControl(controlID);

                if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                {
                    // Fare tıklaması => tile bul
                    // 1) Ray oluştur
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                    // 2) Ray'i board'a çarptır (ör. y=0 plane) ya da boardRef'te bir metod
                    //    Sonra hangi tile'a denk geldiğini bul.
                    if (zp.Board != null)
                    {
                        if (CheckPointOnBoardWithRay(out Vector3 hitPoint))
                        {
                            // row,col bulundu => paint veya erase
                            var zone = zp.Zones[_activeZoneIndex];
                            var list = zone.SubZone.Coordinates;
                            if (list.IsNullOrEmpty())
                            {
                                list = new List<(int, int)>();
                            }
                            GridTileCoordinate gridTileCoordinate = zp.Board.GetRowColFromPosition(hitPoint);
                            if (zp.Board.IsCoordinateValid(gridTileCoordinate))
                            {
                                (int, int) rc = (gridTileCoordinate.Row, gridTileCoordinate.Column);
                                if (_currentMode == PaintMode.Paint)
                                {
                                    if (!list.Contains(rc))
                                    {
                                        list.Add(rc);
                                        // Debug.Log("Painted tile " + rc);
                                    }
                                }
                                else // Erase
                                {
                                    if (list.Contains(rc))
                                    {
                                        list.Remove(rc);
                                        // Debug.Log("Erased tile " + rc);
                                    }
                                }

                                zone.SubZone.Coordinates = list;
                                zp.Zones[_activeZoneIndex] = zone;
                                // Değişiklik bildir
                                UpdateSubZones();
                                EditorUtility.SetDirty(zp);
                            }
                        }
                    }
                    e.Use();
                }
            }
        }
    
         private bool CheckPointOnBoardWithRay(out Vector3 hitPoint)
         {
             GridBoardZonePainter zp = (GridBoardZonePainter)target;
             hitPoint = Vector3.zero;
             Event e = Event.current;
             Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
             int layerMask = 1 << zp.Board.gameObject.layer;
             if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 500f))
             {
                 if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                 {
                     hitPoint = hit.point;
                     return true;
                 }
             }

             return false;
         }

         private void UpdateSubZones()
         {
             GridBoardZonePainter zp = (GridBoardZonePainter)target;
             zp.Board.SubZones = new List<BoardSubZone>();
             foreach (var zone in zp.Zones)
             {
                 zp.Board.SubZones.Add(new BoardSubZone()
                 {
                     Coordinates = new List<(int, int)>(zone.SubZone.Coordinates),
                 });
             }
         }
    }
}