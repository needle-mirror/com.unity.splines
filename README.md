# About

This package contains a framework and tools for working with curves and splines.

## Quick Start

Splines are defined as implementing the `ISpline` interface. There are two default implementations, a mutable `Spline` class, and an immutable `NativeSpline`.

Splines are represented in the scene using the `SplineContainer` MonoBehaviour.

Use `SplineUtility` to extract information from `ISpline` objects (ex, get a position at some interpolation).

Use `Splines.cginc` to import curve data data types and HLSL functions for working with Splines.

## Creating a Spline

The default method of creating a Spline is the draw spline tool, which is accessed through the menu `GameObject/3D Object/Spline/Draw Spline Tool...`. This instantiates a new `SplineContainer` in the active scene, and enters the knot placement tool. 

With the **Knot Placement** tool active, add points to the spline by clicking in the Scene View. Press the `Escape` or `Enter/Return` key to finish placing points.

To edit a Spline, open the Spline Tool Context by selecting a `SplineContainer` and toggling the Tool Context (in the Scene View Tools Toolbar) to **Spline**.

## Extending Splines

Splines can be extended through inheritance, or with `SplineData<T>`. The `SplineData` class is a key value pair collection type where key corresponds to an interpolation value relative to a spline. See the API documentation for more information on working with `SplineData`.

Import the **Spline Examples** scripts and scenes from the Package Manager interface for code samples.
