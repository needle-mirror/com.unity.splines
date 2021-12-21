using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
	[CustomEditor(typeof(SplineExtrude))]
	[CanEditMultipleObjects]
	class SplineExtrudeEditor : UnityEditor.Editor
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

		SplineExtrude[] m_Components;
		bool m_AnyMissingMesh;

		void OnEnable()
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
			m_AnyMissingMesh = m_Components.Any(x => x.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh == null);

			EditorSplineUtility.afterSplineWasModified += OnSplineModified;
		}

		void OnDisable()
		{
			EditorSplineUtility.afterSplineWasModified -= OnSplineModified;
		}

		void OnSplineModified(Spline spline)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			foreach (var extrude in m_Components)
			{
				if (extrude.container != null && extrude.container.Spline == spline)
					extrude.Rebuild();
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_Container);

			if(m_AnyMissingMesh)
			{
				GUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(" ");
				if(GUILayout.Button("Create Mesh Asset"))
					CreateMeshAssets(m_Components);
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.PropertyField(m_RebuildOnSplineChange);
			if (m_RebuildOnSplineChange.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUI.BeginDisabledGroup(!m_RebuildOnSplineChange.boolValue);
					EditorGUILayout.PropertyField(m_RebuildFrequency);
				EditorGUI.EndDisabledGroup();
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.PropertyField(m_UpdateColliders);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_Sides);
			if(EditorGUI.EndChangeCheck())
				m_Sides.intValue = Mathf.Clamp(m_Sides.intValue, 3, 2048);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_SegmentsPerUnit);
			if(EditorGUI.EndChangeCheck())
				m_SegmentsPerUnit.floatValue = Mathf.Clamp(m_SegmentsPerUnit.floatValue, .00001f, 4096f);

			EditorGUILayout.PropertyField(m_Capped);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_Radius);
			if(EditorGUI.EndChangeCheck())
				m_Radius.floatValue = Mathf.Clamp(m_Radius.floatValue, .00001f, 1000f);

			EditorGUI.showMixedValue = m_Range.hasMultipleDifferentValues;
			var range = m_Range.vector2Value;
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.MinMaxSlider(k_RangeContent, ref range.x, ref range.y, 0f, 1f);
			if(EditorGUI.EndChangeCheck())
				m_Range.vector2Value = range;
			EditorGUI.showMixedValue = false;

			serializedObject.ApplyModifiedProperties();

			if(EditorGUI.EndChangeCheck())
				foreach(var extrude in m_Components)
					extrude.Rebuild();
		}

		void CreateMeshAssets(SplineExtrude[] components)
		{
			foreach (var extrude in components)
			{
				if (!extrude.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh != null)
					filter.sharedMesh = extrude.CreateMeshAsset();
			}

			m_AnyMissingMesh = false;
		}
	}
}
