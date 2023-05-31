## Quick Start

Splines are defined as implementing the `ISpline` interface. There are two default implementations: a mutable `Spline` class and an immutable `NativeSpline`.

To see more examples of common API use cases, [import the Splines package samples](https://docs.unity3d.com/Packages/com.unity.splines@latest/index.html?subfolder=/manual/index.html%23import-splines-samples) and review the `Runtime/SplineCommonAPI.cs` class.

Splines are represented in the scene using the `SplineContainer` MonoBehaviour.

```cs
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

class CreateSplineAtStart : MonoBehaviour
{
    void Start()
    {
        // Add a SplineContainer component to this GameObject.
        var container = gameObject.AddComponent<SplineContainer>();

        // Create a new Spline on the SplineContainer.
        var spline = container.AddSpline();

        // Set some knot values.
        var knots = new BezierKnot[3];
        knots[0] = new BezierKnot(new float3(0f,  0f, 0f));
        knots[1] = new BezierKnot(new float3(1f,  1f, 0f));
        knots[2] = new BezierKnot(new float3(2f, -1f, 0f));
        spline.Knots = knots;
    }
}

```

The Splines package provides Editor tools for the `SplineContainer` component and `Spline` objects. Other `ISpline` and `ISplineContainer` derived types cannot be edited by the default tools.

Use `SplineUtility` to extract information from `ISpline` objects. For example, use `SplineUtility` to get a position at some interpolation.

```cs
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
class EvaluateSpline : MonoBehaviour
{
    [SerializeField]
    float m_Interpolation = .5f;

    [SerializeField]
    GameObject m_Prefab;
    
    void Start()
    {
        Spline spline = GetComponent<SplineContainer>()[0];
        SplineUtility.Evaluate(spline, m_Interpolation, out float3 position, out float3 direction, out float3 up);
        var rotation = Quaternion.LookRotation(direction, up);
        Instantiate(m_Prefab, position, rotation);
    }
}
```

