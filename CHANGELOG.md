# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.6.1] - 2024-05-23

### Bug Fixes

- [SPLB-275] Fixed a bug where the `SplineContainer` component would throw warning when entering playmode.
- [SPLB-266] Fixed a bug where instantiating a `SplineExtrude` component at runtime would throw errors.
- [SPLB-269] Fixed a bug where instantiating a `SplineAnimate` component at runtime would throw errors.
- [SPLB-260] Fixed a bug where playing a `SplineAnimate` from a start method would not work.
- [SPLB-267] Fixed a bug where undoing a spline insertion in a container would throw errors from the `SplineInstantiate` component.
- [SPLB-261] Fixed a bug where the knot placement tool would not work when multiple scene views were opened.
- [SPLB-253] Fixed a bug where using the knot placement tool in 2D mode would not work if the grids are turned off.
- [SPLB-258] Fixed a bug where destroying a component would throw errors in the console.

## [2.6.0] - 2024-04-15

### Added

- Added new APIs that enable users to directly provide a collection of `float3` knot positions to a `Spline` constructor, or to the new `Add`, `AddRange`, `Insert`, and `InsertRange` methods, instead of using `BezierKnots`.
- Added an event to the `SplineAnimate` component that is triggered when the animation or loop is completed.

### Changed

- Changed supported version in the documentation dropdown selector to 2022.3 and later. 

### Bug Fixes

- [SPLB-246] Fixed performance and garbage collection (GC) memory allocation issues when evaluating splines using `SplineContainer` API.
- [STO-3176] Fixed an issue where `FitSplineToPoints` utility would not fit splines correctly.
- [SPLB-249] Fixed a bug where setting a spline game object position was overridden by the knot placement tool.
- Fixed a bug where using the knot placement tool on scaled objects would not create the correct tangents.
- Fixed a bug where changing the scale of a spline game object would not update the scene view handles.
- [SPLB-239] Fixed a bug where reverting overrides of a spline prefab instance would not properly update its visuals.
- [SPLB-238] Fixed a bug where `SplineAnimate` component would continuously output messages to the Console when in playmode and animated object was selected.
- [SPLB-245] Fixed a bug where `SplineAnimate` component would rotate the animated object on playmode exit.

## [2.5.2] - 2023-12-22

### Added

- [SPLB-222] Added safety checks to spline changed events. 

### Changed

- [SPLB-233] Removed transform handles while dragging a knot using direct manipulation.
- [SPLB-234] Removed unnecessary spline metadata which was saved on disk.

### Bug Fixes

- [SPLB-230] Fixed a bug where modifying the tangent mode on the last knot would change the spline shape unpredictably.
- [SPLB-225] Fixed a bug where closing a Spline with the `Draw Splines Tool` had inconsistent behaviour.
- [SPLB-236] Fixed a bug where the spline instantiate inspector was throwing an exception when the selection contained objects without that component.
- [SPLB-235] Fixed a bug where hovering a knot was create highlights when dragging tangents.
- [SPLB-234] Fixed a bug where it was impossible to add a knot on the latest added knot.
- [SPLB-214] Fixed an exception that would when adding a spline via the inspector if no other spline exists.
- [SPLB-226] Fixed broken preview when adding knots to a closed spline

## [2.5.1] - 2023-10-17

### Bug Fixes

- [SPLB-208] Fixed a bug where `SplineToolContext` would attempt to draw empty Splines and flood the console with errors.

## [2.5.0] - 2023-10-16

### Added

- Exposed public API for `SplineInstantiate.Seed`.

### Bug Fixes

- [SPLB-201] Fixed error messages showing in edit mode with spline components.
- [SPLB-203] Fixed a bug where baking `SplineInstantiate` instances broke prefab connections.
- [SPLB-196] Fixed a bug where the `Random` state was not restored after an update of the instances in `SplineInstantiate`.
- Fixed a bug where the `SplineCacheUtility` would send null reference exceptions.
- [SPLB-183] Fixed a bug where duplicating the `SplineExtrude` component was not updating the mesh asset reference.
- [SPLB-178] Fixed a bug where linking knots would not trigger a `Spline.changed` event. 
- [SPLB-185] Fixed a bug where the Up vector evaluation would return NaN values when normals and curve tangents are parallel.

### Changed

- [SPLB-187] Fixed a bug where `Spline.EvaluatePosition` was allocating memory when called.
- [SPLB-181] Fixed a bug where the Auto tangent mode computed incorrect knot rotations.
- Changed the `SplineComponent` display name in the Inspector to `Spline Container` as a response to user feedback. 

## [2.4.0] - 2023-07-26

### Added

- Exposed public APIs for `SplineUtility.ReverseFlow`, `SplineUtility.JoinSplinesOnKnots`, and  `SplineUtility.SplitSplineOnKnot`.

### Changed

- For Editor versions of 2023.2 or newer, added clipboard operations for Splines to the Scene view context menu.
- Changed `Splines/Cycle Active Spline` shortcut to Shift-S to remove a conflict with Unity default shortcuts.
- [SPLB-163] Improved performance of spline handles by caching spline data.
- [SPLB-167] Changed the evaluation of up vectors to use a cache instead of re-evaluate the values on every single request.
- In Editor versions of 2023.2 or newer, moved the following actions from the Element Inspector to the Scene view context menu: Link, Unlink, Split, Join, and Reverse Spline Flow. 

### Bug Fixes

- [SPLB-176] Fixed a regression where the up vector would not evaluate correctly for some curves.
- Fixed the setter for SplineAnimate.Normalized time to handle ping-pong direction correctly when set.
- Fixed a bug where setting the time value to 0 after reaching the end of the spline animation was not resetting the object position.
- [SPLB-169] Fixed a bug where `CurveUtility.GetNearestPoint` would return incorrect values for the interpolation.

## [2.3.0] - 2023-05-31

### Added

- Exposed public APIs for `SplineTool` and `SplineHandles`.
- Added a method to *bake* SplineInstantiate objects in the scene so the users can modify the resulting GameObjects. 

### Bug Fixes

- [SPLB-165] Fixed a bug that prevented actions from being retained with prefab instances that contained splines when entering playmode.
- [SPLB-162] Fixed a bug where the start offset in `SplineAnimate` did not work in Play mode. 
- [SPLB-152] Fixed objects created from `SplineInstantiate` not inheriting static flags from parent `GameObject`.
- [SPLB-160] Fixed potential 'look rotation vector is zero' in SplineInstantiate.
- [SPLB-156] Fixed possible null reference exception when accessing curve length.

## [2.2.1] - 2023-04-21

### Added

- [SPLB-100] Added a **Resolution** setting for spline meshes in Splines preference.

### Changed

- [SPLB-100] Improved `SplineMeshHandles` rendering. 

### Fixed

- Fixed a bug where spline selection would be incorrect after a spline was deleted. 
- [SPLB-146] Fixed a bug that prevented actions from being undone with prefab instances that contained splines .
- [SPLB-148] Fixed a bug in the `LoftRoadBehaviour` component where Undo calls prevented the project from building.

### Changed

- Changed `Knot Placement` tool behaviour regarding selection. Element selection is now empty when you exit the tool.

## [2.2.0] - 2023-03-22

### Added

- [SPLB-72] Added a public version of `SplineDataHandles.DataPointHandles` which requires an additional parameter, `splineID`.
- [SPLB-72] Added the `SplineContainer.SplineAdded`, `SplineContainer.SplineRemoved` and `SplineContainer.SplineReordered` public events and removed those same events from `ISplineContainer`
- [SPLB-72] Added the `SplineUtility.ReorderSpline` public method.
- [SPLB-72] Added an example to the **Loft Road** sample that supports spline data for multiple splines.
- Improved UI for editing points in the Inspector.
- Added preference to disable tangent dragging feature in `Knot Placement` tool.
- Added `ISplineContainer.AddSpline(Spline)` function overload.
- Added new functions on `Spline` to get and set `SplineData`.
- Added new `EmbeddedSplineData` type to easily create and reference `SplineData` stored on arbitrary `Spline` objects.

### Changed

- [SPLB-127] Changes made to a spline in the Inspector no longer invoke the `Spline.Changed` event with the `SplineModification.Default` type. The `Spline.Changed` event is still invoked, but with specific modification types like `SplineModification.KnotModified`.
- [SPLB-118] Changed the documentation of `SplineMath.PointLineNearestPoint` and `SplineMath.RayLineNearestPoint` to explain the returned values more precisely. 
- [SPLB-96] Changed the default tension of tangents in **Auto** mode so they are smoother.  **Auto** mode is now closer to the Catmull-Rom behavior of Splines 1.0. 
- [SPLB-72] Removed the `WidthSplineData` and `MultipleRoadBehaviour` components from the samples.
- Reduced heap allocations when modifying Spline properties.
- [SPLB-92] Modified the Inspector to use `SplineModification.KnotRemoved` events to remove knots and pass the index for each removed knot.
- [SPLB-92] Modified the `Spline.SetTangentMode` and `Spline.SetAutoSmoothTension` methods to invoke `SplineModification.KnotModified` events for each modified knot and pass the index.
- Increased the threshold for tangent dragging when placing knots with the `Knot Placement` tool.

### Fixed

- [SPLB-141] Fixed a bug where `SplineExtrude` component would overwrite existing Mesh assets.
- [SPLB-129] Fixed a bug where `CurveUtility.EvaluateUpVector` caused burst errors.
- [SPLB-138] Fixed a bug where `SplineJobs.EvaluatePositionTangentNormal` returned incorrect values.
- [SPLB-136] Fixed a bug where undoing actions on a spline was not marking the spline as dirty.
- [SPLB-123] Fixed an inconsistency between inserting a `BezierKnot` using Editor tools and runtime API.
- [SPLB-130] Fixed a bug that would crash the Editor if the `SplineInstantiate` component had its parent GameObject in the list of items to instantiate. 
- [SPLB-90] Fixed an issue where `Spline.SetKnotNoNotify` triggered change callbacks.
- [SPLB-77] Fixed a null reference exception when spline animate didn't have a spline referenced.
- [SPLB-125] Fixed issue where the spline would have different state during the `Spline.Changed` callback depending if it was modified using the API or inspector.
- [SPLB-74] Fixed a bug that caused an error when knots were deleted from the Inspector.
- [SPLB-121] Fixed a bug where spline data changes done using the Inspector would not undo correctly.
- [SPLB-92] Fixed an issue where adding knot using the inspector would trigger `SplineModification.KnotInserted` event twice.
- [SPLB-107] Fixed a bug where a duplicates of a spline with the Spline Instantiate component would have visible child GameObjects in the Hierarchy window.
- [SPLB-65] Fixed an issue where it wasn't possible to add a knot to a closed spline that is empty or contains a single knot.
- [SPLB-104] Fixed an issue where duplicating a spline using the Inspector would not copy knot link data.
- [SPLB-82] Fixed an issue where it would not be possible to undo adding a knot from the Inspector.
- [SPLB-84] Fixed an issue where the obsolete Spline Edit Mode would show in the Debug Inspector.
- [SPLB-135] Fixed an issue where the Auto Smooth tangent mode would not smooth a spline's curves correctly.
- [SPLB-104] Fixed an issue where shared knots would lose selection when a spline is selected using the Inspector.
- [SPLB-115] Fixed an issue where using the Width tool on MacOS (Metal) would render visual artifacts outside of the Scene view.
- [SPLB-98] Fixed an issue where `LoftRoadBehaviour` would create incorrect mesh geometry normals.
- [STO-2791] Fixed an issue where spline data's reorderable list would break if the data was not the field of a `MonoBehaviour`.
- [SPLB-86] Fixed a bug that caused a spline to change its shape when **Reverse Spline Flow** action was used.
- [SPLB-103] Fixed an issue where the spline tool settings' Visuals popup would display toggle checkboxes on the wrong size and be clipped in some editor versions.
- [SPLB-87] Fixed an issue where the cached Spline length could be incorrect after an undo action.
- Fixed an issue where modifying knot values or order in the Inspector could affect unassociated knot data.
- Fixed use of obsolete `Objects.FindObjectsOfType` API in Unity 2023.1 and newer.
- Fixed incorrect icon import settings.
- Fixed a bug that would prevent Spline Instantiate from instantiating when it was loaded from an addressable build in the Editor. 
- Fixed a bug where calling `SplineDataHandles.DataPointHandles` without a `splineID` argument would crash.
- Fixed a bug that would cause `SplineData` handles to interfere with scene navigation.
- Fixed SplineData handles not setting `GUI.changed` correctly.
- Fixed an issue where `SplineSelection` errors could put the Scene view into an unrecoverable broken state.
- Added a new property drawer for `int` interpreted as a spline index, `SplineIndexAttribute`.
- Fixed a bug where modifying and undoing changes to `Spline` values in the Inspector resulted in an incorrect `Spline` length calculation.
- Added `SplineData.OnSplineModified` function to attempt to preserve knot indices when knots are inserted or removed. This is called automatically for all embedded `SplineData` collections.

## [2.1.0] - 2022-10-18

### Added

### Changed

### Fixed

- [STO-2745] Fixed an issue where the Speed Tilt tool's arrow handle cap would flicker when the View tool toggled on or off.
- [STO-2744] Fixed a bug where tangents would have the wrong orientation.

## [2.1.0-pre.1] - 2022-10-12

### Added

- New `SplineUtility.EvaluateNurbs` function to evaluate positions on NURBS curves.
- New `SplineUtility.FitSplineToPoints` function to fit a spline to a provided set of curve points.
- Added a new public static class, `InterpolatorUtility`, to improve the discoverability of `IInterpolator` implementations.
- Added the functionality to offset the starting position of a `SplineAnimate` component's animation.
- Added copy, paste, and duplicate support to Spline tools.
- Exposed API to draw spline and curve handles.
- Added settings to generate a 3D mesh around spline handles to better visualize depth.
- Added the functionality to disable specific Spline tool handles.
- Added the functionality to tweak knot position without being in the position tool. 
- Added spline index to the Element Inspector when a spline element is selected. 
- Updated public API documentation.
- Updated built-in Spline components to support spline containers with multiple splines.
- New `SplineUtility.ReducePoints` function to remove redundant points on a line and still retain the original shape.
- New `Spline.SetAutoSmoothTangentTension` function to specify the tension used when tangents are calculated.
- Added `Spline Filter` to the Spline tools settings.
- Added spline selection from the Spline Inspector.
- Added support to show knot indices in the Scene View.
- New `Paint` example shows how to create a spline from a list of points.
- Updated the `SplineUtility.GetAutoSmoothTangent` function to use centripetal parameterization to calculate the tangents in **Auto** tangent mode.
- New `SplineUtility.GetAutoSmoothTangent` function takes the current knot position and then uses centripetal parameterization to calculate the tangents in **Auto** tangent mode.
- Added new built-in Spline shapes: Rounded Square, Polygon, Helix, and Star.
- Added the functionality to to delete tangents.
- Removed the disc that displayed around selected knots.
- Added a disc that displays when a user has the Tweak tool enabled and hovers over a tangent.
- Added transparency to the disc that displays when a knot or tangent is hovered on if the disc occludes an object in the scene. 
- Reduced the transparency of the Tweak tool handles when they occlude an object in the scene.
- New `EditorSplineUtility.SetKnotPlacementTool` function to set the active context to `SplineToolContext` and the active tool to `KnotPlacementTool`.

### Changed

- [STO-2666] Disabled the Visual dropdown button when using `KnotPlacementTool`.
- Modified spline element handles to use `Element Color`.
- [STO-2708] Modified spline deletion to remove empty splines.
- [STO-2682] Unified `Draw Splines Tool` naming across menus and documentation.
- [STO-2681] Attenuated the color of the tangents and the curve highlights when they are behind objects.
- [STO-2700] Modified Spline Instantiate so it no longer serializes instances in the Scene or prefabs.
- [STO-2682] Unified `Draw Splines Tool` naming across menus and docs.
- [STO-2728] Changed the label of the `SplineAnimate` component's `World` alignment mode to `World Space` in the Inspector.
- Modified the `Draw Splines Tool` to clear any Spline element selection when it activates.  
- [STO-2490] Made active element selection consistent with standard Editor behavior for GameObjects. Now you can hold Shift and click a knot to set it as the active element. 
- Spline element handles now use the `Element Selection` and `Element Preselection` colors.
- Changed tangent's shapes to diamonds.
- Modified the `Knot Placement Tool` to have a live preview for segments with auto-smoothed knots.
- Dependency on Unity Physics Module is now optional.
- Modified `SplinePath` to support the `Closed` property of Splines.
- Removed and merged redundant Sample scenes.
- Reduced the size of the flow indicator handle.
- Changed default colors and thickness for spline elements and curves.
- Improved the line visibility of handles and segments.
- Changed Burst from required dependency to optional.
- Burst compile `SplineJobs.EvaluationPosition` when Burst package is available.

### Fixed

- [STO-2746] Fixed an issue where it was impossible to delete knots on mac.
- [STO-2743] Fixed an issue where a curve's highlight would flicker when hovering near a knot.
- [STO-2729] Fixed an issue where reordering knots would break knot links until moved.
- [STO-2730] Fixed the curve highlight color when using the Tweak tool.
- [STO-2693] Fixed a bug that prevented users from adding and reordering knots in the Inspector when the spline comes from a class that inherits from `ISplineContainer`.
- [STO-2725] Fixed a bug where knots, discs, and the normal line of knots would use incorrect colors. 
- [STO-2726] Fixed a bug where knots handles were drawn under curves handles.
- [STO-2727] Corrected a typo in the Loop Mode of the `SplineAnimate`.
- [STO-2653] Fixed a bug where the Tweak tool's guide plane would flicker when drawn directly on a surface.
- [STO-2731] Fixed View Tool not working when Spline Context was active.
- [STO-2700] Fixed spline instantiate having a delay before regenerating the instances.
- [STO-2679] Fixed the segmentation of the curve highlight.
- [STO-2665] Fixed sample scenes not rendering correctly when URP or HDRP was used.
- [STO-2702] Removed the **Dist** label in the Inspector when the `SplineInstantiate` component is set to `Exact`.
- [STO-2656] Fixed a bug where hovering on linked knots would display discs on each linked knot. 
- [STO-2686] Fixed an issue where inserting a knot on the closing curve would result in other knots moving around.
- [STO-2685] Fixed a bug where `LoftRoadBehavior `would throw exceptions with knots that had linear tangents. 
- [STO-2657] Fixed a bug where spline gizmos would appear at unexpected moments.
- [STO-2655] Fixed a bug that caused knots to highlight with the wrong color. 
- [STO-2687] Fixed an error that would occur when deleting the last knot of a spline.
- [STO-2684] Fixed a bug that prevented Select All from selecting elements.
- [STO-2680] Fixed a bug where `SplineMesh.Extrude` would create twisted mesh geometry.
- [STO-2701] Fixed a bug where `LoftRoadBehavior` would either throw an exception or extrude incorrectly when the spline was in Linear tangent mode or if it was shorter than 1 unit length.
- [STO-2705] Fixed a bug where `SplineInstantiate` was not instantiating correctly when the instantiation method was set to `Method.InstanceCount`.
- [STO-2668] Fixed a bug where Spline element inspector values would not update when a knot is modified.
- [STO-2706] Fixed a bug where selecting a knot from the inspector was desynchronizing the tool selection.
- [STO-2696] Fixed a bug where clearing knot selection was not updating in the inspector. 
- [STO-2689] Fixed the behavior of the spline inspector selection when clicking on a selected element.
- [STO-2688] Fixed a delay in the scene view update after changing selection from the spline inspector.
- [STO-2669] Fixed a bug where modifying a spline in the Inspector would not invoke the `Changed` callback.
- [STO-2695] Fixed a bug that would throw an exception when the `SplineAnimate` component was destroyed.
- [STO-2658] Fixed a bug that would delay the color change when you hover over a segment.
- [STO-2692] Fixed an issue where deleting a spline would result in errors.
- [STO-2716] Added missing tooltips to the Element Inspector.
- [STO-2636] Fixed an issue that prevented spline framing if the active tool context was not set to Spline.
- [STO-2714] Fixed transformation and direct manipulation tools not working correctly when spline has non-uniform scale.
- [STO-2698] Fixed a bug that could cause a knot link to desync if a linked knot was modified in the Inspector.
- [SPLB-54] Fixed a bug where flow arrows and curve highlights would not be centered on a spline's segments between knots.
- [SPLB-44] Fixed a bug where tangent selection would remain after changing to a tangent mode without modifiable tangents.
- [STO-2632] Fixed Spline Selection Undo when selecting a single element.
- Fixed a bug where the spline inspector was not working if the `Spline` object was not stored in a ISplineContainer.
- [SPLB-40] Fixed a bug where a tangent's Magnitude field in the `Element Inspector` created NaN values.
- [SPLB-39] Fixed a bug where knots from separate splines would link to the wrong knot.
- [SPLB-38] Fixed incorrect auto-smooth knots on reversed splines.
- Fixed a bug where the SplineContainer reorderable list broke the LinkKnots collection.
- Fixed a bug that caused the Inspector to display incorrect spline indexes.
- Fixed spline selection intercepting scene view navigation shortcuts.
- Fixed a bug where setting the Spline Instantiate component's instantiation items with the Inspector would have no effect.
- Fixed a potential exception that occurred when opening scenes with splines created in the 1.0 version of this package.
- Fixed tangent and knot handles incorrectly highlighting while a tool is engaged.
- Fixed a bug where deleting some element selections would result in an exception being thrown.
- Fixed a bug where undoing after deleting a selection would not re-select the restored elements.
- [STO-2690] Fixed a bug that prevented data points from being added to a spline when the spline was clicked on. 
- [STO-2691] Fixed a bug where moving a data point along a spline behaved incorrectly.
- Fixed compile errors in sample scenes when building player.
- Added an ellipsis to the Draw Spline Tool menu item label.
- Fixed `Spline Tool Context` not working with `ISplineContainer` implementations that define a valid `KnotCollection`.

## [2.0.0-pre.2] - 2022-05-11

### Added

- Added the ability to edit multiple spline elements in the element inspector.
- Added functionality to join knots from different splines.
- Added functionality to reverse the flow of a spline.
- Added `SplinePath`, `SplineSlice`, and `SplineRange` types. These types enable interpolation and evaluation of partial or complete sections of discrete splines.

### Changed

- Modified rounding to be based on camera distance.
- [STO-2704] Changed `SplineUtility.GetBounds` to account for tangent positions when calculating bounds.
- Updated the design of the tangent mode fields in the Element Inspector.
- Added a dropdown menu to select tangent modes to the Element Inspector.
- Updated the `Draw Splines Tool` to display only one tangent when a new knot is created.
- Simplified tangents in the `Draw Splines Tool` by removing the interactable handle .
- Renamed `Knot Placement Tool` to `Draw Splines Tool`.
- Modified the `Draw Splines Tool` to account for multiple splines.

### Fixed

- Fixed SplineInspector knot removal not keeping metadata consistent (KnotLinks).
- Fixed an issue that caused auto-smoothed tangents to show in the `Draw Splines Tool` and be selectable by rect selection.
- Added `ReadOnly` to knot's and length's `NativeArray` to fix IndexOutOfRangeException on `NativeSpline`.
- Fixed tangents when closing the spline to keep user-defined values.
- Fixed index error in the `Spline.SendSizeChangeEvent` method.
- Fixed a case where inserting a knot would not update adjacent knots with "auto-smooth" tangent mode.

## [2.0.0-pre.1] - 2022-04-19

### Added

- Added structs and utility methods that use the [Job System](https://docs.unity3d.com/Manual/JobSystem.html) to evaluate splines.

### Changed

- Separated tangent and bezier modes in the Element Inspector.
- Added a feature to split splines at a knot from the Element Inspector.
- Added tool settings to change the default knot type.
- Added ON icons for tangent modes.
- Moved Spline creation menu items to `GameObject/Spline`.
- Modified the Spline Inspector to be reactive to spline element selections in the Scene View.
- New icons set for Spline-related items.
- Hiding knot handles if the EditorTool is not a SplineTool
- Tweaked the spline property drawer to make it a bit more clean.
- Changed the knot rotation property in the inspector to a Vector3Field instead of a QuaternionField.
- Added a new editor API to change the tangent mode of knots.
- Deprecated `Spline.EditType`. Tangent modes are now stored per knot. Use `Spline.GetTangentMode` and `Spline.SetTangentMode` to get and set tangent modes.
- Added ability to link and unlink knots using Element Inspector.

### Fixed

- [1411976] Fixed undo crash in SplineInstantiate component.
- Fixed scale offset in SplineInstantiate component.
- [1410919] Fixed SplineData Inspector PathIndexUnit when updating unit.
- Fixed issues with spline editor tools changes sometimes being overwritten
- Fixed `SplineUtility.Evaluate` incorrectly evaluating the up vector.
- [1403359] Fixed issue where `SplineExtrude` component would not update mesh after an undo operation.
- [1403386] Fixing SplineData Inspector triggering to SplineData.changed events.
- Fixing console InvalidOperationException when creating a Spline with a locked Inspector.
- [1384457] Fix for an exception being sometimes thrown when drawing a spline and rotating the scene view.
- [1384448] Fixed incorrect Rect Selection when using Shift or CTRL/CMD modifiers.
- [1413605] Fixed Linear to Bezier Edit Type conversion incorrectly leaving tangents set to zero length.
- [1413603] Spline creation menu items now respect the preference to place new objects at world origin.
- `SplineFactory.CreateSquare` now respects the `radius` argument.

## [1.0.0] - 2022-02-25

### Changed

- New icons set for Spline-related items.
- `SplineContainer` inspector is now more user-friendly.
- Fixed issue where Spline Inspector fields would not accept negative values.
- Fixed issue where the X shortcut would only cycle through World/Local handle orientations and ignore Parent/Element.
- Fixed samples compatibility issues on 2021.2.
- Spline Inspector no longer shows 2 editable tangent fields for Knots that only have one tangent.
- Fixed poor performance when manipulating long continuous tangents.
- `SplineUtility.ConvertIndexUnit` now wraps when returning normalized interpolations.
- Fixed issue where Knot rotation would not properly align to the surface the Knot is placed on.
- Fixed Spline length serialization issue that would result in incorrect Spline evaluations and length calculations.
- Updated Knot and Tangent handle design.

## [1.0.0-pre.9] - 2022-01-26

### Changed

- Adding new API to interact with SplineData Handles
- Adding a `SplineInstantiate` component and updating associated samples.
- Added a `SplineAnimate` component and sample scene.

### Fixed

- [1395734] Fixing SplineUtility errors with Spline made of 1 knot.
- Fixing Tangent Out when switching from Broken Tangents to Continuous Tangents Mode.
- Fixing Preview Curve for Linear and Catcall Rom when Closing Spline.

## [1.0.0-pre.8] - 2021-12-21

### Changed

- Added a `SplineExtrude` component and an accompanying ExtrudeSpline sample scene.
- When using a spline transform tool, CTRL/CMD + A now selects all spline elements.
- Improving Spline Inspector overlay.
- `SplineUtility.CalculateLength` now accepts `T : ISpline` instead of `Spline`.

### Fixed

- [1384451] Fixing knot handles size being too large.
- [1386704] Fixing SplineData Inspector not being displayed.
- Fixing wrong Spline length when editing spline using the inspector.
- [1384455] Fix single element selections breaking the undo stack.
- [1384448] Fix for CTRL/CMD + Drag not performing a multi selection.
- [1384457] Fix for an exception being sometimes thrown when drawing a spline and rotating the scene view.
- [1384520] Fixing stack overflow when entering playmode.
- Fixing SplineData conversion being wrong with KnotIndex.

## [1.0.0-pre.7] - 2021-11-17

### Changed

- Disable unstable GC alloc tests.

## [1.0.0-pre.6] - 2021-11-15

### Changed

- Replace references to 'time' with 'interpolation ratio' or 't'.
- Move distance to interpolation caching and lookup methods to `CurveUtility`, and document their use.
- Fix compile errors when opened in Unity 2021.2.
- Removed `Spline.ToNativeSpline`, use `new NativeSpline(ISpline)` instead.
- Removed `Spline.ToNativeArray`.

### Fixed

- Fixed issue where hidden start/end knot tangents would be selectable.
- Fixed active tangentOut incorrectly mirroring against tangentIn when changing tangent mode via shortcut.
- Fixed Knot Placement tool preview curve disappearing when cursor hovers over first knot.
- Fixed issue where knot would not align to tangents when switching from broken to mirrored or continuous modes.
- Fixed issue where hovering first knot while placing tangents would hide the last placed knot, its tangents and the preview curve.

## [1.0.0-pre.5] - 2021-11-02

- Initial release
