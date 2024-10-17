# Create a spline

Splines are paths made up of control points called knots. Segments connect knots to other knots. You can place knots onto the surface of objects or [align them to the grid](https://docs.unity3d.com/Manual/GridSnapping.html). Knots can include tangents which control the curvature of the segment at that knot.

## Create a new spline

To create a spline:

> [!NOTE]
> Before you add a knot to a spline, the spline's default location is 0, 0, 0. After you create the spline's first knot, the spline takes that knot's location. 

1. Do one of the following: 
    * In the **Scene** view, from the **Tools** overlay, select **Create Spline**.
    * Go to **GameObject** &gt; **Spline** &gt; **Draw Splines Tool**.
    * In the [Hierarchy window](xref:Hierarchy), right-click and select **Spline** &gt; **Draw Splines Tool**.
1. Click in the [Scene view](xref:UsingTheSceneView) to create knots for the spline's path to follow. If you want to add a curve to the knot's segment, click and drag to create a knot with tangents.
   
    > [!TIP]
    > When you use the Draw Splines Tool, if you click on the surface of a GameObject, the knot snaps to that surface. Otherwise, the knot snaps to the grid.
1. To exit the **Draw Splines Tool**, select a tool in the **Tools** overlay or press Escape.

## Add to an existing spline container

To add a spline to a GameObject's existing spline container:

1. In the **Scene** view, select your spline GameObject.
1. From the **Tools** overlay, select the **Spline** context.
1. From the **Tools** overlay, select **Create Spline**.
1. Create a new spline. It's added to the existing spline container as a new spline. You can attach it to a knot in the existing spline, or leave it disconnected. 