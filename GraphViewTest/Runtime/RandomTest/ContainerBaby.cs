using UnityEngine;

namespace GraphViewTest.Runtime
{
    [GraphEditable, CreateAssetMenu]
    public class ContainerBaby : ScriptableObject
    {
        [SerializeField] private GraphEditorGraphData graphData;
        
        [GraphEditorNodeCollection(typeof(SuperTester), typeof(NotTester), typeof(NotNotTester))]
        [SerializeField] private ScriptableObject[] stuff = null;
    }
}