# Change the orientation and alignment of the animated GameObject

Select the orientation and alignment that a GameObject uses when it animates along a spline.

To change the orientation and alignment of the animated GameObject, do the following in the GameObject's **Spline Animate** component:

1. In the **Up Axis** dropdown, select which axis the animated GameObject uses as its up direction. The y-axis is the default up direction.
1. In the **Forward Axis** dropdown, select which axis the animated GameObject uses as its forward direction. The z-axis is the default forward direction.
1. In the **Align To** dropdown, select a space to orient the animated GameObject to:
    * Select **None** to set no alignment for the animated GameObject. The GameObject's rotation is unaffected.
    * Select **Spline Element** to align animated GameObjects to an interpolated orientation calculated from the rotation of the knots closest to its position.
    * Select **Spline Object** to align animated GameObjects to the orientation of the target spline.
    * Select **World Space** to align animated items to world space orientation.

## Additional resources

* [Animate a GameObject along a spline](animate-spline.md)
* [Control the animated GameObject's movement](animate-movement.md)
* [Spline Animate component reference](animate-component.md)
* [Use components](xref:UsingComponents)