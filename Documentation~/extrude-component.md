# Spline Extrude component reference

Extrude a mesh on a spline to create shapes like wires, ropes, and tubes.  

Use the Spline Extrude component to customize the geometry of the mesh you extrude on the spline. 

Spline Extrude requires that a GameObject has a Mesh Filter and Mesh Renderer component. When you add a Spline Extrude component to a GameObject, a Mesh Filter and Mesh Renderer component also get added to that GameObject. 
 
  
| **Property**          | **Description**           |
| :-------------------- | :------------------------ |
| **Spline** | Select a GameObject that has an attached Spline component you want to extrude a mesh on. |
| **Radius**   |  Set the radius of the extruded mesh.   |
| **Profile Edges**  | Set the number of sides that the radius of the mesh has. | 
| **Segments Per Unit**  | Set how many edge loops make up the length of one unit of the mesh. |
| **Cap Ends** | Enable to fill each end of the mesh. If the spline the mesh is extruded on is closed, then this setting has no effect.   |
| **Range** | Select the section of the spline to extrude the mesh on.    |
| **Percentage** | Set a percentage of the spline to extrude the mesh on.   |
| **Auto-Regen Geometry** | Enable to regenerate the extruded mesh when the target spline is modified at runtime or in the Editor. If you don't want the Spline Extrude component to regenerate the extruded mesh at runtime when the target spline is modified, disable **Auto-Regen Geometry**. |
| **Rebuild Frequency**  | Set the maximum number of times per second that mesh regenerates. This property is visible only when you enable the **Auto-Regen Geometry** method.|
| **Update Colliders** | Enable to automatically update any attached collider components when the mesh extrudes.   |
