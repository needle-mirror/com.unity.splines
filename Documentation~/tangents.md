# Tangents

Tangents define the in and out curvature of a knot's segments.

Tangents have a length and direction. Length determines how much that tangent affects the curvature of its respective segment. A large length value increases a tangent's influence on a segment's curvature and a small length decreases it. Direction determines where the tangent points to from its parent knot.

A spline or knot's tangent mode determines how their tangents are calculated. If a knot is in the **Bezier** tangent mode, then you can directly manipulate its tangents in the Scene view. If a knot is in the [Auto](tangent-modes.md#auto-tangent-mode) or [Linear](tangent-modes.md#linear-tangent-mode) tangent mode, its tangents are calculated automatically.

| **Topic**             | **Description**                                                      |
| :-------------------- |:---------------------------------------------------------------------|
| **[Tangent modes](tangent-modes.md)**    | Understand the different tangent modes.                              |
| **[Select a tangent mode](select-tangent-mode.md)**| Select a tangent mode for a knot.                                    |
| **[Select a default tangent mode](select-default-tangent.md)**| Select the default tangent mode that the **Draw Splines Tool** uses. |