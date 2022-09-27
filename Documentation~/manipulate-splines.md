# Manipulate splines

You can move, rotate, and scale splines like other GameObjects. See [Positioning GameObjects](https://docs.unity3d.com/Manual/PositioningGameObjects.html) in the Unity User Manual to learn more.  

Knots and tangents determine a spline's path and shape. To change a spline's path or shape, manipulate a spline's knots and tangents. 

## Select knots or tangents in the Scene view

Activate the **Spline** tool context to use the Move, Rotate, and Scale tools on knots or tangents in the Scene view. Once your active tool context is **Spline**, a spline's knots and tangents are visible and you can select them in the Scene view. When your active tool context is **Spline**, you can't select GameObjects that aren't splines in the Scene view.

To select the knots or tangents of a spline in the Scene view:  

1. In the [Hierarchy window](https://docs.unity3d.com/Manual/Hierarchy.html) or [Scene view](https://docs.unity3d.com/Manual/UsingTheSceneView.html), select a spline GameObject.
1. In the [Tools overlay](https://docs.unity3d.com/Manual/overlays.html), set the tool context to **Spline**.
1. In the Scene view, select a knot or tangent. 
1. To select multiple knots and tangents, do one of the following:
    * Click and drag to draw a box over multiple knots and tangents. 
    * Hold Shift and then click the knots or tangents you want to select.
 
> [!NOTE]
> When your active tool context is Splines, in the Scene view, you can't select GameObjects that are not splines.


## Toggle tool handle positions with splines
You can toggle tool handle positions for spline elements like you would with other GameObjects. When you have multiple knots and tangents selected, a tool's handle position affects the behavior of some transform tools, such as the **Rotate** and **Scale** tools. 

Use the tool handle position toggles in the [Tool Settings Overlay](https://docs.unity3d.com/2021.2/Documentation/Manual/overlays.html) to select the following tool handle positions for spline elements:

* **Pivot**: Set the tool handle at the active element's pivot point. The active element is the last item you selected. If you select multiple spline elements:
    * The **Rotate** tool rotates the active element around its own pivot point and then applies that same rotation to the other knots in the selection. 
    * The **Scale** tool scales the spline elements from each knot's own pivot point. 

* **Center**: Set the tool handle at the center of a selection. If you select multiple spline elements:
    * The **Rotate** tool rotates the spline elements around a handle centered between the selected elements.
    * The **Scale** tool scales the spline elements from a handle centered between the selected elements.

## Toggle tool handle rotation with splines
You can toggle tool handle rotations for spline elements like you would with other GameObjects. Besides the default tool handle rotation settings, **Global** and **Local**, spline elements have the **Parent** and **Element** handle rotations. When you have multiple knots and tangents selected, a tool's handle rotation setting affects the behavior of some transform tools, such as the **Rotate** and **Scale** tools. 

Use the tool handle rotation position toggles in the [Tool Settings Overlay](https://docs.unity3d.com/2021.2/Documentation/Manual/overlays.html) to select the following tool handle rotation positions for spline elements:

* **Global**: Clamp a spline element to world space orientation. 
* **Local**: Keep a spline element's rotation relative to its parent spline.
* **Parent**: Set spline elements to take their parent element's orientation. For example, a tangent with its tool handle rotation set to **Parent** keeps its orientation relative to its parent knot. A knot with its tool handle rotation set to **Parent** keeps its orientation relative to its parent spline GameObject.  
* **Element**: Set the tool handle to the active element's orientation.
