# Spline Instantiate component reference
  
Use the Spline Instantiate component to instantiate GameObjects on a spline. 
  
 
| **Property**          | **Description**           |
| :-------------------- | :------------------------ |
| **Container** | Select a GameObject that has an attached Spline component you want to instantiate GameObjects or prefabs on.   |
| **Items To Instantiate** | Create a list of GameObjects or prefabs you want to instantiate. For each element in the list, select a GameObject or prefab, and set a probability for that item to instantiate on the spline. |
| **Up Axis** | Select the axis instantiated items use as their up direction. The y-axis is the default up direction. |    
| **Forward Axis** | Select the axis instantiated items use as their forward direction. The z-axis is the default forward direction. | 
| **Align To** | Select the space that instantiated items orient to. </br> The following spaces are available: </br> <ul> <li>**Spline Element**: Align instantiated items to an interpolated orientation calculated from the rotation of the knots closest to where each item instantiates. </li> <li>**Spline Object**: Align the instantiated items to the orientation of the target spline. </li> <li>**World Space**: Align instantiated items to world space orientation. </li> </ul>   | 
| **Instantiate Method** | Select how to instantiate items on the spline. </br> The following instantiate methods are available: <ul> <li> **Instance Count**: Instantiate a number of items on the target spline. </li> <li> **Spline Distance**: Instantiate items separated by distance intervals measured along the spline. </li> <li>**Linear Distance**: Instantiate items separated by distance intervals measured linearly in world space. </li></ul>|    
| **Count** | Set a number or an inclusive random range of items to instantiate. </br> This property is visible only when you select the **Instance Count** instantiate method.  | 
| **Spacing (Spline)** | Set the distance interval to instantiate items at. The distance is measured along the spline. You can set an exact distance or an inclusive random range of values.</br> This property is visible only when you select the **Spline Distance** instantiate method.  |
| **Spacing (Linear)** | Set the distance interval to instantiate items at. The distance is measured linearly in world space. </br> To instantiate as many items that can fit on the spline without overlap, select **Auto**. </br> This property is visible only when you select the **Linear Distance** instantiate method. |
| **Position Offset** | Enable to instantiate items at a position relative to the spline. | 
| **Rotation Offset** | Enable to instantiate items with a rotation relative to the original GameObject. | 
| **Scale Offset** | Enable to instantiate items with a scale relative to the original GameObject. | 
| **Override space** | Enable to apply the offset to a coordinate space you select. If you don't enable **Override space**, the offset applies to the coordinate space you set **Align to** to. </br> This property is visible only when you enable **Position Offset**, **Rotation Offset**, or **Scale Offset**. |
| **X** | Set the offset's x-axis value. You can set an exact value or an inclusive random range of values. </br> This property is visible only when you enable **Position Offset**, **Rotation Offset**, or **Scale Offset**.   |
| **Y** | Set the offset's y-axis value. You can set an exact value or an inclusive random range of values. </br> This property is visible only when you enable **Position Offset**, **Rotation Offset**, or **Scale Offset**.   |
| **Z** | Set the offset's z-axis value. You can set an exact value or an inclusive random range of values. </br> This property is visible only when you enable **Position Offset**, **Rotation Offset**, or **Scale Offset**.  |
| **Auto Refresh Generation** | Enable the automatic regeneration of instantiated items when you change the spline or its instantiation values. |
| **Randomize** | Regenerate all values set to random ranges, and then instantiate items again. |
| **Regenerate** | Instantiate items on the spline again.  |
| **Clear** | Remove all instantiated items from the spline. |
