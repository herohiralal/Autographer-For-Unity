using UnityEngine;

namespace GraphViewTest.Runtime
{
    [GraphEditorNode(
        PortOrientation.Vertical,
        PortDirection.Input,
        PortCapacity.Single)]
    public class NotNotTester : NotTester
    {
        [GraphEditorPort(PortOrientation.Vertical, PortDirection.Output)]
        [SerializeField] private SuperTester tester2;
        [GraphEditorPort(PortOrientation.Vertical, PortDirection.Output)]
        [SerializeField] private SuperTester tester3;
        [GraphEditorPort(PortOrientation.Vertical, PortDirection.Output)]
        [SerializeField] private SuperTester tester4;
        [GraphEditorPort(PortOrientation.Vertical, PortDirection.Output)]
        [SerializeField] private SuperTester tester5;

        [SerializeField] private Color colorInput;
    }
}