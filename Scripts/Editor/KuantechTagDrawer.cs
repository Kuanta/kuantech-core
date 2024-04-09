using Kuantech.Utils;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Editor
{
    [CustomPropertyDrawer(typeof(KTTagAttribute))]
    public class KuantechTagDrawer: PropertyDrawer {

        public const string TagsListPath = "Assets/Kuantech/Data/KTTags.asset";
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                KTTagAttribute tagAttribute = attribute as KTTagAttribute;
                // Try loading the TagSettings ScriptableObject
                var tagSettings = AssetDatabase.LoadAssetAtPath<KTTagsList>(TagsListPath);
                int fallbackCount = 10; // Or any reasonable number
                string[] tags = new string[fallbackCount];
                bool foundGroup = false;
                if(tagSettings == null || tagSettings.tagGroups == null) return;
                foreach (var group in tagSettings.tagGroups)
                {
                    if(group.TagGroupName != tagAttribute.TagGroup) continue;

                    if (tagSettings != null)
                    {
                        // If found, use the tags from TagSettings
                        tags = group.tags.ToArray();
                    }
                    foundGroup = true;
                    break;
                }


                if(!foundGroup)
                {
                    // If not found, fallback to displaying indices
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