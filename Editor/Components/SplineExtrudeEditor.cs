using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomEditor(typeof(SplineExtrude))]
    [CanEditMultipleObjects]
    class SplineExtrudeEditor : SplineComponentEditor
    {
        SerializedProperty m_Container;
        SerializedProperty m_RebuildOnSplineChange;
        SerializedProperty m_RebuildFrequency;
        SerializedProperty m_Sides;
        SerializedProperty m_SegmentsPerUnit;
        SerializedProperty m_Capped;
        SerializedProperty m_Radius;
        SerializedProperty m_Range;
        SerializedProperty m_UpdateColliders;

        static readonly GUIContent k_RangeContent = new GUIContent("Range", "The section of the Spline to extrude.");
        static readonly GUIContent k_AdvancedContent = new GUIContent("Advanced", "Advanced Spline Extrude settings.");
        static readonly GUIContent k_PercentageContent = new GUIContent("Percentage", "The section of the Spline to extrude in percentages.");

        static readonly string k_Spline = "Spline";
        static readonly string k_Geometry = L10n.Tr("Geometry");
        static readonly string k_ProfileEdges = "Profile Edges";
        static readonly string k_CapEnds = "Cap Ends";
        static readonly string k_AutoRegenGeo = "Auto-Regen Geometry";
        static readonly string k_To = L10n.Tr("to");
        static readonly string k_From = L10n.Tr("from");

        SplineExtrude[] m_Components;
        bool m_AnyMissingMesh;

        protected void OnEnable()
        {
            m_Container = serializedObject.FindProperty("m_Container");
            m_RebuildOnSplineChange = serializedObject.FindProperty("m_RebuildOnSplineChange");
            m_RebuildFrequency = serializedObject.FindProperty("m_RebuildFrequency");
            m_Sides = serializedObject.FindProperty("m_Sides");
            m_SegmentsPerUnit = serializedObject.FindProperty("m_SegmentsPerUnit");
            m_Capped = serializedObject.FindProperty("m_Capped");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_Range = serializedObject.FindProperty("m_Range");
            m_UpdateColliders = serializedObject.FindProperty("m_UpdateColliders");

            m_Components = targets.Select(x => x as SplineExtrude).Where(y => y != null).ToArray();
            m_AnyMissingMesh = false;

            Spline.Changed += OnSplineChanged;
            EditorSplineUtility.AfterSplineWasModified += OnSplineModified;
            SplineContainer.SplineAdded += OnContainerSplineSetModified;
            SplineContainer.SplineRemoved += OnContainerSplineSetModified;
        }

        void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
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

        void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
        {
            OnSplineModified(spline);
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

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            m_AnyMissingMesh = m_Components.Any(x => x.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh == null);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_Container, new GUIContent(k_Spline, m_Container.tooltip));
            if(m_Container.objectReferenceValue == null)
                EditorGUILayout.HelpBox(k_Helpbox, MessageType.Warning);
            
            EditorGUILayout.LabelField(k_Geometry, EditorStyles.boldLabel);

            if(m_AnyMissingMesh)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(" ");
                if(GUILayout.Button("Create Mesh Asset"))
                    CreateMeshAssets(m_Components);
                GUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Radius);
            if(EditorGUI.EndChangeCheck())
                m_Radius.floatValue = Mathf.Clamp(m_Radius.floatValue, .00001f, 1000f);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Sides, new GUIContent(k_ProfileEdges, m_Sides.tooltip));
            if(EditorGUI.EndChangeCheck())
                m_Sides.intValue = Mathf.Clamp(m_Sides.intValue, 3, 2048);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SegmentsPerUnit);
            if(EditorGUI.EndChangeCheck())
                m_SegmentsPerUnit.floatValue = Mathf.Clamp(m_SegmentsPerUnit.floatValue, .00001f, 4096f);

            EditorGUILayout.PropertyField(m_Capped, new GUIContent(k_CapEnds, m_Capped.tooltip));
            EditorGUI.indentLevel--;

            m_Range.isExpanded = Foldout(m_Range.isExpanded, k_AdvancedContent);
            if (m_Range.isExpanded)
            {
                EditorGUI.indentLevel++;

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

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_RebuildOnSplineChange, new GUIContent(k_AutoRegenGeo, m_RebuildOnSplineChange.tooltip));
                if (m_RebuildOnSplineChange.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(!m_RebuildOnSplineChange.boolValue);
                    EditorGUILayout.PropertyField(m_RebuildFrequency);
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(m_UpdateColliders);

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            if(EditorGUI.EndChangeCheck())
                foreach(var extrude in m_Components)
                    extrude.Rebuild();
        }

        void CreateMeshAssets(SplineExtrude[] components)
        {
            foreach (var extrude in components)
            {
                if (!extrude.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null)
                    filter.sharedMesh = extrude.CreateMeshAsset();
            }

            m_AnyMissingMesh = false;
        }
    }
}
