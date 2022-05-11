using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    static class SplineSelection
    {
        public static event Action changed;

        static readonly HashSet<Object> s_ObjectSet = new HashSet<Object>();

        static SelectionContext context => SelectionContext.instance;
        static List<SelectableSplineElement> selection => context.selection;

        static int s_SelectionVersion;

        static SplineSelection()
        {
            context.version = 0;

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorSplineUtility.knotInserted += OnKnotInserted;
            EditorSplineUtility.knotRemoved += OnKnotRemoved;
        }

        static void OnUndoRedoPerformed()
        {
            if (context.version != s_SelectionVersion)
            {
                s_SelectionVersion = context.version;
                NotifySelectionChanged();
            }
        }

        public static void Clear()
        {
            if (selection.Count == 0)
                return;

            IncrementVersion();
            ClearNoUndo(true);
        }

        internal static void ClearNoUndo(bool notify)
        {
            selection.Clear();
            if (notify)
                NotifySelectionChanged();
        }

        public static int Count => selection.Count;

        public static bool HasAny<T>(IReadOnlyList<SplineInfo> targets)
            where T : struct, ISplineElement
        {
            for (int i = 0; i < Count; ++i)
                for (int j = 0; j < targets.Count; ++j)
                    if (TryGetElement(selection[i], targets[j], out T _))
                        return true;

            return false;
        }

        public static ISplineElement GetActiveElement(IReadOnlyList<SplineInfo> targets)
        {
            for (int i = 0; i < Count; ++i)
                for (int j = 0; j < targets.Count; ++j)
                    if (TryGetElement(selection[i], targets[j], out ISplineElement result))
                        return result;
            return null;
        }

        public static void GetElements<T>(IReadOnlyList<SplineInfo> targets, ICollection<T> results)
            where T : ISplineElement
        {
            results.Clear();
            for (int i = 0; i < Count; ++i)
                for (int j = 0; j < targets.Count; ++j)
                    if (TryGetElement(selection[i], targets[j], out T result))
                        results.Add(result);
        }

        static bool TryGetElement<T>(SelectableSplineElement element, SplineInfo splineInfo, out T value)
            where T : ISplineElement
        {
            if (element.target == splineInfo.Container as Object)
            {
                if (element.targetIndex == splineInfo.Index)
                {
                    if (element.tangentIndex >= 0)
                    {
                        var tangent = new SelectableTangent(splineInfo, element.knotIndex, element.tangentIndex);
                        if (tangent.IsValid() && tangent is T t)
                        {
                            value = t;
                            return true;
                        }

                        value = default;
                        return false;
                    }

                    var knot = new SelectableKnot(splineInfo, element.knotIndex);
                    if (knot.IsValid() && knot is T k)
                    {
                        value = k;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        static Object[] s_SelectedTargetsBuffer = new Object[0];
        internal static Object[] GetAllSelectedTargets()
        {
            s_ObjectSet.Clear();
            foreach (var element in selection)
            {
                s_ObjectSet.Add(element.target);
            }
            Array.Resize(ref s_SelectedTargetsBuffer, s_ObjectSet.Count);
            s_ObjectSet.CopyTo(s_SelectedTargetsBuffer);
            return s_SelectedTargetsBuffer;
        }

        public static bool IsActive<T>(T element)
            where T : ISplineElement
        {
            if (selection.Count == 0)
                return false;

            return IsEqual(element, selection[0]);
        }

        static bool IsEqual<T>(T element, SelectableSplineElement selectionData)
            where T : ISplineElement
        {
            int tangentIndex = element is SelectableTangent tangent ? tangent.TangentIndex : -1;
            return element.SplineInfo.Target == selectionData.target
                   && element.SplineInfo.Index == selectionData.targetIndex
                   && element.KnotIndex == selectionData.knotIndex
                   && tangentIndex == selectionData.tangentIndex;
        }

        public static void SetActive<T>(T element)
            where T : ISplineElement
        {
            var index = IndexOf(element);
            if (index == 0)
                return;

            IncrementVersion();

            if (index > 0)
                selection.RemoveAt(index);

            var e = new SelectableSplineElement(element);
            selection.Insert(0, e);

            if(e.target is Component component)
            {
                //Set the active unity object so the spline is the first target
                Object[] unitySelection = Selection.objects;
                var target = component.gameObject;

                index = Array.IndexOf(unitySelection, target);
                if(index > 0)
                {
                    Object prevObj = unitySelection[0];
                    unitySelection[0] = unitySelection[index];
                    unitySelection[index] = prevObj;
                    Selection.objects = unitySelection;
                }
            }

            NotifySelectionChanged();
        }

        public static void Set<T>(T element)
            where T : ISplineElement
        {
            IncrementVersion();

            ClearNoUndo(false);
            selection.Insert(0, new SelectableSplineElement(element));
            NotifySelectionChanged();
        }

        public static void Add<T>(T element)
            where T : ISplineElement
        {
            if (Contains(element))
                return;

            IncrementVersion();
            selection.Insert(0, new SelectableSplineElement(element));
            NotifySelectionChanged();
        }

        public static void AddRange<T>(IEnumerable<T> elements)
            where T : ISplineElement
        {
            IncrementVersion();

            bool changed = false;
            foreach (var element in elements)
            {
                if (!Contains(element))
                {
                    if (!changed)
                    {
                        changed = true;
                        IncrementVersion();
                    }

                    selection.Insert(0, new SelectableSplineElement(element));
                }
            }

            if (changed)
                NotifySelectionChanged();
        }

        public static bool Remove<T>(T element)
            where T : ISplineElement
        {
            var index = IndexOf(element);
            if (index >= 0)
            {
                IncrementVersion();
                selection.RemoveAt(index);
                NotifySelectionChanged();
                return true;
            }

            return false;
        }

        public static bool RemoveRange<T>(IReadOnlyList<T> elements)
            where T : ISplineElement
        {
            bool changed = false;
            for (int i = 0; i < elements.Count; ++i)
            {
                var index = IndexOf(elements[i]);
                if (index >= 0)
                {
                    if (!changed)
                    {
                        IncrementVersion();
                        changed = true;
                    }

                    selection.RemoveAt(index);
                }
            }

            if (changed)
                NotifySelectionChanged();

            return changed;
        }

        public static int IndexOf<T>(T element)
            where T : ISplineElement
        {
            for (int i = 0; i < selection.Count; ++i)
                if (IsEqual(element, selection[i]))
                    return i;

            return -1;
        }

        public static bool Contains<T>(T element)
            where T : ISplineElement
        {
            return IndexOf(element) >= 0;
        }

        public static bool IsInSelection(Object splineObject)
        {
            return s_ObjectSet.Contains(splineObject);
        }

        internal static void UpdateObjectSelection(IEnumerable<Object> targets)
        {
            s_ObjectSet.Clear();
            foreach (var target in targets)
                if (target != null)
                    s_ObjectSet.Add(target);

            bool changed = false;
            for (int i = Count - 1; i >= 0; --i)
            {
                if (!s_ObjectSet.Contains(selection[i].target))
                {
                    if (!changed)
                    {
                        changed = true;
                        IncrementVersion();
                    }
                    selection.RemoveAt(i);
                }
                else if(selection[i].tangentIndex > 0)
                {
                    // In the case of a tangent, also check that the tangent is still valid if the spline type
                    // or tangent mode has been updated
                    var spline = SplineToolContext.GetSpline(selection[i].target, selection[i].targetIndex);

                    if(!EditorSplineUtility.AreTangentsModifiable(spline.GetTangentMode(selection[i].knotIndex)))
                    {
                        if (!changed)
                        {
                            changed = true;
                            IncrementVersion();
                        }
                        selection.RemoveAt(i);
                    }
                }
            }

            if (changed)
                NotifySelectionChanged();
        }

        //Used when inserting new elements in spline
        static void OnKnotInserted(SelectableKnot inserted)
        {
            for (var i = 0; i < selection.Count; ++i)
            {
                var knot = selection[i];

                if (knot.target == inserted.SplineInfo.Target
                    && knot.targetIndex == inserted.SplineInfo.Index
                    && knot.knotIndex >= inserted.KnotIndex)
                {
                    ++knot.knotIndex;
                    selection[i] = knot;
                }
            }
        }

        //Used when deleting an element in spline
        static void OnKnotRemoved(SelectableKnot removed)
        {
            for (var i = selection.Count - 1; i >= 0; --i)
            {
                var knot = selection[i];
                if (knot.target == removed.SplineInfo.Target && knot.targetIndex == removed.SplineInfo.Index)
                {
                    if (knot.knotIndex == removed.KnotIndex)
                        selection.RemoveAt(i);
                    else if (knot.knotIndex >= removed.KnotIndex)
                    {
                        --knot.knotIndex;
                        selection[i] = knot;
                    }
                }
            }
        }

        static void IncrementVersion()
        {
            Undo.RecordObject(context, "Spline Selection Changed");

            ++s_SelectionVersion;
            ++context.version;
        }

        static void NotifySelectionChanged()
        {
            changed?.Invoke();
        }
    }
}