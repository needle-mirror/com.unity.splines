using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Splines;

using UObject = UnityEngine.Object;

namespace UnityEditor.Splines
{
    [InitializeOnLoad]
    static class SplineReorderableListUtility
    {
        static Dictionary<int, ReorderableList> s_ReorderableLists = new Dictionary<int, ReorderableList>();

        static List<SplineInfo> s_SplineInfos = new List<SplineInfo>();
        static Dictionary<int, List<int>> s_SelectedIndexes = new Dictionary<int, List<int>>();

        static SplineReorderableListUtility()
        {
            Selection.selectionChanged += ClearReorderableLists;
            SplineSelection.changed += OnSplineSelectionChanged;
        }

        static void ClearReorderableLists()
        {
            s_ReorderableLists.Clear();
        }

        static void OnSplineSelectionChanged()
        {
            EditorSplineUtility.GetSplinesFromTargets(SplineSelection.GetAllSelectedTargets(), s_SplineInfos);
            foreach(var splineInfo in s_SplineInfos)
            {
                var key = splineInfo.GetHashCode();
                if(s_ReorderableLists.TryGetValue(key, out var list))
                {
                    s_SelectedIndexes.TryGetValue(key, out List<int> indexesList);
                    list.ClearSelection();
                    indexesList?.Clear();

                    for(int i = 0; i < splineInfo.Spline.Count; i++)
                    {
                        var knot = new SelectableKnot(new SplineInfo(splineInfo.Container, splineInfo.Index), i);
                        if(SplineSelection.Contains(knot))
                        {
                            indexesList?.Add(i);
                            EditorUtility.SetDirty(list.serializedProperty.serializedObject.targetObject);
                        }
                    }
                }
                else
                {
                    //The list has not been created but a new spline has, preparing the selection for that spline
                    if(!s_SelectedIndexes.TryGetValue(key, out List<int> indexesList))
                    {
                        indexesList = new List<int>();
                        s_SelectedIndexes.Add(key, indexesList);
                    }
                    indexesList?.Clear();

                    for(int i = 0; i < splineInfo.Spline.Count; i++)
                    {
                        var knot = new SelectableKnot(new SplineInfo(splineInfo.Container, splineInfo.Index), i);
                        if(SplineSelection.Contains(knot))
                            indexesList?.Add(i);
                    }
                }
            }
        }

        public static ReorderableList GetKnotReorderableList(SerializedProperty property, SerializedProperty knotsProperty, int splineIndex)
        {
            var splineInfos = EditorSplineUtility.GetSplinesFromTarget(property.serializedObject.targetObject);
            var currentSpline = splineInfos[0];
            if(splineIndex > 0 && splineIndex < splineInfos.Length)
                currentSpline = splineInfos[splineIndex];

            var key = currentSpline.GetHashCode();
            if (s_ReorderableLists.TryGetValue(key, out var list))
            {
                try
                {
                    SerializedProperty.EqualContents(list.serializedProperty, knotsProperty);
                    return list;
                }
                catch(NullReferenceException)
                {
                    s_ReorderableLists.Remove(key);
                    s_SelectedIndexes.Remove(key);
                }
            }

            list = new ReorderableList(knotsProperty.serializedObject, knotsProperty, true, false, true, true);
            s_ReorderableLists.Add(key, list);
            if(!s_SelectedIndexes.TryGetValue(key, out _))
                s_SelectedIndexes.Add(key, new List<int>());

            list.multiSelect = true;

            list.elementHeightCallback = index =>
            {
                return EditorGUI.GetPropertyHeight(knotsProperty.GetArrayElementAtIndex(index));
            };

            list.drawElementCallback = (position, listIndex, _, _) =>
            {
                var ppte = knotsProperty.GetArrayElementAtIndex(listIndex);

                if(s_SelectedIndexes.TryGetValue(key, out List<int> indexesList))
                {
                    if(!list.IsSelected(listIndex) && indexesList.Contains(listIndex))
                        list.Select(listIndex, true);
                }

                ppte.isExpanded = true;
                EditorGUI.LabelField(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position),  new GUIContent($"Knot [{listIndex}]"));
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(position, ppte);
                EditorGUI.indentLevel--;
            };

            list.onSelectCallback = reorderableList =>
            {
                SplineSelection.changed -= OnSplineSelectionChanged;
                if(!EditorGUI.actionKey)
                {
                    SplineSelection.Clear();
                    foreach(var kvp in s_ReorderableLists)
                    {
                        if(kvp.Value != reorderableList)
                            kvp.Value.ClearSelection();
                    }
                }

                List<int> currentSelection = null;
                foreach(var kvp in s_ReorderableLists)
                {
                    s_SelectedIndexes[kvp.Key].Clear();
                    if(kvp.Value == reorderableList)
                        currentSelection = s_SelectedIndexes[kvp.Key];
                }

                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    var selectedIndices = reorderableList.selectedIndices;
                    for(int i = 0; i < reorderableList.count; i++)
                    {
                        var knot = new SelectableKnot(new SplineInfo(container, splineIndex), i);

                        var isInListSelection = selectedIndices.Contains(i);
                        var isInSplineSelection = SplineSelection.Contains(knot);

                        if(isInListSelection)
                            currentSelection?.Add(i);

                        if(isInListSelection && !isInSplineSelection)
                            SplineSelection.Add(knot);

                        if(!isInListSelection && isInSplineSelection)
                            SplineSelection.Remove(knot);
                    }
                }
                SplineSelection.changed += OnSplineSelectionChanged;
            };

            list.onAddCallback = reorderableList =>
            {
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    var selectedIndex = reorderableList.index;
                    if(selectedIndex < reorderableList.count - 1)
                    {
                        var knot = EditorSplineUtility.InsertKnot(new SplineInfo(container, splineIndex), selectedIndex + 1, 0.5f);

                        SplineSelection.Set(knot);

                        reorderableList.index = selectedIndex + 1;
                        reorderableList.Select(selectedIndex + 1);
                    }
                    else // last element from the list
                    {
                        var knot = new SelectableKnot(new SplineInfo(container, splineIndex), selectedIndex);
                        var splineInfo = new SplineInfo(container, splineIndex);
                        if(knot.IsValid())
                        {
                            EditorSplineUtility.AddKnotToTheEnd(
                                splineInfo,
                                knot.Position + 3f * knot.TangentOut.Direction,
                                math.rotate(knot.LocalToWorld, math.up()),
                                knot.TangentOut.Direction);
                        }
                        else
                        {
                            EditorSplineUtility.AddKnotToTheEnd(
                                splineInfo,
                                splineInfo.Transform.position,
                                math.up(),
                                math.forward());
                        }

                        reorderableList.index = reorderableList.count;
                        reorderableList.Select(reorderableList.count);
                    }

                    if(s_SelectedIndexes.TryGetValue(key, out List<int> indexesList))
                    {
                        indexesList.Clear();
                        indexesList.Add(reorderableList.index);
                    }

                    //Force inspector to repaint
                    EditorUtility.SetDirty(list.serializedProperty.serializedObject.targetObject);
                }
            };

            list.onRemoveCallback = reorderableList =>
            {
                SplineSelection.changed += OnSplineSelectionChanged;
                var toRemove = new List<BezierKnot>();
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    var selectedIndices = reorderableList.selectedIndices;
                    foreach(int index in selectedIndices)
                    {
                        var knot = new SelectableKnot(new SplineInfo(container, splineIndex), index);
                        EditorSplineUtility.RecordObject(knot.SplineInfo, "Removing Knot");
                        //Removing from selection
                        SplineSelection.Remove(knot);
                        //Storing to prevent messing up indexes
                        toRemove.Add( knot.GetBezierKnot(false) );
                    }
                    foreach(var knot in toRemove)
                        container.Splines[splineIndex].Remove(knot);

                    toRemove.Clear();
                }
                SplineSelection.changed += OnSplineSelectionChanged;

                ReorderableList.defaultBehaviours.DoRemoveButton(reorderableList);
                reorderableList.ClearSelection();

                if(s_SelectedIndexes.TryGetValue(key, out List<int> indexesList))
                    indexesList.Clear();
            };

            return list;
        }
    }
}