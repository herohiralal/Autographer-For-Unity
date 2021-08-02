using UnityEditor.Experimental.GraphView;

namespace UnityEditor
{
    public class GraphEditorEdge : Edge
    {
        public bool? IsForward
        {
            get
            {
                var anyPort = input ?? output;
                return anyPort is GraphEditorPort gePort ? gePort.IsForward : (bool?) null;
            }
        }

        public (GraphEditorPort owningPort, GraphEditorPort referencePort) Ports
        {
            get
            {
                var isForward = IsForward;
                if (!isForward.HasValue)
                    return (null, null);

                return isForward.Value
                    ? ((GraphEditorPort) output, (GraphEditorPort) input)
                    : ((GraphEditorPort) input, (GraphEditorPort) output);
            }
        }
    }
}