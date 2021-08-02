using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    public class GraphSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        public GraphEditor editor;
        public GraphEditorView GraphView;
        public GraphData Data;
        public GraphEditorPort ConnectedPort = null;

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var list = new List<SearchTreeEntry>();
            list.Add(new SearchTreeGroupEntry(new GUIContent("Add node"), 0));

            foreach (var nodeCollection in Data.NodeCollections)
            {
                list.Add(new SearchTreeGroupEntry(new GUIContent(nodeCollection.FieldName), 1));

                foreach (var supportedType in nodeCollection.SupportedTypes)
                {
                    var tooltip = $"{supportedType.AssemblyQualifiedName} 25186C27-E332-4ADD-81A2-7CD3D49B8F37 {nodeCollection.FieldName}";

                    var searchTreeEntry = new SearchTreeEntry(new GUIContent(supportedType.Name, tooltip))
                    {
                        level = 2,
                        userData = ConnectedPort
                    };

                    switch (ConnectedPort)
                    {
                        case null:
                            list.Add(searchTreeEntry);
                            break;
                        default:
                            switch (ConnectedPort.IsOwning)
                            {
                                case true when ConnectedPort.portType.IsAssignableFrom(supportedType):
                                    list.Add(searchTreeEntry);

                                    break;
                                case false:
                                    foreach (var portData in GraphEditorDatabase.NODE_DATA[supportedType].Ports)
                                    {
                                        if (portData.PortType.IsAssignableFrom(ConnectedPort.portType))
                                        {
                                            searchTreeEntry.content.text += $" [{portData.FieldName}]";
                                            list.Add(searchTreeEntry);
                                        }
                                    }

                                    break;
                            }

                            break;
                    }
                }
            }

            return list;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var tooltip = searchTreeEntry.content.tooltip;
            var split = tooltip.Split(new[] {" 25186C27-E332-4ADD-81A2-7CD3D49B8F37 "}, System.StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2) return false;

            var type = System.Type.GetType(split[0]);
            if (type == null) return false;

            var fieldName = split[1];

            var windowRoot = editor.rootVisualElement;
            var windowMousePos = windowRoot.ChangeCoordinatesTo(windowRoot.parent, context.screenMousePosition - editor.position.position);
            var graphMousePosition = GraphView.contentContainer.WorldToLocal(windowMousePos);
            
            var newNode = GraphView.CreateNode(fieldName, type, graphMousePosition);

            if (searchTreeEntry.userData is GraphEditorPort connectedPort)
            {
                var text = searchTreeEntry.content.text;
                split = text.Split(new[] {" ["}, System.StringSplitOptions.RemoveEmptyEntries);

                var otherPortName = split.Length == 2
                    ? split[1].Substring(0, split.Length - 1) // remove ']' at the end
                    : "selfPort";

                var otherPort = newNode.Q<GraphEditorPort>(otherPortName);
                if (otherPort != null && connectedPort.IsCompatibleWith(otherPort))
                {
                    GraphView.AddElement(connectedPort.ConnectTo<GraphEditorEdge>(otherPort));
                }
            }

            return true;
        }
    }
}