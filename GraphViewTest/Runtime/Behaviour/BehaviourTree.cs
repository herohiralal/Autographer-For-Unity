using UnityEngine;

namespace GraphViewTest.Runtime.Behaviour
{
    [GraphEditable, CreateAssetMenu]
    public class BehaviourTree : ScriptableObject
    {
        [SerializeField] private GraphEditorGraphData graphData;

        [GraphEditorNodeCollection(typeof(Node))]
        [SerializeField] private Node[] nodes;
    }
}