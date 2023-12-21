using System;
using System.Collections;
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
        public bool DebugNode = false;
        protected bool RequiresStart = true;
        public BTNode(string n = "")
        {
            Name = n;
        }

        public void OnEnter()
        {
            RequiresStart = true;
            _currentChildIndex = 0;
        }

        public void OnExit()
        {
            RequiresStart = true;
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
            Children[childIndex].OnExit();
            _currentChildIndex = childIndex;
            Children[_currentChildIndex].OnEnter();
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
        private bool _earlySuccess = false; //Has the node success during EnterNode?
        private bool _earlyFailure = false; //Has the node failed during EnterNode?
        
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
                Type fieldType = fieldInfo.FieldType;
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
                    }else if(fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        string encodedData = keyValuePair.Value;
                        Type itemType = fieldInfo.FieldType.GetGenericArguments()[0];
                        IList list = (IList)Activator.CreateInstance(fieldInfo.FieldType);
                        string[] encodedItems = encodedData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string encodedItem in encodedItems)
                        {
                            object listItem = Convert.ChangeType(encodedItem, itemType);
                            list.Add(listItem);
                        }
                        fieldInfo.SetValue(_leafAction, list);
                    }
                    else if(fieldType == typeof(GameObject) || typeof(Component).IsAssignableFrom(fieldType))
                    {
                        Debug.LogWarning("Couldn't find a way to handle this for now");
                    }
                    else if(typeof(ScriptableObject).IsAssignableFrom(fieldType))
                    {
                        Debug.LogWarning("Couldn't find a way to handle this for now");
                    }
                    // ... Add more types as necessary ...
                }
            }
            
        }
        
        public override NodeStatus Process()
        {
            //Check if EnterNode is needed
            if (RequiresStart)
            {
                _leafAction.ParentNode = this;
                _earlyFailure = false;
                _earlySuccess = false;
                _leafAction.EnterNode(Owner);
                RequiresStart = false;
                if (_earlyFailure) return NodeStatus.FAILURE;
                if (_earlySuccess) return NodeStatus.SUCCESS;
            }

            if (_leafAction == null) return NodeStatus.SUCCESS;
            NodeStatus nodeStatus = _leafAction.Tick(Owner);
            if(nodeStatus != NodeStatus.SUCCESS)
            {
                _leafAction.ExitNode();
            }
            return nodeStatus;
        }

        public void CompleteNode()
        {
            _earlySuccess = true;
            _earlyFailure = false;
        }
        public void FailNode()
        {
            _earlyFailure = true;
            _earlySuccess = false;
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