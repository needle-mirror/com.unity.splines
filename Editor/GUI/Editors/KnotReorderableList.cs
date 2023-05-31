using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    class KnotReorderableList : ReorderableList
    {
        ISplineContainer m_Container;
        int m_ContainerIndex;
        Spline m_Spline;
        SerializedProperty m_KnotElement, m_MetaElement;
        SerializedObject serializedObject => serializedProperty.serializedObject;

        static Dictionary<int, KnotReorderableList> s_Pool = new();

        static KnotReorderableList()
        {
            Selection.selectionChanged += ClearPool;
            SplineSelection.changed += SyncKnotSelection;
        }

        static int GetPropertyHash(SerializedProperty prop)
        {
            if(prop.serializedObject.targetObject == null)
                return 0;

            unchecked
            {
                int hash = prop.serializedObject.targetObject.GetInstanceID();
                hash = hash * 31 * prop.propertyPath.GetHashCode();
                return hash;
            }
        }

        static void ClearPool() => s_Pool.Clear();

        static void SyncKnotSelection()
        {
            foreach (var kvp in s_Pool)
                kvp.Value.SyncSelection();

            // InspectorWindow is private for some reason.
            foreach (var win in Resources.FindObjectsOfTypeAll<EditorWindow>())
                if (win.GetType().Name.Contains("Inspector"))
                    win.Repaint();
        }

        public static KnotReorderableList Get(SerializedProperty splineProperty)
        {
            int hash = GetPropertyHash(splineProperty);
            if (!s_Pool.TryGetValue(hash, out var list))
                s_Pool.Add(hash, list = new KnotReorderableList(splineProperty));
            list.Init(splineProperty);
            return list;
        }

        public KnotReorderableList(SerializedProperty splineProperty)
            : base(splineProperty.serializedObject, splineProperty.FindPropertyRelative("m_Knots"), true, false, true, true)
        {
            Init(splineProperty);
            multiSelect = true;
            elementHeightCallback = GetElementHeight;
            drawElementCallback = OnDrawElement;
            onReorderCallbackWithDetails = OnReorder;
            onSelectCallback = OnSelect;
            onAddCallback = OnAdd;
            onRemoveCallback = OnRemove;
            SyncSelection();
        }

        void Init(SerializedProperty splineProperty)
        {
            m_KnotElement = splineProperty.FindPropertyRelative("m_Knots");
            serializedProperty = m_KnotElement;
            m_MetaElement = splineProperty.FindPropertyRelative("m_MetaData");

            // only set the ISplineContainer if we are able to determine the correct index into the splines array
            if (serializedObject.targetObject is ISplineContainer container)
            {
                // make sure that this correctly handles the case where ISplineContainer does not serialize an array.
                // in these cases we'll assert that there is only one spline. if that isn't the case we can't reasonably
                // handle it so just screw it and give up; the UI can accomodate the case where no container is present.
                if(SerializedPropertyUtility.TryGetSplineIndex(splineProperty, out m_ContainerIndex)
                    || container.Splines.Count == 1)
                {
                    m_Container = container;
                    m_Spline = m_Container.Splines[m_ContainerIndex];
                }
            }
        }

        SerializedProperty GetMetaPropertySafe(int i)
        {
            while(m_MetaElement.arraySize < m_KnotElement.arraySize)
                m_MetaElement.InsertArrayElementAtIndex(m_MetaElement.arraySize);
            return m_MetaElement.GetArrayElementAtIndex(i);
        }

        float GetElementHeight(int i) => KnotPropertyDrawerUI.GetPropertyHeight(
                m_KnotElement.GetArrayElementAtIndex(i),
                GetMetaPropertySafe(i), GUIContent.none);

        void OnDrawElement(Rect position, int i, bool isActive, bool isFocused)
        {
            var knot = m_KnotElement.GetArrayElementAtIndex(i);
            var meta = GetMetaPropertySafe(i);

            bool guiChanged = false;

            // For reasons unknown, a nested reorderable list requires indent to be incremented to draw elements
            // with the correct offset. As a hack, we do so when a container is present since we know it will be nested
            // within a spline reorderable list.
            if (m_Container != null)
            {
                ++EditorGUI.indentLevel;
                guiChanged = KnotPropertyDrawerUI.OnGUI(position, knot, meta, new GUIContent($"Knot [{i}]"));
                --EditorGUI.indentLevel;
            }
            else
            {
                guiChanged = KnotPropertyDrawerUI.OnGUI(position, knot, meta, new GUIContent($"Knot [{i}]"));
            }

            if (guiChanged && m_Spline != null)
            {
                serializedObject.ApplyModifiedProperties();
                m_Spline.EnforceTangentModeNoNotify(m_Spline.PreviousIndex(i));
                m_Spline.EnforceTangentModeNoNotify(i);
                m_Spline.EnforceTangentModeNoNotify(m_Spline.NextIndex(i));
                m_Spline.SetDirty(SplineModification.KnotModified, i);

                // delay repaint because SplineCacheUtility is only clearing it's cache on Spline.afterSplineWasModified
                EditorApplication.delayCall += SceneView.RepaintAll;
            }
        }

        static void EnforceTangentModeWithNeighbors(Spline spline, int index)
        {
            if (spline == null)
                return;
            int p = spline.PreviousIndex(index), n = spline.NextIndex(index);
            spline.EnforceTangentModeNoNotify(index);
            if(p != index) spline.EnforceTangentModeNoNotify(p);
            if(n != index) spline.EnforceTangentModeNoNotify(n);
        }

        void OnReorder(ReorderableList reorderableList, int srcIndex, int dstIndex)
        {
            m_MetaElement.MoveArrayElement(srcIndex, dstIndex);

            if (m_Container != null)
            {
                serializedObject.ApplyModifiedProperties();
                m_Container.KnotLinkCollection.KnotIndexChanged(m_ContainerIndex, srcIndex, dstIndex);
                serializedObject.Update();
            }

            EnforceTangentModeWithNeighbors(m_Spline, srcIndex);
            EnforceTangentModeWithNeighbors(m_Spline, dstIndex);
            m_Spline?.SetDirty(SplineModification.KnotReordered, dstIndex);
        }

        void OnSelect(ReorderableList reorderableList)
        {
            if (m_Container != null)
            {

                SplineSelection.ClearInspectorSelectedSplines();

                var selected = selectedIndices.Select(x => new SelectableSplineElement(
                        new SelectableKnot(new SplineInfo(m_Container, m_ContainerIndex), x))).ToList();

                var evt = Event.current;

                // At the time of this callback, selectedIndices is already set with the correct selection accounting
                // for shift, ctrl and command. If any modifiers where present, we need to _not_ clear the selection
                // on other splines but completely replace the selection on this spline.
                if (evt.modifiers == EventModifiers.Command
                    || evt.modifiers == EventModifiers.Shift
                    || evt.modifiers == EventModifiers.Control)
                {
                    selected.AddRange(SplineSelection.selection.Where(x =>
                        !ReferenceEquals(x.target, m_Container) || x.targetIndex != m_ContainerIndex));
                }

                SplineSelection.Set(selected);
                SceneView.RepaintAll();
            }
        }

        void OnAdd(ReorderableList _)
        {
            if (m_Container != null)
            {
                serializedObject.ApplyModifiedProperties();

                var selectedIndex = index;
                var info = new SplineInfo(m_Container, m_ContainerIndex);
                EditorSplineUtility.RecordObject(info, "Add Knot");

                if (selectedIndex < count - 1)
                {
                    var knot = EditorSplineUtility.InsertKnot(info, selectedIndex + 1, 0.5f);

                    SplineSelection.Set(knot);

                    index = selectedIndex + 1;
                    Select(selectedIndex + 1);
                }
                else // last element from the list
                {
                    var knot = new SelectableKnot(info, selectedIndex);
                    if (knot.IsValid())
                    {
                        EditorSplineUtility.AddKnotToTheEnd(
                            info,
                            knot.Position + 3f * knot.TangentOut.Direction,
                            math.rotate(knot.LocalToWorld, math.up()),
                            knot.TangentOut.Direction);
                    }
                    else
                    {
                        EditorSplineUtility.AddKnotToTheEnd(
                            info,
                            info.Transform.position,
                            math.up(),
                            math.forward());
                    }

                    index = count;
                    Select(count);

                }

                serializedObject.Update();
            }
            else // if the Spline is not in a ISplineContainer, make default reorderable list
            {
                defaultBehaviours.DoAddButton(this);
                m_MetaElement.InsertArrayElementAtIndex(m_MetaElement.arraySize);
            }
        }

        void OnRemove(ReorderableList _)
        {
            var toRemove = new List<int>(selectedIndices);
            toRemove.Sort();

            // Mimic behaviour of a list inspector - if nothing's explicitly selected, remove active or last knot.
            if (toRemove.Count == 0 && m_Spline.Count > 0)
                toRemove.Add(index);

            if (m_Container != null)
            {
                var info = new SplineInfo(m_Container, m_ContainerIndex);
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(serializedObject.targetObject, "Remove Knot");

                for (int i = toRemove.Count - 1; i >= 0; --i)
                    EditorSplineUtility.RemoveKnot(new SelectableKnot(info, toRemove[i]));

                SplineSelection.Clear();
                ClearSelection();

                serializedObject.Update();
            }
            else
            {
                defaultBehaviours.DoRemoveButton(this);

                for (int i = toRemove.Count - 1; i >= 0; --i)
                    m_MetaElement.DeleteArrayElementAtIndex(toRemove[i]);
            }
        }

        public void SyncSelection()
        {
            ClearSelection();

            if (m_Container == null)
                return;

            foreach (var i in SplineSelection.selection
                .Where(x => ReferenceEquals(x.target, m_Container) && x.targetIndex == m_ContainerIndex && x.tangentIndex < 0)
                .Select(y => y.knotIndex))
                Select(i, true);
        }
    }
}
