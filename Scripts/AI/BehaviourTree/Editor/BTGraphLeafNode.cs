using System;
using System.Collections.Generic;
using System.Reflection;
using Kuantech.AI;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kuantech.Editor
{
    public class LeafNode : BtGraphNode
    {
        private VisualElement _parametersContainer;
        private Type _selectedActionType;
        private Dictionary<string, VisualElement> fields;
        private PopupField<Type> _leafActionDropdown;
        private bool _isLoadignData = false;
        public LeafNode(GraphView parentGraphView) : base(parentGraphView)
        {
            List<Type> leafActions = Kuantech.Utils.Helpers.GetAllDerivedTypes(AppDomain.CurrentDomain, typeof(BTLeafAction));
            leafActions.Insert(0, null);
            _leafActionDropdown = new PopupField<Type>(leafActions, 0);

            _leafActionDropdown.RegisterValueChangedCallback(evt =>
            {
                // Code to handle when value is changed
                // For instance, you might update the visual appearance of the node
                // or store the selected action in the underlying graph model
                if (_isLoadignData)
                {
                    //Don't clear the fields if the change is made from data loading
                    _isLoadignData = false;
                    return;
                }
                SetParametersContainer(evt.newValue);
            });

            _selectedActionType = null; //No action initially

            // Add to node's visual container
            extensionContainer.Add(_leafActionDropdown);
            _parametersContainer = new VisualElement();
            extensionContainer.Add(_parametersContainer);
            RefreshExpandedState();
            NodeType = AI.BTNode.NodeTypes.LEAF;
        }

        private void SetParametersContainer(Type actionType)
        {
            // Optionally display parameters
            _selectedActionType = actionType;
            ClearPreviousParameters();
            DisplayParametersForAction(actionType);
        }

        public override BtGraphNodeData GetSaveData()
        {
            BtGraphNodeData data = base.GetSaveData();
            data.ActionClassName = _selectedActionType.AssemblyQualifiedName;
            if (fields == null) return data;
            if (data.ActionClassVariables == null) data.ActionClassVariables = new LeafActionVariablesDict();
            foreach (var pair in fields)
            {
                VisualElement fieldElement = pair.Value;
                string fieldName = pair.Key;
                string encodedData = Kuantech.Editor.EditorUtilities.SerializeVisualField(fieldElement);
                data.ActionClassVariables[fieldName] = encodedData;
            }
            return data;
        }

        private bool IsListContainer(VisualElement element)
        {
            return element is ListVisualElement;
        }

        private string SerializeListData(List<string> listData)
        {
            return string.Join(",", listData);
        }
        public override void LoadSaveData(BtGraphNodeData data)
        {
            base.LoadSaveData(data);

            _isLoadignData = true; //Set it to true so that fields doesn't get reset by the callback of the dropdown

            //Get leaf action type
            if (data.ActionClassName == null)
            {
                Debug.LogError("Null action class name");
                return;
            }
            Type type = Type.GetType(data.ActionClassName);
            if (type == null) return;
            _leafActionDropdown.value = type;
            SetParametersContainer(type);
            foreach (var fieldKey in fields.Keys)
            {
                if (!data.ActionClassVariables.ContainsKey(fieldKey)) continue;
                string encodedValue = data.ActionClassVariables[fieldKey];
                EditorUtilities.LoadFieldData(fields[fieldKey], encodedValue);
            }
            Refresh();
        }

        private void DisplayParametersForAction(Type selectedAction)
        {
            // Get the fields
            FieldInfo[] allFields = Kuantech.Utils.Helpers.GetSerializedFields(selectedAction);
            fields = new Dictionary<string, VisualElement>();
            foreach (FieldInfo field in allFields)
            {
                Type fieldType = field.FieldType;
                VisualElement visualField = null;
                visualField = EditorUtilities.CreateFieldForType(fieldType, field.Name);
                _parametersContainer.Add(visualField);
                fields[field.Name] = visualField;
            }
        }

        private void ClearPreviousParameters()
        {
            _parametersContainer.Clear();
        }
    }
}