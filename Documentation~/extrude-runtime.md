# Extrude a spline at runtime

To extrude a spline at runtime, use a script to create a new GameObject with a `SplineContainer` component. You can then access the spline from the `SplineContainer` and add knots to it.

```
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class ExtrudeExample : MonoBehaviour
{
    void Start()
    {
        var splineContainer = new GameObject("Spline").AddComponent<SplineContainer>();
        splineContainer.Spline = new Spline();
        splineContainer.Spline.AddRange(new float3[]
        {
            new (0, 0, 0),
            new (0, 0, 1),
            new (1, 0, 1),
            new (1, 0, 0)
        });

        var go = splineContainer.gameObject;
        var extrudeComponent = go.AddComponent<SplineExtrude>();
        extrudeComponent.Container = splineContainer;

        var hasMeshFilter = go.TryGetComponent<MeshFilter>(out var meshFilter);
        if (hasMeshFilter)
        {
            if (meshFilter.sharedMesh == null)
            {
                var extrudeMesh = new Mesh();
                extrudeMesh.name = "Spline Extrude Mesh";
                meshFilter.sharedMesh = extrudeMesh;
            }

            extrudeComponent.Radius = 0.25f;
            extrudeComponent.SegmentsPerUnit = 20;
            extrudeComponent.Sides = 8;
            extrudeComponent.Range = new float2(0, 100);

            var hasMeshRenderer = go.TryGetComponent<MeshRenderer>(out var meshRenderer);
            if (hasMeshRenderer)
                meshRenderer.material = new Material(Shader.Find("Standard"));
        }
    }
}
```

## Additional resources

* [Create a 3D mesh along a spline](extrude-mesh.md)
* [Spline Extrude component reference](extrude-component.md)
* [Scriptin in Unity](https://docs.unity3d.com/Manual/scripting.html)
