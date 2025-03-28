# Reverse the flow of a spline

Reverse the flow direction of a spline.

Each spline has a flow direction that moves from its first knot to its last knot. Arrows on the segments and knots of a spline indicate the flow direction of the spline.

If you reverse the flow of a spline, you create a spline that mirrors the original spline. The following occurs:

* The knot indices of the spline reverse. For example, the first knot of the original spline becomes the last knot of the reversed spline.
* For knots in the [Bezier tangent mode](tangent-modes#bezier-tangent-mode), the In tangent becomes the Out tangent and the Out tangent becomes the In tangent.

To reverse the flow of a spline:

1. [!include[select-spline](.\\snippets\\select-spline.md)]
1. [!include[set-spline-context](.\\snippets\\set-spline-context.md)]
1. Select a knot on the spline.
1. In the Scene view, right-click to open the context menu.
1. In the Scene view context menu, select **Reverse Spline Flow**.


## Additional resources

* [Knots](knots.md)
* [Tangent modes](tangent-modes.md)