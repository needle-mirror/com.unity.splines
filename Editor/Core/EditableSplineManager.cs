using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.Splines;
using UObject = UnityEngine.Object;

namespace UnityEditor.Splines
{
    [InitializeOnLoad]
    static class EditableSplineManager
    {
        // ONLY FOR TESTS. Used to add a path to the manager without requiring all the loops to get to it. 
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal sealed class TestManagedSpline : IDisposable
        {
            readonly UObject m_Target;

            public TestManagedSpline(UObject target, IEditableSpline spline)
            {
                m_Target = target;
                spline.conversionTarget = m_Target;
                if (!s_Splines.TryGetValue(target, out TargetData data))
                {
                    data = new TargetData();
                    s_Splines.Add(target, data);
                }

                data.RawSplines.Clear();
                data.EditableSplines = new[] { spline };
            }

            public void Dispose()
            {
                if (m_Target != null)
                    s_Splines.Remove(m_Target);
            }
        }

        internal sealed class TargetData
        {
            public readonly List<Spline> RawSplines = new List<Spline>();
            public IEditableSpline[] EditableSplines = new IEditableSpline[0];
        }

        static readonly List<UObject> s_OwnersBuffer = new List<UObject>();
        static readonly Dictionary<UObject, TargetData> s_Splines = new Dictionary<UObject, TargetData>();
        static readonly Dictionary<UObject, TargetData> s_SplinesBackup = new Dictionary<UObject, TargetData>();

        static EditableSplineManager()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnWillDomainReload;
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            OnSelectionChanged();
        }

        static void OnWillDomainReload()
        {
            FreeEntireCache();
        }

        static void OnSelectionChanged()
        {
            UObject[] selection = Selection.GetFiltered(typeof(ISplineProvider), SelectionMode.Editable);
            UpdateSelection(selection);
        }

        static void OnUndoRedoPerformed()
        {
            FreeEntireCache();
        }

        internal static bool TryGetTargetData(UObject target, out TargetData targetData)
        {
            return s_Splines.TryGetValue(target, out targetData);
        }

        public static IReadOnlyList<IEditableSpline> GetEditableSplines(UObject target, bool createIfNotCached = true)
        {
            if (target == null)
                return null;

            if (!s_Splines.TryGetValue(target, out TargetData result))
            {
                if (!createIfNotCached)
                    return null;

                var splineProvider = target as ISplineProvider;
                if (splineProvider == null)
                    return null;
                
                TargetData data = new TargetData();

                var targetSplines = splineProvider.Splines;
                if (targetSplines != null)
                    data.RawSplines.AddRange(targetSplines);

                s_Splines.Add(target, data);
                result = data;

                SplineConversionUtility.UpdateEditableSplinesForTarget(target);
            }

            return result.EditableSplines;
        }

        public static void GetTargetsOfType(Type type, List<UObject> results)
        {
            ValidatePathOwners();
            results.Clear();
            foreach (var target in s_Splines.Keys)
            {
                if (target != null && type.IsInstanceOfType(target))
                {
                    results.Add(target);
                }
            }
        }

        public static void EnsureCacheForTarget(UObject target)
        {
            GetEditableSplines(target);
        }

        public static void UpdateSelection(IEnumerable<UObject> selected)
        {
            if (selected == null)
                return;

            //Copy to backup
            s_SplinesBackup.Clear();
            foreach (var keyValuePair in s_Splines)
            {
                s_SplinesBackup.Add(keyValuePair.Key, keyValuePair.Value);
            }
            
            //Copy all that are still selected to real dictionary and ensure cache for newly selected
            s_Splines.Clear();
            foreach (var target in selected)
            {
                if (target != null)
                {
                    if (s_SplinesBackup.TryGetValue(target, out TargetData data))
                    {
                        s_Splines.Add(target, data);
                        s_SplinesBackup.Remove(target);
                    }
                    else
                    {
                        EnsureCacheForTarget(target);
                    }
                }
            }
        }

        public static void FreeEntireCache()
        {
            s_Splines.Clear();
        }

        public static void FreeCacheForTarget(UObject target)
        {
            if (s_Splines.TryGetValue(target, out TargetData data))
            {
                s_Splines.Remove(target);
            }
        }

        static void ValidatePathOwners()
        {
            s_OwnersBuffer.Clear();
            foreach (var data in s_Splines)
            {
                // A dictionary key will never be fully null but the object could be destroyed
                if (data.Key == null)
                    s_OwnersBuffer.Add(data.Key);
            }

            foreach (var o in s_OwnersBuffer)
            {
                s_Splines.Remove(o);
            }
        }
    }
}
