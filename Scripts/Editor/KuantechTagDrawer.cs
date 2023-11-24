using Kuantech.Core.Utils;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Editor
{
    [CustomPropertyDrawer(typeof(KTTagAttribute))]
    public class KuantechTagDrawer: PropertyDrawer {

        public const string TagsListPath = "Assets/Kuantech/Settings/KTTags.asset";
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                // Try loading the TagSettings ScriptableObject
                var tagSettings = AssetDatabase.LoadAssetAtPath<KTTagsList>(TagsListPath);

                string[] tags;
                if (tagSettings != null)
                {
                    // If found, use the tags from TagSettings
                    tags = tagSettings.tags.ToArray();
                }
                else
                {
                    // If not found, fallback to displaying indices
                    int fallbackCount = 10; // Or any reasonable number
                    tags = new string[fallbackCount];
                    for (int i = 0; i < fallbackCount; i++)
                    {
                        tags[i] = $"Index {i}";
                    }
                }

                // Display the dropdown and update the property's value
                property.intValue = EditorGUI.Popup(position, label.text, property.intValue, tags);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use CustomTag with integer fields only.");
            }
        }
    }
}