using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor
{
    public readonly struct EditTimeData
    {
        public EditTimeData(string fieldName)
        {
            FieldName = fieldName;
        }

        public readonly string FieldName;
    }

    public class GraphData
    {
        public GraphData(EditTimeData editTimeData, IReadOnlyList<NodeCollectionData> nodeCollections)
        {
            EditTimeData = editTimeData;
            NodeCollections = nodeCollections;
        }

        public readonly EditTimeData EditTimeData;
        public readonly IReadOnlyList<NodeCollectionData> NodeCollections;
    }

    public readonly struct NodeCollectionData
    {
        public NodeCollectionData(string fieldName, IReadOnlyList<Type> supportedTypes)
        {
            FieldName = fieldName;
            SupportedTypes = supportedTypes;
        }

        public readonly string FieldName;
        public readonly IReadOnlyList<Type> SupportedTypes;
    }

    public class NodeData
    {
        public NodeData(
            EditTimeData editTimeData,
            PortData selfPort,
            IReadOnlyList<InlineFieldData> inlineFieldPorts,
            IReadOnlyList<PortData> ports)
        {
            EditTimeData = editTimeData;
            SelfPort = selfPort;
            InlineFieldPorts = inlineFieldPorts;
            Ports = ports;
        }

        public readonly EditTimeData EditTimeData;
        public readonly PortData SelfPort;
        public readonly IReadOnlyList<InlineFieldData> InlineFieldPorts;
        public readonly IReadOnlyList<PortData> Ports;
    }

    public readonly struct InlineFieldData
    {
        public InlineFieldData(string fieldName)
        {
            FieldName = fieldName;
        }

        public readonly string FieldName;
    }

    public readonly struct PortData
    {
        public PortData(string fieldName, string label, Type portType, Orientation orientation, Direction direction, Port.Capacity capacity)
        {
            FieldName = fieldName;
            Label = label;
            PortType = portType;
            Orientation = orientation;
            Direction = direction;
            Capacity = capacity;
        }

        public readonly string FieldName;
        public readonly string Label;
        public readonly Type PortType;
        public readonly Orientation Orientation;
        public readonly Direction Direction;
        public readonly Port.Capacity Capacity;
    }
}