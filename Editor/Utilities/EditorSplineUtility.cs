using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.EditorTools;
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

        static readonly List<ISelectableElement> s_ElementBuffer = new List<ISelectableElement>();

        [UserSetting]
        internal static Pref<TangentMode> s_DefaultTangentMode = new("Splines.DefaultTangentMode", TangentMode.AutoSmooth);

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
        /// <param name="results">A list to store the <see cref="SplineInfo"/> of splines found in the targets.</param>
        internal static void GetSplinesFromTargets(IEnumerable<Object> targets, List<SplineInfo> results)
        {
            results.Clear();
            foreach (var target in targets)
                GetSplineInfosFromContainer(target, results);
        }

        /// <summary>
        /// Get a <see cref="SplineInfo"/> representation of the splines in a target.
        /// </summary>
        /// <param name="target">An Object inheriting from <see cref="ISplineContainer"/>.</param>
        /// <param name="results">A list to store the <see cref="SplineInfo"/> of splines found in the targets.</param>
        internal static void GetSplinesFromTarget(Object target, List<SplineInfo> results)
        {
            results.Clear();
            GetSplineInfosFromContainer(target, results);
        }

        /// <summary>
        /// Get a <see cref="SplineInfo"/> representation of the splines in a target.
        /// </summary>
        /// <param name="target">An Object inheriting from <see cref="ISplineContainer"/>.</param>
        /// <param name="results">A list to store the <see cref="SplineInfo"/> of splines found in the targets.</param>
        /// <returns>True if a at least a spline was found in the target.</returns>
        internal static bool TryGetSplinesFromTarget(Object target, List<SplineInfo> results)
        {
            results.Clear();
            GetSplineInfosFromContainer(target, results);
            return results.Count > 0;
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

        /// <summary>
        /// Sets the current active context to the <see cref="SplineToolContext"/> and the current active tool to the
        /// Draw Splines Tool (<see cref="KnotPlacementTool"/>)
        /// </summary>
        public static void SetKnotPlacementTool()
        {
            if(ToolManager.activeContextType != typeof(SplineToolContext))
            {
                ToolManager.SetActiveContext<SplineToolContext>();
                ToolManager.SetActiveTool<KnotPlacementTool>();
            }
            else if(ToolManager.activeToolType != typeof(KnotPlacementTool))
            {
                ToolManager.SetActiveTool<KnotPlacementTool>();
            }
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

        internal static Bounds GetBounds(IReadOnlyList<SplineInfo> splines)
        {
            Bounds bounds = default;
            bool initialized = false;

            for (int i = 0; i < splines.Count; ++i)
            {
                var spline = splines[i].Spline;

                if (spline.Count > 0 && !initialized)
                    bounds = spline.GetBounds(splines[i].LocalToWorld);
                else
                    bounds.Encapsulate(spline.GetBounds(splines[i].LocalToWorld));
            }

            return bounds;
        }

        internal static Bounds GetElementBounds<T>(IReadOnlyList<T> elements, bool useKnotPositionForTangents)
            where T : ISelectableElement
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
            where T : ISelectableElement
        {
            return new SelectableKnot(element.SplineInfo, element.KnotIndex);
        }

        internal static void RecordSelection(string name)
        {
            Undo.RecordObjects(SplineSelection.GetAllSelectedTargets(), name);
        }

        internal static void RecordObjects<T>(IReadOnlyList<T> elements, string name)
            where T : ISelectableElement
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
            var knot = AddKnotToTheEnd(splineInfo, from.Position, math.mul(from.Rotation, math.up()), tangentOut, false);
            LinkKnots(knot, from);
            return knot;
        }

        internal static TangentMode GetModeFromPlacementTangent(float3 tangent)
        {
            return math.lengthsq(tangent) < float.Epsilon ? DefaultTangentMode : TangentMode.Mirrored;
        }

        static SelectableKnot AddKnotInternal(SplineInfo splineInfo, float3 worldPosition, float3 normal, float3 tangentOut, int index, int previousIndex, bool updateSelection)
        {
            var spline = splineInfo.Spline;

            if (spline.Closed && spline.Count >= 2)
                Debug.LogWarning("Cannot add a point to the extremity of a closed spline.");


            var localToWorld = splineInfo.LocalToWorld;
            var mode = GetModeFromPlacementTangent(tangentOut);

            var localPosition = math.transform(math.inverse(splineInfo.LocalToWorld), worldPosition);
            quaternion localRotation;
            BezierKnot newKnot;

            // If we're in AutoSmooth mode
            if (!SplineUtility.AreTangentsModifiable(mode))
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
            if (spline.Count > 1 && !SplineUtility.AreTangentsModifiable(spline.GetTangentMode(previousIndex)))
            {
                // calculate rotation from the average direction from points p0 -> p1 -> p2
                BezierKnot current = spline[previousIndex];
                BezierKnot previous = spline.Previous(previousIndex);
                BezierKnot next = spline.Next(previousIndex);

                current.Rotation = CalculateKnotRotation(previous.Position, current.Position, next.Position, normal);
                spline[previousIndex] = current;
            }

            // If the element is part of a prefab, the changes have to be recorded AFTER being done on the prefab instance
            // otherwise they would not be saved in the scene.
            PrefabUtility.RecordPrefabInstancePropertyModifications(splineInfo.Object);

            var knot = new SelectableKnot(splineInfo, index);
            if(updateSelection)
                SplineSelection.Set(knot);

            return knot;
        }

        internal static SelectableKnot AddKnotToTheEnd(SplineInfo splineInfo, float3 worldPosition, float3 normal, float3 tangentOut, bool updateSelection = true)
        {
            return AddKnotInternal(splineInfo, worldPosition, normal, tangentOut, splineInfo.Spline.Count, splineInfo.Spline.Count - 1, updateSelection);
        }

        internal static SelectableKnot AddKnotToTheStart(SplineInfo splineInfo, float3 worldPosition, float3 normal, float3 tangentIn, bool updateSelection = true)
        {
            return AddKnotInternal(splineInfo, worldPosition, normal, -tangentIn, 0, 1, updateSelection);
        }

        internal static void RemoveKnot(SelectableKnot knot)
        {
            knot.SplineInfo.Spline.RemoveAt(knot.KnotIndex);

            //Force to record changes if part of a prefab instance
            PrefabUtility.RecordPrefabInstancePropertyModifications(knot.SplineInfo.Object);
            knotRemoved?.Invoke(knot);
        }

        internal static bool ShouldRemoveSpline(SplineInfo splineInfo)
        {
            // Spline is Empty
            if (splineInfo.Spline.Count == 0)
                return true;

            // Spline has one knot that is linked to another. This makes it "hidden" to the user because it's on top of another knot without having a curve associated with it.
            if (splineInfo.Spline.Count == 1 && splineInfo.Container.KnotLinkCollection.TryGetKnotLinks(new SplineKnotIndex(splineInfo.Index, 0), out _))
                return true;

            return false;
        }

        // Zero out the given tangent and switch knot's tangent mode to Continous if it's Mirrored.
        internal static void ClearTangent(SelectableTangent tangent)
        {
            var spline = tangent.SplineInfo.Spline;
            var knotIndex = tangent.Owner.KnotIndex;
            var selectableKnot = tangent.Owner;
            var bezierKnot = spline[knotIndex];

            if (selectableKnot.Mode == TangentMode.Mirrored)
                selectableKnot.Mode = TangentMode.Continuous;

            switch (tangent.TangentIndex)
            {
                case (int)BezierTangent.In:
                    bezierKnot.TangentIn = float3.zero;
                    break;

                case (int)BezierTangent.Out:
                    bezierKnot.TangentOut = float3.zero;
                    break;
            }

            spline[knotIndex] = bezierKnot;
        }

        /// <summary>
        /// Returns the interpolation value that corresponds to the middle (distance wise) of the curve.
        /// If spline and curveIndex are provided, the function leverages the spline's LUTs, otherwise the LUT is built on the fly.
        /// </summary>
        /// <param name="curve">The curve to evaluate.</param>
        /// <param name="spline">The ISpline that curve belongs to. Not used if curve is not part of any spline.</param>
        /// <param name="curveIndex">The index of the curve if it's part of the spine.</param>
        /// <typeparam name="T">A type implementing ISpline.</typeparam>
        internal static float GetCurveMiddleInterpolation<T>(BezierCurve curve, T spline, int curveIndex) where T: ISpline
        {
            var curveMidT = 0f;
            if (curveIndex >= 0)
                curveMidT = spline.GetCurveInterpolation(curveIndex, spline.GetCurveLength(curveIndex) * 0.5f);
            else
                curveMidT = CurveUtility.GetDistanceToInterpolation(curve, CurveUtility.ApproximateLength(curve) * 0.5f);

            return curveMidT;
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

            if (!SplineUtility.AreTangentsModifiable(aMode))
                p1 = aMode == TangentMode.Linear ? p0 : p0 + SplineUtility.GetAutoSmoothTangent(math.transform(trs, spline[previousIndex].Position), p0, p3, SplineUtility.CatmullRomTension);

            if (!SplineUtility.AreTangentsModifiable(bMode))
                p2 = bMode == TangentMode.Linear ? p3 : p3 + SplineUtility.GetAutoSmoothTangent(p3, p3, p0, SplineUtility.CatmullRomTension);

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

        //Get the affected curves when trying to add a knot on an existing segment
        //internal static void GetAffectedCurves(SplineCurveHit hit, Dictionary<Spline, Dictionary<int, List<BezierKnot>>> affectedCurves)
        internal static void GetAffectedCurves(SplineCurveHit hit, List<(Spline s, int index,  List<BezierKnot> knots)> affectedCurves)
        {
            var spline = hit.PreviousKnot.SplineInfo.Spline;
            var curveIndex = hit.PreviousKnot.KnotIndex;
            var hitLocalPosition = hit.PreviousKnot.SplineInfo.Transform.InverseTransformPoint(hit.Position);

            var previewKnots = new List<BezierKnot>();

            var sKnot = hit.PreviousKnot;
            var bKnot = new BezierKnot(sKnot.LocalPosition, sKnot.TangentIn.LocalPosition, sKnot.TangentOut.LocalPosition, sKnot.LocalRotation);

            var insertedKnot = GetInsertedKnotPreview(hit.PreviousKnot.SplineInfo, hit.NextKnot.KnotIndex, hit.T, out var leftTangent, out var rightTangent);

            if(spline.GetTangentMode(sKnot.KnotIndex) == TangentMode.AutoSmooth)
            {
                var previousKnot = spline.Previous(sKnot.KnotIndex);
                var previousKnotIndex = spline.PreviousIndex(sKnot.KnotIndex);
                bKnot = SplineUtility.GetAutoSmoothKnot(sKnot.LocalPosition, previousKnot.Position, hitLocalPosition);

                affectedCurves.Add((spline, previousKnotIndex, new List<BezierKnot>() { previousKnot, bKnot }));
            }
            else
                bKnot.TangentOut = math.mul(math.inverse(sKnot.LocalRotation), leftTangent);

            previewKnots.Add(bKnot);

            previewKnots.Add(insertedKnot);
            var affectedCurveIndex = affectedCurves.FindIndex(x => x.s == spline && x.index == curveIndex);
            if(affectedCurveIndex >= 0)
                affectedCurves.RemoveAt(affectedCurveIndex);
            affectedCurves.Add((spline, curveIndex, previewKnots));

            sKnot = hit.NextKnot;
            bKnot = new BezierKnot(sKnot.LocalPosition, sKnot.TangentIn.LocalPosition, sKnot.TangentOut.LocalPosition, sKnot.LocalRotation);

            if(spline.GetTangentMode(sKnot.KnotIndex) == TangentMode.AutoSmooth)
            {
                var nextKnot = spline.Next(sKnot.KnotIndex);
                bKnot = SplineUtility.GetAutoSmoothKnot(sKnot.LocalPosition, hitLocalPosition, nextKnot.Position);

                affectedCurves.Add((spline, sKnot.KnotIndex, new List<BezierKnot>() { bKnot, nextKnot }));
            }
            else
                bKnot.TangentIn = math.mul(math.inverse(sKnot.LocalRotation), rightTangent);


            previewKnots.Add(bKnot);
        }

        internal static void GetAffectedCurves(SplineInfo splineInfo, Vector3 knotPosition, bool addingToStart, SelectableKnot lastKnot, int previousKnotIndex, List<(Spline s, int index, List<BezierKnot> knots)> affectedCurves)
        {
            var spline = splineInfo.Spline;
            if (spline != null)
            {
                var lastKnotIndex = lastKnot.KnotIndex;

                var affectedCurveIndex = affectedCurves.FindIndex(x => x.s == spline && x.index == (addingToStart ? lastKnotIndex :  previousKnotIndex));

                var lastTangentMode = spline.GetTangentMode(lastKnotIndex);
                if(lastTangentMode == TangentMode.AutoSmooth)
                {
                    var previousKnot = spline[previousKnotIndex];
                    var autoSmoothKnot = addingToStart ?
                        SplineUtility.GetAutoSmoothKnot(lastKnot.LocalPosition, knotPosition, previousKnot.Position) :
                        SplineUtility.GetAutoSmoothKnot(lastKnot.LocalPosition, previousKnot.Position, knotPosition);

                    if (affectedCurveIndex < 0)
                    {
                        if (addingToStart)
                            affectedCurves.Insert(0, (spline, lastKnot.KnotIndex, new List<BezierKnot>() { autoSmoothKnot, previousKnot }));
                        else
                            affectedCurves.Add((spline, previousKnotIndex, new List<BezierKnot>() { previousKnot, autoSmoothKnot }));
                    }
                    else
                    {
                        //The segment as already some changes due to some modifications on the previous knots in the previews
                        //So we only want to adapt the last knot in that case
                        var knots = affectedCurves[affectedCurveIndex].knots;
                        knots[addingToStart ? 0 : 1] = autoSmoothKnot;
                    }
                }
            }
        }

        internal static BezierKnot GetInsertedKnotPreview(SplineInfo splineInfo, int index, float t, out Vector3 leftOutTangent, out Vector3 rightInTangent)
        {
            var spline = splineInfo.Spline;

            var previousIndex = SplineUtility.PreviousIndex(index, spline.Count, spline.Closed);
            var previous = spline[previousIndex];

            var curveToSplit = new BezierCurve(previous, spline[index]);
            CurveUtility.Split(curveToSplit, t, out var leftCurve, out var rightCurve);

            var nextIndex = SplineUtility.NextIndex(index, spline.Count, spline.Closed);
            var next = spline[nextIndex];

            var up = CurveUtility.EvaluateUpVector(curveToSplit, t, math.rotate(previous.Rotation, math.up()), math.rotate(next.Rotation, math.up()));
            var rotation = quaternion.LookRotationSafe(math.normalizesafe(rightCurve.Tangent0), up);
            var inverseRotation = math.inverse(rotation);

            leftOutTangent = leftCurve.Tangent0;
            rightInTangent = rightCurve.Tangent1;

            return new BezierKnot(leftCurve.P3, math.mul(inverseRotation, leftCurve.Tangent1), math.mul(inverseRotation, rightCurve.Tangent0), rotation);
        }

        internal static SelectableKnot InsertKnot(SplineInfo splineInfo, int index, float t)
        {
            var spline = splineInfo.Spline;
            if (spline == null)
                return default;

            spline.InsertOnCurve(index, t);

            var knot = new SelectableKnot(splineInfo, index);
            knotInserted?.Invoke(knot);
            return knot;
        }

        internal static SplineKnotIndex GetIndex(SelectableKnot knot)
        {
            return new SplineKnotIndex(knot.SplineInfo.Index, knot.KnotIndex);
        }

        internal static void GetKnotLinks(SelectableKnot knot, List<SelectableKnot> knots)
        {
            var container = knot.SplineInfo.Container;
            if (container == null)
                return;

            knots.Clear();
            if (container.KnotLinkCollection == null)
            {
                knots.Add(knot);
                return;
            }

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

            //Force to record changes if part of a prefab instance
            if(knots.Count > 0 && knots[0].IsValid())
                PrefabUtility.RecordPrefabInstancePropertyModifications(knots[0].SplineInfo.Object);
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

            //Force to record changes if part of a prefab instance
            if(knots.Count > 0 && knots[0].IsValid())
                PrefabUtility.RecordPrefabInstancePropertyModifications(knots[0].SplineInfo.Object);
        }

        internal static void LinkKnots(SelectableKnot a, SelectableKnot b)
        {
            var containerA = a.SplineInfo.Container;
            var containerB = b.SplineInfo.Container;

            if (containerA != containerB)
                return;

            containerA.KnotLinkCollection.Link(GetIndex(a), GetIndex(b));

            //Force to record changes if part of a prefab instance
            if(a.IsValid())
                PrefabUtility.RecordPrefabInstancePropertyModifications(a.SplineInfo.Object);
        }

        internal static bool AreKnotLinked(SelectableKnot a, SelectableKnot b)
        {
            var containerA = a.SplineInfo.Container;
            var containerB = b.SplineInfo.Container;

            if (containerA != containerB)
                return false;

            return containerA.AreKnotLinked(
                new SplineKnotIndex(a.SplineInfo.Index, a.KnotIndex),
                new SplineKnotIndex(b.SplineInfo.Index, b.KnotIndex));

        }

        internal static bool TryGetNearestKnot(IReadOnlyList<SplineInfo> splines, out SelectableKnot knot, float maxDistance = SplineHandleUtility.pickingDistance)
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

        internal static bool TryGetNearestPositionOnCurve(IReadOnlyList<SplineInfo> splines, out SplineCurveHit hit, float maxDistance = SplineHandleUtility.pickingDistance)
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

        internal static SelectableKnot SplitKnot(SelectableKnot knot)
        {
            if (!knot.IsValid())
                throw new ArgumentException("Knot is invalid", nameof(knot));

            var newKnot = knot.SplineInfo.Container.SplitSplineOnKnot(new SplineKnotIndex(knot.SplineInfo.Index, knot.KnotIndex));

            if (newKnot.IsValid())
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(knot.SplineInfo.Object);
                return new SelectableKnot(new SplineInfo(knot.SplineInfo.Container, newKnot.Spline), newKnot.Knot);
            }
            return new SelectableKnot();
        }

        internal static SelectableKnot JoinKnots(SelectableKnot knotA, SelectableKnot knotB)
        {
            if (!knotA.IsValid())
                throw new ArgumentException("Knot is invalid", nameof(knotA));

            if (!knotB.IsValid())
                throw new ArgumentException("Knot is invalid", nameof(knotB));

            //Check knots properties
            var isKnotAActive = !SplineSelection.IsActive(knotB);
            var knotIndexA = new SplineKnotIndex(knotA.SplineInfo.Index, knotA.KnotIndex);
            var knotIndexB = new SplineKnotIndex(knotB.SplineInfo.Index, knotB.KnotIndex);
            var activeKnot = isKnotAActive ? knotIndexA : knotIndexB;
            var otherKnot = isKnotAActive ? knotIndexB : knotIndexA;

            var res = knotA.SplineInfo.Container.JoinSplinesOnKnots(activeKnot, otherKnot);

            //Force to record changes if part of a prefab instance
            var activeSplineInfo = isKnotAActive ? knotA.SplineInfo : knotB.SplineInfo;
            PrefabUtility.RecordPrefabInstancePropertyModifications(activeSplineInfo.Object);

            return new SelectableKnot(new SplineInfo(knotA.SplineInfo.Container, res.Spline), res.Knot);
        }

        internal static void ReverseSplinesFlow(IReadOnlyList<SplineInfo> selectedSplines)
        {
            List<ISelectableElement> selectedElements = new List<ISelectableElement>();

            var formerActiveElement = SplineSelection.GetActiveElement(selectedSplines);
            SplineSelection.GetElements(selectedSplines, selectedElements);
            var splines = GetSplines(selectedElements);
            foreach (var splineInfo in splines)
                SplineUtility.ReverseFlow(splineInfo);

            int newActiveElementIndex = -1;
            for (int i = 0; i < selectedElements.Count; ++i)
            {
                var element = selectedElements[i];
                if (element.Equals(formerActiveElement))
                    newActiveElementIndex = i;

                if (element is SelectableKnot knot)
                    selectedElements[i] = new SelectableKnot(knot.SplineInfo, knot.SplineInfo.Spline.Count - knot.KnotIndex - 1);
                else if (element is SelectableTangent tangent)
                    selectedElements[i] = new SelectableTangent(tangent.SplineInfo, tangent.SplineInfo.Spline.Count - tangent.KnotIndex - 1, (tangent.TangentIndex + 1) % 2);
            }

            SplineSelection.Clear();
            SplineSelection.AddRange(selectedElements);

            if(newActiveElementIndex >= 0)
                SplineSelection.SetActive(selectedElements[newActiveElementIndex]);
            else
                SplineSelection.SetActive(selectedElements[^1]);
        }

        internal static HashSet<SplineInfo> GetSplines<T>(IReadOnlyList<T> elements)
            where T : ISelectableElement
        {
            HashSet<SplineInfo> splines = new HashSet<SplineInfo>();
            for (int i = 0; i < elements.Count; ++i)
                splines.Add(elements[i].SplineInfo);
            return splines;
        }

        internal static void GetAdjacentTangents<T>(
            T element,
            out SelectableTangent previousOut,
            out SelectableTangent currentIn,
            out SelectableTangent currentOut,
            out SelectableTangent nextIn)
            where T : ISelectableElement
        {
            var knot = GetKnot(element);
            var spline = knot.SplineInfo.Spline;
            bool isFirstKnot = knot.KnotIndex == 0 && !spline.Closed;
            bool isLastKnot = knot.KnotIndex == spline.Count - 1 && !spline.Closed;

            previousOut = isFirstKnot ? default : new SelectableTangent(knot.SplineInfo, spline.PreviousIndex(knot.KnotIndex), BezierTangent.Out);
            currentIn = isFirstKnot ? default : knot.TangentIn;
            currentOut = isLastKnot ? default : knot.TangentOut;
            nextIn = isLastKnot ? default : new SelectableTangent(knot.SplineInfo, spline.NextIndex(knot.KnotIndex), BezierTangent.In);
        }

        internal static quaternion GetElementRotation<T>(T element)
            where T : ISelectableElement
        {
            if (element is SelectableTangent editableTangent)
            {
                float3 forward;
                var knotUp = math.rotate(editableTangent.Owner.Rotation, math.up());

                if (math.length(editableTangent.Direction) > 0)
                    forward = math.normalize(editableTangent.Direction);
                else // Treat zero length tangent same way as when it's parallel to knot's up vector
                    forward = knotUp;

                float3 right;
                var dotForwardKnotUp = math.dot(forward, knotUp);
                if (Mathf.Approximately(math.abs(dotForwardKnotUp), 1f))
                    right = math.rotate(editableTangent.Owner.Rotation, math.right()) * math.sign(dotForwardKnotUp);
                else
                    right = math.cross(forward, knotUp);

                return quaternion.LookRotationSafe(forward, math.cross(right, forward));
            }

            if (element is SelectableKnot editableKnot)
                return editableKnot.Rotation;

            return quaternion.identity;
        }

        /// <summary>
        /// Sets the position of a tangent. This could actually result in the knot being rotated depending on the tangent mode
        /// </summary>
        /// <param name="tangent">The tangent to place</param>
        /// <param name="position">The position that should be used to place the tangent</param>
        internal static void ApplyPositionToTangent(SelectableTangent tangent, float3 position)
        {
            var knot = tangent.Owner;

            switch (knot.Mode)
            {
                case TangentMode.Broken:
                    tangent.Position = position;
                    break;

                case TangentMode.Continuous:
                case TangentMode.Mirrored:
                    var deltas = TransformOperation.CalculateMirroredTangentTranslationDeltas(tangent, position);

                    knot.Rotation = math.mul(deltas.knotRotationDelta, knot.Rotation);
                    tangent.LocalDirection += math.normalize(tangent.LocalDirection) * deltas.tangentLocalMagnitudeDelta;

                    break;
            }
        }

        internal static bool Exists(ISplineContainer container, int index)
        {
            if (container == null)
                return false;

            return index < container.Splines.Count;
        }

        internal static bool Exists(SplineInfo spline)
        {
            return Exists(spline.Container, spline.Index);
        }

        /// <summary>
        /// Copy an embedded <see cref="SplineData{T}"/> collection to a new <see cref="Spline"/> if the destination
        /// does not already contain an entry matching the <paramref name="type"/> and <paramref name="key"/>.
        /// </summary>
        /// <param name="type">The <see cref="EmbeddedSplineDataType"/>.</param>
        /// <param name="key">A string value used to identify and access a <see cref="SplineData{T}"/>.</param>
        /// <param name="container">The <see cref="ISplineContainer"/> that contains the spline.</param>
        /// <param name="source">The index of the <see cref="Spline"/> in the <paramref name="container"/> to copy data from.</param>
        /// <param name="destination">The index of the <see cref="Spline"/> in the <paramref name="container"/> to copy data to.</param>
        /// <returns>True if data was copied, otherwise false.</returns>
        public static bool CopySplineDataIfEmpty(ISplineContainer container, int source, int destination, EmbeddedSplineDataType type, string key)
        {
            if (container == null)
                return false;

            var splines = container.Splines;

            if (source < 0 || source >= splines.Count || destination < 0 || destination >= splines.Count)
                return false;

            var src = splines[source];
            var dst = splines[destination];

            // copy SplineData if the target spline is empty
            switch (type)
            {
                case EmbeddedSplineDataType.Int:
                    if((dst.TryGetIntData(key, out var existingIntData) && existingIntData.Count > 0)
                       || !src.TryGetIntData(key, out var srcIntData))
                        return false;
                    dst.SetIntData(key, srcIntData);
                    return true;
                case EmbeddedSplineDataType.Float:
                    if((dst.TryGetFloatData(key, out var existingFloatData) && existingFloatData.Count > 0)
                       || !src.TryGetFloatData(key, out var srcFloatData))
                        return false;
                    dst.SetFloatData(key, srcFloatData);
                    return true;
                case EmbeddedSplineDataType.Float4:
                    if((dst.TryGetFloat4Data(key, out var existingFloat4Data) && existingFloat4Data.Count > 0)
                       || !src.TryGetFloat4Data(key, out var srcFloat4Data))
                        return false;
                    dst.SetFloat4Data(key, srcFloat4Data);
                    return true;
                case EmbeddedSplineDataType.Object:
                    if((dst.TryGetObjectData(key, out var existingObjectData) && existingObjectData.Count > 0)
                       || !src.TryGetObjectData(key, out var srcObjectData))
                        return false;
                    dst.SetObjectData(key, srcObjectData);
                    return true;
            }

            return false;
        }
    }
}
