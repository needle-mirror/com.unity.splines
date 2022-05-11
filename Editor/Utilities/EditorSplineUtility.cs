using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.SettingsManagement;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    struct SplineCurveHit
    {
        public float T;
        public float3 Normal;
        public float3 Position;
        public SelectableKnot PreviousKnot;
        public SelectableKnot NextKnot;
    }

    /// <summary>
    /// Editor utility functions for working with <see cref="Spline"/> and <see cref="SplineData{T}"/>.
    /// </summary>
    public static class EditorSplineUtility
    {
        /// <summary>
        /// Invoked once per-frame if a spline property has been modified.
        /// </summary>
        [Obsolete("Use AfterSplineWasModified instead.", false)]
        public static event Action<Spline> afterSplineWasModified;
        /// <summary>
        /// Invoked once per-frame if a spline property has been modified.
        /// </summary>
        public static event Action<Spline> AfterSplineWasModified;

        static readonly List<SplineInfo> s_SplinePtrBuffer = new List<SplineInfo>(16);

        internal static event Action<SelectableKnot> knotInserted;
        internal static event Action<SelectableKnot> knotRemoved;

        [UserSetting]
        internal static Pref<TangentMode> s_DefaultTangentMode = new ("Splines.DefaultTangentMode", TangentMode.AutoSmooth);

        /// <summary>
        /// Represents the default TangentMode used to place or insert knots. If the user does not define tangent
        /// handles, then the tangent takes the default TangentMode.
        /// </summary>
        public static TangentMode DefaultTangentMode => s_DefaultTangentMode;

        static EditorSplineUtility()
        {
            Spline.afterSplineWasModified += (spline) =>
            {
                afterSplineWasModified?.Invoke(spline);
                AfterSplineWasModified?.Invoke(spline);
            };
        }

        /// <summary>
        /// Use this function to register a callback that gets invoked
        /// once per-frame if any <see cref="SplineData{T}"/> changes occur.
        /// </summary>
        /// <param name="action">The callback to register.</param>
        /// <typeparam name="T">
        /// The type parameter of <see cref="SplineData{T}"/>.
        /// </typeparam>
        public static void RegisterSplineDataChanged<T>(Action<SplineData<T>> action)
        {
            SplineData<T>.afterSplineDataWasModified += action;
        }

        /// <summary>
        /// Use this function to unregister <see cref="SplineData{T}"/> change callback.
        /// </summary>
        /// <param name="action">The callback to unregister.</param>
        /// <typeparam name="T">
        /// The type parameter of <see cref="SplineData{T}"/>.
        /// </typeparam>
        public static void UnregisterSplineDataChanged<T>(Action<SplineData<T>> action)
        {
            SplineData<T>.afterSplineDataWasModified -= action;
        }

        internal static IReadOnlyList<SplineInfo> GetSplinesFromTargetsInternal(IEnumerable<Object> targets)
        {
            GetSplinesFromTargets(targets, s_SplinePtrBuffer);
            return s_SplinePtrBuffer;
        }

        /// <summary>
        /// Get a <see cref="SplineInfo"/> representation of the splines in a list of targets.
        /// </summary>
        /// <param name="targets">A list of Objects inheriting from <see cref="ISplineContainer"/>.</param>
        /// <returns>An array to store the <see cref="SplineInfo"/> of splines found in the targets.</returns>
        internal static SplineInfo[] GetSplinesFromTargets(IEnumerable<Object> targets)
        {
            return GetSplinesFromTargetsInternal(targets).ToArray();
        }

        /// <summary>
        /// Get a <see cref="SplineInfo"/> representation of the splines in a list of targets.
        /// </summary>
        /// <param name="targets">A list of Objects inheriting from <see cref="ISplineContainer"/>.</param>
        /// <param name="results">An array to store the <see cref="SplineInfo"/> of splines found in the targets.</param>
        internal static void GetSplinesFromTargets(IEnumerable<Object> targets, List<SplineInfo> results)
        {
            results.Clear();
            foreach (var target in targets)
            {
                GetSplineInfosFromContainer(target, results);
            }
        }

        /// <summary>
        /// Get a <see cref="SplineInfo"/> representation of the splines in a target.
        /// </summary>
        /// <param name="target">An Object inheriting from <see cref="ISplineContainer"/>.</param>
        /// <param name="results">An array to store the <see cref="SplineInfo"/> of splines found in the targets.</param>
        internal static void GetSplinesFromTarget(Object target, List<SplineInfo> results)
        {
            results.Clear();
            GetSplineInfosFromContainer(target, results);
        }

        /// <summary>
        /// Get a <see cref="SplineInfo"/> representation of the splines in a target.
        /// </summary>
        /// <param name="target">An Object inheriting from <see cref="ISplineContainer"/>.</param>
        /// <returns>An array to store the <see cref="SplineInfo"/> of splines found in the target.</returns>
        internal static SplineInfo[] GetSplinesFromTarget(Object target)
        {
            GetSplinesFromTarget(target, s_SplinePtrBuffer);
            return s_SplinePtrBuffer.ToArray();
        }

        /// <summary>
        /// Get a <see cref="SplineInfo"/> representation of the first spline found in the target.
        /// </summary>
        /// <param name="target">An Object inheriting from <see cref="ISplineContainer"/>.</param>
        /// <param name="splineInfo">The <see cref="SplineInfo"/> of the first spline found in the target.</param>
        /// <returns>True if a spline was found in the target.</returns>
        internal static bool TryGetSplineFromTarget(Object target, out SplineInfo splineInfo)
        {
            GetSplinesFromTarget(target, s_SplinePtrBuffer);
            if (s_SplinePtrBuffer.Count > 0)
            {
                splineInfo = s_SplinePtrBuffer[0];
                return true;
            }

            splineInfo = default;
            return false;
        }

        static void GetSplineInfosFromContainer(Object target, List<SplineInfo> results)
        {
            if (target != null && target is ISplineContainer container)
            {
                var splines = container.Splines;
                for (int i = 0; i < splines.Count; ++i)
                    results.Add(new SplineInfo(container, i));
            }
        }

        internal static Bounds GetBounds<T>(IReadOnlyList<T> elements, bool useKnotPositionForTangents)
            where T : ISplineElement
        {
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            if (elements.Count == 0)
                return new Bounds(Vector3.positiveInfinity, Vector3.zero);

            var element = elements[0];

            var position = (useKnotPositionForTangents && element is SelectableTangent tangent)
                ? tangent.Owner.Position
                : element.Position;

            Bounds bounds = new Bounds(position, Vector3.zero);
            for (int i = 1; i < elements.Count; ++i)
            {
                element = elements[i];
                if (useKnotPositionForTangents && element is SelectableTangent t)
                    bounds.Encapsulate(t.Owner.Position);
                else
                    bounds.Encapsulate(element.Position);
            }

            return bounds;
        }

        internal static SelectableKnot GetKnot<T>(T element)
            where T : ISplineElement
        {
            return new SelectableKnot(element.SplineInfo, element.KnotIndex);
        }

        internal static void RecordSelection(string name)
        {
            Undo.RecordObjects(SplineSelection.GetAllSelectedTargets(), name);
        }

        internal static void RecordObjects<T>(IReadOnlyList<T> elements, string name)
            where T : ISplineElement
        {
            foreach (var spline in GetSplines(elements))
                RecordObject(spline, name);
        }

        internal static void RecordObject(SplineInfo splineInfo, string name)
        {
            if (splineInfo.Container is Object target && target != null)
                Undo.RecordObject(target, name);
        }

        internal static SplineInfo CreateSpline(ISplineContainer container)
        {
            container.AddSpline();
            return new SplineInfo(container, container.Splines.Count - 1);
        }

        internal static SelectableKnot CreateSpline(SelectableKnot from, float3 tangentOut)
        {
            var splineInfo = CreateSpline(from.SplineInfo.Container);
            var knot = AddKnotToTheEnd(splineInfo, from.Position, math.mul(from.Rotation, math.up()), tangentOut);
            LinkKnots(knot, from);
            return knot;
        }

        internal static TangentMode GetModeFromPlacementTangent(float3 tangent)
        {
            return math.lengthsq(tangent) < float.Epsilon ? DefaultTangentMode : TangentMode.Mirrored;
        }

        static SelectableKnot AddKnotToTheEndInternal(SplineInfo splineInfo, float3 worldPosition, float3 normal, float3 tangentOut, int index, int previousIndex)
        {
            var spline = splineInfo.Spline;

            if (spline.Closed)
                throw new ArgumentException("Cannot add a point to the end of a closed spline", nameof(spline));

            var localToWorld = splineInfo.LocalToWorld;
            var mode = GetModeFromPlacementTangent(tangentOut);

            var localPosition = math.transform(math.inverse(splineInfo.LocalToWorld), worldPosition);
            quaternion localRotation;
            BezierKnot newKnot;

            // If we're in AutoSmooth mode
            if (!AreTangentsModifiable(mode))
                newKnot = SplineUtility.GetAutoSmoothKnot(localPosition, previousIndex != -1 ? spline[previousIndex].Position : localPosition, localPosition, normal);
            else
            {
                localRotation = math.mul(math.inverse(math.quaternion(localToWorld)), quaternion.LookRotationSafe(tangentOut, normal));
                var tangentMagnitude = math.length(tangentOut);
                // Tangents are always assumed to be +/- forward when TangentMode is not Broken.
                var localTangentIn = new float3(0f, 0f, -tangentMagnitude);
                var localTangentOut = new float3(0f, 0f, tangentMagnitude);
                newKnot = new BezierKnot(localPosition, localTangentIn, localTangentOut, localRotation);
            }

            spline.Insert(index, newKnot, mode);

            // When appending a knot, update the previous knot with an average rotation accounting for the new point.
            // This is assuming that if the previous knot is Continuous the rotation was explicitly set, and thus will
            // not update the rotation.
            if (spline.Count > 1 && !AreTangentsModifiable(spline.GetTangentMode(previousIndex)))
            {
                // calculate rotation from the average direction from points p0 -> p1 -> p2
                BezierKnot current = spline[previousIndex];
                BezierKnot previous = spline.Previous(previousIndex);
                BezierKnot next = spline.Next(previousIndex);

                current.Rotation = CalculateKnotRotation(previous.Position, current.Position, next.Position, normal);
                spline[previousIndex] = current;
            }

            var knot = new SelectableKnot(splineInfo, spline.Count - 1);
            SplineSelection.Set(knot);

            return knot;
        }

        internal static SelectableKnot AddKnotToTheEnd(SplineInfo splineInfo, float3 worldPosition, float3 normal, float3 tangentOut)
        {
            return AddKnotToTheEndInternal(splineInfo, worldPosition, normal, tangentOut, splineInfo.Spline.Count, splineInfo.Spline.Count - 1);
        }

        internal static SelectableKnot AddKnotToTheStart(SplineInfo splineInfo, float3 worldPosition, float3 normal, float3 tangentIn)
        {
            return AddKnotToTheEndInternal(splineInfo, worldPosition, normal, -tangentIn, 0, 1);
        }

        internal static void RemoveKnot(SelectableKnot knot)
        {
            knot.SplineInfo.Spline.RemoveAt(knot.KnotIndex);

            // If the spline has no knots, remove
            if (knot.SplineInfo.Spline.Count == 0)
                knot.SplineInfo.Container.RemoveSplineAt(knot.SplineInfo.Index);

            knotRemoved?.Invoke(knot);
        }

        // Calculate curve control points in world space given a new end knot.
        internal static BezierCurve GetPreviewCurve(SplineInfo info, int a, int b, float3 lastWorldPoint)
        {
            var spline = info.Spline;
            var trs = info.Transform.localToWorldMatrix;

            var aMode = spline.GetTangentMode(a);
            var bMode = spline.GetTangentMode(b);

            var p0 = spline[a].Position;
            var p1 = p0 + math.mul(spline[a].Rotation, spline[a].TangentOut);
            var p3 = spline[b].Position;
            var p2 = p3 + math.mul(spline[b].Rotation, spline[b].TangentIn);

            if (!AreTangentsModifiable(aMode))
                p1 = aMode == TangentMode.Linear
                    ? p0
                    : p0 + SplineUtility.GetCatmullRomTangent(spline.Previous(a).Position, p3);

            if (!AreTangentsModifiable(bMode))
            {
                var p3Next = b >= spline.Count - 1 ? math.transform(math.inverse(trs), lastWorldPoint) : spline.Next(b).Position;
                p2 = bMode == TangentMode.Linear
                    ? p3
                    : p3 + SplineUtility.GetCatmullRomTangent(p3Next, p0);
            }

            return new BezierCurve(p0, p1, p2, p3).Transform(trs);
        }

        static BezierCurve GetPreviewCurveInternal(SplineInfo info, int from, float3 fromWorldTangent, float3 toWorldPoint, float3 toWorldTangent, TangentMode toMode, int previousIndex)
        {
            var spline = info.Spline;
            var trs = info.Transform.localToWorldMatrix;

            var aMode = spline.GetTangentMode(from);
            var bMode = toMode;

            var p0 = math.transform(trs, spline[from].Position);
            var p1 = math.transform(trs, spline[from].Position + math.mul(spline[from].Rotation, fromWorldTangent));
            var p3 = toWorldPoint;
            var p2 = p3 - toWorldTangent;

            if (!AreTangentsModifiable(aMode))
                p1 = aMode == TangentMode.Linear ? p0 : p0 + SplineUtility.GetCatmullRomTangent(math.transform(trs, spline[previousIndex].Position), p3);

            if (!AreTangentsModifiable(bMode))
                p2 = bMode == TangentMode.Linear ? p3 : p3 - SplineUtility.GetCatmullRomTangent(p0, p3);

            return new BezierCurve(p0, p1, p2, p3);
        }

        // Calculate the curve control points in world space given a new end knot.
        internal static BezierCurve GetPreviewCurveFromEnd(SplineInfo info, int from, float3 toWorldPoint, float3 toWorldTangent, TangentMode toMode)
        {
            var tangentOut = info.Spline[from].TangentOut;
            if(info.Spline.Closed && (from == 0 || AreKnotLinked( new SelectableKnot(info, from), new SelectableKnot(info, 0))))
            {
                var fromKnot = info.Spline[from];
                tangentOut = -fromKnot.TangentIn;
            }

            return GetPreviewCurveInternal(info, from, tangentOut, toWorldPoint, toWorldTangent, toMode, info.Spline.PreviousIndex(from));
        }

        // Calculate the curve control points in world space given a new start knot.
        internal static BezierCurve GetPreviewCurveFromStart(SplineInfo info, int from, float3 toWorldPoint, float3 toWorldTangent, TangentMode toMode)
        {
            var tangentIn = info.Spline[from].TangentIn;
            if(info.Spline.Closed && (from == info.Spline.Count - 1  || AreKnotLinked( new SelectableKnot(info, from), new SelectableKnot(info, info.Spline.Count - 1))))
            {
                var fromKnot = info.Spline[from];
                tangentIn = -fromKnot.TangentOut;
            }

            return GetPreviewCurveInternal(info, from, tangentIn, toWorldPoint, toWorldTangent, toMode, info.Spline.NextIndex(from));
        }

        internal static quaternion CalculateKnotRotation(float3 previous, float3 position, float3 next, float3 normal)
        {
            float3 tangent = new float3(0f, 0f, 1f);
            bool hasPrevious = math.distancesq(position, previous) > float.Epsilon;
            bool hasNext = math.distancesq(position, next) > float.Epsilon;

            if (hasPrevious && hasNext)
                tangent = ((position - previous) + (next - position)) * 5f;
            else if (hasPrevious)
                tangent = position - previous;
            else if (hasNext)
                tangent = next - position;

            return SplineUtility.GetKnotRotation(tangent, normal);
        }

        internal static SelectableKnot InsertKnot(SplineInfo splineInfo, int index, float t)
        {
            var spline = splineInfo.Spline;
            if (spline == null || index < 0)
                return default;

            var curveToSplit = new BezierCurve(spline[SplineUtility.PreviousIndex(index, spline.Count, spline.Closed)], spline[index]);
            CurveUtility.Split(curveToSplit, t, out var leftCurve, out var rightCurve);

            spline.Insert(index, default);

            var previousIndex = SplineUtility.PreviousIndex(index, spline.Count, spline.Closed);
            var nextIndex = SplineUtility.NextIndex(index, spline.Count, spline.Closed);
            var previous = spline[previousIndex];
            var next = spline[nextIndex];

            if (spline.GetTangentMode(previousIndex) == TangentMode.Mirrored)
                spline.SetTangentMode(previousIndex, TangentMode.Continuous);

            if (spline.GetTangentMode(nextIndex) == TangentMode.Mirrored)
                spline.SetTangentMode(nextIndex, TangentMode.Continuous);

            previous.TangentOut = math.mul(math.inverse(previous.Rotation), leftCurve.Tangent0);
            next.TangentIn = math.mul(math.inverse(next.Rotation), rightCurve.Tangent1);

            var up = CurveUtility.EvaluateUpVector(curveToSplit, t, math.rotate(previous.Rotation, math.up()), math.rotate(next.Rotation, math.up()));
            var rotation = quaternion.LookRotationSafe(math.normalizesafe(rightCurve.Tangent0), up);
            var inverseRotation = math.inverse(rotation);

            spline[previousIndex] = previous;
            spline[nextIndex] = next;
            spline[index] = new BezierKnot(leftCurve.P3, math.mul(inverseRotation, leftCurve.Tangent1), math.mul(inverseRotation, rightCurve.Tangent0), rotation);

            var knot = new SelectableKnot(splineInfo, index);
            knotInserted?.Invoke(knot);
            return knot;
        }

        internal static bool AreTangentsModifiable(TangentMode mode)
        {
            return mode == TangentMode.Broken || mode == TangentMode.Continuous || mode == TangentMode.Mirrored;
        }

        internal static SplineKnotIndex GetIndex(SelectableKnot knot)
        {
            return new SplineKnotIndex(knot.SplineInfo.Index, knot.KnotIndex);
        }

        internal static void GetKnotLinks(SelectableKnot knot, List<SelectableKnot> knots)
        {
            var container = knot.SplineInfo.Container;
            knots.Clear();

            var linkedKnots = container.KnotLinkCollection.GetKnotLinks(new SplineKnotIndex(knot.SplineInfo.Index, knot.KnotIndex));
            foreach (var index in linkedKnots)
                knots.Add(new SelectableKnot(new SplineInfo(container, index.Spline), index.Knot));
        }

        internal static void LinkKnots(IReadOnlyList<SelectableKnot> knots)
        {
            for (int i = 0; i < knots.Count; ++i)
            {
                var knot = knots[i];
                var container = knot.SplineInfo.Container;
                var spline = knot.SplineInfo.Spline;
                var splineKnotIndex = new SplineKnotIndex() { Spline = knot.SplineInfo.Index, Knot = knot.KnotIndex };

                for (int j = i + 1; j < knots.Count; ++j)
                {
                    var otherKnot = knots[j];
                    // Do not link knots from different containers
                    if (otherKnot.SplineInfo.Container != container)
                      continue;

                    var otherSplineInfo = otherKnot.SplineInfo;
                    // Do not link same knots
                    if (otherSplineInfo.Spline == spline && otherKnot.KnotIndex == knot.KnotIndex)
                    continue;

                    var otherSplineKnotIndex = new SplineKnotIndex() { Spline = otherKnot.SplineInfo.Index, Knot = otherKnot.KnotIndex };

                    RecordObject(knot.SplineInfo, "Link Knots");

                    container.KnotLinkCollection.Link(splineKnotIndex, otherSplineKnotIndex);
                    container.SetLinkedKnotPosition(splineKnotIndex);
                }
            }
        }

        internal static void UnlinkKnots(IReadOnlyList<SelectableKnot> knots)
        {
            foreach (var knot in knots)
            {
                var container = knot.SplineInfo.Container;
                var splineKnotIndex = new SplineKnotIndex() { Spline = knot.SplineInfo.Index, Knot = knot.KnotIndex };

                RecordObject(knot.SplineInfo, "Unlink Knots");
                container.KnotLinkCollection.Unlink(splineKnotIndex);
            }
        }

        internal static void LinkKnots(SelectableKnot a, SelectableKnot b)
        {
            var containerA = a.SplineInfo.Container;
            var containerB = b.SplineInfo.Container;

            if (containerA != containerB)
                return;

            containerA.KnotLinkCollection.Link(GetIndex(a), GetIndex(b));
        }

        internal static bool AreKnotLinked(SelectableKnot a, SelectableKnot b)
        {
            var containerA = a.SplineInfo.Container;
            var containerB = b.SplineInfo.Container;

            if (containerA != containerB)
                return false;

            if (!containerA.KnotLinkCollection.TryGetKnotLinks(GetIndex(a), out var linkedKnots))
                return false;

            var bIndex = GetIndex(b);

            for (int i = 0; i < linkedKnots.Count; ++i)
                if (linkedKnots[i] == bIndex)
                    return true;

            return false;
        }

        internal static bool TryGetNearestKnot(IReadOnlyList<SplineInfo> splines, Vector2 mousePosition, out SelectableKnot knot, float maxDistance = SplineHandleUtility.pickingDistance)
        {
            float nearestDist = float.MaxValue;
            SelectableKnot nearest = knot = default;

            for (int i = 0; i < splines.Count; ++i)
            {
                var spline = splines[i].Spline;
                var localToWorld = splines[i].LocalToWorld;
                for (int j = 0; j < spline.Count; ++j)
                {
                    var dist = SplineHandleUtility.DistanceToCircle(spline[j].Transform(localToWorld).Position, SplineHandleUtility.pickingDistance);
                    if (dist <= nearestDist)
                    {
                        nearestDist = dist;
                        nearest = new SelectableKnot(splines[i], j);
                    }
                }
            }

            if (nearestDist > maxDistance)
                return false;

            knot = nearest;
            return true;
        }

        internal static bool TryGetNearestPositionOnCurve(IReadOnlyList<SplineInfo> splines, Vector2 mousePosition, out SplineCurveHit hit, float maxDistance = SplineHandleUtility.pickingDistance)
        {
            SplineCurveHit nearestHit = hit = default;
            BezierCurve nearestCurve = default;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < splines.Count; ++i)
            {
                var spline = splines[i].Spline;
                var localToWorld = splines[i].LocalToWorld;
                for (int j = 0; j < spline.GetCurveCount(); ++j)
                {
                    var curve = spline.GetCurve(j).Transform(localToWorld);
                    SplineHandleUtility.GetNearestPointOnCurve(curve, out Vector3 position, out float t, out float dist);
                    if (dist < nearestDist && t > 0f && t < 1f)
                    {
                        nearestCurve = curve;
                        nearestDist = dist;
                        nearestHit = new SplineCurveHit
                        {
                            Position = position,
                            T = t,
                            PreviousKnot = new SelectableKnot(splines[i], j),
                            NextKnot = new SelectableKnot(splines[i], spline.NextIndex(j))
                        };
                    }
                }
            }

            if (nearestDist > maxDistance)
                return false;

            var up = CurveUtility.EvaluateUpVector(nearestCurve, nearestHit.T, math.rotate(nearestHit.PreviousKnot.Rotation, math.up()), math.rotate(nearestHit.NextKnot.Rotation, math.up()));
            nearestHit.Normal = up;

            hit = nearestHit;
            return true;
        }

        internal static bool IsEndKnot(SelectableKnot knot)
        {
            return knot.IsValid() && knot.KnotIndex == knot.SplineInfo.Spline.Count - 1;
        }

        internal static bool TryDuplicateSpline(SelectableKnot fromKnot, SelectableKnot toKnot, out int newSplineIndex)
        {
            newSplineIndex = -1;
            if(!(fromKnot.IsValid() && toKnot.IsValid()))
                return false;

            if(!fromKnot.SplineInfo.Equals(toKnot.SplineInfo))
            {
                Debug.LogError("Duplicate failed: The 2 knots must be on the same Spline.");
                return false;
            }

            var container = fromKnot.SplineInfo.Container;
            var duplicate = container.AddSpline();

            //Copy knots to the new spline
            int startIndex = Math.Min(fromKnot.KnotIndex, toKnot.KnotIndex);
            int toIndex = Math.Max(fromKnot.KnotIndex, toKnot.KnotIndex);

            var originalSpline = fromKnot.SplineInfo.Spline;
            var originalSplineIndex = fromKnot.SplineInfo.Index;
            newSplineIndex = container.Splines.Count - 1;
            for (int i = startIndex; i <= toIndex; ++i)
            {
                duplicate.Add(originalSpline[i], originalSpline.GetTangentMode(i));

                // If the old knot had any links we link both old and new knot.
                // This will result in the new knot linking to what the old knot was linked to.
                // The old knot being removed right after that takes care of cleaning the old knot from the link.
                if (container.KnotLinkCollection.TryGetKnotLinks(new SplineKnotIndex(originalSplineIndex, i), out _))
                    container.KnotLinkCollection.Link(new SplineKnotIndex(originalSplineIndex, i), new SplineKnotIndex(newSplineIndex, i - startIndex));
            }
            return true;
        }

        internal static SelectableKnot SplitKnot(SelectableKnot knot)
        {
            if (!knot.IsValid())
                throw new ArgumentException("Knot is invalid", nameof(knot));

            var formerSpline = knot.SplineInfo.Spline;

            if (formerSpline.Closed)
            {
                // Unclose and add a knot to the end with the same data
                formerSpline.Closed = false;
                var firstKnot = new SelectableKnot(knot.SplineInfo, 0);
                var lastKnot = DuplicateKnot(firstKnot, knot.SplineInfo, formerSpline.Count);

                // If the knot was the first one of the spline nothing else needs to be done to split the knot
                if (knot.KnotIndex == 0)
                    return firstKnot;

                // If the knot wasn't the first one we also need need to link both ends of the spline to keep the same spline we had before
                LinkKnots(new List<SelectableKnot> {firstKnot, lastKnot});
            }

            // If not closed, split does nothing one of the ends of the spline
            else if (knot.KnotIndex == 0 || knot.KnotIndex == knot.SplineInfo.Spline.Count - 1)
                return knot;

            if(TryDuplicateSpline(knot, new SelectableKnot(knot.SplineInfo, knot.SplineInfo.Spline.Count - 1), out int splineIndex))
            {
                formerSpline.Resize(knot.KnotIndex + 1);
                return new SelectableKnot(new SplineInfo(knot.SplineInfo.Container, knot.SplineInfo.Container.Splines.Count - 1), 0);
            }

            return new SelectableKnot();
        }

        internal static SelectableKnot DuplicateKnot(SelectableKnot original, SplineInfo targetSpline, int targetIndex)
        {
            targetSpline.Spline.Insert(targetIndex, original.GetBezierKnot(false));
            targetSpline.Spline.SetTangentMode(targetIndex, original.Mode);
            return new SelectableKnot(targetSpline, targetIndex);
        }

        internal static SelectableKnot JoinKnots(SelectableKnot knotA, SelectableKnot knotB)
        {
            if (!knotA.IsValid())
                throw new ArgumentException("Knot is invalid", nameof(knotA));

            if (!knotB.IsValid())
                throw new ArgumentException("Knot is invalid", nameof(knotB));

            //Check knots properties
            var isKnotAActive = !SplineSelection.IsActive(knotB);
            var activeKnot = isKnotAActive ? knotA : knotB;
            var otherKnot = isKnotAActive ? knotB : knotA;

            var isActiveKnotAtStart = activeKnot.KnotIndex == 0;
            var isOtherKnotAtStart = otherKnot.KnotIndex == 0;

            //Reverse spline if needed, this is needed when the 2 knots are both starts or ends of their respective spline
            if(isActiveKnotAtStart == isOtherKnotAtStart)
                //We give more importance to the active knot, so we reverse the spline associated to otherKnot
                ReverseFlow(otherKnot.SplineInfo);

            //Save Links
            var links = new List<List<SelectableKnot>>();

            // Get all LinkedKnots on the splines
            for(int i = 0; i < activeKnot.SplineInfo.Spline.Count; ++i)
            {
                links.Add(new List<SelectableKnot>());
                GetKnotLinks(new SelectableKnot(activeKnot.SplineInfo, i), links[i]);
            }
            for(int i = 0; i < otherKnot.SplineInfo.Spline.Count; ++i)
            {
                var otherLinks = new List<SelectableKnot>();
                links.Add(otherLinks);
                GetKnotLinks(new SelectableKnot(otherKnot.SplineInfo, i), otherLinks);
            }

            //Unlink all knots in the spline
            foreach(var linkedKnots in links)
                UnlinkKnots(linkedKnots);

            //Save relevant data before joining the splines
            var activeSpline = activeKnot.SplineInfo.Spline;
            var activeSplineIndex = activeKnot.SplineInfo.Index;
            var activeSplineCount = activeSpline.Count;
            var otherSpline = otherKnot.SplineInfo.Spline;
            var otherSplineIndex = otherKnot.SplineInfo.Index;
            var otherSplineCount = otherSpline.Count;
            if(otherSplineCount > 1)
            {
                //Join Splines
                if(isActiveKnotAtStart)
                {
                    //All position from the other spline must be added before the knot A
                    //Don't copy the last knot of the other spline as this is the one to join
                    for(int i = otherSplineCount - 2; i >= 0 ; i--)
                        activeSpline.Insert(0,otherSpline[i], otherSpline.GetTangentMode(i));
                }
                else
                {
                    //All position from the other spline must be added after the knot A
                    //Don't copy the first knot of the other spline as this is the one to join
                    for(int i = 1; i < otherSplineCount; i++)
                        activeSpline.Add(otherSpline[i], otherSpline.GetTangentMode(i));
                }
            }

            otherKnot.SplineInfo.Container.RemoveSplineAt(otherSplineIndex);
            var newActiveSplineIndex = otherSplineIndex > activeSplineIndex ? activeSplineIndex : activeKnot.SplineInfo.Index - 1;
            var activeSplineInfo = new SplineInfo(activeKnot.SplineInfo.Container, newActiveSplineIndex);

            //Restore links
            foreach (var linkedKnots in links)
            {
                if(linkedKnots.Count == 1)
                    continue;

                for(int i = 0; i < linkedKnots.Count; ++i)
                {
                    var knot = linkedKnots[i];
                    if(knot.SplineInfo.Index == activeSplineIndex || knot.SplineInfo.Index == otherSplineIndex)
                    {
                        var newIndex = knot.KnotIndex;

                        if(knot.SplineInfo.Index == activeSplineIndex && isActiveKnotAtStart)
                            newIndex += otherSplineCount - 1;

                        if(knot.SplineInfo.Index == otherSplineIndex && !isActiveKnotAtStart)
                            newIndex += activeSplineCount - 1;

                        linkedKnots[i] = new SelectableKnot(activeSplineInfo, newIndex);
                    }
                    else
                    {
                        if(knot.SplineInfo.Index > otherSplineIndex)
                            linkedKnots[i] = new SelectableKnot(new SplineInfo(knot.SplineInfo.Container, knot.SplineInfo.Index - 1),knot.KnotIndex);
                    }
                }
                LinkKnots(linkedKnots);
            }

            return new SelectableKnot(activeSplineInfo, isActiveKnotAtStart ? otherSplineCount - 1 : activeKnot.KnotIndex);
        }

        internal static void ReverseFlow(SplineInfo splineInfo)
        {
            var spline = splineInfo.Spline;

            var knots = splineInfo.Spline.ToArray();
            var tangentModes = new TangentMode[spline.Count];

            for (int i = 0; i < tangentModes.Length; ++i)
                tangentModes[i] = spline.GetTangentMode(i);

            var splineLinks = new List<List<SelectableKnot>>();

            // GetAll LinkedKnots on the spline
            for(int previousKnotIndex = 0; previousKnotIndex < spline.Count; ++previousKnotIndex)
            {
                splineLinks.Add(new List<SelectableKnot>());
                GetKnotLinks(new SelectableKnot(splineInfo, previousKnotIndex), splineLinks[previousKnotIndex]);
            }

            //Unlink all knots in the spline
            foreach(var linkedKnots in splineLinks)
                UnlinkKnots(linkedKnots);

            // Reverse order and tangents
            for (int previousKnotIndex = 0; previousKnotIndex < spline.Count; ++previousKnotIndex)
            {
                var knot = knots[previousKnotIndex];
                var worldKnot = knot.Transform(splineInfo.LocalToWorld);
                var tangentIn = worldKnot.TangentIn;
                var tangentOut = worldKnot.TangentOut;

                var reverseRotation = quaternion.AxisAngle(math.mul(knot.Rotation, math.up()), math.radians(180));

                // Reverse the tangents to keep the same shape while reversing the order
                knot.Rotation = math.mul(reverseRotation, knot.Rotation);
                if(tangentModes[previousKnotIndex] is TangentMode.Broken)
                {
                    var localRot = quaternion.AxisAngle(math.up(), math.radians(180));
                    knot.TangentIn = math.rotate(localRot,tangentOut);
                    knot.TangentOut = math.rotate(localRot,tangentIn);
                }
                else if(tangentModes[previousKnotIndex] is TangentMode.Continuous)
                {
                    knot.TangentIn = -tangentOut;
                    knot.TangentOut = -tangentIn;
                }

                var newKnotIndex = spline.Count - 1 - previousKnotIndex;
                spline.SetTangentMode(newKnotIndex, tangentModes[previousKnotIndex]);
                spline[newKnotIndex] = knot;
            }

            //Redo all links
            foreach (var linkedKnots in splineLinks)
            {
                if(linkedKnots.Count == 1)
                    continue;

                for(int i = 0; i < linkedKnots.Count; ++i)
                {
                    var knot = linkedKnots[i];
                    if(knot.SplineInfo.Equals(splineInfo))
                        linkedKnots[i] = new SelectableKnot(splineInfo, spline.Count - 1 - knot.KnotIndex);
                }
                LinkKnots(linkedKnots);
            }

        }

        internal static HashSet<SplineInfo> GetSplines<T>(IReadOnlyList<T> elements)
            where T : ISplineElement
        {
            HashSet<SplineInfo> splines = new HashSet<SplineInfo>();
            for (int i = 0; i < elements.Count; ++i)
                splines.Add(elements[i].SplineInfo);
            return splines;
        }
    }
}