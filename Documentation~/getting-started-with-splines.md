# Getting started with Splines

## Creating the Asset
To create a spline game object there are three different methods.

In the Unity menu, go to **GameObject** > **Spline** > **New Spline**.
<br/><img src="images/getting-started-create-spline-unity-menu.png" alt="Create Spline, Unity Menu" width="350"/>

In the Hierarchy window. RMB > **Spline** > **New Spline**
<br/><img src="images/getting-started-create-spline-hierarchy.png" alt="Create Spline, Hierarchy window" width="350"/>

In the Inspector window, on a GameObject, **Add Component** > **Spline Container** (Script).
<br/><img src="images/getting-started-create-spline-inspector.png" alt="Create Spline, Inspector window" width="350"/>

For more information, see also [Spline Container](spline-container.md).

## Component Editor Tools
The **Knot Placement**, **Knot Move**, and the **Tangent Move** tools are available in the [Component Editor Tools](https://docs.unity3d.com/Manual/UsingCustomEditorTools.html#ToolModesAccessSceneViewPanel) overlay in the Scene window. The **Knot Placement** tool will be automatically engaged after the spline is created with the Unity menu or the Hierarchy window.
<br/><img src="images/getting-started-component-editor-tools.png" alt="Component editor tools" width="350"/>

### Knot Placement Tool
Use the **Knot Placement** tool to add knots. ![](images/KnotPlacementTool.png "Knot Placement Tool")

When the tool is engaged, you can place knots on a surface, such as, a Terrain object or a mesh face.
<br/><img src="images/getting-started-knot-placement.gif" alt="Point Placement" width="350"/>

Clicking on the first point will close the spline.
<br/><img src="images/getting-started-close-loop.gif" alt="Close Loop" width="350"/>

Placing knots not on a surface will place it on the grid instead.

Use **Ctrl** + **z** to delete the last created knot.

Use the **Esc** key to exit the **Knot Placement** creation.


### Knot Move Tool
Use the **Knot Move** tool to move knots.<img src="images/KnotMoveTool.png" alt="Knot Move Tool"/>

You can then select one or more knots to get a position handle to move them around.
<br/><img src="images/getting-started-knot-tool-move-handle.gif" alt="Knot Move Handle" width="350"/>

Clicking on the spline will create a new knot.
<br/><img src="images/getting-started-knot-tool-move-create.gif" alt="Knot Create" width="350"/>

Selecting a knot and then pressing the delete key will delete the knot.
<br/><img src="images/getting-started-knot-tool-move-delete.gif" alt="Knots Delete" width="350"/>


### Tangent Move Tool
Use the **Tangent Move** tool to move knot tangents. <img src="images/TangentMoveTool.png" alt="Tangent Move Tool"/>

This can tool can only be engaged when in the Inspector window on the Spline Container the Edit Mode Type property is set to **Bezier**.
<br/><img src="images/getting-started-knot-tool-tangent-type.png" alt="Edit Mode Type" width="350"/>

The tangent can be manipulated by moving the tangent handle.
<br/><img src="images/getting-started-knot-tool-tangent-handles.gif" alt="Tangent Movement" width="350"/>

Holding Shift while using a tangent handle will display the radial rotation gizmo.
<br/><img src="images/getting-started-knot-tool-tangent-shift.gif" alt="Tangent Shift Rotation" width="350"/>
