# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.0-pre.2] - 2022-05-11

### Added

- Added the ability to edit multiple spline elements in the element inspector.
- Added functionality to join knots from different splines.
- Added functionality to reverse the flow of a spline.

### Changed

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