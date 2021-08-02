using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [InitializeOnLoad]
    public static class GraphEditorDatabase
    {
        public static readonly IReadOnlyDictionary<Type, NodeData> NODE_DATA;
        public static readonly IReadOnlyDictionary<Type, GraphData> SUPPORTED_TYPES;

        static GraphEditorDatabase()
        {
            NODE_DATA = GenerateNodeDatabase();
            SUPPORTED_TYPES = GenerateSupportedTypesDatabase();
        }

        private static Dictionary<Type, NodeData> GenerateNodeDatabase()
        {
            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.GetCustomAttribute<GraphEditorNodeAttribute>(true) != null
                    && typeof(ScriptableObject).IsAssignableFrom(t)
                    && !t.IsAbstract
                    && !t.IsGenericType);

            var inlineFieldDataList = new List<InlineFieldData>();
            var portDataList = new List<PortData>();

            var nodeData = new Dictionary<Type, NodeData>();

            foreach (var type in types)
            {
                inlineFieldDataList.Clear();
                portDataList.Clear();

                var so = ScriptableObject.CreateInstance(type);
                so.hideFlags = HideFlags.HideAndDontSave;

                var serO = new SerializedObject(so);

                var it = serO.GetIterator();
                it.Next(true);
                it.NextVisible(false); // skip m_Script

                EditTimeData? editTimeData = null;
                while (it.NextVisible(false))
                {
                    if (!type.TryGetField(it.name, out var field))
                    {
                        Debug.Log($"Could not locate field {it.name} in {type.FullName}.");
                        continue;
                    }

                    var fieldType = field.FieldType;
                    if (fieldType == typeof(GraphEditorNodeData))
                    {
                        editTimeData = new EditTimeData(it.name);
                        continue;
                    }

                    var portAttribute = field.GetCustomAttribute<GraphEditorPortAttribute>();
                    if (portAttribute == null)
                    {
                        inlineFieldDataList.Add(new InlineFieldData(it.name));
                        continue;
                    }

                    var (portType, capacity) = DeterminePortType(it, fieldType);

                    if (portType == null)
                    {
                        continue;
                    }

                    var portData = new PortData(it.name,
                        it.displayName,
                        portType,
                        portAttribute.Orientation.Convert(),
                        portAttribute.Direction.Convert(),
                        capacity.Convert());

                    portDataList.Add(portData);
                }

                if (editTimeData.HasValue)
                {
                    var nodeAttribute = type.GetCustomAttribute<GraphEditorNodeAttribute>(true);
                    var selfPort = new PortData("",
                        "",
                        type,
                        nodeAttribute.SelfReferencePortOrientation.Convert(),
                        nodeAttribute.SelfReferencePortDirection.Convert(),
                        nodeAttribute.SelfReferencePortCapacity.Convert());

                    var typeData = new NodeData(editTimeData.Value,
                        selfPort,
                        inlineFieldDataList.ToArray(),
                        portDataList.ToArray());

                    nodeData.Add(type, typeData);
                }
                else
                {
                    Debug.LogError($"{type.FullName} will not be exposed to graph editor" +
                                   $" because it does not contain a serialized GraphEditorNodeData field.");
                }

                serO.Dispose();
                Object.DestroyImmediate(so);
            }

            return nodeData;
        }

        private static Dictionary<Type, GraphData> GenerateSupportedTypesDatabase()
        {
            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.GetCustomAttribute<GraphEditableAttribute>(true) != null
                    && typeof(ScriptableObject).IsAssignableFrom(t)
                    && !t.IsAbstract
                    && !t.IsGenericType);

            var allNodeTypes = NODE_DATA.Keys.ToArray();

            var output = new Dictionary<Type, GraphData>();
            var nodeCollectionList = new List<NodeCollectionData>();
            var supportedTypesList = new List<Type>();

            foreach (var type in types)
            {
                nodeCollectionList.Clear();

                var so = ScriptableObject.CreateInstance(type);
                so.hideFlags = HideFlags.HideAndDontSave;

                var serO = new SerializedObject(so);

                var it = serO.GetIterator();
                it.Next(true);
                it.NextVisible(false); // skip m_Script

                EditTimeData? editTimeData = null;
                while (it.NextVisible(false))
                {
                    if (!type.TryGetField(it.name, out var field))
                    {
                        Debug.Log($"Could not locate field {it.name} in {type.FullName}.");
                        continue;
                    }

                    var fieldType = field.FieldType;
                    if (fieldType == typeof(GraphEditorGraphData))
                    {
                        editTimeData = new EditTimeData(it.name);
                        continue;
                    }

                    var nodeCollectionAttribute = field.GetCustomAttribute<GraphEditorNodeCollectionAttribute>();
                    if (nodeCollectionAttribute == null)
                    {
                        continue;
                    }

                    if (!it.isArray)
                    {
                        Debug.LogError($"{it.name} in {type.FullName} is not a valid collection.");
                        continue;
                    }

                    it.arraySize++;
                    var arrayPropertyElementType  = it.GetArrayElementAtIndex(0).propertyType;
                    it.arraySize--;

                    if (arrayPropertyElementType  != SerializedPropertyType.ObjectReference)
                    {
                        Debug.LogError($"{it.name} in {type.FullName} does not have an appropriate element type.");
                        continue;
                    }

                    Type arrayElementType = null;

                    if (fieldType.IsArray) arrayElementType = fieldType.GetElementType();
                    else if (fieldType.IsGenericType) arrayElementType = fieldType.GenericTypeArguments[0];

                    if (arrayElementType == null)
                    {
                        Debug.LogError($"Could not parse the element type for {it.name} in {type.FullName}.");
                        continue;
                    }

                    if (!typeof(ScriptableObject).IsAssignableFrom(arrayElementType))
                    {
                        Debug.LogError($"Collection element type {arrayElementType.FullName} of node collection {it.name}" +
                                       $" in {type.FullName} must derive from ScriptableObject.");
                        continue;
                    }
                    
                    supportedTypesList.Clear();
                    foreach (var requestedType in nodeCollectionAttribute.RequestedTypes)
                    {
                        if (!arrayElementType.IsAssignableFrom(requestedType))
                        {
                            Debug.LogError($"{requestedType.FullName} cannot be assigned to " +
                                           $"element type {arrayElementType.FullName} of node collection" +
                                           $"{it.name} in {type.FullName}.");
                            continue;
                        }
                        
                        supportedTypesList.AddRange(allNodeTypes.Where(requestedType.IsAssignableFrom));
                    }
                    
                    nodeCollectionList.Add(new NodeCollectionData(it.name, supportedTypesList.ToArray()));
                }

                serO.Dispose();
                Object.DestroyImmediate(so);

                if (editTimeData.HasValue)
                {
                    output.Add(type, new GraphData(editTimeData.Value, nodeCollectionList.ToArray()));
                }
                else
                {
                    Debug.LogError($"{type.FullName} will not be editable as a graph because " +
                                   $"it does not contain a serializable GraphEditorGraphData field.");
                }
            }

            return output;
        }

        private static (Type portType, PortCapacity capacity) DeterminePortType(SerializedProperty property, Type fieldType)
        {
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                property.arraySize++;
                if (property.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ObjectReference)
                {
                    property.arraySize--;

                    if (fieldType.IsArray)
                    {
                        return (fieldType.GetElementType(), PortCapacity.Multi);
                    }

                    if (fieldType.IsGenericType)
                    {
                        return (fieldType.GenericTypeArguments[0], PortCapacity.Multi);
                    }

                    Debug.LogError($"Unrecognized array type in field {property.name}.");
                    return (null, PortCapacity.Multi);
                }

                Debug.LogError($"GraphEditorAttribute used on an invalid array field {property.name}.\n" +
                               $"Only 1D arrays of Object references are supported.");
                return (null, PortCapacity.Multi);
            }

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                return (fieldType, PortCapacity.Single);
            }

            Debug.LogError($"GraphEditorPortAttribute used on an invalid field {property.name}.\n" +
                           $"Only Object references and their 1D arrays are supported.");
            return (null, PortCapacity.Single);
        }

        private static bool TryGetField(this Type t, string name, out FieldInfo outField)
        {
            for (var current = t; current != null; current = current.BaseType)
            {
                var field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    outField = field;
                    return true;
                }
            }

            outField = null;
            return false;
        }

        private static Orientation Convert(this PortOrientation orientation)
        {
            switch (orientation)
            {
                case PortOrientation.Horizontal: return Orientation.Horizontal;
                case PortOrientation.Vertical: return Orientation.Vertical;
                default: throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null);
            }
        }

        private static Direction Convert(this PortDirection direction)
        {
            switch (direction)
            {
                case PortDirection.Input: return Direction.Input;
                case PortDirection.Output: return Direction.Output;
                default: throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private static Port.Capacity Convert(this PortCapacity capacity)
        {
            switch (capacity)
            {
                case PortCapacity.Single: return Port.Capacity.Single;
                case PortCapacity.Multi: return Port.Capacity.Multi;
                default: throw new ArgumentOutOfRangeException(nameof(capacity), capacity, null);
            }
        }
    }
}