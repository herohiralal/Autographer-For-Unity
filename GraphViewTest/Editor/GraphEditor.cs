using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public class GraphEditor : EditorWindow
    {
        [SerializeField] private ScriptableObject target = null;
        [SerializeField] private GraphSearchProvider searchProvider = null;
        
        private GraphEditorView _graphEditorView;

        [MenuItem("Window/GraphEditor")]
        public static void OpenWindow()
        {
            var wnd = GetWindow<GraphEditor>();
            wnd.minSize = new Vector2(800, 600);
            wnd.titleContent = new GUIContent("GraphEditor");
            wnd.Show();
            wnd.Focus();
        }

        private void Awake()
        {
            searchProvider = CreateInstance<GraphSearchProvider>();
            searchProvider.editor = this;
        }

        private void OnEnable()
        {
            TryOpen(Selection.activeObject);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Rebuild;
        }

        private void OnDestroy()
        {
            searchProvider.editor = null;
            DestroyImmediate(searchProvider);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/GraphViewTest/Editor/GraphEditor.uxml");
            visualTree.CloneTree(root);

            _graphEditorView = root.Q<GraphEditorView>();
        }

        private void OnSelectionChange()
        {
            TryOpen(Selection.activeObject);
        }

        private void TryOpen(Object current)
        {
            if (current is ScriptableObject so && GraphEditorDatabase.SUPPORTED_TYPES.ContainsKey(current.GetType()))
            {
                Undo.undoRedoPerformed -= Rebuild;
                Undo.undoRedoPerformed += Rebuild;

                target = so;
                Rebuild();
            }
        }

        private void Rebuild()
        {
            var graphData = GraphEditorDatabase.SUPPORTED_TYPES[target.GetType()];
            searchProvider.Data = graphData;
            searchProvider.GraphView = _graphEditorView;
            _graphEditorView?.PopulateView(target, graphData, searchProvider);
        }
    }
}