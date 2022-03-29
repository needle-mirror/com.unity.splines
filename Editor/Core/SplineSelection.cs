using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    interface ISplineElement
    {
        float3 position { get; set; }
        float3 localPosition { get; set; }
    }

    static class SplineSelection
    {
        public static event Action changed;

        static readonly List<SelectableSplineElement> s_ElementBuffer = new List<SelectableSplineElement>();
        static HashSet<Object> s_ObjectBuffer = new HashSet<Object>();

        static SelectionContext context => SelectionContext.instance;
        static List<SelectableSplineElement> selection => context.selection;

        static int s_SelectionVersion;

        static SplineSelection()
        {
            context.version = 0; 

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
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

        static bool GetKnotFromElement(SelectableSplineElement element, out EditableKnot knot)
        {
            var paths = EditableSplineManager.GetEditableSplines(element.target, false);

            if (paths == null || element.pathIndex >= paths.Count)
            {
                knot = null;
                return false;
            }

            var path = paths[element.pathIndex];
            if (element.knotIndex < 0 || element.knotIndex >= path.knotCount)
            {
                knot = null;
                return false;
            }

            knot = path.GetKnot(element.knotIndex);
            return true;
        }

        static bool GetTangentFromElement(SelectableSplineElement element, out EditableTangent tangent)
        {
            if (!GetKnotFromElement(element, out EditableKnot knot))
            {
                tangent = null;
                return false;
            }

            tangent = knot.GetTangent(element.tangentIndex);
            return tangent != null;
        }

        public static void GetSelectedKnots(List<EditableKnot> knots)
        {
            knots.Clear();
            foreach (var element in selection)
                if (element.isKnot && GetKnotFromElement(element, out EditableKnot knot))
                    knots.Add(knot);
        }

        public static void GetSelectedKnots(IEnumerable<Object> targets, List<EditableKnot> knots)
        {
            knots.Clear();
            GetSelectedElementsInternal(targets, s_ElementBuffer);
            foreach (var element in s_ElementBuffer)
                if (element.isKnot && GetKnotFromElement(element, out EditableKnot knot))
                    knots.Add(knot);
        }

        public static void GetSelectedTangents(List<EditableTangent> tangents)
        {
            tangents.Clear();
            foreach (var element in selection)
                if (element.isTangent && GetTangentFromElement(element, out EditableTangent tangent))
                    tangents.Add(tangent);
        }

        public static void GetSelectedTangents(IEnumerable<Object> targets, List<EditableTangent> tangents)
        {
            tangents.Clear();
            GetSelectedElementsInternal(targets, s_ElementBuffer);
            foreach (var element in s_ElementBuffer)
                if (element.isTangent && GetTangentFromElement(element, out EditableTangent tangent))
                    tangents.Add(tangent);
        }

        public static int count => selection.Count;

        static ISplineElement ToSplineElement(SelectableSplineElement rawElement)
        {
            if (rawElement.isKnot)
            {
                if (GetKnotFromElement(rawElement, out EditableKnot knot))
                    return knot;
            }
            else if (rawElement.isTangent)
            {
                if (GetTangentFromElement(rawElement, out EditableTangent tangent))
                    return tangent;
            }

            return null;
        }

        public static ISplineElement GetActiveElement()
        {
            //Get first valid element
            foreach (var rawElement in selection)
            {
                var element = ToSplineElement(rawElement);
                if (element != null)
                    return element;
            }

            return null;
        }

        public static void GetSelectedElements(ICollection<ISplineElement> elements)
        {
            elements.Clear();
            foreach (var rawElement in selection)
            {
                var element = ToSplineElement(rawElement);
                if (element != null)
                    elements.Add(element);
            }
        }

        public static void GetSelectedElements(IEnumerable<Object> targets, ICollection<ISplineElement> elements)
        {
            elements.Clear();
            GetSelectedElementsInternal(targets, s_ElementBuffer);
            foreach (var rawElement in s_ElementBuffer)
            {
                var element = ToSplineElement(rawElement);
                if (element != null)
                    elements.Add(element);
            }
        }

        static void GetSelectedElementsInternal(IEnumerable<Object> targets, List<SelectableSplineElement> results)
        {
            results.Clear();
            foreach (var element in selection)
                foreach(var target in targets)
                {
                    if(target != null && element.target == target)
                    {
                        results.Add(element);
                        break;
                    }
                }
        }
        
        public static bool IsActiveElement(ISplineElement element)
        {
            switch (element)
            {
                case EditableKnot knot: return IsActiveElement(knot);
                case EditableTangent tangent: return IsActiveElement(tangent);
                default: return false;
            }
        }

        public static bool IsActiveElement(EditableKnot knot)
        {
            return IsActiveElement(new SelectableSplineElement(knot));
        }

        public static bool IsActiveElement(EditableTangent tangent)
        {
            return IsActiveElement(new SelectableSplineElement(tangent));
        }

        static bool IsActiveElement(SelectableSplineElement element)
        {
            return selection.Count > 0 && selection[0].Equals(element);
        }

        public static void SetActive(ISplineElement element)
        {
            switch (element)
            {
                case EditableKnot knot:
                    SetActive(knot);
                    break;
                case EditableTangent tangent:
                    SetActive(tangent);
                    break;
            }
        }

        public static void SetActive(EditableKnot knot)
        {
            SetActiveElement(new SelectableSplineElement(knot));
        }

        public static void SetActive(EditableTangent tangent)
        {
            SetActiveElement(new SelectableSplineElement(tangent));
        }

        static void SetActiveElement(SelectableSplineElement element)
        {
            int index = selection.IndexOf(element);
            if (index == 0)
                return;

            IncrementVersion();

            if (index > 0)
                selection.RemoveAt(index);
            
            selection.Insert(0, element);

            if(element.target is Component component)
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

        public static void Add(ISplineElement element)
        {
            switch (element)
            {
                case EditableKnot knot:
                    Add(knot);
                    break;
                case EditableTangent tangent:
                    Add(tangent);
                    break;
            }
        }

        public static void Add(IEnumerable<ISplineElement> elements)
        {
            IncrementVersion();

            bool changed = false;
            foreach (var element in elements)
                changed |= AddElement(new SelectableSplineElement(element));

            if (changed)
                NotifySelectionChanged();
        }

        public static void Add(EditableKnot knot)
        {
            IncrementVersion();

            if (AddElement(new SelectableSplineElement(knot)))
                NotifySelectionChanged();
        }

        public static void Add(IEnumerable<EditableKnot> knots)
        {
            IncrementVersion();

            bool changed = false;
            foreach (var knot in knots)
                changed |= AddElement(new SelectableSplineElement(knot));

            if (changed)
                NotifySelectionChanged();
        }

        public static void Add(EditableTangent tangent)
        {
            IncrementVersion();

            if (AddElement(new SelectableSplineElement(tangent)))
                NotifySelectionChanged();
        }

        public static void Add(IEnumerable<EditableTangent> tangents)
        {
            IncrementVersion();

            bool changed = false;
            foreach (var tangent in tangents)
                changed |= AddElement(new SelectableSplineElement(tangent));

            if (changed)
                NotifySelectionChanged();
        }

        static bool AddElement(SelectableSplineElement element)
        {
            if (!selection.Contains(element))
            {
                selection.Insert(0,element);
                return true;
            }

            return false;
        }

        public static bool Remove(ISplineElement element)
        {
            switch (element)
            {
                case EditableKnot knot: return Remove(knot);
                case EditableTangent tangent: return Remove(tangent);
                default: return false;
            }
        }

        public static bool Remove(EditableKnot knot)
        {
            IncrementVersion();

            return RemoveElement(new SelectableSplineElement(knot));
        }

        public static bool Remove(EditableTangent tangent)
        {
            IncrementVersion();

            return RemoveElement(new SelectableSplineElement(tangent));
        }

        static bool RemoveElement(SelectableSplineElement element) 
        {
            if (selection.Remove(element))
            {
                NotifySelectionChanged();
                return true;
            }

            return false;
        }

        public static bool Contains(ISplineElement element)
        {
            switch (element)
            {
                case EditableKnot knot: return Contains(knot);
                case EditableTangent tangent: return Contains(tangent);
                default: return false;
            }
        }

        public static bool Contains(EditableKnot knot)
        {
            return ContainsElement(new SelectableSplineElement(knot));
        }

        public static bool Contains(EditableTangent tangent)
        {
            return ContainsElement(new SelectableSplineElement(tangent));
        }

        static bool ContainsElement(SelectableSplineElement element)
        {
            return selection.Contains(element);
        }

        internal static void UpdateObjectSelection(IEnumerable<Object> targets)
        {
            s_ObjectBuffer.Clear();
            foreach (var target in targets)
                if (target != null)
                    s_ObjectBuffer.Add(target);
            
            IncrementVersion();
            if (selection.RemoveAll(ObjectRemovePredicate) > 0)
                NotifySelectionChanged();
        }

        static bool ObjectRemovePredicate(SelectableSplineElement element)
        {
            return !s_ObjectBuffer.Contains(element.target);
        }

        //Used when inserting new elements in spline
        internal static void MoveAllIndexUpFromIndexToEnd(IEditableSpline spline, int index)
        {
            for (var i = 0; i < selection.Count; ++i)
            {
                var knot = selection[i];
                if (knot.IsFromPath(spline))
                {
                    if (knot.knotIndex >= index)
                        ++knot.knotIndex;

                    selection[i] = knot;
                }
            }
        }

        //Used when deleting an element in spline
        internal static void OnKnotRemoved(IEditableSpline spline, int index)
        {
            for (var i = selection.Count - 1; i >= 0; --i)
            {
                var knot = selection[i];
                if (knot.IsFromPath(spline))
                {
                    if (knot.knotIndex == index)
                        selection.RemoveAt(i);
                    else if (knot.knotIndex >= index)
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
