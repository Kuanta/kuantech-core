using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Kuantech.AI.Utils;

[CustomEditor(typeof(WorldZone))]
public class PNavmeshTargetZoneViewer : Editor
{
    SerializedProperty pointsProperty;

    void OnEnable()
    {
        pointsProperty = serializedObject.FindProperty("Points");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(pointsProperty, true); // For editing points in the Inspector
        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        WorldZone polygon = (WorldZone)target;

        for (int i = 0; i < polygon.Points.Count; i++)
        {
            // Handle for each point
            Vector3 point = polygon.transform.TransformPoint(polygon.Points[i]);
            point.y = 0;
            // You can change the handle type here. For example, use DotHandleCap for a dot.
            float handleSize = HandleUtility.GetHandleSize(point) * 0.1f;
            point = Handles.FreeMoveHandle(point, handleSize, Vector3.zero, Handles.DotHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(polygon, "Move Point");
                polygon.Points[i] = polygon.transform.InverseTransformPoint(point);
            }
        }
    }
}