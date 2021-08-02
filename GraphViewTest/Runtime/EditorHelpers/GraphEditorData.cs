using System;

namespace UnityEngine
{
    [Serializable]
    public struct GraphEditorNodeData
    {
#if UNITY_EDITOR
        public Vector2 position;
#endif
    }

    [Serializable]
    public struct GraphEditorGraphData
    {
#if UNITY_EDITOR
        public Vector3 position;
        public Vector3 scale;
#endif
    }

    public enum PortOrientation
    {
        Horizontal,
        Vertical
    }

    public enum PortDirection
    {
        Input,
        Output
    }

    public enum PortCapacity
    {
        Single,
        Multi
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class GraphEditableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class GraphEditorNodeCollectionAttribute : Attribute
    {
        public GraphEditorNodeCollectionAttribute(params Type[] requestedTypes) =>
            RequestedTypes = requestedTypes ?? new Type[0];
        public readonly Type[] RequestedTypes;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class GraphEditorNodeAttribute : Attribute
    {
        public GraphEditorNodeAttribute(
            PortOrientation selfReferencePortOrientation = PortOrientation.Horizontal,
            PortDirection selfReferencePortDirection = PortDirection.Input,
            PortCapacity selfReferencePortCapacity = PortCapacity.Multi)
        {
            SelfReferencePortOrientation = selfReferencePortOrientation;
            SelfReferencePortDirection = selfReferencePortDirection;
            SelfReferencePortCapacity = selfReferencePortCapacity;
        }

        public readonly PortOrientation SelfReferencePortOrientation;
        public readonly PortDirection SelfReferencePortDirection;
        public readonly PortCapacity SelfReferencePortCapacity;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class GraphEditorPortAttribute : Attribute
    {
        public GraphEditorPortAttribute(
            PortOrientation orientation = PortOrientation.Horizontal,
            PortDirection direction = PortDirection.Output)
        {
            Orientation = orientation;
            Direction = direction;
        }

        public readonly PortOrientation Orientation;
        public readonly PortDirection Direction;
    }
}