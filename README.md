
# Autographer for Unity!
﻿

## Have you ever wanted to create a graph tool for your systems without much effort?

### Well, you're at the right place.

This tool lets you automatically create simple graph tools with minimal input.

I created it to get some practice with UIElements and GraphView, so this tool is just a weekend project, that I do not plan on continuing. However, in its current state it's still quite feature-rich, and easily readable despite being being a bit ill-organized.

> Current feature-set includes:
* Automatic graph editor generation
* Customizing the graph using attributes
* Renaming objects for better readability in-graph.
* Clipboard functionalities - a bit buggy at times, but works 9/10 times.
* Undo/Redo functionalities - a bit buggy at times, but works 9/10 times.
* Automatic Unity serialization.
* Searcher integration to search for nodes.
* Fully validated type-safety, integrated with the search functionality.
* Correctly handles inherited types.

### How to get started?
1. Create a ScriptableObject class and add GraphEditable attribute to it.
```c#
namespace UnityEngine
{
	[CreateAssetMenu, GraphEditable]
	public class SomeGraph : ScriptableObject
	{
	}
}
```

2. Add a `GraphEditorGraphData` member variable to it. The name does not matter. This is for storing some editor-only state.
```c#
#if UNITY_EDITOR
		[SerializeField] private GraphEditorGraphData graphData =  default;
		// you can name it anything you'd like
#endif
```

3. Add a member variable which is a collection of `ScriptableObject` or a class derived from it. This variable will hold all the nodes. You can have multiple node collections.
```c#
		[GraphEditorNodeCollection(typeof(SomeOtherSubtype), typeof(YetAnotherSubtype))]
		[SerializeField] private SomeScriptableObjectSubtype[] collection;
		// List<T> works fine too.
		// feel free to add as many filters as you'd like
		// they must be assignable to the type of collection,
		// but the data is validated right after compiling,
		// with detailed feedback.
```

4. Create your node classes, which must also derive from `ScriptableObject`.
```c#
namespace UnityEngine
{
	[GraphEditorNode()]
	public class SomeScriptableObjectSubtype : ScriptableObject
	{
	}

	// [GraphEditorNode()] gets inherited, but can be overridden
	// with custom values
	public class SomeOtherSubtype: SomeScriptableObjectSubtype
	{
	}

	// [GraphEditorNode()] implicit
	public class NewType : SomeOtherSubtype
	{
	}

	// [GraphEditorNode()] implicit
	public class YetAnotherSubtype: SomeScriptableObjectSubtype
	{
	}
}
```

5. You can modify where the port to represent the node's reference should be present from `[GraphEditorNode()]`'s arguments.
```c#
[GraphEditorNode(PortOrientation.Vertical, PortDirection.Input, PortCapacity.Multi)]
// ORIENTATION can be vertical or horizontal - lets you modify the flow
// DIRECTION can be input or output - which is only visual
// direction only determines the position of the port, and nothing else
// CAPACITY can be single or multi - which will modify the behaviour of 
// the graph editor, with an example use-case being Behaviour Trees where
// you don't want multiple nodes referencing a node.
```

6. Add a `GraphEditorNodeData` member variable to it. The name does not matter. This is for storing some editor-only state such as node position.
```c#
#if UNITY_EDITOR
		[SerializeField] private GraphEditorNodeData nodeData =  default;
		// you can name it anything you'd like
#endif
```

7. Add your serialized fields to the class like you would in a normal Unity class. These will be displayed as inlined fields within the node.
```c#
namespace UnityEngine
{
	public class RanOutOfNames : YetAnotherSubtype
	{
		[SerializeField] private float health = 0.0f;
		[SerializeField] private int lives = 999;
		// all serialized fields will be turned into inlined fields
		// within the graph editor
	}
}
```

8. To add ports, use the `[GraphEditorPort]` attribute.
```c#
		[GraphEditorPortAttribute()]
		[SerializeField] private SomeScriptableObjectSubtype nextInChain = null;
		
		[GraphEditorPortAttribute()]
		[SerializeField] private RunOutOfName[] runners = new RunOutOfName[0];
		// using an array will automatically generate a port with
		// multiple reference capacity
```

9. You can modify where the port to represent the node's reference should be present from `[GraphEditorPort()]`'s arguments.
```c#
[GraphEditorPort(PortOrientation.Vertical, PortDirection.Output)]
// ORIENTATION can be vertical or horizontal - lets you modify the flow
// DIRECTION can be input or output - which is only visual
// direction only determines the position of the port, and nothing else
// CAPACITY is automatically determined by whether the port is a
// single reference or a collection
```

10. Open `Windows/GraphEditor`, create the `SomeGraph` object from `Assets/Create/SomeGraph`, and keep it as the selected object. You're good to go.

11. You can use space-bar to open search window to add nodes to a graph. Autographer internally handles type safety, so you'll only see relevant additions.

### Warning
I will not be providing any support for this tool, and it's provided as-is. The runtime assembly contains a couple examples in how to use it.

# Made by Rohan Jadav
## Thanks
