using System;
using System.Linq;
using Kuantech.Core.Combat;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Rpg.Editor
{
    [CustomPropertyDrawer(typeof(StatusEffectType))]
    public class StatusEffectTypeDrawer : PropertyDrawer
    {
        
        private string[] _typeNames;
        private string[] _displayNames;
        private Type[] _types;
        private bool initialized = false;

        private void Init()
        {
            if (initialized) return;
            _types = Helpers.GetAllDerivedTypes(AppDomain.CurrentDomain, typeof(StatusEffect)).ToArray();
            _typeNames = _types.Select(t => t.AssemblyQualifiedName).ToArray();
            _displayNames = _types.Select(t => t.Name).ToArray();
            initialized = true;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init();

            SerializedProperty classNameProp = property.FindPropertyRelative("className");
            string current = classNameProp.stringValue;
            int index = Array.IndexOf(_typeNames, current);
            if (index < 0) index = 0;

            EditorGUI.BeginProperty(position, label, property);
            int selected = EditorGUI.Popup(position, label.text, index, _displayNames);
            classNameProp.stringValue = _typeNames[selected];
            EditorGUI.EndProperty();
        }
    }
}