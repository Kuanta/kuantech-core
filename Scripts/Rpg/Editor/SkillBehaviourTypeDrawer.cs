using System;
using System.Linq;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Rpg.Editor
{
    [CustomPropertyDrawer(typeof(SkillBehaviourType))]
    public class SkillBehaviourTypeDrawer : PropertyDrawer
    {

        private string[] _typeNames;
        private string[] _displayNames;
        private Type[] _types;
        private bool initialized = false;

        private void Init()
        {
            if (initialized) return;
            _types = Helpers.GetAllDerivedTypes(AppDomain.CurrentDomain, typeof(SkillBehaviour)).ToArray();
            _typeNames = _types.Select(t => t.AssemblyQualifiedName).ToArray();
            _displayNames = _types.Select(t => t.Name).ToArray();
            initialized = true;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init();

            EditorGUI.BeginProperty(position, label, property);

            if (_typeNames == null || _typeNames.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "No SkillBehaviour types found");
                EditorGUI.EndProperty();
                return;
            }

            SerializedProperty classNameProp = property.FindPropertyRelative("className");
            string current = classNameProp.stringValue;
            int index = Array.IndexOf(_typeNames, current);
            if (index < 0) index = 0;

            int selected = EditorGUI.Popup(position, label.text, index, _displayNames);
            selected = Mathf.Clamp(selected, 0, _typeNames.Length - 1);
            classNameProp.stringValue = _typeNames[selected];
            EditorGUI.EndProperty();
        }

    }
}