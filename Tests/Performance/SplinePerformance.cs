using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

class PerformanceTests
{
	const string k_TestScenePath = "Packages/com.unity.splines/Tests/Scenes/SplinePerfTest.unity";

	static readonly string[] k_Splines = new string[]
	{
		"Small",
		"Medium",
		"Large",
		"LotsOfKnots",
		"TonsOfKnots",
	};

	string m_TempScene;

	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		string sceneName = Path.GetFileName(k_TestScenePath);
		AssetDatabase.CopyAsset(k_TestScenePath, m_TempScene = $"Assets/{sceneName}.unity");
	}

	[SetUp]
	public void SetUp()
	{
		string sceneName = Path.GetFileName(k_TestScenePath);
		EditorSceneManager.OpenScene(m_TempScene, OpenSceneMode.Single);
		Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
	}

	[OneTimeTearDown]
	public void OnTimeTearDown()
	{
		AssetDatabase.DeleteAsset(m_TempScene);
	}

	static SplineContainer GetContainer(string spline)
	{
		var gameObject = GameObject.Find(spline);
		Assert.That(gameObject, Is.Not.Null, $"failed to find spline: {spline}");
		var container = gameObject.GetComponent<SplineContainer>();
		Assert.That(container, Is.Not.Null, $"failed to find spline: {spline}");
		return container;
	}

	static Spline GetSpline(string spline)
	{
		var gameObject = GameObject.Find(spline);
		Assert.That(gameObject, Is.Not.Null, $"failed to find spline: {spline}");
		var container = gameObject.GetComponent<SplineContainer>();
		Assert.That(container, Is.Not.Null, $"failed to find spline: {spline}");
		return container.Spline;
	}

	[Test, TestCaseSource(nameof(k_Splines)), Performance, Version("1")]
	public void GetNearestPointRay(string spline)
	{
		var container = GetContainer(spline);

		Measure.Method(() =>
		{
			using var read = container.Spline.ToNativeSpline();
			SplineUtility.GetNearestPoint(read, new Ray(Vector3.up * 10f, -Vector3.up), out _, out _);
		}).Definition("GetNearestPointRay").Run();
	}

	[Test, TestCaseSource(nameof(k_Splines)), Performance, Version("1")]
	public void GetNearestPointPoint(string spline)
	{
		var container = GetContainer(spline);

		Measure.Method(() =>
		{
			using var read = container.Spline.ToNativeSpline();
			SplineUtility.GetNearestPoint(read, new float3(0), out _, out _);
		}).Definition("GetNearestPointPoint").Run();
	}

	[Test, TestCaseSource(nameof(k_Splines)), Performance, Version("2")]
	public void EvaluatePosition(string gameObject)
	{
		var container = GetContainer(gameObject);
		var spline = container.Spline;

		// Resolution of 5 is generally appropriate for drawing splines, so we'll use it as the benchmark for evaluation
		const int k_EvaluatePositionResolution = 5;
		int segments = SplineUtility.GetSegmentCount(container.Spline.GetLength(), k_EvaluatePositionResolution);
		float3[] splineUtility = new float3[segments];

		Measure.Method(() =>
		{
			for(int i = 0; i < segments; i++)
				splineUtility[i] = SplineUtility.EvaluatePosition(spline, i/(segments - 1f));
		}).Definition("Evaluate Position").Run();
	}

	[Test, TestCaseSource(nameof(k_Splines)), Performance, Version("2")]
	public void CalculateSplineLength(string spline)
	{
		var container = GetContainer(spline);

		Measure.Method(() =>
		{
			SplineUtility.CalculateLength(container.Spline, float4x4.identity);
		}).Definition("Calculate Length", SampleUnit.Microsecond).Run();
	}

	[Test, TestCaseSource(nameof(k_Splines)), Performance, Version("1")]
	public void SplitCurve(string gameObject)
	{
		var spline = GetSpline(gameObject);

		Measure.Method(() =>
		{
			for(int i = 0, c = spline.Closed ? spline.KnotCount : spline.KnotCount - 1; i < c; ++i)
				CurveUtility.Split(spline.GetCurve(i), .5f, out _, out _);
		}).Definition("CurveUtility.SplitCurve Control Points", SampleUnit.Microsecond).Run();
	}

	[Test, TestCaseSource(nameof(k_Splines)), Performance, Version("1")]
	public void EvaluateTangent(string gameObject)
	{
		var spline = GetSpline(gameObject);

		Measure.Method(() =>
		{
			for(int i = 0, c = spline.Closed ? spline.KnotCount : spline.KnotCount - 1; i < c; ++i)
				CurveUtility.EvaluateTangent(spline.GetCurve(i), .5f);
		}).Definition("CurveUtility.EvaluateTangent", SampleUnit.Microsecond).Run();
	}

	[Test, TestCaseSource(nameof(k_Splines)), Performance, Version("1")]
	public void UnrollSpline(string gameObject)
	{
		var spline = GetSpline(gameObject);

		// Measure.Method(() =>
		// {
		// 	var localToWorld = float4x4.identity;
		// 	SplineUtility.GetLineStripFromSpline(spline, localToWorld, Allocator.TempJob, out NativeArray<float3> points).Complete();
		// 	points.Dispose();
		// }).Definition("SplineUtility.GetLineStripFromSpline", SampleUnit.Microsecond).Run();

		Measure.Method(() =>
		{
			for (int n = 0, c = spline.KnotCount; n < c; n++)
			{
				var curve = spline.GetCurve(n);

				for (int i = 0; i < 40; i++)
					CurveUtility.EvaluatePosition(curve, i / 39f);
			}

		}).Definition("EvaluatePosition (match GetLineStrip resolution)", SampleUnit.Microsecond).Run();
		
		Measure.Method(() =>
		{
			int segments = SplineUtility.GetSegmentCount(spline.GetLength(), SplineUtility.DrawResolutionDefault);
			float inv = 1f / (segments - 1);
			for (int i = 0; i < segments; i++)
				SplineUtility.EvaluatePosition(spline, i * inv);

		}).Definition("EvaluatePosition (dynamic resolution)", SampleUnit.Microsecond).Run();
	}
}
