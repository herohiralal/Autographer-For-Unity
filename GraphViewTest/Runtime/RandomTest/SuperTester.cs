using UnityEngine;

namespace GraphViewTest.Runtime
{
    [GraphEditorNode(
        PortOrientation.Vertical,
        PortDirection.Input,
        PortCapacity.Single)]
    public class SuperTester : ScriptableObject
    {
        [SerializeField] private GraphEditorNodeData graphData;

        [GraphEditorPort(PortOrientation.Vertical, PortDirection.Output)]
        [SerializeField] private SuperTester[] others;

        [SerializeField] private float floatInput;
        [SerializeField] private int intInput;
        [SerializeField] private Gradient gradientInput;
    }
}