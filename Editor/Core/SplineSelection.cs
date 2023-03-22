using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    static class SplineSelection
    {
        public static event Action changed;

        static readonly HashSet<Object> s_ObjectSet = new HashSet<Object>();
        static readonly HashSet<SplineInfo> s_SelectedSplineInfo = new HashSet<SplineInfo>();
        static Object[] s_SelectedTargetsBuffer = new Object[0];

        static SelectionContext context => SelectionContext.instance;
        internal static List<SelectableSplineElement> selection => context.selection;

        // Tracks selected splines in the SplineReorderableList
        static List<SplineInfo> s_SelectedSplines = new ();
        public static int Count => selection.Count;
        static HashSet<SelectableTangent> s_AdjacentTangentCache = new HashSet<SelectableTangent>();

        static int s_SelectionVersion;

        static SplineSelection()
        {
            context.version = 0;

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorSplineUtility.knotInserted += OnKnotInserted;
            EditorSplineUtility.knotRemoved += OnKnotRemoved;
            Selection.selectionChanged += OnSelectionChanged;
        }

        static void OnSelectionChanged()
        {
            ClearInspectorSelectedSplines();
        }

        static void OnUndoRedoPerformed()
        {
            if (context.version != s_SelectionVersion)
            {
                s_SelectionVersion = context.version;
                ClearInspectorSelectedSplines();
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

        public static void GetElements<T>(SplineInfo target, ICollection<T> results)
            where T : ISplineElement
        {
            results.Clear();
            for (int i = 0; i < Count; ++i)
                if (TryGetElement(selection[i], target, out T result))
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

        internal static IEnumerable<SplineInfo> SelectedSplines => s_SelectedSplines;

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
            return element.SplineInfo.Object == selectionData.target
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

        internal static void Set(IEnumerable<SelectableSplineElement> selection)
        {
            IncrementVersion();
            context.selection.Clear();
            context.selection.AddRange(selection);
            NotifySelectionChanged();
        }

        public static bool Add<T>(T element)
            where T : ISplineElement
        {
            if (Contains(element))
                return false;

            IncrementVersion();
            selection.Insert(0, new SelectableSplineElement(element));
            NotifySelectionChanged();
            return true;
        }

        public static void AddRange<T>(IEnumerable<T> elements)
            where T : ISplineElement
        {
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

        internal static void UpdateObjectSelection(IEnumerable<Object> targets)
        {
            s_ObjectSet.Clear();
            foreach (var target in targets)
                if (target != null)
                    s_ObjectSet.Add(target);

            bool changed = false;
            for (int i = Count - 1; i >= 0; --i)
            {
                bool removeElement = false;
                if (!EditorSplineUtility.Exists(selection[i].target as ISplineContainer, selection[i].targetIndex))
                {
                    removeElement = true;
                }
                else if (!s_ObjectSet.Contains(selection[i].target))
                {
                    ClearInspectorSelectedSplines();
                    removeElement = true;
                }
                else if(selection[i].tangentIndex > 0)
                {
                    // In the case of a tangent, also check that the tangent is still valid if the spline type
                    // or tangent mode has been updated
                    var spline = SplineToolContext.GetSpline(selection[i].target, selection[i].targetIndex);
                    removeElement = !SplineUtility.AreTangentsModifiable(spline.GetTangentMode(selection[i].knotIndex));
                }

                if (removeElement)
                {
                    if (!changed)
                    {
                        changed = true;
                        IncrementVersion();
                    }

                    if (i < selection.Count)
                        selection.RemoveAt(i);
                }
            }

            if (changed)
            {
                RebuildAdjacentCache();
                NotifySelectionChanged();
            }
        }

        //Used when inserting new elements in spline
        static void OnKnotInserted(SelectableKnot inserted)
        {
            for (var i = 0; i < selection.Count; ++i)
            {
                var knot = selection[i];

                if (knot.target == inserted.SplineInfo.Object
                    && knot.targetIndex == inserted.SplineInfo.Index
                    && knot.knotIndex >= inserted.KnotIndex)
                {
                    ++knot.knotIndex;
                    selection[i] = knot;
                }
            }
            RebuildAdjacentCache();
        }

        //Used when deleting an element in spline
        static void OnKnotRemoved(SelectableKnot removed)
        {
            bool changed = false;
            for (var i = selection.Count - 1; i >= 0; --i)
            {
                var knot = selection[i];
                if (knot.target == removed.SplineInfo.Object && knot.targetIndex == removed.SplineInfo.Index)
                {
                    if (knot.knotIndex == removed.KnotIndex)
                    {
                        if (!changed)
                        {
                            changed = true;
                            IncrementVersion();
                        }
                        selection.RemoveAt(i);
                    }
                    else if (knot.knotIndex >= removed.KnotIndex)
                    {
                        --knot.knotIndex;
                        selection[i] = knot;
                    }
                }
            }
            RebuildAdjacentCache();

            if (changed)
                NotifySelectionChanged();
        }

        static void IncrementVersion()
        {
            Undo.RecordObject(context, "Spline Selection Changed");

            ++s_SelectionVersion;
            ++context.version;
        }

        static void NotifySelectionChanged()
        {
            RebuildAdjacentCache();
            changed?.Invoke();
        }

        static bool TryGetSplineInfo(SelectableSplineElement element, out SplineInfo splineInfo)
        {
            //Checking null in case the object was destroyed
            if (element.target != null && element.target is ISplineContainer container)
            {
                splineInfo = new SplineInfo(container, element.targetIndex);
                return true;
            }

            splineInfo = default;
            return false;
        }

        static bool TryCast(SelectableSplineElement element, out SelectableTangent result)
        {
            if (TryGetSplineInfo(element, out var splineInfo) && element.tangentIndex >= 0)
            {
                result = new SelectableTangent(splineInfo, element.knotIndex, element.tangentIndex);
                return true;
            }

            result = default;
            return false;
        }

        static bool TryCast(SelectableSplineElement element, out SelectableKnot result)
        {
            if (TryGetSplineInfo(element, out var splineInfo) && element.tangentIndex < 0)
            {
                result = new SelectableKnot(splineInfo, element.knotIndex);
                return true;
            }

            result = default;
            return false;
        }

        internal static bool IsSelectedOrAdjacentToSelected(SelectableTangent tangent)
        {
            return s_AdjacentTangentCache.Contains(tangent);
        }

        static void RebuildAdjacentCache()
        {
            s_AdjacentTangentCache.Clear();

            foreach(var element in selection)
            {
                SelectableTangent previousOut, currentIn, currentOut, nextIn;
                if(TryCast(element, out SelectableKnot knot))
                    EditorSplineUtility.GetAdjacentTangents(knot, out previousOut, out currentIn, out currentOut, out nextIn);
                else if(TryCast(element, out SelectableTangent tangent))
                    EditorSplineUtility.GetAdjacentTangents(tangent, out previousOut, out currentIn, out currentOut, out nextIn);
                else
                    continue;

                s_AdjacentTangentCache.Add(previousOut);
                s_AdjacentTangentCache.Add(currentIn);
                s_AdjacentTangentCache.Add(currentOut);
                s_AdjacentTangentCache.Add(nextIn);
            }
        }

        internal static void ClearInspectorSelectedSplines()
        {
            s_SelectedSplines.Clear();
        }

        internal static bool HasActiveSplineSelection()
        {
            return s_SelectedSplines.Count > 0;
        }

        // Inspector spline selection is a one-way operation. It can only be set by the SplineReorderableList. Changes
        // to selected splines in the Scene or Hierarchy will only clear the selected inspector splines.
        internal static void SetInspectorSelectedSplines(SplineContainer container, IEnumerable<int> selected)
        {
            s_SelectedSplines.Clear();
            foreach (var index in selected)
                s_SelectedSplines.Add(new SplineInfo(container, index));

            IncrementVersion();
            context.selection = selection.Where(x => x.target == container &&
                (selected.Contains(x.targetIndex) || container.KnotLinkCollection.TryGetKnotLinks(new SplineKnotIndex(x.targetIndex, x.knotIndex), out _))).ToList();
            NotifySelectionChanged();
        }

        internal static bool Contains(SplineInfo info)
        {
            return s_SelectedSplines.Contains(info);
        }
    }
}
