# Tangent modes

You can [select a tangent mode](select-tangent-mode.md) for knots that determines how their tangents are calculated.

Knots support the following tangent modes:

* [Linear](#linear-tangent-mode)
* [Auto](#auto-tangent-mode)
* [Bezier](#bezier-tangent-mode)


## Linear tangent mode

Use the **Linear** tangent mode to create a spline with straight lines or sharp corners.

The **Linear** tangent mode sets a knot's tangents to a length of `0` so that they point directly at the preceding and following knots.

In **Linear** mode, tangents are automatically computed and cannot be directly manipulated in the Scene view.

## Auto tangent mode

Use the **Auto** mode to create splines with smooth curves.

The **Auto** tangent mode calculates a knot's tangents based on the positions of its preceding and following knots. When you create a new knot on a spline in **Auto** mode, the preceding knot's segment curve adjusts according to the position of the new knot. If you rotate a knot in **Auto** mode, its tangents do not rotate with it.

In **Auto** mode, tangents are automatically computed and cannot be directly manipulated in the Scene view.

> [!NOTE]
> The **Auto** tangent mode creates Catmull-Rom splines.


## Bezier tangent mode

Use the **Bezier** tangent mode to create splines with tangents you can directly manipulate and modify in the Scene view.

You can select the following **Bezier** modes for knots in the **Bezier** tangent mode:

* [**Mirrored**](#mirrored-bezier-mode)
* [**Continuous**](#continuous-bezier-mode)
* [**Broken**](#broken-bezier-moode)

### Mirrored Bezier mode

Set a knot's tangents to point in opposite directions and have equal lengths.

A knot in **Mirrored** mode always points to its **Out** tangent. If you move tangents in **Mirrored** mode, the parent knot rotates to point to its **Out** tangent. If you rotate a knot in **Mirrored** mode, its tangents rotate with it.

> [!NOTE]
> For splines with non-uniform scaling, a knot in **Mirrored** mode might not point to its **Out** tangent. Non-uniform scaling is when the Scale in a Transform has different values for the x-axis, y-axis, and z-axis. For example, a spline with Scale values of (1 , 5, 10) has non-uniform scaling.

If you select a tangent and set it to **Mirrored** mode, it mirrors the opposite tangent. For example, if you set an Out tangent to **Mirrored**, the In tangent's length and direction change, but the Out tangent's length and direction do not change.

### Continuous Bezier mode

Align a knot's tangents so they always point in opposite directions. The length of tangents in **Continuous** mode are independent of each other and you can set them to different values.

A knot in **Continuous** mode always points to its **Out** tangent. If you move tangents in **Continuous** mode, the parent knot rotates to point to its **Out** tangent. If you rotate a knot in **Continuous** mode, its tangents rotate with it.

> [!NOTE]
> For splines with non-uniform scaling, a knot in **Continuous** mode might not point to its **Out** tangent. Non-uniform scaling is when the Scale in a Transform has different values for the x-axis, y-axis, and z-axis. For example, a spline with Scale values of (1 , 5, 10) has non-uniform scaling.

If you select a tangent and set it to **Continuous** mode, it aligns with the opposite tangent. For example, if you set an Out tangent to **Continuous**, the In tangent's direction changes, but the Out tangent's direction does not change.

### Broken Bezier mode

Dissociate a knot's tangents from each other. Use the **Broken** mode to  directly manipulate each tangent's length and direction.

If you rotate a knot in **Broken** mode, its tangents rotate with it.

