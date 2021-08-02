using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphEditorView : GraphView
{
    private ScriptableObject _target;
    private GraphData _data;
    private SerializedObject _serializedObject;
    private GraphSearchProvider _searchProvider;

    public new class UxmlFactory : UxmlFactory<GraphEditorView, UxmlTraits>
    {
    }

    public GraphEditorView()
    {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer {maxScale = 2f, minScale = 0.1f});
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        
        RegisterCallback<KeyDownEvent>(KeyDownEventCallback);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/GraphViewTest/Editor/GraphEditor.uss");
        styleSheets.Add(styleSheet);

        serializeGraphElements = Serialize;
        unserializeAndPaste = Unserialize;
    }

    private static string Serialize(IEnumerable<GraphElement> elements)
    {
        if (elements == null)
            return "";

        var elementsArray = elements.OfType<NodeView>().ToArray();

        var sb = new System.Text.StringBuilder(1000);
        foreach (var nv in elementsArray)
        {
            sb
                .Append($"{nv.SerializedObjectData}\n")
                .Append("    GRAPH_OBJECT_END\n");
        }

        sb
            .Append("\n")
            .Append("    GRAPH_SECTION_END\n");

        foreach (var nv in elementsArray)
        {
            sb
                .Append($"{nv.SerializedConnectionsData}\n")
                .Append("    GRAPH_OBJECT_END\n");
        }

        return sb.ToString();
    }

    private void Unserialize(string operationName, string data)
    {
        var sectionedData = data.Split(new[] {"\n    GRAPH_SECTION_END\n"}, StringSplitOptions.RemoveEmptyEntries);
        if (sectionedData.Length != 2)
            return;

        var fieldName = _data.NodeCollections[0].FieldName;
        _serializedObject.Update();
        var property = _serializedObject.FindProperty(fieldName);

        var newCreatedObjects = new Dictionary<string, ScriptableObject>();

        Undo.SetCurrentGroupName("Paste to graph");
        var group = Undo.GetCurrentGroup();
        foreach (var jargon in sectionedData[0].Split(new[] {"\n    GRAPH_OBJECT_END\n"}, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!NodeView.DeserializeObjectData(jargon, out var guid, out var createdObject))
                continue;

            createdObject.hideFlags |= HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            Undo.RegisterCreatedObjectUndo(createdObject, $"Created {createdObject.name}");

            AssetDatabase.AddObjectToAsset(createdObject, _target);

            var index = property.arraySize++;
            property.GetArrayElementAtIndex(index).objectReferenceValue = createdObject;

            newCreatedObjects.Add(guid, createdObject);
        }

        var newNodes = newCreatedObjects.Values.Select(nco => CreateNodeView(fieldName, nco, null)).ToArray();

        foreach (var jargon in sectionedData[1].Split(new[] {"\n    GRAPH_OBJECT_END\n"}, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!NodeView.DeserializeConnectionsData(jargon, out var guid, out var output))
                continue;

            if (!newCreatedObjects.TryGetValue(guid, out var newCreatedObject))
                continue;

            var so = new SerializedObject(newCreatedObject);

            foreach (var (objectFieldName, otherObjectGuid) in output)
            {
                if (!newCreatedObjects.TryGetValue(otherObjectGuid, out var newOtherCreatedObject))
                    continue;

                var newCreatedObjectPropertyToModify = so.FindProperty(objectFieldName);
                if (newCreatedObjectPropertyToModify == null) continue;

                if (!newCreatedObjectPropertyToModify.isArray)
                    newCreatedObjectPropertyToModify.objectReferenceValue = newOtherCreatedObject;
                else
                {
                    var index = newCreatedObjectPropertyToModify.arraySize++;
                    newCreatedObjectPropertyToModify.GetArrayElementAtIndex(index).objectReferenceValue = newOtherCreatedObject;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            so.Dispose();
        }

        _serializedObject.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(group);

        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();

        for (var i = selection.Count - 1; i >= 0; i--) selection[i].Unselect(this);

        foreach (var newNode in newNodes)
        {
            newNode.LoadReferences(this);
            newNode.Select(this, true);
        }
    }

    public void PopulateView(ScriptableObject target, GraphData graphData, GraphSearchProvider searchProvider)
    {
        _target = target;
        _data = graphData;
        _searchProvider = searchProvider;

        _serializedObject = new SerializedObject(target);

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements.ToList());
        graphViewChanged += OnGraphViewChanged;

        if (ScaleProperty.vector3Value == Vector3.zero)
        {
            ScaleProperty.vector3Value = Vector3.one;
            _serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        viewTransformChanged -= OnViewTransformChanged;
        viewTransform.position = PositionProperty.vector3Value;
        viewTransform.scale = ScaleProperty.vector3Value;
        viewTransformChanged += OnViewTransformChanged;

        SynchronizeCollectionAndAsset();

        foreach (var nodeCollection in _data.NodeCollections)
        {
            var fieldName = nodeCollection.FieldName;
            var nodeCollectionProperty = _serializedObject.FindProperty(fieldName);
            var count = nodeCollectionProperty.arraySize;
            for (var i = 0; i < count; i++)
            {
                var current = nodeCollectionProperty.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableObject;
                CreateNodeView(fieldName, current, null);
            }
        }

        nodes.ForEach(n => ((NodeView) n).LoadReferences(this));
    }

    public override List<Port> GetCompatiblePorts(Port startPortx, NodeAdapter nodeAdapter)
    {
        var output = new List<Port>();

        if (!(startPortx is GraphEditorPort geStartPort))
            return output;

        output.AddRange(ports.ToList().Where(geStartPort.IsCompatibleWith));

        return output;
    }

    private void OnViewTransformChanged(GraphView graphview)
    {
        _serializedObject.Update();
        PositionProperty.vector3Value = viewTransform.position;
        ScaleProperty.vector3Value = viewTransform.scale;
        _serializedObject.ApplyModifiedProperties();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (change.elementsToRemove != null)
        {
            foreach (var elementToRemove in change.elementsToRemove)
            {
                switch (elementToRemove)
                {
                    case NodeView nodeViewElement:
                        DeleteNode(nodeViewElement);
                        break;
                    case GraphEditorEdge edgeElement:
                        var (owningPort, referencePort) = edgeElement.Ports;
                        NodeView.BreakReference(owningPort.Node, referencePort.Node, owningPort);
                        break;
                }
            }
        }

        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                if (!(edge is GraphEditorEdge geEdge))
                {
                    Debug.LogError($"Unrecognized edge in graph editor for {_target.name}.");
                    continue;
                }

                var (owningPort, referencePort) = geEdge.Ports;
                NodeView.ResolveReference(owningPort.Node, referencePort.Node, owningPort);
            }
        }

        return change;
    }

    private NodeView CreateNodeView(string fieldName, ScriptableObject nodeTarget, Vector2? position)
    {
        if (nodeTarget == null || !GraphEditorDatabase.NODE_DATA.TryGetValue(nodeTarget.GetType(), out var nodeData)) return null;

        var nodeView = new NodeView(fieldName, nodeTarget, nodeData, position, _searchProvider);
        AddElement(nodeView);
        return nodeView;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);

        evt.menu.AppendAction("Add node", a =>
        {
            _searchProvider.ConnectedPort = null;
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(evt.mousePosition)), _searchProvider);
        });

        evt.menu.AppendSeparator();
    }
    
    private void KeyDownEventCallback(KeyDownEvent kde)
    {
        if (kde.keyCode == KeyCode.Space)
        {
            _searchProvider.ConnectedPort = null;
            SearchWindow.Open(new SearchWindowContext(kde.originalMousePosition), _searchProvider);
        }
    }

    private SerializedProperty PositionProperty =>
        _serializedObject
            .FindProperty(_data.EditTimeData.FieldName)
            .FindPropertyRelative(nameof(GraphEditorGraphData.position));

    private SerializedProperty ScaleProperty =>
        _serializedObject
            .FindProperty(_data.EditTimeData.FieldName)
            .FindPropertyRelative(nameof(GraphEditorGraphData.scale));

    public NodeView CreateNode(string fieldName, Type t, Vector2 position)
    {
        var nodeTarget = ScriptableObject.CreateInstance(t);
        nodeTarget.name = t.Name;
        nodeTarget.hideFlags |= HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        Undo.RegisterCreatedObjectUndo(nodeTarget, $"Add {t.Name} Object");

        AssetDatabase.AddObjectToAsset(nodeTarget, _target);

        _serializedObject.Update();
        var property = _serializedObject.FindProperty(fieldName);
        var index = property.arraySize++;
        property.GetArrayElementAtIndex(index).objectReferenceValue = nodeTarget;

        _serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();

        return CreateNodeView(fieldName, nodeTarget, position);
    }

    private void DeleteNode(NodeView nodeView)
    {
        var objectToRemove = nodeView.Target;
        _serializedObject.Update();

        var property = _serializedObject.FindProperty(nodeView.FieldName);
        SerializedProperty elementProperty = null;

        var index = -1;

        var count = property.arraySize;
        for (var i = 0; i < count; i++)
        {
            var current = property.GetArrayElementAtIndex(i);
            if (current.objectReferenceValue != objectToRemove) continue;

            elementProperty = current;
            index = i;
            break;
        }

        if (elementProperty == null)
        {
            Debug.LogError($"Error deleting {objectToRemove.name}.");
            return;
        }

        elementProperty.objectReferenceValue = null;
        property.DeleteArrayElementAtIndex(index);

        _serializedObject.ApplyModifiedProperties();

        Undo.SetCurrentGroupName($"Destroy {objectToRemove.name}");
        var group = Undo.GetCurrentGroup();
        {
            AssetDatabase.RemoveObjectFromAsset(objectToRemove);
            Undo.DestroyObjectImmediate(objectToRemove);
        }
        Undo.CollapseUndoOperations(group);

        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();
    }

    private void SynchronizeCollectionAndAsset()
    {
        _serializedObject.Update();
        var assetsInFile = new HashSet<UnityEngine.Object>(AssetDatabase
            .LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_target))
            .OfType<ScriptableObject>());

        var ownedObjects = new HashSet<ScriptableObject>();

        var assetDirty = false;

        foreach (var nodeCollection in _data.NodeCollections)
        {
            var arrayProperty = _serializedObject.FindProperty(nodeCollection.FieldName);
            var arraySize = arrayProperty.arraySize;

            for (var i = 0; i < arraySize; i++)
            {
                if (!(arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableObject so)) continue;
                if (ownedObjects.Add(so)) continue;
                Debug.LogError($"{so.name} is present in multiple node collections.");
            }
        }

        foreach (var asset in assetsInFile
            .Where(asset => asset != _target && !ownedObjects.Contains(asset)))
        {
            AssetDatabase.RemoveObjectFromAsset(asset);
            assetDirty = true;
        }

        foreach (var ownedObject in ownedObjects
            .Where(objectSupposedToBeInlined => !assetsInFile.Contains(objectSupposedToBeInlined)))
        {
            AssetDatabase.AddObjectToAsset(ownedObject, _target);
            assetDirty = true;
        }

        if (assetDirty)
        {
            EditorUtility.SetDirty(_target);
            AssetDatabase.SaveAssets();
        }
    }
}