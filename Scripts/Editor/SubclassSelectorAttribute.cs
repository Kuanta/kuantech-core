using Kuantech.Utils;

namespace Kuantech.Editor
{
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    private Type[] _derivedTypes;
    private string[] _typeNames;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.managedReferenceFieldTypename == "")
        {
            EditorGUI.LabelField(position, label.text, "Null");
            return;
        }

        if (_derivedTypes == null)
        {
            Type baseType = fieldInfo.FieldType;
            _derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType)
                .ToArray();

            _typeNames = _derivedTypes.Select(t => t.Name).ToArray();
        }

        int currentIndex = Array.FindIndex(_derivedTypes, t => t.FullName == property.managedReferenceFullTypename?.Split(' ')[1]);

        Rect popupRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        int selectedIndex = EditorGUI.Popup(popupRect, label.text, currentIndex, _typeNames);

        if (selectedIndex != currentIndex)
        {
            property.managedReferenceValue = Activator.CreateInstance(_derivedTypes[selectedIndex]);
        }

        if (property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;

            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            iterator.NextVisible(true);

            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // dropdown alanı sonrası boşluk

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                float height = EditorGUI.GetPropertyHeight(iterator, true);
                Rect fieldRect = new Rect(position.x, position.y, position.width, height);
                EditorGUI.PropertyField(fieldRect, iterator, true);

                position.y += height + EditorGUIUtility.standardVerticalSpacing;
                iterator.NextVisible(false);
            }

            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (property.managedReferenceValue != null)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            iterator.NextVisible(true);

            height += EditorGUIUtility.standardVerticalSpacing;

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                iterator.NextVisible(false);
            }
        }

        return height;
    }
}
#endif

}