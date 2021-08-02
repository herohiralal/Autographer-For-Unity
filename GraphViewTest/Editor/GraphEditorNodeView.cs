using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeView : Node
{
    public readonly ScriptableObject Target;
    public readonly string FieldName;

    private readonly NodeData _data;
    private readonly SerializedObject _serializedObject;

    private readonly VisualElement _verticalInputContainer;
    private readonly VisualElement _verticalOutputContainer;
    private readonly VisualElement _titleLabel;

    public NodeView(string fieldName, ScriptableObject target, NodeData data, Vector2? overridePosition, GraphSearchProvider searchProvider)
        : base("Assets/Scripts/GraphViewTest/Editor/Node.uxml")
    {
        capabilities |= Capabilities.Renamable;

        _verticalInputContainer = this.Q<VisualElement>("input-container");
        _verticalOutputContainer = this.Q<VisualElement>("output-container");

        FieldName = fieldName;
        Target = target;
        _data = data;
        base.title = target.name;

        var current = data.SelfPort.PortType;
        do
        {
            AddToClassList($"type{current.Name}");
            current = current.BaseType;
        } while (current != null);

        _serializedObject = new SerializedObject(target);

        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out long localID))
        {
            viewDataKey = $"{guid} {localID}";
        }

        if (overridePosition.HasValue)
        {
            PositionProperty.vector2Value = overridePosition.Value;
            _serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        var position = PositionProperty.vector2Value;
        base.SetPosition(new Rect(position.x, position.y, 0, 0));

        CreateSelfPort(searchProvider);
        CreateReferencePorts(searchProvider);
        CreateInlineFieldsView();

        m_CollapseButton.RemoveFromHierarchy();

        bool inputEmpty = false, outputEmpty = false;
        if (inputContainer.childCount == 0)
        {
            inputEmpty = true;
            inputContainer.RemoveFromHierarchy();
        }

        var inlineFieldsContainer = this.Q("inline-fields-container");
        if (inlineFieldsContainer.childCount == 0)
        {
            this.Q("divider", "inlineFieldsContainerDivider").RemoveFromHierarchy();
            inlineFieldsContainer.RemoveFromHierarchy();
        }

        if (outputContainer.childCount == 0)
        {
            outputEmpty = true;
            outputContainer.RemoveFromHierarchy();
        }

        if (inputEmpty || outputEmpty)
        {
            this.Q("divider", "vertical").RemoveFromHierarchy();
        }

        if (inputEmpty && outputEmpty)
        {
            this.Q("divider", "mainContainerDivider").RemoveFromHierarchy();
            this.Q("top").RemoveFromHierarchy();
        }

        if (_verticalInputContainer.childCount == 0)
        {
            _verticalInputContainer.RemoveFromHierarchy();
            this.Q("divider", "inputContainerDivider").RemoveFromHierarchy();
        }

        if (_verticalOutputContainer.childCount == 0)
        {
            _verticalOutputContainer.RemoveFromHierarchy();
            this.Q("divider", "outputContainerDivider").RemoveFromHierarchy();
        }

        _titleLabel = this.Q("title-label");
        _titleLabel.RegisterCallback<MouseDownEvent>(OnDoubleClickTitleText);
    }

    public string SerializedObjectData
    {
        get
        {
            PositionProperty.vector2Value += new Vector2(20, 20);
            _serializedObject.ApplyModifiedPropertiesWithoutUndo();
            var json = EditorJsonUtility.ToJson(Target, true);
            PositionProperty.vector2Value -= new Vector2(20, 20);
            _serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return $"{viewDataKey}\n" +
                   "    GRAPH_OBJECT_SECTION_END\n" +
                   $"{_data.SelfPort.PortType.AssemblyQualifiedName}\n" +
                   "    GRAPH_OBJECT_SECTION_END\n" +
                   $"{json}";
        }
    }

    public static bool DeserializeObjectData(string jargon, out string guid, out ScriptableObject outputObject)
    {
        var data = jargon.Split(new[]
        {
            "\n" +
            "    GRAPH_OBJECT_SECTION_END\n"
        }, StringSplitOptions.RemoveEmptyEntries);

        if (data.Length != 3)
        {
            guid = null;
            outputObject = null;
            return false;
        }

        var type = Type.GetType(data[1]);
        if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
        {
            guid = null;
            outputObject = null;
            return false;
        }

        guid = data[0];
        outputObject = ScriptableObject.CreateInstance(type);
        EditorJsonUtility.FromJsonOverwrite(data[2], outputObject);

        if (!GraphEditorDatabase.NODE_DATA.TryGetValue(type, out var nodeData))
        {
            UnityEngine.Object.DestroyImmediate(outputObject);
            return false;
        }

        var so = new SerializedObject(outputObject);
        foreach (var port in nodeData.Ports)
        {
            var property = so.FindProperty(port.FieldName);
            if (property.isArray) property.arraySize = 0;
            else property.objectReferenceValue = null;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        return outputObject != null;
    }

    public string SerializedConnectionsData
    {
        get
        {
            var sb = new System.Text.StringBuilder(100);

            sb
                .Append($"{viewDataKey}\n")
                .Append("    GRAPH_OBJECT_SECTION_END\n");

            foreach (var portData in _data.Ports)
            {
                var associatedPort = this.Q<Port>(portData.FieldName);
                if (associatedPort.connected)
                {
                    foreach (var associatedPortConnection in associatedPort.connections)
                    {
                        var otherPort = associatedPort.direction == Direction.Input ? associatedPortConnection.output : associatedPortConnection.input;
                        sb
                            .Append($"{portData.FieldName}\n")
                            .Append("    GRAPH_CONNECTION_SUBSECTION_END\n")
                            .Append($"{otherPort.node.viewDataKey}\n")
                            .Append("    GRAPH_CONNECTION_SECTION_END\n");
                    }
                }
            }

            return sb.ToString();
        }
    }

    public static bool DeserializeConnectionsData(string jargon, out string guid, out (string fieldName, string guid)[] output)
    {
        var data = jargon.Split(new[]
        {
            "\n" +
            "    GRAPH_OBJECT_SECTION_END\n"
        }, StringSplitOptions.RemoveEmptyEntries);

        switch (data.Length)
        {
            case 1:
                guid = data[0];
                output = new (string fieldName, string guid)[0];
                return true;

            case 2:
                guid = data[0];
                var connectionsData = data[1].Split(new[]
                {
                    "\n" +
                    "    GRAPH_CONNECTION_SECTION_END\n"
                }, StringSplitOptions.RemoveEmptyEntries);

                output = new (string fieldName, string guid)[connectionsData.Length];
                for (var i = 0; i < connectionsData.Length; i++)
                {
                    var connectionData = connectionsData[i].Split(new[]
                    {
                        "\n" +
                        "    GRAPH_CONNECTION_SUBSECTION_END\n"
                    }, StringSplitOptions.RemoveEmptyEntries);

                    if (connectionData.Length == 2)
                        output[i] = (connectionData[0], connectionData[1]);
                    else return false;
                }

                return true;

            default:
                guid = null;
                output = null;
                return false;
        }
    }

    private void OnDoubleClickTitleText(MouseDownEvent evt)
    {
        if (evt.clickCount != 2) return;

        var textField = new TextField(null) {name = "object-name-editor", isDelayed = true, value = title,};
        title = "";

        var styling = textField.style;
        styling.paddingBottom = styling.paddingLeft = styling.paddingRight = styling.paddingTop = 0;
        styling.marginLeft = styling.marginRight = styling.marginBottom = styling.marginTop = 0;
        styling.alignSelf = Align.Stretch;
        styling.flexGrow = 1;

        _titleLabel.Add(textField);

        textField.RegisterValueChangedCallback(OnObjectNameChange);
    }

    private void OnObjectNameChange(ChangeEvent<string> evt)
    {
        var oldName = evt.previousValue;
        var newName = evt.newValue;

        if (oldName != newName)
        {
            Undo.RegisterCompleteObjectUndo(Target, $"Renamed {oldName}");
            Target.name = newName;
            title = newName;
        }

        var textField = this.Q<TextField>("object-name-editor");
        if (textField != null)
        {
            textField.UnregisterValueChangedCallback(OnObjectNameChange);
            textField.RemoveFromHierarchy();
        }
    }

    public void LoadReferences(GraphView g)
    {
        _serializedObject.Update();

        foreach (var portData in _data.Ports)
        {
            var property = _serializedObject.FindProperty(portData.FieldName);
            var selfPort = this.Q<Port>(portData.FieldName);

            var references = new List<ScriptableObject>();
            switch (portData.Capacity)
            {
                case Port.Capacity.Single:
                    var referenceValue = property.objectReferenceValue;
                    if (referenceValue is ScriptableObject soReferenceValue)
                        references.Add(soReferenceValue);
                    break;
                case Port.Capacity.Multi:
                    var size = property.arraySize;
                    for (var i = 0; i < size; i++)
                    {
                        var value = property.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (value is ScriptableObject soValue)
                            references.Add(soValue);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var reference in references)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference, out var guid, out long localID))
                {
                    Debug.LogError($"Could not resolve reference from {Target.name} to {reference.name}.");
                    continue;
                }

                if (!(g.GetNodeByGuid($"{guid} {localID}") is NodeView referenceNodeView))
                {
                    Debug.LogError($"Could not locate a node-view of {reference.name} for {Target.name}.\n" +
                                   $"key: {guid} {localID}");
                    continue;
                }

                var otherPort = referenceNodeView.Q<Port>("selfPort");

                var edge = selfPort.ConnectTo<GraphEditorEdge>(otherPort);
                g.AddElement(edge);
            }
        }
    }

    public override bool IsRenamable()
    {
        return true;
    }

    public static void ResolveReference(NodeView owner, NodeView reference, Port ownerPort)
    {
        owner._serializedObject.Update();
        var property = owner._serializedObject.FindProperty(ownerPort.name);
        switch (ownerPort.capacity)
        {
            case Port.Capacity.Single:
                property.objectReferenceValue = reference.Target;
                break;
            case Port.Capacity.Multi:
                var i = property.arraySize;
                property.arraySize++;
                property.GetArrayElementAtIndex(i).objectReferenceValue = reference.Target;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        owner._serializedObject.ApplyModifiedProperties();
    }

    public static void BreakReference(NodeView owner, NodeView reference, Port ownerPort)
    {
        owner._serializedObject.Update();
        var property = owner._serializedObject.FindProperty(ownerPort.name);
        switch (ownerPort.capacity)
        {
            case Port.Capacity.Single:
                property.objectReferenceValue = null;
                break;
            case Port.Capacity.Multi:
                var arraySize = property.arraySize;
                for (var i = arraySize - 1; i >= 0; i--)
                {
                    var current = property.GetArrayElementAtIndex(i);
                    if (current.objectReferenceValue != reference.Target)
                        continue;

                    current.objectReferenceValue = null;
                    property.DeleteArrayElementAtIndex(i);
                    break;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        owner._serializedObject.ApplyModifiedProperties();
    }

    private void CreateSelfPort(GraphSearchProvider searchProvider)
    {
        var selfPortData = _data.SelfPort;
        var port = GraphEditorPort.Create<GraphEditorEdge>(selfPortData.Orientation,
            selfPortData.Direction,
            selfPortData.Capacity,
            selfPortData.PortType,
            selfPortData.Direction == Direction.Input,
            "selfPort",
            searchProvider);

        DetermineContainer(selfPortData.Orientation, selfPortData.Direction).Add(port);
    }

    private void CreateReferencePorts(GraphSearchProvider searchProvider)
    {
        var portDataCollection = _data.Ports;
        foreach (var portData in portDataCollection)
        {
            var port = GraphEditorPort.Create<GraphEditorEdge>(portData.Orientation,
                portData.Direction,
                portData.Capacity,
                portData.PortType,
                portData.Direction == Direction.Output,
                portData.FieldName,
                searchProvider,
                portData.Label);

            DetermineContainer(portData.Orientation, portData.Direction).Add(port);
        }
    }

    private VisualElement DetermineContainer(Orientation orientation, Direction direction)
    {
        switch (orientation)
        {
            case Orientation.Horizontal:
                switch (direction)
                {
                    case Direction.Input: return inputContainer;
                    case Direction.Output: return outputContainer;
                }

                break;
            case Orientation.Vertical:
                switch (direction)
                {
                    case Direction.Input: return _verticalInputContainer;
                    case Direction.Output: return _verticalOutputContainer;
                }

                break;
        }

        throw new ArgumentOutOfRangeException($"Unsupported orientation {orientation} and/or direction {direction}.");
    }

    private void CreateInlineFieldsView()
    {
        if (_data.InlineFieldPorts.Count == 0) return;

        var container = this.Q("inline-fields-container");
        if (container == null) return;

        _serializedObject.Update();

        foreach (var inlineFieldPort in _data.InlineFieldPorts)
            container.Add(new PropertyField(_serializedObject.FindProperty(inlineFieldPort.FieldName)));

        container.Bind(_serializedObject);
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);

        _serializedObject.Update();
        PositionProperty.vector2Value = new Vector2(newPos.xMin, newPos.yMin);
        _serializedObject.ApplyModifiedProperties();
    }

    private SerializedProperty PositionProperty =>
        _serializedObject
            .FindProperty(_data.EditTimeData.FieldName)
            .FindPropertyRelative(nameof(GraphEditorNodeData.position));
}