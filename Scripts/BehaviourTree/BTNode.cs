using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.AI
{
    [Serializable]
    public class BTNode
    {
        public enum NodeTypes
        {
            None,
            SELECTOR,
            SEQUENCE,
            LEAF,
        }
        public enum NodeStatus
        {
            SUCCESS, RUNNING, FAILURE,
        }
        [NonSerialized] public BehaviourTree Owner;
        [NonSerialized] public NodeStatus CurrentStatus;
        [SerializeField] public List<BTNode> Children = new List<BTNode>();
        private int _currentChildIndex = 0;
        public string Name;
        protected bool RequiresStart;
        public BTNode(string n = "")
        {
            Name = n;
        }

        public void AddChild(BTNode n)
        {
            n.SetOwner(Owner);
            Children.Add(n);
        }

        public int GetChildIndex()
        {
            return _currentChildIndex;
        }

        public BTNode GetCurrentChild()
        {
            return Children[_currentChildIndex];
        }
        
        public void SetChildIndex(int childIndex)
        {
            Children[childIndex].RequiresStart = true;
            _currentChildIndex = childIndex;
            Children[_currentChildIndex].RequiresStart = true;
        }

        public virtual NodeStatus Process()
        {
            return NodeStatus.SUCCESS;}
        
        [Button("Add Node")]
        public void AddChildNode(NodeTypes nodeType, string nodeName = "", string actionName = "")
        {
            switch (nodeType)
            {
                case NodeTypes.SELECTOR:
                    AddChild(new BTSelector(nodeName));
                    break;
                case NodeTypes.SEQUENCE:
                    AddChild(new BTSequence(nodeName));
                    break;
                case NodeTypes.LEAF:
                    BTLeafAction action = (BTLeafAction) Assembly.GetExecutingAssembly().CreateInstance(actionName);
                    AddChild(new BTLeaf(nodeName, action));
                    break;
                default:
                    break;
            }
        }
        
        public void SetOwner(BehaviourTree tree)
        {
            Owner = tree;
            foreach (var child in Children)
            {
                child.SetOwner(tree);
            }
        }
    }
    
    public class BTLeaf : BTNode
    {
        // public delegate NodeStatus Tick();
        // public Tick ProcessMethod;
        private BTLeafAction _leafAction;
        
        public BTLeaf(){}

        public BTLeaf(string name, BTLeafAction leafAction = null)
        {
            Name = name;
            _leafAction = leafAction;
        }

        public void ParseNodeData(BtGraphNodeData nodeData)
        {
            Type type = Type.GetType(nodeData.ActionClassName);
            if (type == null)
            {
                Debug.LogError($"Could not find type for {nodeData.ActionClassName}");
                return;
            }

            _leafAction = (BTLeafAction) Activator.CreateInstance(type);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach (var keyValuePair in nodeData.ActionClassVariables)
            {
                FieldInfo fieldInfo = type.GetField(keyValuePair.Key, flags);
                if (fieldInfo != null)
                {
                    if (fieldInfo.FieldType == typeof(int))
                    {
                        int value;
                        if (int.TryParse(keyValuePair.Value, out value))
                        {
                            fieldInfo.SetValue(_leafAction, value);
                        }
                    }
                    else if (fieldInfo.FieldType == typeof(float))
                    {
                        float value;
                        if (float.TryParse(keyValuePair.Value, out value))
                        {
                            fieldInfo.SetValue(_leafAction, value);
                        }
                    }
                    else if (fieldInfo.FieldType == typeof(string))
                    {
                        fieldInfo.SetValue(_leafAction, keyValuePair.Value);
                    }
                    else if (fieldInfo.FieldType.IsEnum)  // Check if the property is an enum
                    {
                        object enumValue;
                        try
                        {
                            enumValue = Enum.Parse(fieldInfo.FieldType, keyValuePair.Value);
                            fieldInfo.SetValue(_leafAction, enumValue);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to parse enum value {keyValuePair.Value} for type {fieldInfo.FieldType.Name}: {e.Message}");
                        }
                    }
                    // ... Add more types as necessary ...
                }
            }
            
        }
        
        public override NodeStatus Process()
        {
            if (RequiresStart)
            {
                _leafAction.EnterNode(Owner);
                RequiresStart = false;
            }
            if (_leafAction == null) return NodeStatus.SUCCESS;
            return _leafAction.Tick(Owner);
        }
        
    }
    [Serializable]
    public class BTSequence : BTNode
    {
        public BTSequence(string name = "Sequence")
        {
            Name = name;
        }
        public override NodeStatus Process()
        {
            if (Children.Count == 0) return NodeStatus.SUCCESS;
            NodeStatus childStatus = GetCurrentChild().Process();
            if (childStatus == NodeStatus.RUNNING) return childStatus;
            if (childStatus == NodeStatus.SUCCESS)
            {
                if (GetChildIndex() >= Children.Count - 1)
                {
                    SetChildIndex(0);
                    return NodeStatus.SUCCESS;
                }
                SetChildIndex(GetChildIndex()+1);
                return NodeStatus.RUNNING;
            }

            if (childStatus == NodeStatus.FAILURE)
            {
                SetChildIndex(0);
                return NodeStatus.FAILURE;
            }

            return NodeStatus.RUNNING;
        }
    }
    [Serializable]
    public class BTSelector : BTNode
    {
        public BTSelector(string name = "Selector")
        {
            Name = name;
        }
        public override NodeStatus Process()
        {
            if (Children.Count == 0) return NodeStatus.SUCCESS;
            NodeStatus childStatus = GetCurrentChild().Process();
            if (childStatus == NodeStatus.RUNNING) return childStatus;
            if (childStatus == NodeStatus.SUCCESS)
            {
                SetChildIndex(0);
                return NodeStatus.SUCCESS;
            }

            if (childStatus == NodeStatus.FAILURE)
            {
                if (GetChildIndex() >= Children.Count - 1)
                {
                    return NodeStatus.FAILURE;
                }
                SetChildIndex(GetChildIndex()+1);
                return NodeStatus.RUNNING;
            }
            return NodeStatus.RUNNING;
        }
    }
}