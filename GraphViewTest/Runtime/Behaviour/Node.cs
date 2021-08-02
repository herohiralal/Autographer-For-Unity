using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour
{
    [GraphEditorNode(
        PortOrientation.Vertical,
        PortDirection.Input,
        PortCapacity.Single)]
    public abstract class Node : ScriptableObject
    {
        [SerializeField] private GraphEditorNodeData nodeData;
    }
}