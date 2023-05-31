# Spline Extrude component reference

Extrude a mesh along a spline to create shapes such as wires, ropes, noodles, and tubes.  

The Spline Extrude component requires a GameObject to have a Mesh Filter and Mesh Renderer component. When you add a Spline Extrude component to a GameObject, a Mesh Filter and Mesh Renderer component are added to that GameObject. 

If you add Spline Extrude to a GameObject that already has a mesh, then Spline Extrude removes that mesh from the GameObject and creates a new mesh for the GameObject to use. 

Use the Spline Extrude component to customize the geometry of the mesh you extrude on the spline. 
 
  
| **Property**          | **Description**           |
| :-------------------- | :------------------------ |
| **Spline** | Select a GameObject that has an attached Spline component you want to extrude a mesh on. |
| **Create Mesh Asset** | Create a new mesh asset to attach to this GameObject. </br> This property is visible only if you haven't selected a mesh for the GameObject the Spline Extrude is attached to. | 
| **Radius**   |  Set the radius of the extruded mesh.   |
| **Profile Edges**  | Set the number of sides that the radius of the mesh has. The minimum value is 3. | 
| **Segments Per Unit**  | Set how many edge loops make up the length of one unit of the mesh. |
| **Cap Ends** | Enable to fill each end of the mesh. If the spline the mesh is extruded on is closed, then this setting has no effect.   |
| **Range** | Select the section of the spline to extrude the mesh on.    |
| **Percentage** | Set a percentage of the spline to extrude the mesh on.   |
| **Auto-Regen Geometry** | Enable to regenerate the extruded mesh when the target spline is modified at runtime or in the Editor. If you don't want the Spline Extrude component to regenerate the extruded mesh at runtime when the target spline is modified, disable **Auto-Regen Geometry**. |
| **Rebuild Frequency**  | Set the maximum number of times per second that mesh regenerates. This property is visible only when you enable the **Auto-Regen Geometry** method.|
| **Update Colliders** | Enable to automatically update any attached collider components when the mesh extrudes.   |

## Additional resources

* [Create a tube-shaped mesh along a spline](extrude-mesh.md)
* [Use components](xref:UsingComponents)
