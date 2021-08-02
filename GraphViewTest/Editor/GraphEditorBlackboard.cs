using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

public class GraphEditorBlackboard : Blackboard
{
    public new class UxmlFactory : UxmlFactory<GraphEditorBlackboard, UxmlTraits>
    {
    }
}