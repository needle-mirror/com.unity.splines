# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2022-03-29

### Changes

- Added new icons for Spline UI elements.
- Modified knot handles so they are hidden if the `EditorTool` is not a `SplineTool`. 


### Bug fixes

- [1403386] Fixing SplineData Inspector triggering to SplineData.changed events.
- [1403359] Fixed issue where `SplineExtrude` component would not update mesh after an undo operation.
- Fixed `SplineUtility.Evaluate` not evaluating the up vector correctly.
- Fixed InvalidOperationException thrown when Spline is created with a locked Inspector.
- [1384448] Fixed incorrect Rect Selection when using Shift or CTRL/CMD modifiers.
- [1384457] Fixed an issue where an exception was thrown when drawing a spline and rotating the scene view.

## [1.0.0] - 2022-02-25

### Changes

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

### Changes 

- Adding new API to interact with SplineData Handles
- Adding a `SplineInstantiate` component and updating associated samples.
- Added a `SplineAnimate` component and sample scene.

### Bug fixes

- [1395734] Fixing SplineUtility errors with Spline made of 1 knot.
- Fixing Tangent Out when switching from Broken Tangents to Continuous Tangents Mode.
- Fixing Preview Curve for Linear and Catcall Rom when Closing Spline.

## [1.0.0-pre.8] - 2021-12-21

### Bug Fixes

- [1384451] Fixing knot handles size being too large.
- [1386704] Fixing SplineData Inspector not being displayed.
- Fixing wrong Spline length when editing spline using the inspector.
- [1384455] Fix single element selections breaking the undo stack.
- [1384448] Fix for CTRL/CMD + Drag not performing a multi selection.
- [1384457] Fix for an exception being sometimes thrown when drawing a spline and rotating the scene view.
- [1384520] Fixing stack overflow when entering playmode.
- Fixing SplineData conversion being wrong with KnotIndex.

### Changes

- Added a `SplineExtrude` component and an accompanying ExtrudeSpline sample scene.
- When using a spline transform tool, CTRL/CMD + A now selects all spline elements.
- Improving Spline Inspector overlay.
- `SplineUtility.CalculateLength` now accepts `T : ISpline` instead of `Spline`.

## [1.0.0-pre.7] - 2021-11-17

### Changes

- Disable unstable GC alloc tests.

## [1.0.0-pre.6] - 2021-11-15

### Bug Fixes

- Fixed issue where hidden start/end knot tangents would be selectable.
- Fixed active tangentOut incorrectly mirroring against tangentIn when changing tangent mode via shortcut.
- Fixed Knot Placement tool preview curve disappearing when cursor hovers over first knot.
- Fixed issue where knot would not align to tangents when switching from broken to mirrored or continuous modes.
- Fixed issue where hovering first knot while placing tangents would hide the last placed knot, its tangents and the preview curve.

### Changes

- Replace references to 'time' with 'interpolation ratio' or 't'.
- Move distance to interpolation caching and lookup methods to `CurveUtility`, and document their use.
- Fix compile errors when opened in Unity 2021.2.
- Removed `Spline.ToNativeSpline`, use `new NativeSpline(ISpline)` instead.
- Removed `Spline.ToNativeArray`.

## [1.0.0-pre.5] - 2021-11-02

### Features

- Initial release.
