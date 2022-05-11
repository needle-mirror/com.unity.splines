# Create a spline

Splines are paths made up of control points called knots. You can place knots onto the surface of objects or [align them to the grid](https://docs.unity3d.com/Manual/GridSnapping.html). Knots can include tangents which control the curvature of path at that knot.

By default, new splines have **Auto Smooth** enabled. 

To create a spline GameObject:

> [!NOTE]
> Before you add a knot to a spline, its default location is 0, 0, 0. After you create the spline's first knot, the spline takes that knot's location. 

1. Do one of the following: 
    * Go to **GameObject** > **Spline** > **Draw Spline Tool**.
    * In the Hierarchy window, right-click and select **Spline** > **Draw Spline Tool**.
1. Click in the Scene view to create knots for the spline's path to follow. If you want to add a curve to the path, click and drag to create a knot with tangents.
   
    > [!TIP]
    > When you use the Draw Spline Tool, if you click on the surface of a GameObject, the knot snaps to that surface. Otherwise, the knot snaps to the grid.
1. To exit the **Draw Spline Tool**, select the **Draw Spline Tool** in the tools overlay or press Escape.

