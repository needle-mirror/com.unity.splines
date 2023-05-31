# About

This package contains a framework and tools for working with curves and splines.

## Quick Start

Splines are defined as implementing the `ISpline` interface. There are two default implementations, a mutable `Spline` class, and an immutable `NativeSpline`.

Splines are represented in the scene using the `SplineContainer` MonoBehaviour. Multiple splines can be stored in a single container.

Use `SplineUtility` to extract information from `ISpline` objects (ex, get a position at some interpolation).

Use `Splines.cginc` to import curve data data types and HLSL functions for working with Splines.

## Creating a Spline

The default method of creating a Spline is the draw spline tool, which is accessed through the menu `GameObject/Spline/Draw Splines Tool`. This instantiates a new `SplineContainer` in the active scene, and enters the tool. 

With the **Draw Splines** tool active, add points to the spline by clicking in the Scene View. Press the `Enter/Return` to end the point placement on the current spline and add points to a new spline of the same container. Press the `Escape` key to finish placing points.

To edit a Spline, open the Spline Tool Context by selecting a `SplineContainer` and toggling the Tool Context (in the Scene View Tools Toolbar) to **Spline**. Then, use the Move, Rotate, Scale tools to modify spline knots and tangents.

## Extending Splines

Splines can be extended through inheritance, or with `SplineData<T>`. The `SplineData` class is a key value pair collection type where key corresponds to an interpolation value relative to a spline. See the API documentation for more information on working with `SplineData`.

Import the **Spline Examples** scripts and scenes from the Package Manager Samples page.
