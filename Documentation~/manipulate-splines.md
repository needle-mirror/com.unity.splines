# Manipulate splines

You can move, rotate, and scale splines like other GameObjects. Refer to [Positioning GameObjects](xref:PositioningGameObjects) in the Unity User Manual to learn more.

Knots and tangents determine a spline's path and shape. To change a spline's path or shape, manipulate a spline's knots and tangents.

## Select knots or tangents in the Scene view

Activate the **Spline** tool context to use the Move, Rotate, and Scale tools on knots or tangents in the Scene view. Once your active tool context is **Spline**, a spline's knots and tangents are visible and you can select them in the Scene view. When your active tool context is **Spline**, you can't select GameObjects that aren't splines in the Scene view.

To select the knots or tangents of a spline in the Scene view:

1. [!include[select-spline](.\\snippets\\select-spline.md)]
1. [!include[set-spline-context](.\\snippets\\set-spline-context.md)]
1. In the Scene view, select a knot or tangent.
1. To select multiple knots and tangents, do one of the following:
    * Click and drag to draw a box over multiple knots and tangents.
    * Hold Shift and then click the knots or tangents you want to select.


## Toggle tool handle positions with splines

You can toggle tool handle positions for knots and tangents like you would with other GameObjects. When you have multiple knots and tangents selected, a tool's handle position affects the behavior of some transform tools, such as the **Rotate** and **Scale** tools.

Use the tool handle position toggles in the [Tool Settings Overlay](https://docs.unity3d.com/Documentation/Manual/overlays.html) to select the following tool handle positions for knots and tangents:

* **Pivot**: Set the tool handle at the active element's pivot point. The active element is the last item you selected. If you select multiple knots or tangents:
    * The **Rotate** tool rotates the active element around its own pivot point and then applies that same rotation to the other knots in the selection.
    * The **Scale** tool scales the knots and tangents from each element's own pivot point.

* **Center**: Set the tool handle at the center of a selection. If you select multiple knots or tangents:
    * The **Rotate** tool rotates the knots or tangents around a handle centered between the selected elements.
    * The **Scale** tool scales the knots or tangents from a handle centered between the selected elements.

## Toggle tool handle rotation with splines

You can toggle tool handle rotations for knots and tangents like you would with other GameObjects. Besides the default tool handle rotation settings, **Global** and **Local**, knots and tangents have the **Parent** and **Element** handle rotations. When you have multiple knots and tangents selected, a tool's handle rotation setting affects the behavior of some transform tools, such as the **Rotate** and **Scale** tools.

Use the tool handle rotation position toggles in the [Tool Settings Overlay](xref:overlays) to select the following tool handle rotation positions for knots and tangents:

* **Global**: Clamp a spline element to world space orientation.
* **Local**: Keep a spline element's rotation relative to its parent spline.
* **Parent**: Set spline elements to take their parent element's orientation. For example, a tangent with its tool handle rotation set to **Parent** keeps its orientation relative to its parent knot. A knot with its tool handle rotation set to **Parent** keeps its orientation relative to its parent spline GameObject.
* **Element**: Set the tool handle to the active element's orientation.
