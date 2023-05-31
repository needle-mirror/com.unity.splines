# Create a tube-shaped mesh along a spline

Use the [Spline Extrude](extrude-component.md) component to extrude a mesh along a spline to create shapes such as tubes, wires, ropes, and noodles.  

To create a tube-shaped mesh along a spline:

1. Do one of the following to create a spline: 
    * Go to **GameObject** &gt; **Spline** &gt; **Draw Splines Tool**.
    * In the [Hierarchy window](xref:Hierarchy), right-click and select **Spline** &gt; **Draw Splines Tool**.
1. Draw a shape for the spline:
    * Click in the [Scene view](xref:UsingTheSceneView) to create knots for the spline's path to follow.
    * To add a curve to a knot's segment, click and drag to create a knot with tangents.
    * To exit the **Draw Splines Tool**, select a tool in the Tools overlay or press Escape.  

    > [!TIP]
    > You can quickly manipulate the position of knots and tangents. In the [Tools overlay](xref:overlays), set the tool context to **Spline** and make sure that **Draw Splines Tool** is disabled. Select and drag a knot or tangent to move it.

1. Add the **Spline Extrude** component to the GameObject that has the spline you created attached to it. 
1. In the **Spline Extrude** component, do the following to configure the extruded mesh:
    * Enter a value for the **Radius** property to set the thickness of the extruded mesh. A higher value creates a thicker mesh and lower value creates a thinner mesh. 
    * Enter a value for the **Profile Edges** property to select how many sides the radius of the mesh has. 
    * Enter a value for the **Segments Per Unit** property. If your spline has lot of curves, enter a high value to make the extruded mesh look smoother.  
    * If your spline isn't closed and you want to fill in the ends of your extruded mesh, enable **Cap Ends**. 

## Additional resources

* [Create a spline](create-a-spline.md)
* [Spline Extrude component reference](extrude-component.md)
* [Use components](xref:UsingComponents)
