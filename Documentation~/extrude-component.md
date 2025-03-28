# Spline Extrude component reference

Use the Spline Extrude component to customize the geometry of the mesh you extrude on a spline.

**Note:** If you add Spline Extrude to a GameObject that already has a mesh, that mesh is replaced with a new one.

## Source Spline Container

Select a GameObject that has an attached Spline component you want to extrude a mesh on.

You can use a Spline as a source for the extruded mesh of one or more GameObjects. Edits you make to the source Spline then update those GameObjects. For example, if you want to create a street with a sidewalk, you can create a Spline for the street, and then use the street Spline as a source for a sidewalk GameObject. Any changes you make to the street layout update the sidewalk to match.

## Shape Extrude

| **Property** | | **Description** |
| --- | --- | --- |
| **Type** > **Circle**| | Create a shape with a round cross-section. |
| | **Circle Settings** > **Sides**| Increase to create a smoother surface. The minimum value is `2`. |
| **Type** > **Square** | | Create a shape with a square cross-section. |
| **Type** >  **Road** | | Create a shape with a flat cross-section and a slight lip. |
| **Type** > **Spline Profile** | | Use a different spline as a template for the current spline. |
| | **Spline Profile Settings** > **Template** |  Select the spline container you want to use as a template. |
| | **Spline Profile Settings** > **Spline Index** | If the template container has more than one spline, select the spline you want.|
| | **Spline Profile Settings** > **Side Count** | Increase to create a smoother surface. The minimum value is `2`. |
| | **Spline Profile Settings** > **Axis** | Which of the template spline axes to follow when drawing this spline. |


## Geometry

| **Property** | **Description** |
| --- | --- |
| **Auto Refresh Generation** | Allow the mesh to update at runtime if the spline changes. |
| **Frequency** | How many times a second to refresh the mesh. |
| **Radius** | Width of the extrusion, measured from the spline path. |
| **Segments Per Unit** | Increase the value to create smoother curves. |
| **Cap Ends** | Fill the ends of an open-ended mesh. |
| **Flip Normals** | Reveal the inside of the 3D shape. |

## Advanced

| **Property** | **Description** |
| --- | --- |
| **Range** | To extrude only some of the spline, define a range. `0` is the first spline point, and `100` is the last one. |
| **Percentage** | Another way to control the **Range**. |
| **Update Colliders** | If the spline has a collider, update the collider to match the 3D shape as you change the extrusion properties. |


## Mesh Filter and Mesh Renderer components

When you add the Spline Extrude component to a GameObject, the Unity Editor adds two more components that all 3D GameObject need:

* [Mesh Renderer Component](https://docs.unity3d.com/Manual/class-MeshRenderer.html)
* [Mesh Filter Component](https://docs.unity3d.com/Manual/class-MeshFilter.html)

## Additional resources

* [Create a 3D mesh along a spline](extrude-mesh.md)
* [Use components](xref:UsingComponents)
