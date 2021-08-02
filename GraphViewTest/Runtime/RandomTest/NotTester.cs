using UnityEngine;

namespace GraphViewTest.Runtime
{
    [GraphEditorNode(
        PortOrientation.Vertical,
        PortDirection.Input,
        PortCapacity.Single)]
    public class NotTester : ScriptableObject
    {
        [SerializeField] private GraphEditorNodeData graphData;

        [GraphEditorPort(PortOrientation.Vertical, PortDirection.Output)]
        [SerializeField] private SuperTester tester1;

        [SerializeField] private AnimationCurve animCInput;
    }
}