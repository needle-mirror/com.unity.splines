# Link and unlink knots

You can link knots from splines attached to the same Spline component. Linked knots share a position. When you move a knot, its linked knots also move.

Use linked knots to create a spline with branched paths. For example, you can create a spline that represents the following:
- A road with merging lanes
- A river with tributaries
- A diverging sets of mountain paths.

You can link knots in the Scene view context menu or with the **Draw Splines Tool** in the [Scene view](xref:UsingTheSceneView). Use the Scene view context menu to unlink knots.

> [!NOTE]
> You can link more than two knots to each other. If you link knots that are already linked to other knots, all the knots link to each other.

## Link knots with the Draw Splines Tool

To create a linked knot with the **Draw Splines Tool**:
1. [!include[select-spline](.\\snippets\\select-spline.md)]
1. [!include[set-spline-context](.\\snippets\\set-spline-context.md)]
1. In the Tools overlay, select the **Draw Splines Tool**.
1. Select a knot that has two segments to create a knot that links to it.
    The new knot is the first knot of a new spline.

> [!NOTE]
> If you use the **Draw Splines Tool** to create a knot on a linked knot, then that new knot is added to the existing link.

## Link knots in the Scene view context menu

To link knots in the Scene view context menu:

1. [!include[select-spline](.\\snippets\\select-spline.md)]
1. [!include[set-spline-context](.\\snippets\\set-spline-context.md)]
1. Select at least two knots.
1. In the Scene view, right-click to open the context menu.
1. In the Scene view context menu, select **Link Knots**.

## Unlink knots

To unlink knots:

1. [!include[select-spline](.\\snippets\\select-spline.md)]
1. [!include[set-spline-context](.\\snippets\\set-spline-context.md)]
1. Select a linked knot.
1. In the Scene view, right-click to open the context menu.
1. In the Scene view context menu, select **Unlink Knots**.


## Additional resources

* [Knots](knots.md)
