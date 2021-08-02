using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour
{
    public abstract class Composite : ConcreteNode
    {
        [GraphEditorPort(PortOrientation.Vertical)]
        [SerializeField] private ConcreteNode[] children;
    }
}