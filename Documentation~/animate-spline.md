# Animate a GameObject along a spline

Move a GameObject along a spline.

Use the [Spline Animate](animate-component.md) component to define the movement of cameras, characters, or other GameObjects in the Editor.  

You must have a GameObject with a **Spline** component attached to it in your scene to select as the target spline for the **Spline Animate** component.  

By default, the **Spline Animate** component uses the **Time** method to animate a GameObject with set to complete after 1 second. To change what animation method your GameObject uses and how it moves along its target spline, refer to **[Configure the movement of a GameObject](animate-movement.md)**.  

To animate a GameObject along a spline:

1. Add the **Spline Animate** component to a GameObject that you want to animate along a spline. 
1. In the **Spline Animate** component, for the **Spline** property, select a GameObject that has an attached Spline component that you want to animate on. 
1. To view the animation in the Scene view, select **Play** in the **Spline Animate** component's **Preview** panel.


## Additional resources

* [Control the animated GameObject's movement](animate-movement.md)
* [Change the alignment of an animated GameObject](animate-alignment.md)
* [Spline Animate component reference](animate-component.md)
* [Use components](xref:UsingComponents)