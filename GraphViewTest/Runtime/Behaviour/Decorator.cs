using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour
{
    public abstract class Decorator : ConcreteNode
    {
        [GraphEditorPort(PortOrientation.Vertical)]
        [SerializeField] private ConcreteNode child;
    }
}