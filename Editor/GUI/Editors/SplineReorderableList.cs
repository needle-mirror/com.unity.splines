using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    class SplineReorderableList : ReorderableList
    {
        SplineContainer m_Container;

        static Dictionary<int, SplineReorderableList> s_Pool = new();

        static SplineReorderableList()
        {
            Selection.selectionChanged += ClearPool;
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

        public static SplineReorderableList Get(SerializedProperty splineArrayElement)
        {
            int hash = GetPropertyHash(splineArrayElement);
            if (!s_Pool.TryGetValue(hash, out var list))
                s_Pool.Add(hash, list = new SplineReorderableList(splineArrayElement.serializedObject, splineArrayElement));
            list.Init(splineArrayElement);
            return list;
        }

        void Init(SerializedProperty splineArrayElement)
        {
            serializedProperty = splineArrayElement;
            if (splineArrayElement.serializedObject.targetObject is SplineContainer container)
                m_Container = container;
        }

        public SplineReorderableList(
            SerializedObject serializedObject,
            SerializedProperty splineArrayElement) : base(serializedObject, splineArrayElement, true, false, true, true)
        {
            Init(splineArrayElement);
            multiSelect = true;
            elementHeightCallback += GetElementHeight;
            drawElementCallback += DrawElement;
            onReorderCallbackWithDetails += OnReorder;
            onSelectCallback += OnSelect;
            onAddCallback += OnAdd;
            onRemoveCallback += OnRemove;
        }

        float GetElementHeight(int i)
        {
            return EditorGUI.GetPropertyHeight(serializedProperty.GetArrayElementAtIndex(i));
        }

        void DrawElement(Rect position, int listIndex, bool isactive, bool isfocused)
        {
            ++EditorGUI.indentLevel;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position,
                serializedProperty.GetArrayElementAtIndex(listIndex),
                new GUIContent($"Spline {listIndex}"));
            EditorGUI.EndChangeCheck();
            --EditorGUI.indentLevel;
        }

        void OnReorder(ReorderableList reorderableList, int srcIndex, int dstIndex)
        {
            if (m_Container == null)
                return;

            Undo.RecordObject(serializedProperty.serializedObject.targetObject, "Reordering Spline in SplineContainer");
            m_Container.ReorderSpline(srcIndex, dstIndex);
            serializedProperty.serializedObject.ApplyModifiedProperties();
            serializedProperty.serializedObject.Update();
        }

        void OnSelect(ReorderableList _)
        {
            if (m_Container == null)
                return;
            SplineSelection.SetInspectorSelectedSplines(m_Container, selectedIndices);
            SceneView.RepaintAll();
        }

        void OnAdd(ReorderableList _)
        {
            if(m_Container == null)
             return;

            Undo.RecordObject(serializedProperty.serializedObject.targetObject, "Add new Spline");

            int added = 0;

            if(selectedIndices.Count > 0)
            {
                foreach(var i in selectedIndices)
                {
                    var spline = m_Container.AddSpline();
                    spline.Copy(m_Container.Splines[i]);
                    m_Container.CopyKnotLinks(i, m_Container.Splines.Count - 1);
                    added++;
                }
            }
            else
            {
                var spline = m_Container.AddSpline();
                var srcSplineIndex = m_Container.Splines.Count - 2;
                if (srcSplineIndex >= 0)
                {
                    spline.Copy(m_Container.Splines[srcSplineIndex]);
                    m_Container.CopyKnotLinks(srcSplineIndex, m_Container.Splines.Count - 1);
                }
                added++;
            }

            int maxCount = m_Container.Splines.Count;
            SelectRange(maxCount - added, maxCount - 1);
            onSelectCallback(this);
        }

        void OnRemove(ReorderableList _)
        {
            if (m_Container == null)
                return;

            Undo.RecordObject(serializedProperty.serializedObject.targetObject, "Removing Spline from SplineContainer");

            for(int i = selectedIndices.Count - 1; i >= 0; i--)
                m_Container.RemoveSplineAt(selectedIndices[i]);

            ClearSelection();
            SplineSelection.ClearInspectorSelectedSplines();
            
            SceneView.RepaintAll();
        }
    }
}
