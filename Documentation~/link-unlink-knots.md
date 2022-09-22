# Link and unlink knots

In a GameObject with a Spline Container, you can link knots from disconnected splines or on the same spline. Linked knots share a position, so when you move one linked knot, all the knots it links to also move. 

Use linked knots to create a spline with branched paths. For example, you can create a spline that represents the following:
- A road with merging lanes 
- A river with tributaries
- A diverging sets of mountain paths.

You can link knots in the Element Inspector overlay or with the **Draw Splines Tool** in the [Scene view](https://docs.unity3d.com/Manual/UsingTheSceneView.html). Use the Element Inspector overlay to unlink knots. 

> [!NOTE]
> You can link more than two knots to each other. If you link knots that are already linked to other knots, all the knots link to each other.  

## Link knots with the Draw Splines Tool

To create a linked knot with the **Draw Splines Tool**:
1. In the [Hierarchy window](https://docs.unity3d.com/Manual/Hierarchy.html) or [Scene view](https://docs.unity3d.com/Manual/UsingTheSceneView.html), select a spline.
1. In the [Tools overlay](https://docs.unity3d.com/Manual/overlays.html), set the tool context to **Spline**.
1. In the Tools overlay, select the **Draw Splines Tool**.
1. Select a knot that has two segments. 

> [!NOTE]
> If you use the **Draw Splines Tool** to create a knot on a linked knot, then that new knot is added to the existing link.

## Link knots in the Element Inspector

To link knots in the Element Inspector: 

1. In the [Hierarchy window](https://docs.unity3d.com/Manual/Hierarchy.html) or [Scene view](https://docs.unity3d.com/Manual/UsingTheSceneView.html), select a spline.
1. In the [Tools overlay](https://docs.unity3d.com/Manual/overlays.html), set the tool context to **Spline**.
1. Select at least two knots.
1. In the Element Inspector overlay, select **Link**. 

## Unlink knots

To unlink knots:

1. In the [Hierarchy window](https://docs.unity3d.com/Manual/Hierarchy.html) or [Scene view](https://docs.unity3d.com/Manual/UsingTheSceneView.html), select a spline.
1. In the [Tools overlay](https://docs.unity3d.com/Manual/overlays.html), set the tool context to **Spline**.
1. Select a linked knot. 
1. In the Element Inspect overlay, select **Unlink**. 

 
<!-- ## Additional resources
- Links to related content
- Can be doc links or other Unity-owned resources -->