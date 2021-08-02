using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    public class GraphEditorPort : Port
    {
        private GraphEditorPort(Orientation orientation,
            Direction direction,
            Capacity capacity,
            Type portType,
            bool isForward)
            : base(orientation, direction, capacity, portType)
        {
            IsForward = isForward;

            pickingMode = PickingMode.Position;
            foreach (var child in Children())
                child.pickingMode = PickingMode.Position;

            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/GraphViewTest/Editor/Port.uss");
            styleSheets.Add(sheet);

            var current = portType.BaseType;
            while (current != null)
            {
                AddToClassList($"type{current.Name}");
                current = current.BaseType;
            }
        }

        public readonly bool IsForward;
        public bool IsOwning => (IsForward && direction == Direction.Output) || (!IsForward && direction == Direction.Input);

        public static GraphEditorPort Create<TEdge>(
            Orientation orientation,
            Direction direction,
            Capacity capacity,
            Type portType,
            bool isForward,
            string fieldName,
            GraphSearchProvider searchProvider,
            string displayText = null)
            where TEdge : GraphEditorEdge, new()
        {
            var listener = new EdgeConnectorListener(searchProvider);

            var output = new GraphEditorPort(orientation, direction, capacity, portType, isForward)
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(listener)
            };

            output.AddManipulator(output.m_EdgeConnector);

            output.name = fieldName;
            if (displayText == null) output.m_ConnectorText.RemoveFromHierarchy();
            else output.portName = displayText;
            
            return output;
        }

        public NodeView Node => node as NodeView;

        public bool IsCompatibleWith(Port endPort)
        {
            if (!(endPort is GraphEditorPort otherPort))
                return false;

            if (node == otherPort.node)
                return false;

            if (IsForward != otherPort.IsForward)
                return false;

            if (direction == otherPort.direction)
                return false;

            var (owning, reference) = IsForward
                ? direction == Direction.Input
                    ? (otherPort, this)
                    : (this, otherPort)
                : direction == Direction.Input
                    ? (this, otherPort)
                    : (otherPort, this);

            if (!owning.portType.IsAssignableFrom(reference.portType))
                return false;

            return true;
        }

        private class EdgeConnectorListener : IEdgeConnectorListener
        {
            private readonly GraphViewChange _change;
            private readonly List<Edge> _edgesToCreate;
            private readonly List<GraphElement> _edgesToDelete;
            private GraphSearchProvider _searchProvider;

            public EdgeConnectorListener(GraphSearchProvider searchProvider)
            {
                _searchProvider = searchProvider;
                _edgesToCreate = new List<Edge>();
                _edgesToDelete = new List<GraphElement>();
                _change.edgesToCreate = _edgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                var draggedPort = edge.output?.edgeConnector.edgeDragHelper.draggedPort ?? edge.input?.edgeConnector.edgeDragHelper.draggedPort;
                if (!(draggedPort is GraphEditorPort geDraggedPort)) return;
                _searchProvider.ConnectedPort = geDraggedPort;
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), _searchProvider);
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                _edgesToCreate.Clear();
                _edgesToCreate.Add(edge);
                _edgesToDelete.Clear();

                if (edge.input.capacity == Capacity.Single)
                    foreach (var connection in edge.input.connections)
                        _edgesToDelete.Add(connection);

                if (edge.output.capacity == Capacity.Single)
                    foreach (var connection in edge.output.connections)
                        _edgesToDelete.Add(connection);

                if (_edgesToDelete.Count > 0)
                    graphView.DeleteElements(_edgesToDelete);

                var edgesToCreate = graphView.graphViewChanged == null
                    ? _edgesToCreate
                    : graphView.graphViewChanged(_change).edgesToCreate;

                foreach (var createdEdge in edgesToCreate)
                {
                    graphView.AddElement(createdEdge);
                    edge.input.Connect(createdEdge);
                    edge.output.Connect(createdEdge);
                }
            }
        }
    }
}