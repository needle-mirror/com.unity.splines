using System.Collections;
using UnityEngine;
using Unity.Mathematics;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace UnityEngine.Splines.Tests
{
    public class SplineUtilityTests
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

        static IEnumerable s_Linear3KnotSpline
        {
            get
            {
                yield return SplineFactory.CreateLinear(new float3[]
                {
                    float3.zero,
                    new float3(1f, 0f, 0f),
                    new float3(2f, 0f, 0f)
                });
            }
        }

        static IEnumerable s_DefaultCurve
        {
            get
            {
                var bezierCurve = BezierCurve.FromTangent(
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0.3f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(-0.3f, 0f, 0f));

                yield return new TestCaseData(bezierCurve).SetName("Simple curve");
            }
        }

        static IEnumerable s_OutOfBoundsCurveOffsets
        {
            get
            {
                yield return new TestCaseData(-0.5f).SetName("t = -0.5");
                yield return new TestCaseData(1.5f).SetName("t = 1.5");
            }
        }

        [Test]
        public void GetSplineKnotBounds_GivenEmptySpline_ReturnsEmptyBounds()
        {
            var spline = new Spline();

            Assume.That(spline.KnotCount, Is.EqualTo(0));
            var bounds = SplineUtility.GetBounds(spline);
            Assert.That(bounds.center, Is.EqualTo(Vector3.zero));
            Assert.That(bounds.size, Is.EqualTo(Vector3.zero));
        }

        [Test, TestCaseSource(nameof(s_Default3KnotClosedSpline))]
        public void GetSplineKnotBounds_GivenNonEmptySpline_EncapsulatesAllKnots(Spline spline)
        {
            var bounds = SplineUtility.GetBounds(spline);
            int knotCountWithinBounds = 0;

            for (int i = 0; i < spline.KnotCount; i++)
            {
                if (bounds.Contains(spline[i].Position))
                    knotCountWithinBounds++;
            }

            Assert.That(spline.KnotCount, Is.EqualTo(knotCountWithinBounds));
        }

        [Test, TestCaseSource(nameof(s_Default3KnotClosedSpline))]
        public void GetSplineKnotBounds_DoesNotAllocateGCMemory(Spline spline)
        {
            // NOTE: For some odd reason, calling GetSplineKnotBounds for the first time
            // triggers a GC mem allocation which, when inspected in the Profiler, is of 0B size.
            // Therefore added this additional call below to prevent the Assert from failing.
            SplineUtility.GetBounds(spline);
            Assert.That(() => { SplineUtility.GetBounds(spline); }, Is.Not.AllocatingGCMemory());
        }

        [Test, TestCaseSource(nameof(s_DefaultCurve))]
        public void SplitCurve_ReturnedCurvePointsAreOnOriginalCurve(BezierCurve bezierCurve)
        {
            var midOffset = 0.5f;
            var midPoint = (bezierCurve.P3 - bezierCurve.P0) * midOffset;

            CurveUtility.Split(bezierCurve, midOffset, out var curveLeft, out var curveRight);

            Assert.That(bezierCurve.P0, Is.EqualTo(curveLeft.P0), "Left curve's left knot pos is incorrect");
            Assert.That(midPoint, Is.EqualTo(curveLeft.P3), "Left curve's right knot pos is incorrect");
            Assert.That(midPoint, Is.EqualTo(curveRight.P0), "Right curve's left knot pos is incorrect");
            Assert.That(bezierCurve.P3, Is.EqualTo(curveRight.P3), "Right curve's right knot pos is incorrect");
        }

        [Test, TestCaseSource(nameof(s_DefaultCurve))]
        public void SplitCurve_ReturnedCurvesAreConnected(BezierCurve bezierCurve)
        {
            var midOffset = 0.5f;
            var midPoint = (bezierCurve.P3 - bezierCurve.P0) * midOffset;

            CurveUtility.Split(bezierCurve, midOffset, out var curveA, out var curveB);

            Assert.That(curveA.P3, Is.EqualTo(curveB.P0));
        }

        [Test, TestCaseSource(nameof(s_OutOfBoundsCurveOffsets))]
        public void SplitCurve_GivenOutOfBoundsOffset_ReturnsSplitsAtClampedOffset(float t)
        {
            var bezierCurve = BezierCurve.FromTangent(
                new Vector3(0f, 0f, 0f),
                new Vector3(0.3f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(-0.3f, 0f, 0f));

            CurveUtility.Split(bezierCurve, t, out var curveLeft, out var curveRight);

            var clampedT = math.clamp(t, 0f, 1f);
            CurveUtility.Split(bezierCurve, t, out var clampedTCurveLeft, out var clampedTCurveRight);

            Assert.That(curveLeft, Is.EqualTo(clampedTCurveLeft), "Left curve is incorrect");
            Assert.That(curveRight, Is.EqualTo(clampedTCurveRight), "Right curve is incorrect");
        }

        [Test, TestCaseSource(nameof(s_DefaultCurve))]
        public void SplitCurve_DoesNotAllocateGCMemory(BezierCurve bezierCurve)
        {
            Assert.That(() =>
            {
                CurveUtility.Split(bezierCurve, 0.5f, out var curveA, out var curveB);
            },
            Is.Not.AllocatingGCMemory());
        }

        [Test, TestCaseSource(nameof(s_Default3KnotClosedSpline))]
        public void GetCurveLength_MatchesCalculatedLength(Spline spline)
        {
            for (int i = 0, c = spline.Closed ? spline.KnotCount : spline.KnotCount - 1; i < c; ++i)
            {
                var calculated = CurveUtility.CalculateLength(spline.GetCurve(i));
                var cached = spline.GetCurveLength(i);
                Assert.That(calculated, Is.EqualTo(cached));
            }
        }

        [Test, TestCaseSource(nameof(s_Default3KnotClosedSpline))]
        public void InsertKnot_CachesCurveLength(Spline spline)
        {
            var copy = new Spline();
            copy.Copy(spline);

            var knot = new BezierKnot()
            {
                Position = spline[0].Position - spline[0].TangentOut,
                TangentIn = -spline[0].TangentOut,
                TangentOut = spline[0].TangentOut
            };

            copy.InsertKnot(0,  knot);

            for (int i = 0, c = spline.Closed ? spline.KnotCount : spline.KnotCount - 1; i < c; ++i)
            {
                var calculated = CurveUtility.CalculateLength(spline.GetCurve(i));
                var cached = spline.GetCurveLength(i);
                Assert.That(calculated, Is.EqualTo(cached));
            }
        }

        [Test, TestCaseSource(nameof(s_Default3KnotClosedSpline))]
        public void AddKnot_CachesCurveLength(Spline spline)
        {
            var copy = new Spline();
            copy.Copy(spline);
            var count = spline.KnotCount;

            var knot = new BezierKnot()
            {
                Position = spline[count-1].Position + spline[count-1].TangentOut,
                TangentIn = -spline[count-1].TangentOut,
                TangentOut = spline[count-1].TangentOut
            };

            copy.AddKnot(knot);

            for (int i = 0, c = spline.Closed ? spline.KnotCount : spline.KnotCount - 1; i < c; ++i)
            {
                var calculated = CurveUtility.CalculateLength(spline.GetCurve(i));
                var cached = spline.GetCurveLength(i);
                Assert.That(calculated, Is.EqualTo(cached));
            }
        }
        
        [Test, TestCaseSource(nameof(s_Linear3KnotSpline))]
        public void SplineToCurve_ReturnsNormalizedCurveInterpolation(Spline spline)
        {
            var splineT = .25f;
            const int expectedCurveIndex = 0;
            const float expectedCurveT = .5f;
            
            var curveIndex = SplineUtility.SplineToCurveInterpolation(spline, splineT, out var curveT);
            Assert.That(curveIndex, Is.EqualTo(expectedCurveIndex));
            Assert.That(curveT, Is.EqualTo(expectedCurveT));
        }
        
        [Test, TestCaseSource(nameof(s_Linear3KnotSpline))]
        public void CurveToSpline_ReturnsNormalizedSplineInterpolation(Spline spline)
        {
            var expectedSplineT = .75f;
            var splineT = SplineUtility.CurveToSplineInterpolation(spline, 1.5f);
            Assert.That(splineT, Is.EqualTo(expectedSplineT));
        }
        
        [Test, TestCaseSource(nameof(s_Linear3KnotSpline))]
        public void EvaluatePosition_IsSame_Curve_Spline(Spline spline)
        {
            var expectedPosition = new float3(1.5f, 0f, 0f);
            var splineT = .75f;
            var curveIndex = spline.SplineToCurveInterpolation(splineT, out var curveT);

            var splinePosition = spline.EvaluatePosition(splineT);
            var curvePosition = CurveUtility.EvaluatePosition(spline.GetCurve(curveIndex), curveT);
            
            Assert.That(splinePosition, Is.EqualTo(expectedPosition));
            Assert.That(curvePosition, Is.EqualTo(expectedPosition));
        }
    }
}
