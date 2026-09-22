using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Editor
{
    /// <summary>
    /// Type picker for [SerializeReference] fields. Unity's own managed-reference picker does not produce
    /// a usable row in this project, so every polymorphic field goes through this drawer instead.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        private const string NoneLabel = "None";

        private Type[] _derivedTypes;
        private string[] _typeNames;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "[SubclassSelector] needs [SerializeReference]");
                return;
            }

            EnsureTypes();

            EditorGUI.BeginProperty(position, label, property);

            // managedReferenceFullTypename is an EMPTY STRING while the reference is null, never null
            // itself, which is what made the old `?.Split(' ')[1]` throw on every unassigned field and
            // abort the whole inspector below it.
            int currentIndex = IndexOfType(GetTypeName(property.managedReferenceFullTypename));

            Rect popupRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int selectedIndex = EditorGUI.Popup(popupRect, label.text, currentIndex, _typeNames);

            if (selectedIndex != currentIndex)
            {
                // Index 0 is None, so clearing a reference is possible from the Inspector rather than
                // being a one-way trip once a type has been picked.
                property.managedReferenceValue = selectedIndex <= 0
                    ? null
                    : Activator.CreateInstance(_derivedTypes[selectedIndex - 1]);
                property.serializedObject.ApplyModifiedProperties();
            }

            DrawChildren(position, property);

            EditorGUI.EndProperty();
        }

        private void DrawChildren(Rect position, SerializedProperty property)
        {
            if (property.managedReferenceValue == null) return;

            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();

            // The bool matters: a type with no serialized fields of its own (a stateless filter, say)
            // leaves the iterator sitting on the property itself, and drawing that would recurse.
            if (!iterator.NextVisible(true)) return;

            EditorGUI.indentLevel++;
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                float height = EditorGUI.GetPropertyHeight(iterator, true);
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, height), iterator, true);

                position.y += height + EditorGUIUtility.standardVerticalSpacing;
                if (!iterator.NextVisible(false)) break;
            }

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.propertyType != SerializedPropertyType.ManagedReference) return height;
            if (property.managedReferenceValue == null) return height;

            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            if (!iterator.NextVisible(true)) return height;

            height += EditorGUIUtility.standardVerticalSpacing;
            while (!SerializedProperty.EqualContents(iterator, end))
            {
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                if (!iterator.NextVisible(false)) break;
            }

            return height;
        }

        private void EnsureTypes()
        {
            if (_derivedTypes != null) return;

            Type baseType = GetReferenceBaseType(fieldInfo.FieldType);

            // TypeCache rather than walking every loaded assembly: it is what the editor already maintains,
            // and GetTypes() on some assemblies throws ReflectionTypeLoadException outright.
            List<Type> types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(IsSelectable)
                .ToList();

            // A concrete base is a legitimate choice for itself; an abstract one is filtered out by
            // IsSelectable above.
            if (IsSelectable(baseType)) types.Insert(0, baseType);

            types.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            _derivedTypes = types.ToArray();
            _typeNames = new[] { NoneLabel }.Concat(_derivedTypes.Select(t => ObjectNames.NicifyVariableName(t.Name))).ToArray();
        }

        private static bool IsSelectable(Type type) =>
            !type.IsAbstract
            && !type.IsInterface
            && !type.IsGenericTypeDefinition
            && !typeof(UnityEngine.Object).IsAssignableFrom(type)
            && type.GetConstructor(Type.EmptyTypes) != null;

        /// <summary>
        /// A [SerializeReference] list gives the drawer the LIST's type, not the element's, so the picker
        /// would otherwise come up empty on a collection of polymorphic entries.
        /// </summary>
        private static Type GetReferenceBaseType(Type fieldType)
        {
            if (fieldType.IsArray) return fieldType.GetElementType();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                return fieldType.GetGenericArguments()[0];
            return fieldType;
        }

        /// <summary>Managed reference typenames are "AssemblyName Namespace.TypeName".</summary>
        private static string GetTypeName(string managedReferenceTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceTypename)) return null;
            int separator = managedReferenceTypename.IndexOf(' ');
            return separator >= 0 ? managedReferenceTypename.Substring(separator + 1) : managedReferenceTypename;
        }

        // Returns 0 (None) for an unassigned or no-longer-existing type, so a renamed or deleted filter
        // shows up as empty rather than as somebody else's entry.
        private int IndexOfType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            int index = Array.FindIndex(_derivedTypes, t => t.FullName == fullName);
            return index < 0 ? 0 : index + 1;
        }
    }
}
