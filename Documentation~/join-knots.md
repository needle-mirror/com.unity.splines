# Join splines

Join knots to connect the ends of two splines to each other.

When you join two knots, the new, joined knot takes the position of the active knot. The active knot is the last knot you selected. To join knots, both knots must each have only one segment and be part of different splines attached to the same Spline component.

If you join the knots of two splines that flow in different directions, the new spline takes the direction of the active knot.

To join two splines:
1. [!include[select-spline](.\\snippets\\select-spline.md)]
1. [!include[set-spline-context](.\\snippets\\set-spline-context.md)]
1. Select two knots that are from different splines and have only one segment each.
1. In the Scene view, right-click to open the context menu.
1. In the Scene view context menu, and select **Join Knots**.
