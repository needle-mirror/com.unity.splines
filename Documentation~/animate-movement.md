# Configure an animated GameObject's movement

Control how an animation begins on a spline, its speed, and the animation method it uses.

To control the movement of an animated GameObject, do the following in the GameObject's **Spline Animate** component:

1. To start the animation when the GameObject first loads, enable **Play On Awake**.
1. To set a distance on the spline to start the GameObject's animation at, enter a value for the **Start Offset** property. The range is 0 through 1. A value of 0 starts the animation at the beginning of the spline and a value of 1 starts the animation at the end of the spline.
1. In the **Method** dropdown, select an animation method:
    * Select **Time** to animate the GameObject along the spline over a period of time measured in seconds.
    * Select **Speed** to animate the GameObject along the spline at a set speed measured in meters per second.
1. In the **Easing** dropdown, select an easing mode for the animation to use:
    * Select **None** to add no easing to the animation. The animation speed is linear.
    * Select **Ease In Only** to have the animation start slowly and then speed up.
    * Select **Ease Out Only** to have the animation slow down at the end of its sequence.
    * Select **Ease In-Out** to have the animation start slowly, speed up, and then end slowly. **Ease In-Out** is a combination of **Ease In** and **Ease Out**.

    > [!NOTE]
    > Easing varies the speed of the animation to make it seem more natural and organic.

1. In  the **Loop Mode** dropdown, select if and how the animation repeats after its initial sequence finishes:
    * Select **Once** to play the animation once.
    * Select **Loop Continuous** to restart the animation from the beginning after it finishes.
    * Select **Ease In Then Continuous** to have the animation start slowly and then restart from its beginning after it finishes. If **Ease In Only** looping is set, then the easing applies only to the first animation loop.
    * Select **Ping Pong** to have the animation play in reverse after it finishes. The animation plays repeatedly.

## Additional resources

* [Animate a GameObject along a spline](animate-spline.md)
* [Change the alignment of an animated GameObject](animate-alignment.md)
* [Spline Animate component reference](animate-component.md)
* [Use components](xref:UsingComponents)