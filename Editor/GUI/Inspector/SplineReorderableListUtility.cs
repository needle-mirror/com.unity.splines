using System;
using System.Collections.Generic;
using System.Linq;
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
        static Dictionary<int, ReorderableList> s_SplinesReorderableLists = new ();
        static Dictionary<int, ReorderableList> s_KnotsReorderableLists = new ();

        static List<SplineInfo> s_SplineInfos = new ();
        static Dictionary<int, List<int>> s_SelectedKnotsIndexes = new ();

        static bool s_SplineSelectionDirty = false;

        static SplineReorderableListUtility()
        {
            Selection.selectionChanged += ClearReorderableLists;
            SplineSelection.changed += OnSplineSelectionChanged;
        }

        static void ClearReorderableLists()
        {
            s_KnotsReorderableLists.Clear();
            s_SplinesReorderableLists.Clear();
        }

        static void OnSplineSelectionChanged()
        {
            EditorSplineUtility.GetSplinesFromTargets(SplineSelection.GetAllSelectedTargets(), s_SplineInfos);

            if (s_SplineInfos.Count == 0)
            {
                foreach (var kvp in s_KnotsReorderableLists)
                {
                    var list = kvp.Value;
                    list.ClearSelection();

                    if (s_SelectedKnotsIndexes.TryGetValue(kvp.Key, out var indexesList))
                        indexesList?.Clear();

                    try
                    {
                        var target = list.serializedProperty.serializedObject.targetObject;
                        if (target != null)
                            EditorUtility.SetDirty(target);
                    }
                    catch (ArgumentNullException)
                    {
                    }
                }
            }

            foreach(var splineInfo in s_SplineInfos)
            {
                var key = splineInfo.GetHashCode();
                if(s_KnotsReorderableLists.TryGetValue(key, out var list))
                {
                    s_SelectedKnotsIndexes.TryGetValue(key, out List<int> indexesList);
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
                    if(!s_SelectedKnotsIndexes.TryGetValue(key, out List<int> indexesList))
                    {
                        indexesList = new List<int>();
                        s_SelectedKnotsIndexes.Add(key, indexesList);
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
            s_SplineSelectionDirty = true;
        }

        static int s_LastSelectedIndex = -1;

        public static ReorderableList GetSplinesReorderableList(SerializedProperty property)
        {
            var key = property.serializedObject.targetObject.GetInstanceID();
            if (s_SplinesReorderableLists.TryGetValue(key, out var list))
            {
                try
                {
                    SerializedProperty.EqualContents(list.serializedProperty, property);
                    return list;
                }
                catch(NullReferenceException)
                {
                    s_SplinesReorderableLists.Remove(key);
                }
            }

            list = new ReorderableList(property.serializedObject, property, true, false, true, true);
            s_SplinesReorderableLists.Add(key, list);

            list.multiSelect = true;

            list.elementHeightCallback = index =>
            {
                //easy way to pass the index to the property drawer: Using the unused GUIContent label to do that
                return EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(index), new GUIContent(index.ToString()));
            };

            list.drawElementCallback = (position, listIndex, _, _) =>
            {
                var ppte = property.GetArrayElementAtIndex(listIndex);

                CheckForSplineSelectionUpdate(list);

                EditorGUI.indentLevel++;
                //easy way to pass the index to the property drawer: Using the unused GUIContent label to do that
                EditorGUI.PropertyField(position, ppte, new GUIContent(listIndex.ToString()));
                EditorGUI.indentLevel--;
            };

            list.onReorderCallbackWithDetails = (_, index, newIndex) =>
            {
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    property.serializedObject.ApplyModifiedProperties();
                    container.KnotLinkCollection.SplineIndexChanged(index, newIndex);
                    property.serializedObject.Update();
                }
            };

            list.onMouseDragCallback = reorderableList =>
            {
                bool noElementSelectedForDrag = reorderableList.selectedIndices.Count == 0;
                if (s_LastSelectedIndex >= 0 && noElementSelectedForDrag)
                    list.Select(s_LastSelectedIndex);
            };

            list.onMouseUpCallback = reorderableList =>
            {
                s_LastSelectedIndex = -1;
            };

            list.onSelectCallback = reorderableList =>
            {
                if(!EditorGUI.actionKey)
                {
                    SplineSelection.ClearSplineSelection();
                    foreach(var kvp in s_SplinesReorderableLists)
                    {
                        if(kvp.Value != reorderableList)
                            kvp.Value.ClearSelection();
                    }
                }

                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    var previousSelection = SplineSelection.SelectedSplines.GetRange(0, SplineSelection.SelectedSplines.Count);
                    var selectedIndices = reorderableList.selectedIndices;
                    //Remove elements that are not present anymore
                    for(int i = 0; i < previousSelection.Count;  i++)
                    {
                        var selected = previousSelection[i];
                        if(selected.Container != container || !selectedIndices.Contains(selected.Index))
                            SplineSelection.Remove(selected);
                    }

                    //Update selection: Add or Remove element depending if
                    foreach(var selectedSplineIndex in selectedIndices)
                    {
                        var splineInfo = new SplineInfo(container, selectedSplineIndex);
                        var contained = previousSelection.Contains(splineInfo);

                        var addToSelection = !contained || Event.current.shift;
                        if(addToSelection)
                            SplineSelection.Add(splineInfo);
                    }
                }
                SceneView.RepaintAll();
            };

            list.onAddCallback = reorderableList =>
            {
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    Undo.RecordObject(property.serializedObject.targetObject, "Adding Spline to SplineContainer");
                    var selectedIndices = reorderableList.selectedIndices;
                    var count = 0;
                    if(selectedIndices.Count > 0)
                    {
                        foreach(var index in selectedIndices)
                        {
                            var spline = container.AddSpline();
                            spline.Copy(container.Splines[index]);
                            count++;
                        }
                    }
                    else
                    {
                        var spline = container.AddSpline();
                        spline.Copy(container.Splines[^2]);
                        count++;
                    }

                    reorderableList.ClearSelection();
                    var maxCount = container.Splines.Count;
                    reorderableList.SelectRange(maxCount - count, maxCount - 1);
                }
            };

            list.onRemoveCallback = reorderableList =>
            {
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    Undo.RecordObject(property.serializedObject.targetObject, "Removing Spline from SplineContainer");
                    var selectedIndices = reorderableList.selectedIndices;
                    for(int i = selectedIndices.Count - 1; i >= 0; i--)
                        container.RemoveSplineAt(selectedIndices[i]);

                    reorderableList.ClearSelection();
                    SceneView.RepaintAll();
                }
            };

            return list;
        }

        static void CheckForSplineSelectionUpdate(ReorderableList list)
        {
            if (s_SplineSelectionDirty)
            {
                var ppteList = list.serializedProperty;
                if (ppteList.serializedObject.targetObject is ISplineContainer container)
                {
                    var splineSelection = SplineSelection.SelectedSplines;
                    for (int i = 0; i < splineSelection.Count; ++i)
                    {
                        var selected = splineSelection[i];
                        if (selected.Container == container && !list.IsSelected(selected.Index))
                            list.Select(selected.Index, true);
                    }
                }
                s_SplineSelectionDirty = false;
            }
        }

        public static ReorderableList GetKnotReorderableList(SerializedProperty property, SerializedProperty knotsProperty, int splineIndex, Action<SplineInfo, int> onKnotChanged = null)
        {
            List<SplineInfo> splineInfos = new ();
            SplineInfo? currentSpline = null;
            int key = 0;
            if(EditorSplineUtility.TryGetSplinesFromTarget(property.serializedObject.targetObject, splineInfos))
            {
                currentSpline = splineInfos[0];
                if(splineIndex > 0 && splineIndex < splineInfos.Count)
                    currentSpline = splineInfos[splineIndex];

                key = currentSpline.GetHashCode();
            }
            else
                key = property.propertyPath.GetHashCode();

            if (s_KnotsReorderableLists.TryGetValue(key, out var list))
            {
                try
                {
                    SerializedProperty.EqualContents(list.serializedProperty, knotsProperty);
                    return list;
                }
                catch(NullReferenceException)
                {
                    s_KnotsReorderableLists.Remove(key);
                    s_SelectedKnotsIndexes.Remove(key);
                }
            }

            list = new ReorderableList(knotsProperty.serializedObject, knotsProperty, true, false, true, true);
            s_KnotsReorderableLists.Add(key, list);
            if(!s_SelectedKnotsIndexes.TryGetValue(key, out _))
                s_SelectedKnotsIndexes.Add(key, new List<int>());

            list.multiSelect = true;

            list.elementHeightCallback = index =>
            {
                return EditorGUI.GetPropertyHeight(knotsProperty.GetArrayElementAtIndex(index));
            };

            list.drawElementCallback = (position, listIndex, _, _) =>
            {
                var ppte = knotsProperty.GetArrayElementAtIndex(listIndex);

                if(s_SelectedKnotsIndexes.TryGetValue(key, out List<int> indexesList))
                {
                    if(!list.IsSelected(listIndex) && indexesList.Contains(listIndex))
                        list.Select(listIndex, true);
                }

                ppte.isExpanded = true;
                EditorGUI.LabelField(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position),  new GUIContent($"Knot [{listIndex}]"));
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, ppte);
                if (EditorGUI.EndChangeCheck())
                {
                    if (currentSpline != null)
                        onKnotChanged?.Invoke(currentSpline.Value, listIndex);
                }

                EditorGUI.indentLevel--;
            };

            list.onReorderCallbackWithDetails = (_, index, newIndex) =>
            {
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    property.serializedObject.ApplyModifiedProperties();
                    container.KnotLinkCollection.KnotIndexChanged(splineIndex, index, newIndex);
                    property.serializedObject.Update();
                }
            };

            list.onSelectCallback = reorderableList =>
            {
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    SplineSelection.changed -= OnSplineSelectionChanged;
                    if(!EditorGUI.actionKey)
                    {
                        SplineSelection.Clear();
                        SplineSelection.ClearSplineSelection();
                        foreach (var kvp in s_KnotsReorderableLists)
                        {
                            if(kvp.Value != reorderableList)
                                kvp.Value.ClearSelection();
                        }
                    }

                    List<int> currentSelection = null;
                    foreach(var kvp in s_KnotsReorderableLists)
                    {
                        s_SelectedKnotsIndexes[kvp.Key].Clear();
                        if(kvp.Value == reorderableList)
                            currentSelection = s_SelectedKnotsIndexes[kvp.Key];
                    }

                    var selectedIndices = reorderableList.selectedIndices;
                    for(int i = 0; i < reorderableList.count; ++i)
                    {
                        var knot = new SelectableKnot(new SplineInfo(container, splineIndex), i);

                        var isInListSelection = selectedIndices.Contains(i);
                        var isInSplineSelection = SplineSelection.Contains(knot);

                        if (isInListSelection)
                        {
                            currentSelection?.Add(i);
                            if(!isInSplineSelection)
                            {
                                SplineSelection.Add(knot);

                                var splineInfo = knot.SplineInfo;
                                if (SplineSelection.HasActiveSplineSelection() && !SplineSelection.Contains(splineInfo))
                                    SplineSelection.Add(splineInfo);
                            }
                        }

                        if(!isInListSelection && isInSplineSelection)
                            SplineSelection.Remove(knot);
                    }
                    //Has to be handled separately to correctly work with SHIFT select
                    OnSplineSelectionChanged();
                    SplineSelection.changed += OnSplineSelectionChanged;
                }
            };

            list.onAddCallback = reorderableList =>
            {
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    property.serializedObject.ApplyModifiedProperties();
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

                    if(s_SelectedKnotsIndexes.TryGetValue(key, out List<int> indexesList))
                    {
                        indexesList.Clear();
                        indexesList.Add(reorderableList.index);
                    }

                    //Force inspector to repaint
                    EditorUtility.SetDirty(list.serializedProperty.serializedObject.targetObject);
                    property.serializedObject.Update();
                }
                else // if the Spline is not in a ISplineContainer, make default reorderable list
                    ReorderableList.defaultBehaviours.DoAddButton(reorderableList);
            };

            list.onRemoveCallback = reorderableList =>
            {
                if(property.serializedObject.targetObject is ISplineContainer container)
                {
                    property.serializedObject.ApplyModifiedProperties();
                    SplineSelection.changed -= OnSplineSelectionChanged;
                    var toRemove = new List<BezierKnot>();
                    var selectedIndices = reorderableList.selectedIndices;
                    foreach(int index in selectedIndices.ToList())
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
                    SplineSelection.changed += OnSplineSelectionChanged;
                    property.serializedObject.Update();
                }

                ReorderableList.defaultBehaviours.DoRemoveButton(reorderableList);
                reorderableList.ClearSelection();

                if(s_SelectedKnotsIndexes.TryGetValue(key, out List<int> indexesList))
                    indexesList.Clear();
            };

            return list;
        }
    }
}