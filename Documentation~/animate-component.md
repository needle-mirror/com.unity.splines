# Spline Animate component reference
  
Use the Spline Animate component to animate the position and rotation of a GameObject along a spline. 
 
  
| **Property**          | **Description**           |
| :-------------------- | :------------------------ |
| **Spline** | Select a GameObject that has an attached Spline component you want to animate on. |
| **Up Axis**  | Select which axis the animated GameObject uses as its up direction. The y-axis is the default up direction.   |
| **Forward Axis** | Select which axis the animated GameObject uses as its forward direction. The z-axis is the default forward direction.  |
| **Align To**  | Select one of the following spaces to orient animated GameObjects to: </br> <ul><li>**None**: Set no alignment for the animated GameObject. The GameObject's rotation is unaffected. </li><li>**Spline Element**: Align animated GameObjects to an interpolated orientation calculated from the rotation of the knots closest to its position.</li> <li>**Spline Object**: Align animated GameObjects to the orientation of the target spline.</li> <li>**World Space**: Align animated items to world space orientation. </li> |
| **Play On Awake**  | Start the animation when the GameObject first loads. |
| **Start Offset**  | Set a distance on the target spline to start the GameObject's animation at. The range is 0 through 1. A value of 0 starts the animation at the beginning of the spline and a value of 1 starts the animation at the end of the spline. |
| **Method** | Select the animation method that the animation uses. </br> The **Time** method animates the GameObject along the spline from over a period of time measured in seconds. The **Speed** method animates the GameObject along the spline at a set speed measured in meters per second.  |
| **Duration**  | Set the period of time that it takes for the GameObject to complete its animation along the spline. </br> This property is visible only when you enable the **Time** method. |
| **Speed** | Set the speed that the GameObject animates along the spline at.  </br> This property is visible only when you enable the **Speed** method.   |
| **Easing** | Select the easing mode that the animation uses. Easing varies the speed of the animation to make it seem more natural and organic. The following easing modes are available: <ul><li>**None**: Set no easing on the animation. The animation speed is linear. </li><li>**Ease In Only**: The animation starts slowly and then speeds up. </li><li>**Ease Out Only**: The animation slows down at the end of its sequence. </li><li>**Ease In-Out**: The animation starts slowly, speeds up, and then ends slowly. **Ease In-Out** is a combination of **Ease In** and **Ease Out**. </li></ul>  |
| **Loop Mode** | Select the loop mode that the animation uses. Loop modes cause the animation to repeat after it finishes. The following loop modes are available: <ul><li>**Once**: Set the animation to play only once. </li><li>**Loop Continuous**: Set the animation to restart from its beginning after it finishes. </li><li>**Ease In Then Continuous**: Set the animation to start slowly and then restart from its beginning after it finishes. If **Ease In Only** looping is set, then the easing applies only to the first animation loop.  </li><li>**Ping Pong**: Set the animation to play in reverse after it finishes. The animation plays repeatedly. </li></ul>  |
| **Preview** | Play, pause, or reset the animation. |
|**Time** |  Select a specific time in the sequence of the animation. |

## Additional resources

* [Animate a GameObject along a spline](animate-spline.md)
* [Control the animated GameObject's movement](animate-movement.md)
* [Change the alignment of an animated GameObject](animate-alignment.md)
* [Use components](xref:UsingComponents)
