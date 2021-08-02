using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour
{
    public class Root : Node
    {
        [GraphEditorPort(PortOrientation.Vertical)]
        [SerializeField] private Composite root;
    }
}