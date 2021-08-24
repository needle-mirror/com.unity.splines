using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using NUnit.Framework;
using Unity.Mathematics;
using Is = NUnit.Framework.Is;

namespace UnityEngine.Splines.Tests
{
    public class SplineDataTests
    {
        static IEnumerable s_Default3KnotClosedSpline
        {
            get
            {
                var knotA = new BezierKnot(new float3(0f, 0f, 0f), new float3(-0.5f, 0f, 0f), new float3(0.5f, 0f, 0f), quaternion.identity);
                var knotB = new BezierKnot(new float3(2f, 0f, 0f), new float3(1.5f, 0f, 0f), new float3(2.5f, 0f, 0f), quaternion.identity);
                var knotC = new BezierKnot(new float3(1f, 2f, 0f), new float3(0.5f, 2f, 0f), new float3(1.5f, 2f, 0f), quaternion.identity);

                var spline = new Spline();

                spline.AddKnot(knotA);
                spline.AddKnot(knotB);
                spline.AddKnot(knotC);

                spline.Closed = true;

                yield return new TestCaseData(spline).SetName("3 knot closed spline");
            }
        }

        static IEnumerable s_DistancePathUnitTestCases
        {
            get
            {
                var knotA = new BezierKnot(new float3(0f, 0f, 0f), new float3(-0.5f, 0f, 0f), new float3(0.5f, 0f, 0f), quaternion.identity);
                var knotB = new BezierKnot(new float3(1f, 0f, 0f), new float3(0.5f, 0f, 0f), new float3(1.5f, 0f, 0f), quaternion.identity);
                var knotC = new BezierKnot(new float3(0.5f, 0f, 1f), new float3(1f, 0f, 0f), new float3(0f, 0f, 0f), quaternion.identity);

                var spline = new Spline();
                spline.AddKnot(knotA);
                spline.AddKnot(knotB);
                spline.AddKnot(knotC);
                spline.Closed = false;
                var splineDistance = spline.GetLength();

                var splineData = new SplineData<float>();
                splineData.PathIndexUnit = PathIndexUnit.Distance;
                splineData.Add(0f, 0f);
                splineData.Add(splineDistance * 0.5f, 1f);
                splineData.Add(splineDistance, 2f);
                splineData.Add(splineDistance * 1.5f, 3f);

                yield return new TestCaseData(spline, splineData, splineDistance * -1f).SetName("open spline: t = dist * -1.0, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, splineDistance * -0.5f).SetName("open spline: t = dist * -0.5, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, 0f).SetName("open spline: t = 0, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, splineDistance * 0.5f).SetName("open spline: t = dist * 0.5, value = 1").Returns(1f);
                yield return new TestCaseData(spline, splineData, splineDistance).SetName("open spline: t = dist, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, splineDistance * 1.5f).SetName("open spline: t = dist * 1.5, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, splineDistance * 2.0f).SetName("open spline: t = dist * 2.0, value = 2").Returns(2f);

                spline = new Spline();
                spline.AddKnot(knotA);
                spline.AddKnot(knotB);
                spline.AddKnot(knotC);
                spline.Closed = true;
                splineDistance = spline.GetLength();

                splineData = new SplineData<float>();
                splineData.PathIndexUnit = PathIndexUnit.Distance;
                splineData.Add(0f, 0f);
                splineData.Add(splineDistance * 0.5f, 1f);
                splineData.Add(splineDistance, 2f);
                splineData.Add(splineDistance * 1.5f, 3f);

                yield return new TestCaseData(spline, splineData, splineDistance * -1f).SetName("closed spline: t = dist * -1.0, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, splineDistance * -0.5f).SetName("closed spline: t = dist * -0.5, value = 3").Returns(3f);
                yield return new TestCaseData(spline, splineData, 0f).SetName("closed spline: t = 0, value = 0f").Returns(0f);
                yield return new TestCaseData(spline, splineData, splineDistance * 0.5f).SetName("closed spline: t = dist * 0.5, value = 1").Returns(1f);
                yield return new TestCaseData(spline, splineData, splineDistance).SetName("closed spline: t = dist, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, splineDistance * 1.5f).SetName("closed spline: t = dist * 1.5, value = 3").Returns(3f);
                yield return new TestCaseData(spline, splineData, splineDistance * 2.0f).SetName("closed spline: t = dist * 2.0, value = 0").Returns(0f);
            }
        }

        static IEnumerable s_NormalizedPathUnitTestCases
        {
            get
            {
                var knotA = new BezierKnot(new float3(0f, 0f, 0f), new float3(-0.5f, 0f, 0f), new float3(0.5f, 0f, 0f), quaternion.identity);
                var knotB = new BezierKnot(new float3(1f, 0f, 0f), new float3(0.5f, 0f, 0f), new float3(1.5f, 0f, 0f), quaternion.identity);
                var knotC = new BezierKnot(new float3(0.5f, 0f, 1f), new float3(1f, 0f, 0f), new float3(0f, 0f, 0f), quaternion.identity);

                var spline = new Spline();
                spline.AddKnot(knotA);
                spline.AddKnot(knotB);
                spline.AddKnot(knotC);
                spline.Closed = false;

                var splineData = new SplineData<float>();
                splineData.PathIndexUnit = PathIndexUnit.Normalized;
                splineData.Add(0f, 0f);
                splineData.Add(0.5f, 1f);
                splineData.Add(1.0f, 2f);
                splineData.Add(1.5f, 3f);

                yield return new TestCaseData(spline, splineData, -1f).SetName("open spline: t = -1.0, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, -0.5f).SetName("open spline: t = -0.5, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, 0f).SetName("open spline: t = 0, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, 0.5f).SetName("open spline: t = 0.5, value = 1").Returns(1f);
                yield return new TestCaseData(spline, splineData, 1.0f).SetName("open spline: t = 1.0, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, 1.5f).SetName("open spline: t = 1.5, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, 2.0f).SetName("open spline: t = 2.0, value = 2").Returns(2f);

                spline = new Spline();
                spline.AddKnot(knotA);
                spline.AddKnot(knotB);
                spline.AddKnot(knotC);
                spline.Closed = true;

                yield return new TestCaseData(spline, splineData, -1f).SetName("closed spline: t = -1.0, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, -0.5f).SetName("closed spline: t = -0.5, value = 3").Returns(3f);
                yield return new TestCaseData(spline, splineData, 0f).SetName("closed spline: t = 0, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, 0.5f).SetName("closed spline: t = 0.5, value = 1").Returns(1f);
                yield return new TestCaseData(spline, splineData, 1.0f).SetName("closed spline: t = 1.0, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, 1.5f).SetName("closed spline: t = 1.5, value = 3").Returns(3f);
                yield return new TestCaseData(spline, splineData, 2.0f).SetName("closed spline: t = 2.0, value = 0").Returns(0f);
            }
        }

        static IEnumerable s_KnotPathUnitTestCases
        {
            get
            {
                var knotA = new BezierKnot(new float3(0f, 0f, 0f), new float3(-0.5f, 0f, 0f), new float3(0.5f, 0f, 0f), quaternion.identity);
                var knotB = new BezierKnot(new float3(1f, 0f, 0f), new float3(0.5f, 0f, 0f), new float3(1.5f, 0f, 0f), quaternion.identity);
                var knotC = new BezierKnot(new float3(0.5f, 0f, 1f), new float3(1f, 0f, 0f), new float3(0f, 0f, 0f), quaternion.identity);

                var spline = new Spline();
                spline.AddKnot(knotA);
                spline.AddKnot(knotB);
                spline.AddKnot(knotC);
                spline.Closed = false;

                var splineData = new SplineData<float>();
                splineData.PathIndexUnit = PathIndexUnit.Knot;
                splineData.Add(0f, 0f);
                splineData.Add(0.5f, 1f);
                splineData.Add(2.0f, 2f);
                splineData.Add(2.5f, 3f);

                yield return new TestCaseData(spline, splineData, -1f).SetName("open spline: t = -1.0, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, -0.5f).SetName("open spline: t = -0.5, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, 0f).SetName("open spline: t = 0, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, 0.5f).SetName("open spline: t = 0.5, value = 1").Returns(1f);
                yield return new TestCaseData(spline, splineData, 2.0f).SetName("open spline: t = 2.0, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, 2.5f).SetName("open spline: t = 2.5, value = 2").Returns(2f);

                spline = new Spline();
                spline.AddKnot(knotA);
                spline.AddKnot(knotB);
                spline.AddKnot(knotC);
                spline.Closed = true;

                yield return new TestCaseData(spline, splineData, -1f).SetName("open spline: t = -1.0, value = 0").Returns(2f);
                yield return new TestCaseData(spline, splineData, -0.5f).SetName("open spline: t = -0.5, value = 3").Returns(3f);
                yield return new TestCaseData(spline, splineData, 0f).SetName("open spline: t = 0, value = 0").Returns(0f);
                yield return new TestCaseData(spline, splineData, 0.5f).SetName("open spline: t = 0.5, value = 1").Returns(1f);
                yield return new TestCaseData(spline, splineData, 2.0f).SetName("open spline: t = 2.0, value = 2").Returns(2f);
                yield return new TestCaseData(spline, splineData, 2.5f).SetName("open spline: t = 2.5, value = 2").Returns(3f);
            }
        }

        [Test]
        public void AddKeyframe_KeyframeAdded()
        {
            var splineData = new SplineData<float>();

            Assume.That(splineData.Count, Is.EqualTo(0));
            splineData.Add(0f, 0f);
            Assert.That(splineData.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveKeyframe_KeyframeRemoved()
        {
            var splineData = new SplineData<float>();

            splineData.Add(0f, 0f);
            Assume.That(splineData.Count, Is.EqualTo(1));
            splineData.RemoveAt(0);
            Assert.That(splineData.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClearKeyframes_SplineDataEmpty()
        {
            var splineData = new SplineData<float>();

            splineData.Add(0f, 0f);
            splineData.Add(1f, 1f);
            splineData.Add(2f, 2f);
            Assume.That(splineData.Count, Is.EqualTo(3));
            splineData.Clear();
            Assert.That(splineData.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddMultipleKeyframes_AtMatchingPathUnitIndices_KeyframesDoNotOverwrite()
        {
            var splineData = new SplineData<float>();

            Assume.That(splineData.Count, Is.EqualTo(0));
            splineData.Add(0f, 0f);
            splineData.Add(0f, 0.5f);
            splineData.Add(0f, 1.0f);
            Assert.That(splineData.Count, Is.EqualTo(3));
        }

        [Test]
        public void RemoveKey_AtNegativeIndex_Throws()
        {
            var splineData = new SplineData<float>();
            splineData.Add(0f, 0f);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                splineData.RemoveAt(-1);
            });
        }

        [Test]
        public void RemoveKey_AtIndexAboveUpperBound_Throws()
        {
            var splineData = new SplineData<float>();
            splineData.Add(0f, 0f);

            Assume.That(splineData.Count, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                splineData.RemoveAt(1);
            });
        }

        [Test]
        public void SetKeyframe_ReplacesPreviousKeyframe()
        {
            var keyframeA = new Keyframe<float>(0f, 0f);
            var keyframeB = new Keyframe<float>(1f, 0f);

            Assume.That(keyframeA, Is.Not.EqualTo(keyframeB));

            var splineData = new SplineData<float>();
            splineData.Add(keyframeA);
            Assume.That(splineData.Count, Is.EqualTo(1));
            Assume.That(splineData[0], Is.EqualTo(keyframeA));

            splineData.SetKeyframe(0, keyframeB);

            Assert.That(splineData[0], Is.EqualTo(keyframeB));
        }

        [Test]
        public void SetKeyframe_AtNegativeIndex_Throws()
        {
            var keyframeA = new Keyframe<float>(0f, 0f);
            var keyframeB = new Keyframe<float>(1f, 0f);

            var splineData = new SplineData<float>();
            splineData.Add(keyframeA);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                splineData.SetKeyframe(-1, keyframeB);
            });
        }

        [Test]
        public void SetKeyframe_AtIndexAboveUpperBound_Throws()
        {
            var keyframeA = new Keyframe<float>(0f, 0f);
            var keyframeB = new Keyframe<float>(1f, 0f);

            var splineData = new SplineData<float>();
            splineData.Add(keyframeA);

            Assume.That(splineData.Count, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                splineData.SetKeyframe(1, keyframeB);
            });
        }

        [Test, TestCaseSource(nameof(s_DistancePathUnitTestCases))]
        public float Evaluate_GivenDistanceIndexSplineData_ReturnsExpectedValue(Spline spline,
            SplineData<float> splineData, float pathUnitIndex)
        {
            using (var nativeSpline = spline.ToNativeSpline())
            {
                return splineData.Evaluate(nativeSpline, pathUnitIndex, new Interpolators.LerpFloat());
            }
        }

        [Test, TestCaseSource(nameof(s_NormalizedPathUnitTestCases))]
        public float Evaluate_GivenNormalizedIndexSplineData_ReturnsExpectedValue(Spline spline,
            SplineData<float> splineData, float pathUnitIndex)
        {
            using (var nativeSpline = spline.ToNativeSpline())
            {
                return splineData.Evaluate(nativeSpline, pathUnitIndex, new Interpolators.LerpFloat());
            }
        }

        [Test, TestCaseSource(nameof(s_KnotPathUnitTestCases))]
        public float Evaluate_GivenKnotIndexSplineData_ReturnsExpectedValue(Spline spline,
            SplineData<float> splineData, float pathUnitIndex)
        {
            using (var nativeSpline = spline.ToNativeSpline())
            {
                return splineData.Evaluate(nativeSpline, pathUnitIndex, new Interpolators.LerpFloat());
            }
        }

        [Test, TestCaseSource(nameof(s_Default3KnotClosedSpline))]
        public void Evaluate_DoesNotAllocateGCMemory(Spline spline)
        {
            var splineData = new SplineData<float>();
            splineData.Add(0f, 0f);
            splineData.Add(1f, 1f);

            using (var nativeSpline = spline.ToNativeSpline())
            {
                splineData.Evaluate(nativeSpline, 0.5f, new Interpolators.LerpFloat());

                Assert.That(() =>
                    {
                        splineData.Evaluate(nativeSpline, 0.5f, new Interpolators.LerpFloat());
                    },
                    Is.Not.AllocatingGCMemory());
            }
        }
    }
}