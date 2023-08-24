using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Kuantech.AI;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Node = UnityEditor.Experimental.GraphView.Node;

namespace Kuantech.Editor
{
    public class SimpleEdgeConnectorListener : IEdgeConnectorListener
    {
        GraphView _graphView;
        public SimpleEdgeConnectorListener(GraphView graphView)
        {
            _graphView = graphView;
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position) { }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            // This gets called when a connection is made

            var outputNode = (BtGraphNode)edge.output.node;
            var inputNode = (BtGraphNode)edge.input.node;

            // Ensure the nodes aren't null
            if(outputNode == null || inputNode == null)
            {
                return;
            }

            // Logic for what to do when a connection is made
            // e.g. adding child nodes or setting some kind of relationship
        }
    }
    
    public class BehaviorTreeEditor : EditorWindow
    {
        private BehaviorTreeGraphView _graphView;
        public BehaviourTreeBlueprint DataToLoad;

        [MenuItem("Tools/Behavior Tree Editor")]
        public static void OpenBehaviorTreeEditorWindow()
        {
            var window = GetWindow<BehaviorTreeEditor>();
            window.titleContent = new GUIContent("Behavior Tree Editor");
        }
        
        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            if (DataToLoad != null && _graphView != null)
            {
                _graphView.LoadGraph(DataToLoad.RootNodeData);
            }
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            // Create the Save button
            var saveButton = new Button(SaveGraph)
            {
                text = "Save"
            };

            var loadButton = new Button(LoadGraph)
            {
                text = "Load"
            };
            toolbar.Add(saveButton);
            toolbar.Add(loadButton);

            // Add the toolbar to the root visual element
            rootVisualElement.Add(toolbar);
        }

        private void ConstructGraphView()
        {
            _graphView = new BehaviorTreeGraphView
            {
                name = "Behavior Tree Graph"
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }
        
        public void SaveGraph()
        {
            if (DataToLoad == null) return;
            BtGraphNodeData rootNodeData = _graphView.GetSaveData(_graphView.RootNode);
            DataToLoad.RootNodeData = rootNodeData;
            EditorUtility.SetDirty(DataToLoad); // Mark the object as dirty so Unity knows it has changed.
            AssetDatabase.SaveAssets();
        }

        public void LoadGraph()
        {
            if (DataToLoad == null || _graphView == null) return;
            ClearGraph();
            _graphView.LoadGraph(DataToLoad.RootNodeData);
        }
                
        public void ClearGraph()
        {
            // Disconnect and remove all edges
            List<Edge> edges = _graphView.edges.ToList();
            for (int i = edges.Count - 1; i >= 0; i--)
            {
                Edge edge = edges[i];
                edge.input.Disconnect(edge);
                edge.output.Disconnect(edge);
                _graphView.RemoveElement(edge);
            }

            // Remove all nodes
            List<BtGraphNode> nodes = _graphView.nodes.ToList().Cast<BtGraphNode>().ToList();
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                _graphView.RemoveElement(nodes[i]);
            }
            
            //Add the start node again
            _graphView.CreateStartNode();
        }
    }
    
    public class BehaviorTreeGraphView : GraphView
    {
        public StartNode RootNode;
        public BehaviorTreeGraphView()
        {
            this.AddManipulator(new ContentZoomer()); 
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        
            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            
            CreateStartNode();
        }

        public void CreateStartNode()
        {
            RootNode = new StartNode(this);
            RootNode.SetPosition(new Rect(new Vector2(200, 200), Vector2.zero)); // Example position
            AddElement(RootNode);
        }
        
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList()!.Where(endPort => endPort.direction != startPort.direction &&
                                                    endPort.node != startPort.node &&
                                                    endPort.portType == startPort.portType).ToList();
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Check for the mouse position inside the graph
            Vector2 mousePosition = evt.localMousePosition;
        
            // Add a menu item to create a new custom node
            evt.menu.AppendAction("Add Node", (action) => CreateNodeAtPosition("Node", mousePosition));
            evt.menu.AppendAction("Add Leaf Node", (action) => CreateLeafAtPosition("Leaf", mousePosition));
            base.BuildContextualMenu(evt);
        }

        public BtGraphNode CreateNodeAtPosition(string name, Vector2 position)
        {
            var node = new BtGraphNode(this);
            AddElement(node);
            node.SetPosition(new Rect(position, Vector2.zero)); // This sets the position of the node.
            return node;
        }
        
        public BtGraphNode CreateLeafAtPosition(string name, Vector2 position)
        {
            var node = new LeafNode(this);
            AddElement(node);
            node.SetPosition(new Rect(position, Vector2.zero)); // This sets the position of the node.
            return node;
        }

        public void LoadGraph(BtGraphNodeData dataToLoad)
        {
            RootNode.ClearOutputPorts();
            for(int i=0;i<dataToLoad.ChildNodeData.Count;++i)
            {
                RootNode.AddOutputPort();
                BtGraphNodeData childData = dataToLoad.ChildNodeData[i];
                BtGraphNode childNode = ParseGraphNodeData(childData, RootNode, i);
                ConnectNodes(RootNode, childNode, i);
            }
        }

        private BtGraphNode ParseGraphNodeData(BtGraphNodeData data, BtGraphNode parentNode, int connectionIndex)
        {
            BtGraphNode node = null;
            if (data.NodeType == AI.BTNode.NodeTypes.LEAF)
            {
                node = new LeafNode(this);
            }
            else
            {
                node = new BtGraphNode(this);
            }
            
            AddElement(node);
            node.LoadSaveData(data);
            node.ClearOutputPorts();
            if (data.ChildNodeData != null)
            {
                int outCount = data.ChildNodeData.Count;
                for (int i = 0; i < outCount; ++i)
                {
                    var outputPort = node.AddOutputPort();
                    BtGraphNode child = ParseGraphNodeData(data.ChildNodeData[i], node, i);
                    var childInputPort = child.AddInputPort();
                    ConnectNodes(node, child, i);
                }
            }
        
            return node;
        }

        /// <summary>
        /// Connects two nodes. The connection goes from parentNode's output to childNode's input
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="childNode"></param>
        /// <param name="connectionIndex">Index of output pin of the parent node</param>
        private void ConnectNodes(BtGraphNode parentNode, BtGraphNode childNode, int connectionIndex = 0)
        {
            Port outputPort = parentNode.outputContainer[connectionIndex] as Port;
            Port inputPort = childNode.inputContainer[0] as Port;
            Edge edge = new Edge()
            {
                input = inputPort,
                output = outputPort,
            };
        
            outputPort.Connect(edge);
            inputPort.Connect(edge);
            childNode.inputContainer.Add(edge);
        }
        public BtGraphNodeData GetSaveData(BtGraphNode node)
        {
            BtGraphNodeData nodeData = node.GetSaveData();
        
            List<Edge> connectedEdges = new List<Edge>();
            
            //Children
            foreach (var port in node.outputContainer.Children())
            {
                if (port is Port outputPort)
                {
                    connectedEdges.AddRange(outputPort.connections);
                }
            }

            foreach (var edge in connectedEdges)
            {
                if(edge.input == null) continue;
                BtGraphNode btNode = edge.input.node as BtGraphNode;
                if(btNode == null || btNode == node) continue;
                
                //Get btGraphnodedata from btNode
                if (nodeData.ChildNodeData == null) nodeData.ChildNodeData = new List<BtGraphNodeData>();
                nodeData.ChildNodeData.Add(GetSaveData(btNode));
            }
            return nodeData;
        }
    }

    public class BtGraphNode : Node
    {
        public string NodeName;
        private TextField _nodeNameField;
        private Button _addOutputButton;
        private Button _removeOutputButton;
        public AI.BTNode.NodeTypes NodeType;
        private EnumField _nodeTypeField;
        private GraphView _graphView;
        public BtGraphNode(GraphView parentGraphView)
        {
            _graphView = parentGraphView;
            NodeName = "Node";
            title = NodeName;
            
       
            //Properties
            // Create and configure the TextField for the node name
            _nodeNameField = new TextField("Node Name:");
            _nodeNameField.value = NodeName;
            _nodeNameField.RegisterValueChangedCallback(evt =>
            {
                NodeName = evt.newValue;
                title = NodeName; // Update the node's title too
            });
            _nodeNameField.style.marginBottom = 5; 
            
            // Create and configure the EnumField for the node type
            _nodeTypeField = new EnumField("Node Type:", NodeType);
            _nodeTypeField.RegisterValueChangedCallback(evt =>
            {
                NodeType = (AI.BTNode.NodeTypes)evt.newValue;
            });
            _nodeTypeField.style.marginBottom = 5; 

            // Add the TextField and EnumField to the node's main container
            mainContainer.Add(_nodeNameField);
            mainContainer.Add(_nodeTypeField);
            
            //Buttons
            _addOutputButton = new Button(() => { AddOutputPort(); }) { text = "Add Output" };
            _addOutputButton.style.marginRight = 5; 
            
            // Remove Output Button
            _removeOutputButton = new Button(() => { RemoveOutputPort(); }) { text = "Remove Output" };
            titleContainer.Add(_addOutputButton);
            titleContainer.Add(_removeOutputButton);
            
            AddInputPort();
        }

        public Port AddOutputPort()
        {
            Port port = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            var connector = new EdgeConnector<Edge>(new SimpleEdgeConnectorListener(_graphView));
            port.AddManipulator(connector);
            port.portName = "Output " + (outputContainer.childCount + 1);
            outputContainer.Add(port);
            Refresh();
            return port;
        }

        public Port AddInputPort()
        {
            Port inputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Multi, typeof(bool));
            var connector = new EdgeConnector<Edge>(new SimpleEdgeConnectorListener(_graphView));
            inputPort.AddManipulator(connector);
            inputPort.portName = "Input";
            inputContainer.Add(inputPort);
            Refresh();
            return inputPort;
        }
        
        private void RemoveOutputPort()
        {
            // Ensure there's at least one output port to remove
            if (outputContainer.childCount > 0)
            {
                outputContainer.RemoveAt(outputContainer.childCount - 1);
                Refresh();
            }
        }

        protected void Refresh()
        {
            RefreshExpandedState();
            RefreshPorts();
        }

        public void ClearOutputPorts()
        {
            // Iterate through all output ports
            foreach (var visualElement in outputContainer.Children())
            {
                var outputPort = (Port) visualElement;
                // Make a copy of the list of edges as modifying a list while iterating it can cause exceptions
                List<Edge> edgesToDisconnect = new List<Edge>(outputPort.connections);

                // Disconnect and remove each edge
                foreach (Edge edge in edgesToDisconnect)
                {
                    edge.input.Disconnect(edge);
                    edge.output.Disconnect(edge);
            
                    // Remove the edge from the graph
                    this.Remove(edge);  // Assuming 'this' is an instance of GraphView or similar.
                }
            } 
            Refresh();
        }

        public virtual BtGraphNodeData GetSaveData()
        {
            Rect nodeRect = localBound;
            Vector2 position = nodeRect.position;
            Vector2 size = nodeRect.size;

            return new BtGraphNodeData
            {
                NodeName = NodeName,
                NodeType = NodeType,
                NodePosition = position,
                NodeSize = size,
            };
        }

        public virtual void LoadSaveData(BtGraphNodeData data)
        {
            NodeType = data.NodeType;
            NodeName = data.NodeName;
            
            // Update the UI elements
            _nodeNameField.value = NodeName;
            _nodeTypeField.value = NodeType;
            
            SetPosition(new Rect()
            {
                position = data.NodePosition,
                size = data.NodeSize,
            });
        }
    }
    
    public class StartNode : BtGraphNode
    {
        public StartNode(GraphView graphView) : base(graphView)
        {
            title = "Start";
            // Add any default ports, visuals, etc. specific to the start node here.

            // Disable certain capabilities:
            capabilities &= ~Capabilities.Deletable;   // Make node non-deletable
            capabilities &= ~Capabilities.Renamable;   // Make node non-renamable
        }
    }

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

                string encodedData = null;
                if (fieldElement is IntegerField intField)
                {
                    encodedData = intField.value.ToString();
                }
                else if (fieldElement is FloatField floatField)
                {
                    encodedData = floatField.value.ToString(CultureInfo.InvariantCulture);
                }else if (fieldElement is TextField textField)
                {
                    encodedData = textField.value;
                }
                else if (fieldElement is EnumField enumField)
                {
                    encodedData = enumField.value.ToString();
                }

                data.ActionClassVariables[fieldName] = encodedData;
            }
            return data;
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
                if(!data.ActionClassVariables.ContainsKey(fieldKey)) continue;
                string encodedValue = data.ActionClassVariables[fieldKey];
                    
                if (fields[fieldKey] is IntegerField intField)
                {
                    int decodedValue;
                    if (int.TryParse(encodedValue, out decodedValue))
                    {
                        intField.value = decodedValue;
                    }
                }
                else if (fields[fieldKey] is FloatField floatField)
                {
                    float decodedValue;
                    if (float.TryParse(encodedValue, out decodedValue))
                    {
                        floatField.value = decodedValue;
                    }
                }else if (fields[fieldKey] is TextField textField)
                {
                    textField.value = encodedValue;
                }
                else if (fields[fieldKey] is EnumField enumField)
                {
                    Type enumType = enumField.value.GetType();
                    object enumValue = Enum.Parse(enumType, encodedValue);
                    enumField.value = (Enum)enumValue;
                }
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
                if (fieldType == typeof(int))
                {
                    // Add IntegerField
                    IntegerField intField = new IntegerField(field.Name);
                    _parametersContainer.Add(intField);
                    fields[field.Name] = intField;
                }
                else if (fieldType == typeof(float))
                {
                    // Add FloatField
                    FloatField floatField = new FloatField(field.Name);
                    _parametersContainer.Add(floatField);
                    fields[field.Name] = floatField;
                }else if (fieldType == typeof(string))
                {
                    TextField textField = new TextField(field.Name);
                    _parametersContainer.Add(textField);
                    fields[field.Name] = textField;
                }
                else if (fieldType.IsEnum)
                {
                    EnumField enumField = CreateEnumDropdownForType(fieldType);
                    enumField.label = field.Name;
                    _parametersContainer.Add(enumField);
                    fields[field.Name] = enumField;
                }
                // ... handle other types similarly

                // You might also want to handle complex types or enums.
                // Enums, for instance, can be handled with EnumField.
            }
        }

        EnumField CreateEnumDropdownForType(Type enumType)
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
        
        private void ClearPreviousParameters()
        {
            _parametersContainer.Clear();
        }
    }
}