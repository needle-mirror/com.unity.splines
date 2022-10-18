using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Collection of commonly used Spline functions
/// </summary>
class SplineExamples : MonoBehaviour
{
    void Example()
    {
        // Splines exist in a scene as properties in a SplineContainer. Think Mesh and MeshFilter relationship.
        var container = GetComponent<SplineContainer>();

        // SplineContainer can hold many Splines. Access them through the Splines property.
        var splines = container.Splines;

        // Get the position along a Spline at a ratio from 0 (start of Spline) to 1 (end of Spline).
        // Calling the SplineContainer version of this method gives results in world space.
        var worldPosition = container.EvaluatePosition(.5f);

        // Get a position, tangent, and direction from a Spline and rotate some object to match.
        container.Evaluate(.3f, out var position, out var tangent, out var normal);
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(tangent);

        // Knot connections are stored in the KnotLinkConnection type
        var links = container.KnotLinkCollection;

        // Knots are referenced by an index to the SplineContainer.Splines array and Knot Index. Here we are taking
        // the 4th knot of the first Spline and querying for any other linked knots.
        var knotIndex = new SplineKnotIndex(0, 3);
        if (links.TryGetKnotLinks(knotIndex, out var linked))
            Debug.Log($"found {linked.Count} connected knots!");

        // SplineSlice represents a partial or complete range of curves from another Spline. Here we create a new Spline
        // from the first curve of another Spline. Slices can be iterated either forwards or backwards.
        // Slices are value types, and do not make copies of the referenced splines. They are very cheap to create.
        var slice = new SplineSlice<Spline>(splines.First(), new SplineRange(0, 2));

        // Evaluate multiple slices of many Splines as a single path by creating a SplinePath.
        var path = new SplinePath(new SplineSlice<Spline>[]
        {
            slice,
            // This range is starting at the 4th knot, and iterating backwards 3 indices.
            new SplineSlice<Spline>(splines[1], new SplineRange(3, -3))
        });

        // SplinePath implements ISpline, and can be evaluated using any of the usual SplineUtility methods.
        var _ = path.EvaluatePosition(.42f);

        // Where performance is a concern, NativeSpline can be used. These are NativeArray backed representations of
        // any ISpline type that are very efficient to query because all transformations are baked at the time of
        // construction. Unlike Spline, NativeSpline is not mutable.
        using var native = new NativeSpline(path, transform.localToWorldMatrix);
    }
}
