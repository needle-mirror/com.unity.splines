# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2022-09-27

### Added

- Added a new public static class, `InterpolatorUtility`, to improve the discoverability of `IInterpolator` implementations.
- Removed the disc that displayed around selected knots.
- Added a disc that displays when hovering over a tangent.
- Added transparency to the disc that displays when a knot or tangent is hovered on if the disc occludes an object in the scene.
- Added spline index to the Element Inspector when a spline element is selected.
- Updated public API documentation.
- Added the functionality to disable specific Spline tool handles.
- Added spline selection from the Spline Inspector.
- Updated built-in Spline components (Instantiate and Extrude) to support spline containers with multiple splines.
- Added the functionality to to delete tangents.

### Changed

- Modified spline element handles to use `Element Color`.
- [STO-2681] Attenuated the color of the tangents and the curve highlights when they are behind objects.
- Modified the `Draw Splines Tool` to clear any Spline element selection when it activates.
- Spline element handles now use the `Element Selection` and `Element Preselection` colors.
- [STO-2731] Fixed View Tool not working when Spline Context was active.
- Changed tangent's shapes to diamonds.
- [STO-2728] Changed the label of the `SplineAnimate` component's `World` alignment mode to `World Space` in the Inspector.
- Modified the `Knot Placement Tool` to have live preview for segments with auto-smoothed knots.
- Dependency on Unity Physics Module is now optional.
- Reduced the size of the flow indicator handle.
- Changed default colors and thickness for spline elements and curves.
- Improved the line visibility of handles and segments.
- Changed Burst from required dependency to optional.
- Burst compile `SplineJobs.EvaluationPosition` when Burst package is available.

### Fixed

- [STO-2729] Fixed an issue where reordering knots would break knot links until moved.
- [STO-2725] Fixed a bug where knots, discs, and the normal line of knots would use incorrect colors.
- [STO-2726] Fixed a bug where knots handles were drawn under curves handles.
- [STO-2693] Fixed a bug that prevented users from adding and reordering knots in the Inspector when the spline comes from a class that inherits from `ISplineContainer`.
- [STO-2727] Corrected a typo in the Loop Mode of the `SplineAnimate`.
- [STO-2656] Fixed a bug where hovering on linked knots would display discs on each linked knot.
- [STO-2714] Fixed transformation tools not working correctly when spline has non-uniform scale.
- [STO-2679] Fixed the segmentation of the curve highlight.
- [STO-2665] Fixed sample scenes not rendering correctly when URP or HDRP was used.
- [STO-2702] Removed the **Dist** label in the Inspector when the `SplineInstantiate` component is set to `Exact`.
- [STO-2706] Fixed a bug where selecting a knot from the inspector was desynchronizing the tool selection.
- [STO-2655] Fixed a bug that caused knots to highlight with the wrong color.
- [STO-2696] Fixed a bug where clearing knot selection was not updating in the inspector.
- [STO-2680] Fixed a bug where `SplineMesh.Extrude` would create twisted mesh geometry.
- [STO-2701] Fixed a bug where `LoftRoadBehavior` would either throw exception or loft incorrectly when spline was linear or shorter than unit length.
- [STO-2689] Fixed the behavior of the spline inspector selection when clicking on a selected element.
- [STO-2705] Fixed a bug where `SplineInstantiate` was not instantiating correctly when the instantiation method was set to `Method.InstanceCount`.
- [STO-2698] Fixed a bug that could cause a knot link to desync if a linked knot was modified in the Inspector.
- [STO-2688] Fixed a delay in the scene view update after changing selection from the spline inspector.
- [STO-2685] Fixed a bug where `LoftRoadBehavior `would throw exceptions with knots that had linear tangents.
- [STO-2682] Unified `Draw Splines Tool` naming across menus and documentation.
- [SPLB-53] Fixed a bug where flow arrows and curve highlights would not be centered on a spline's segments between knots.
- [SPLB-51] Fixed SplineUtility.GetNearestPoint method returning a wrong t value.
- Fixed a bug where the spline inspector was not working if the `Spline` object was not stored in a ISplineContainer.
- [STO-2490] Made active element selection consistent with standard Editor behavior for GameObjects. Now you can hold Shift and click a knot to set it as the active element.
- [STO-2632] Fixed Spline Selection Undo when selecting a single element.
- [STO-2658] Fixed a bug that would delay the color change when you hover over a segment.
- [SPLB-32] Fixed a bug where a tangent's Magnitude field in the `Element Inspector` created NaN values.
- [SPLB-44] Fixed a bug where tangent selection would remain after changing to a mode without modifiable tangents.
- [SPLB-34] Fixed a bug where knots from separate splines would link to the wrong knot.
- [SPLB-30] Fixed incorrect auto-smooth knots on reversed splines.
- Fixed a warning when opening Splines 2.0 project ('Variable never used').
- Fixed a bug where the SplineContainer reorderable list broke the LinkKnots collection.
- Fixed a bug that caused the Inspector to display incorrect spline indexes.
- Fixed spline selection intercepting scene view navigation shortcuts.
- Fixed a bug where setting the Spline Instantiate component's instantiation items with the Inspector would have no effect.
- Fixed a potential exception that occurred when opening scenes with splines created in the 1.0 version of this package.
- Fixed tangent and knot handles incorrectly highlighting while a tool is engaged.
- Fixed compile errors in sample scenes when building player.
- Added an ellipsis to the Draw Spline Tool menu item label.
- Fixed `Spline Tool Context` not working with `ISplineContainer` implementations that define a valid `KnotCollection`.

## [2.0.0-pre.2] - 2022-05-11

### Added

- Added the ability to edit multiple spline elements in the element inspector.
- Added functionality to join knots from different splines.
- Added functionality to reverse the flow of a spline.

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