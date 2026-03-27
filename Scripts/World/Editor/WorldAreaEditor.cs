using System;
using System.Collections.Generic;
using Kuantech.World;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldArea))]
public class WorldAreaEditor : Editor
{
    private enum EditMode { Move, AddQuad }

    private WorldArea _area;
    private EditMode  _mode           = EditMode.Move;
    private int       _selectedVertex = -1;

    // Edge use-count: (minIdx, maxIdx) → how many quads reference this edge
    private readonly Dictionary<(int, int), int> _edgeUseCount = new();

    private void OnEnable()  => _area = (WorldArea)target;

    // ─── Inspector ────────────────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawModeButton("Move",   EditMode.Move,    Color.cyan);
        DrawModeButton("+ Quad", EditMode.AddQuad, Color.green);
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("FillColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("OutlineColor"));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Add Quad at Center"))
        {
            Undo.RecordObject(_area, "Add Zone Quad");
            _area.AddDefaultQuad();
            EditorUtility.SetDirty(_area);
        }

        GUI.enabled = _area.Quads.Count > 0;
        if (GUILayout.Button("Remove Last Quad"))
        {
            Undo.RecordObject(_area, "Remove Zone Quad");
            RemoveLastQuadWithVertices();
            EditorUtility.SetDirty(_area);
        }
        GUI.enabled = true;

        // Selected vertex inspector
        if (_selectedVertex >= 0 && _selectedVertex < _area.Vertices.Count)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Vertex {_selectedVertex}", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            Vector3 next = EditorGUILayout.Vector3Field("Local Position", _area.Vertices[_selectedVertex]);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_area, "Edit Vertex");
                _area.Vertices[_selectedVertex] = next;
                EditorUtility.SetDirty(_area);
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Vertices: {_area.Vertices.Count}   Quads: {_area.Quads.Count}", EditorStyles.helpBox);
    }

    private void DrawModeButton(string label, EditMode mode, Color activeColor)
    {
        GUI.backgroundColor = _mode == mode ? activeColor : Color.white;
        if (GUILayout.Button(label, GUILayout.Height(28)))
        {
            _mode = mode;
            _selectedVertex = -1;
            SceneView.RepaintAll();
        }
    }

    // ─── Scene GUI ────────────────────────────────────────────────────────────

    private void OnSceneGUI()
    {
        if (_area.Quads == null || _area.Vertices == null) return;

        DrawSceneToolbar();
        DrawQuadFills();

        switch (_mode)
        {
            case EditMode.Move:    HandleMoveMode();    break;
            case EditMode.AddQuad: HandleAddQuadMode(); break;
        }
    }

    private void DrawSceneToolbar()
    {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(8, 8, 180, 36));
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = _mode == EditMode.Move    ? Color.cyan  : Color.white;
        if (GUILayout.Button("Move",   GUILayout.Height(30))) SetMode(EditMode.Move);
        GUI.backgroundColor = _mode == EditMode.AddQuad ? Color.green : Color.white;
        if (GUILayout.Button("+ Quad", GUILayout.Height(30))) SetMode(EditMode.AddQuad);
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void SetMode(EditMode mode) { _mode = mode; _selectedVertex = -1; }

    // ─── Drawing ──────────────────────────────────────────────────────────────

    private void DrawQuadFills()
    {
        if (_area.Vertices.Count == 0) return;
        Handles.matrix = _area.transform.localToWorldMatrix;

        foreach (var q in _area.Quads)
        {
            if (!_area.ValidQuad(q)) continue;
            Vector3 v0 = _area.Vertices[q.I0];
            Vector3 v1 = _area.Vertices[q.I1];
            Vector3 v2 = _area.Vertices[q.I2];
            Vector3 v3 = _area.Vertices[q.I3];

            Handles.color = _area.FillColor;
            Handles.DrawAAConvexPolygon(v0, v1, v2);
            Handles.DrawAAConvexPolygon(v0, v2, v3);

            Handles.color = _area.OutlineColor;
            Handles.DrawLine(v0, v1);
            Handles.DrawLine(v1, v2);
            Handles.DrawLine(v2, v3);
            Handles.DrawLine(v3, v0);
        }

        Handles.matrix = Matrix4x4.identity;
    }

    // ─── Move Mode ────────────────────────────────────────────────────────────

    private void HandleMoveMode()
    {
        // One handle per unique vertex (shared = moves all quads at once)
        for (int vi = 0; vi < _area.Vertices.Count; vi++)
        {
            bool selected = vi == _selectedVertex;
            Vector3 world = _area.transform.TransformPoint(_area.Vertices[vi]);
            float   size  = HandleUtility.GetHandleSize(world) * (selected ? 0.10f : 0.07f);

            Handles.color = selected ? Color.yellow : _area.OutlineColor;
            if (Handles.Button(world, Quaternion.identity, size, size * 1.4f, Handles.SphereHandleCap))
            {
                _selectedVertex = vi;
                Repaint();
            }
        }

        if (_selectedVertex < 0 || _selectedVertex >= _area.Vertices.Count) return;

        Vector3 worldPos = _area.transform.TransformPoint(_area.Vertices[_selectedVertex]);
        Quaternion rot   = Tools.pivotRotation == PivotRotation.Global
            ? Quaternion.identity
            : _area.transform.rotation;

        EditorGUI.BeginChangeCheck();
        Vector3 newWorld = Handles.PositionHandle(worldPos, rot);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_area, "Move Vertex");
            _area.Vertices[_selectedVertex] = _area.transform.InverseTransformPoint(newWorld);
            EditorUtility.SetDirty(_area);
        }
    }

    // ─── Add Quad Mode ────────────────────────────────────────────────────────

    private void RebuildEdgeMap()
    {
        _edgeUseCount.Clear();
        foreach (var q in _area.Quads)
            for (int ei = 0; ei < 4; ei++)
                IncrementEdge(q.GetIndex(ei), q.GetIndex((ei + 1) % 4));
    }

    private void IncrementEdge(int a, int b)
    {
        var key = (Math.Min(a, b), Math.Max(a, b));
        _edgeUseCount.TryGetValue(key, out int n);
        _edgeUseCount[key] = n + 1;
    }

    private int EdgeUseCount(int a, int b)
    {
        _edgeUseCount.TryGetValue((Math.Min(a, b), Math.Max(a, b)), out int n);
        return n;
    }

    private void HandleAddQuadMode()
    {
        if (_area.Quads.Count == 0)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(8, 48, 270, 24));
            GUILayout.Label("No quads — use 'Add Quad at Center' to start.", EditorStyles.helpBox);
            GUILayout.EndArea();
            Handles.EndGUI();
            return;
        }

        RebuildEdgeMap();

        // Find closest OPEN edge (used by exactly 1 quad)
        int   bestQi = -1, bestEi = -1;
        float bestDist = float.MaxValue;

        for (int qi = 0; qi < _area.Quads.Count; qi++)
        {
            var q = _area.Quads[qi];
            for (int ei = 0; ei < 4; ei++)
            {
                int iA = q.GetIndex(ei), iB = q.GetIndex((ei + 1) % 4);
                if (EdgeUseCount(iA, iB) != 1) continue;   // skip interior edges

                Vector3 wA = _area.transform.TransformPoint(_area.Vertices[iA]);
                Vector3 wB = _area.transform.TransformPoint(_area.Vertices[iB]);
                float d = HandleUtility.DistanceToLine(wA, wB);
                if (d < bestDist) { bestDist = d; bestQi = qi; bestEi = ei; }
            }
        }

        // Draw all edges
        for (int qi = 0; qi < _area.Quads.Count; qi++)
        {
            var q = _area.Quads[qi];
            for (int ei = 0; ei < 4; ei++)
            {
                int iA = q.GetIndex(ei), iB = q.GetIndex((ei + 1) % 4);
                bool open = EdgeUseCount(iA, iB) == 1;
                bool best = qi == bestQi && ei == bestEi;
                if (best) continue; // drawn separately below

                Vector3 wA = _area.transform.TransformPoint(_area.Vertices[iA]);
                Vector3 wB = _area.transform.TransformPoint(_area.Vertices[iB]);
                Handles.color = open
                    ? new Color(0.3f, 1f, 0.3f, 0.5f)   // open: green
                    : new Color(0.5f, 0.5f, 0.5f, 0.4f); // interior: grey
                Handles.DrawLine(wA, wB, open ? 2f : 1f);
            }
        }

        // Highlight + click best open edge
        if (bestQi >= 0 && bestDist < 25f)
        {
            int iA = _area.Quads[bestQi].GetIndex(bestEi);
            int iB = _area.Quads[bestQi].GetIndex((bestEi + 1) % 4);
            Vector3 wA  = _area.transform.TransformPoint(_area.Vertices[iA]);
            Vector3 wB  = _area.transform.TransformPoint(_area.Vertices[iB]);
            Vector3 mid = (wA + wB) * 0.5f;

            Handles.color = Color.green;
            Handles.DrawLine(wA, wB, 4f);

            float size = HandleUtility.GetHandleSize(mid) * 0.12f;
            if (Handles.Button(mid, Quaternion.identity, size, size * 1.5f, Handles.SphereHandleCap))
                ExtrudeQuadFromEdge(bestQi, bestEi);
        }
    }

    private void ExtrudeQuadFromEdge(int qi, int ei)
    {
        var src = _area.Quads[qi];
        int iA = src.GetIndex(ei);
        int iB = src.GetIndex((ei + 1) % 4);

        Vector3 lA = _area.Vertices[iA];
        Vector3 lB = _area.Vertices[iB];

        // Outward = away from quad centroid
        Vector3 centroid = (
            _area.Vertices[src.I0] + _area.Vertices[src.I1] +
            _area.Vertices[src.I2] + _area.Vertices[src.I3]) * 0.25f;
        Vector3 outward = ((lA + lB) * 0.5f) - centroid;
        outward.y = 0f;
        outward   = outward.normalized;

        float extLen = Vector3.Distance(lA, lB);

        // Two NEW vertices — extruded from iA and iB
        int iC = _area.Vertices.Count;
        int iD = iC + 1;

        Undo.RecordObject(_area, "Extrude Zone Quad");
        _area.Vertices.Add(lA + outward * extLen); // iC
        _area.Vertices.Add(lB + outward * extLen); // iD

        // New quad shares the reversed original edge (iB→iA), then iC, iD
        _area.Quads.Add(new ZoneQuad { I0 = iB, I1 = iA, I2 = iC, I3 = iD });

        // Switch to move mode with first new vertex selected
        _selectedVertex = iC;
        _mode = EditMode.Move;
        EditorUtility.SetDirty(_area);
        Repaint();
    }

    // ─── Remove Last Quad ─────────────────────────────────────────────────────

    private void RemoveLastQuadWithVertices()
    {
        if (_area.Quads.Count == 0) return;

        var last = _area.Quads[^1];
        _area.Quads.RemoveAt(_area.Quads.Count - 1);

        // Collect vertices still used by remaining quads
        var used = new HashSet<int>();
        foreach (var q in _area.Quads)
        { used.Add(q.I0); used.Add(q.I1); used.Add(q.I2); used.Add(q.I3); }

        // Orphan = in removed quad but not used elsewhere
        var orphans = new List<int>();
        foreach (int idx in new[] { last.I0, last.I1, last.I2, last.I3 })
            if (!used.Contains(idx) && !orphans.Contains(idx))
                orphans.Add(idx);

        // Remove descending so indices don't shift mid-loop
        orphans.Sort((a, b) => b.CompareTo(a));
        foreach (int idx in orphans)
        {
            _area.Vertices.RemoveAt(idx);
            RemapIndicesAbove(idx);
        }

        _selectedVertex = -1;
    }

    private void RemapIndicesAbove(int removed)
    {
        foreach (var q in _area.Quads)
        {
            if (q.I0 > removed) q.I0--;
            if (q.I1 > removed) q.I1--;
            if (q.I2 > removed) q.I2--;
            if (q.I3 > removed) q.I3--;
        }
    }
}
