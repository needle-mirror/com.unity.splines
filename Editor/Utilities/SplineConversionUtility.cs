using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UObject = UnityEngine.Object;

namespace UnityEditor.Splines
{
    static class SplineConversionUtility
    {
        internal static Action splinesUpdated;
        internal static Action splineTypeChanged;

        static readonly List<UObject> s_TargetsBuffer = new List<UObject>();
        static readonly List<BezierKnot> s_KnotBuffer = new List<BezierKnot>();

        public static void UpdateEditableSplinesForTargetType<T>() where T : UObject
        {
            UpdateEditableSplinesForTargetType(typeof(T));
        }

        public static void UpdateEditableSplinesForTargetType(Type type)
        {
            EditableSplineManager.GetTargetsOfType(type, s_TargetsBuffer);
            UpdateEditableSplinesForTargets(s_TargetsBuffer);
        }

        public static void UpdateEditableSplinesForTargets(IEnumerable<UObject> target)
        {
            foreach (var owner in target)
            {
                UpdateEditableSplinesInternal(owner, true);
            }

            splinesUpdated?.Invoke();
        }

        public static void UpdateEditableSplinesForTarget(UObject target)
        {
            UpdateEditableSplinesInternal(target, false);
        }

        static void UpdateEditableSplinesInternal(UObject target, bool silent)
        {
            //Update IEditableSpline from its Spline target
            if (!EditableSplineManager.TryGetTargetData(target, out EditableSplineManager.TargetData data))
                return;

            if (data.RawSplines.Count == 0)
                return;
            
            var provider = (ISplineProvider) target;
            data.RawSplines.Clear();
            data.RawSplines.AddRange(provider.Splines);

            //Ensure same number of editable splines than spline contained on the target
            Array.Resize(ref data.EditableSplines, data.RawSplines.Count);

            bool typeChanged = false;
            for (var i = 0; i < data.RawSplines.Count; ++i)
            {
                var spline = data.RawSplines[i];
                var editableSpline = data.EditableSplines[i];

                if (editableSpline == null || EditableSplineUtility.GetSplineType(editableSpline) != spline.EditType)
                {
                    var newPath = EditableSplineUtility.CreatePathOfType(spline.EditType);
                    newPath.conversionIndex = i;
                    newPath.conversionTarget = target;

                    data.EditableSplines[i] = newPath;
                    editableSpline = newPath;

                    typeChanged = true;
                }
                
                editableSpline.closed = spline.Closed;
                s_KnotBuffer.Clear();
                for (int j = 0; j < spline.Count; ++j)
                {
                    s_KnotBuffer.Add(spline[j]);
                }

                editableSpline.FromBezier(s_KnotBuffer);

                //Remove dirty flags to not reapply back to target 
                editableSpline.isDirty = false;
                editableSpline.ValidateData();
            }

            if (typeChanged)
                splineTypeChanged?.Invoke();

            if (!silent)
                splinesUpdated?.Invoke();
        }

        public static bool ApplyEditableSplinesOfTypeIfDirty<T>() where T : UObject
        {
            return ApplyEditableSplinesOfTypeIfDirty(typeof(T));
        }

        public static bool ApplyEditableSplinesOfTypeIfDirty(Type type) 
        {
            EditableSplineManager.GetTargetsOfType(type, s_TargetsBuffer);
            return ApplyEditableSplinesIfDirty(s_TargetsBuffer);
        }

        public static bool ApplyEditableSplinesIfDirty(IEnumerable<UObject> owners)
        {
            bool result = false;
            foreach (var owner in owners)
            {
                result |= ApplyEditableSplinesIfDirty(owner);
            }
            return result;
        }

        public static bool ApplyEditableSplinesIfDirty(UObject target)
        {
            if (!EditableSplineManager.TryGetTargetData(target, out EditableSplineManager.TargetData data))
                return false;

            if (!IsAnyEditableSplineDirty(data.EditableSplines))
                return false;

            Undo.RecordObject(target, "Apply Changes to Spline");

            for (var i = 0; i < data.EditableSplines.Length; ++i)
            {
                var editablePath = data.EditableSplines[i];
                var targetSpline = data.RawSplines[i];
                
                if (editablePath.isDirty)
                {
                    editablePath.isDirty = false;
                    targetSpline.Closed = editablePath.closed;
                    s_KnotBuffer.Clear();
                    editablePath.ToBezier(s_KnotBuffer);

                    targetSpline.Resize(s_KnotBuffer.Count);
                    for (int j = 0; j < s_KnotBuffer.Count; ++j)
                    {
                        targetSpline[j] = s_KnotBuffer[j];
                    }
                }
            }

            return true;
        }

        static bool IsAnyEditableSplineDirty(IEnumerable<IEditableSpline> paths)
        {
            foreach (var path in paths)
            {
                if (path.isDirty)
                    return true;
            }

            return false;
        }
    }
}
