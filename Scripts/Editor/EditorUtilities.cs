using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using UnityEditor.Search;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace Kuantech.Editor
{
    public class EditorUtilities
    {
        public static VisualElement CreateFieldForType(Type type, object value, Action<object> onValueChanged = null)
        {
            // IntegerField for int
            if (type == typeof(int))
            {
                var field = new IntegerField { value = (int)value };
                if (onValueChanged != null) field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
                return field;
            }

            // FloatField for float
            if (type == typeof(float))
            {
                var field = new FloatField { value = (float)value };
                if (onValueChanged != null) field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
                return field;
            }

            // TextField for string
            if (type == typeof(string))
            {
                var field = new TextField { value = (string)value };
                if (onValueChanged != null) field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
                return field;
            }

            // EnumField for enums
            if (type.IsEnum)
            {
                var field = new EnumField { value = (Enum)value };
                field.Init((Enum)value);
                if (onValueChanged != null) field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
                return field;
            }

            // ObjectField for GameObject
            if (type == typeof(GameObject))
            {
                var field = new ObjectField { objectType = typeof(GameObject), value = (GameObject)value };
                if (onValueChanged != null) field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue as GameObject));
                return field;
            }

            // ObjectField for MonoBehaviour and ScriptableObject
            if (typeof(MonoBehaviour).IsAssignableFrom(type) || typeof(ScriptableObject).IsAssignableFrom(type))
            {
                var field = new ObjectField { objectType = type, value = (UnityEngine.Object)value };
                if (onValueChanged != null) field.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue as UnityEngine.Object));
                return field;
            }

            // Add additional cases as needed

            return null;
        }

        public static string SerializeVisualField(VisualElement fieldElement)
        {
            string encodedData = null;
            if (fieldElement is IntegerField intField)
            {
                encodedData = intField.value.ToString();
            }
            else if (fieldElement is FloatField floatField)
            {
                encodedData = floatField.value.ToString(CultureInfo.InvariantCulture);
            }
            else if (fieldElement is TextField textField)
            {
                encodedData = textField.value;
            }
            else if (fieldElement is EnumField enumField)
            {
                encodedData = enumField.value.ToString();
            }
            else if (fieldElement is ListVisualElement listVisualElement)
            {
                encodedData = listVisualElement.EncodeData();
            }else if(fieldElement is ObjectField objectField)
            {
                UnityEngine.Object unityObject = objectField.value;
                if(unityObject)
                {
                    string path = AssetDatabase.GetAssetPath(unityObject);
                    return AssetDatabase.AssetPathToGUID(path);
                }
            }
            return encodedData;
        }

        public static void LoadFieldData(VisualElement fieldToLoad, string encodedValue)
        {
            if (fieldToLoad is IntegerField intField)
            {
                int decodedValue;
                if (int.TryParse(encodedValue, out decodedValue))
                {
                    intField.value = decodedValue;
                }
            }
            else if (fieldToLoad is FloatField floatField)
            {
                float decodedValue;
                if (float.TryParse(encodedValue, out decodedValue))
                {
                    floatField.value = decodedValue;
                }
            }
            else if (fieldToLoad is TextField textField)
            {
                textField.value = encodedValue;
            }
            else if (fieldToLoad is EnumField enumField)
            {
                Type enumType = enumField.value.GetType();
                object enumValue = Enum.Parse(enumType, encodedValue);
                enumField.value = (Enum)enumValue;
            }
            else if(fieldToLoad is ListVisualElement listVisualElement)
            {
                listVisualElement.DecodeData(encodedValue);
            }
            else if (fieldToLoad is ObjectField objectField)
            {
                string path = AssetDatabase.GUIDToAssetPath(encodedValue);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, objectField.objectType);
                objectField.value = asset;
            }
        }

        public static VisualElement CreateFieldForType(Type fieldType, string fieldName = null)
        {
            if (fieldType == typeof(int))
            {
                // Add IntegerField
                return new IntegerField(fieldName);

            }
            else if (fieldType == typeof(float))
            {
                // Add FloatField
                return new FloatField(fieldName);
   
            }
            else if (fieldType == typeof(string))
            {
                return new TextField(fieldName);

            }
            else if (fieldType.IsEnum)
            {
                EnumField enumField = CreateEnumDropdownForType(fieldType);
                enumField.label = fieldName;
                return enumField;
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return new ListVisualElement(fieldName, fieldType.GetGenericArguments()[0]);
            }
            else if (fieldType == typeof(GameObject) || typeof(Component).IsAssignableFrom(fieldType))
            {
                // Add ObjectField for GameObject and Component (including MonoBehaviour)
                return new ObjectField { objectType = fieldType, label = fieldName };
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
            {
                // Add ObjectField for ScriptableObject
                return new ObjectField { objectType = fieldType, label = fieldName };
            }
            return null;
        }

        public static EnumField CreateEnumDropdownForType(Type enumType)
        {
            if (!enumType.IsEnum) throw new ArgumentException("Type provided must be an enum.");

            // Initialize with the first value (or any other default value if you have in mind)
            var firstEnumValue = Enum.GetValues(enumType).GetValue(0);
            EnumField enumField = new EnumField((Enum)firstEnumValue);

            // If you wish to be notified when the dropdown selection changes:
            enumField.RegisterValueChangedCallback(evt =>
            {
                // Handle any logic if needed. Maybe update some model data, etc.
                // Here, you don't have an instance to set, so you might handle other logic if required.
            });

            return enumField;
        }
    }

    public class ListVisualElement : VisualElement
    {
        public List<object> childFields;
        private Type _itemType;

        public ListVisualElement(string labelName, Type itemType)
        {
            _itemType = itemType;
            // Add a label for the list variable name
            var listLabel = new Label(labelName);
            Add(listLabel);

            // Add a button to add new items to the list
            var addButton = new Button(() => AddNewItemToListUI(itemType)) { text = "Add" };
            Add(addButton);
        }

        public void AddNewItemToListUI(Type itemType)
        {
            object defaultValue = itemType.IsValueType ? Activator.CreateInstance(itemType) : null;
            VisualElement newItemField = CreateFieldForType(itemType, newValue => { /* Handle changes */ });
            if(newItemField == null) return;
            var itemContainer = new VisualElement();
            itemContainer.Add(newItemField);

            // Add a remove button for each item
            var removeButton = new Button(() => RemoveItemFromListUI(itemContainer)) { text = "Remove" };
            itemContainer.Add(removeButton);

            Add(itemContainer);
        }
        public void RemoveItemFromListUI(VisualElement itemContainer)
        {
            Remove(itemContainer);
        }

        public VisualElement CreateFieldForType(Type type, Action<object> onValueChanged)
        {
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Debug.LogError("Dafaq?");
                return null;
            }
            return EditorUtilities.CreateFieldForType(type);
        }

        public string EncodeData()
        {
            List<string> encodedItems = new List<string>();

            foreach (VisualElement child in Children())
            {
                // Assuming the actual field is the first child of the item container
                if(child.Children().Count() <= 1) continue;

                //We are adding a button to remove to each field. That is why we are checking the child of the child
                string encodedItem = EditorUtilities.SerializeVisualField(child.ElementAt(0));
                if (!string.IsNullOrEmpty(encodedItem))
                {
                    encodedItems.Add(encodedItem);
                }
            }

            return string.Join(";", encodedItems);
        }

        public void DecodeData(string encodedData)
        {
            string[] encodedItems = encodedData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string encodedItem in encodedItems)
            {
                // Create a new item field
                VisualElement newItemField = CreateFieldForType(_itemType, newValue => { /* Handle changes */ });
                if(newItemField == null) return;
                var itemContainer = new VisualElement();
                itemContainer.Add(newItemField);

                // Add a remove button for each item
                var removeButton = new Button(() => RemoveItemFromListUI(itemContainer)) { text = "Remove" };
                itemContainer.Add(removeButton);

                Add(itemContainer);

                // Decode and load data into the field
                EditorUtilities.LoadFieldData(newItemField, encodedItem);
            }
        }
    }
}
