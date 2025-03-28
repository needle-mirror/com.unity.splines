using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Splines.ExtrusionShapes;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineExtrude))]
    [CanEditMultipleObjects]
    class SplineExtrudeEditor : SplineComponentEditor
    {
        SerializedProperty m_Container;
        SerializedProperty m_RebuildOnSplineChange;
        SerializedProperty m_RebuildFrequency;
        SerializedProperty m_SegmentsPerUnit;
        SerializedProperty m_Capped;
        SerializedProperty m_Radius;
        SerializedProperty m_Range;
        SerializedProperty m_Shape;
        SerializedProperty m_UpdateColliders;
        SerializedProperty m_FlipNormals;
        SerializedProperty m_TargetMesh;

        static readonly GUIContent k_RangeContent = new GUIContent(L10n.Tr("Range"), L10n.Tr("The section of the Spline to extrude."));
        static readonly GUIContent k_AdvancedContent = new GUIContent(L10n.Tr("Advanced"), L10n.Tr("Advanced Spline Extrude settings."));
        static readonly GUIContent k_PercentageContent = new GUIContent(L10n.Tr("Percentage"), L10n.Tr("The section of the Spline to extrude in percentages."));
        static readonly GUIContent k_ShapeContent = new GUIContent(L10n.Tr("Shape Extrude"), L10n.Tr("Shape Extrude settings."));
        static readonly GUIContent k_ShapeSettings = EditorGUIUtility.TrTextContent("Settings");
        static readonly GUIContent k_GeometryContent = new GUIContent(L10n.Tr("Geometry"), L10n.Tr("Mesh Geometry settings."));
        static readonly GUIContent k_MeshTargetContent = new GUIContent(L10n.Tr("Target Mesh Asset"));
        static readonly GUIContent k_CreateMeshTargetContent = new GUIContent(L10n.Tr("Create Mesh Asset"));

        static readonly string k_SourceSplineContainer = L10n.Tr("Source Spline Container");
        static readonly string k_CapEnds = L10n.Tr("Cap Ends");
        static readonly string k_AutoRefreshGeneration = L10n.Tr("Auto Refresh Generation");
        static readonly string k_To = L10n.Tr("to");
        static readonly string k_From = L10n.Tr("from");

        SplineExtrude[] m_Components;

        protected void OnEnable()
        {
            m_Container = serializedObject.FindProperty("m_Container");
            m_RebuildOnSplineChange = serializedObject.FindProperty("m_RebuildOnSplineChange");
            m_RebuildFrequency = serializedObject.FindProperty("m_RebuildFrequency");
            m_SegmentsPerUnit = serializedObject.FindProperty("m_SegmentsPerUnit");
            m_Capped = serializedObject.FindProperty("m_Capped");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_Range = serializedObject.FindProperty("m_Range");
            m_UpdateColliders = serializedObject.FindProperty("m_UpdateColliders");
            m_Shape = serializedObject.FindProperty("m_Shape");
            m_TargetMesh = serializedObject.FindProperty("m_TargetMesh");

            m_FlipNormals = serializedObject.FindProperty("m_FlipNormals");

            m_Components = targets.Select(x => x as SplineExtrude).Where(y => y != null).ToArray();

            EditorSplineUtility.AfterSplineWasModified += OnSplineModified;
            SplineContainer.SplineAdded += OnContainerSplineSetModified;
            SplineContainer.SplineRemoved += OnContainerSplineSetModified;
        }

        void OnDisable()
        {
            EditorSplineUtility.AfterSplineWasModified -= OnSplineModified;
            SplineContainer.SplineAdded -= OnContainerSplineSetModified;
            SplineContainer.SplineRemoved -= OnContainerSplineSetModified;
        }

        void OnSplineModified(Spline spline)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            foreach (var extrude in m_Components)
            {
                if (extrude.Container != null && extrude.Splines.Contains(spline))
                    extrude.Rebuild();
            }
        }

        void OnContainerSplineSetModified(SplineContainer container, int spline)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            foreach (var extrude in m_Components)
            {
                if (extrude.Container == container)
                    extrude.Rebuild();
            }
        }

        void SetShapeType(ShapeType type)
        {
            foreach (var extrude in m_Components)
            {
                if (ShapeTypeUtility.GetShapeType(extrude.Shape) == type)
                    continue;

                Undo.RecordObject(extrude, "Set Extrude Shape");

                extrude.Shape = ShapeTypeUtility.CreateShape(type);
                m_Shape.isExpanded = true;
            }
        }

        bool CanCapEnds()
        {
            foreach (var extrude in m_Components)
            {
                if (!extrude.CanCapEnds)
                    return false;
            }

            return true;
        }

        void SetRebuildOnSplineChange(bool value)
        {
            foreach (var extrude in m_Components)
            {
                Undo.RecordObject(extrude, "Set Rebuild on Spline Change.");

                extrude.RebuildOnSplineChange = value;
            }
        }

        bool HasEmptyTargetMeshAssets(SplineExtrude[] components)
        {
            foreach (var extrude in components)
                if (extrude.targetMesh == null)
                    return true;

            return false;
        }

        void Rebuild()
        {
            foreach (var extrude in m_Components)
                extrude.Rebuild();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_Container, new GUIContent(k_SourceSplineContainer, m_Container.tooltip));

            if (m_Container.objectReferenceValue == null)
                EditorGUILayout.HelpBox(k_Helpbox, MessageType.Warning);

            // shape section
            m_Shape.isExpanded = Foldout(m_Shape.isExpanded, k_ShapeContent, true);

            if (m_Shape.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUI.showMixedValue = m_Shape.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                var shapeType = ShapeTypeUtility.GetShapeType(m_Shape.managedReferenceValue);
                shapeType = (ShapeType)EditorGUILayout.EnumPopup(L10n.Tr("Type"), shapeType);
                if (EditorGUI.EndChangeCheck())
                    SetShapeType(shapeType);
                EditorGUI.showMixedValue = false;

                if (m_Shape.hasVisibleChildren)
                    EditorGUILayout.PropertyField(m_Shape, k_ShapeSettings, true);

                EditorGUI.indentLevel--;
            }

            // https://unityeditordesignsystem.unity.com/patterns/content-organization recommends 8px spacing for
            // vertical groups. padding already adds 4 so just nudge that up for a total of 8
            EditorGUILayout.Space(4);

            // geometry section
            m_Radius.isExpanded = Foldout(m_Radius.isExpanded, k_GeometryContent, true);

            if (m_Radius.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_RebuildOnSplineChange, new GUIContent(k_AutoRefreshGeneration, m_RebuildOnSplineChange.tooltip));
                if (m_RebuildOnSplineChange.boolValue)
                {
                    EditorGUI.BeginDisabledGroup(!m_RebuildOnSplineChange.boolValue);
                    using (new LabelWidthScope(80f))
                        EditorGUILayout.PropertyField(m_RebuildFrequency, new GUIContent() { text = L10n.Tr("Frequency") });
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    if (GUILayout.Button(new GUIContent(L10n.Tr("Regenerate"))))
                        Rebuild();
                }

                if (EditorGUI.EndChangeCheck() && !m_RebuildOnSplineChange.boolValue)
                {
                    // This is needed to set m_RebuildRequested to the appropriate value.
                    SetRebuildOnSplineChange(m_RebuildOnSplineChange.boolValue);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_Radius);
                if (EditorGUI.EndChangeCheck())
                    m_Radius.floatValue = Mathf.Clamp(m_Radius.floatValue, .00001f, 1000f);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_SegmentsPerUnit);
                if (EditorGUI.EndChangeCheck())
                    m_SegmentsPerUnit.floatValue = Mathf.Clamp(m_SegmentsPerUnit.floatValue, .00001f, 4096f);

                var canCapEnds = CanCapEnds();
                using (new EditorGUI.DisabledScope(!canCapEnds))
                {
                    EditorGUILayout.PropertyField(m_Capped, new GUIContent(k_CapEnds, m_Capped.tooltip));
                    if (m_Capped.boolValue && !canCapEnds)
                        m_Capped.boolValue = false;
                }

                EditorGUILayout.PropertyField(m_FlipNormals);

                // Range
                EditorGUI.showMixedValue = m_Range.hasMultipleDifferentValues;
                var range = m_Range.vector2Value;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.MinMaxSlider(k_RangeContent, ref range.x, ref range.y, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                    m_Range.vector2Value = range;

                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(k_PercentageContent);
                EditorGUI.indentLevel--;

                EditorGUI.indentLevel--;
                EditorGUI.BeginChangeCheck();
                var newRange = new Vector2(range.x, range.y);
                using (new LabelWidthScope(30f))
                    newRange.x = EditorGUILayout.FloatField(k_From, range.x * 100f) / 100f;

                using (new LabelWidthScope(15f))
                    newRange.y = EditorGUILayout.FloatField(k_To, range.y * 100f) / 100f;

                if (EditorGUI.EndChangeCheck())
                {
                    newRange.x = Mathf.Min(Mathf.Clamp(newRange.x, 0f, 1f), range.y);
                    newRange.y = Mathf.Max(newRange.x, Mathf.Clamp(newRange.y, 0f, 1f));
                    m_Range.vector2Value = newRange;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.showMixedValue = false;
            }

            // advanced section
            EditorGUILayout.Space(4);

            m_UpdateColliders.isExpanded = Foldout(m_UpdateColliders.isExpanded, k_AdvancedContent, true);

            if (m_UpdateColliders.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_UpdateColliders);

                EditorGUILayout.PropertyField(m_TargetMesh, k_MeshTargetContent);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(18);
                if (HasEmptyTargetMeshAssets(m_Components) && GUILayout.Button(k_CreateMeshTargetContent))
                {
                    Undo.RecordObjects(m_Components, $"Modified {m_TargetMesh.displayName} in GameObject");
                    foreach (var extrude in m_Components)
                    {
                        if (extrude.targetMesh == null)
                            extrude.targetMesh = extrude.CreateMeshAsset();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var extrude in m_Components)
                    extrude.Rebuild();
            }
        }
    }
}
