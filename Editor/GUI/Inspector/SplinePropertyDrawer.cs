using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(Spline))]
    class SplinePropertyDrawer : PropertyDrawer
    {
        static List<(SplineInfo splineInfo, int knotIndex)> s_KnotModificationBuffer = new List<(SplineInfo splineInfo, int knotIndex)>();
        // int - knot index
        readonly Action<SplineInfo, int> m_KnotModified = (splineInfo, knotIndex) =>
        {
            s_KnotModificationBuffer.Add((splineInfo, knotIndex));
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = SplineGUIUtility.lineHeight;
            if (!property.isExpanded)
                return height;

            // Closed property
            height += SplineGUIUtility.lineHeight;

            // Knots properties
            var knotProperty = property.FindPropertyRelative("m_Knots");

            // Easy way to pass the index to the property drawer: Using the unused label to do that
            var index = -1;
            if (!int.TryParse(label.text, out index))
                index = property.serializedObject.targetObject is ISplineContainer ? GetSplineIndexFromLabel(label.text) : -1;

            // Default case, this is not a Spline Container, so the index is not relevant
            return height + SplineReorderableListUtility.GetKnotReorderableList(property, knotProperty, index, m_KnotModified).GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var splineIndex = -1;
            if (!int.TryParse(label.text, out splineIndex))
                splineIndex = property.serializedObject.targetObject is ISplineContainer ? GetSplineIndexFromLabel(label.text) : -1;

            var splineLabel = splineIndex == -1 ? "Spline" : "Spline " + splineIndex;

            EditorGUI.BeginChangeCheck();
            property.isExpanded = EditorGUI.Foldout(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position), property.isExpanded, new GUIContent(splineLabel));
            if(property.isExpanded)
            {
                var closedProperty = property.FindPropertyRelative("m_Closed");
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position), closedProperty);
                var knotProperty = property.FindPropertyRelative("m_Knots");
                SplineReorderableListUtility.GetKnotReorderableList(property, knotProperty, splineIndex, m_KnotModified).DoList(position);
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();

                if (s_KnotModificationBuffer.Count > 0)
                {
                    foreach (var knotModification in s_KnotModificationBuffer)
                        knotModification.splineInfo.Spline.SendSplineModificationEvent(SplineModification.KnotModified, knotModification.knotIndex);

                    s_KnotModificationBuffer.Clear();
                }

                foreach (var obj in property.serializedObject.targetObjects)
                {
                    var val = fieldInfo.GetValue(obj);
                    if(val is Spline[] splines)
                    {
                        foreach (var spline in splines)
                        {
                            spline?.SetDirty();
                            spline?.SendSplineModificationEvent(SplineModification.Default);
                        }
                    }
                    else if (val is Spline spline)
                    {
                        spline.SetDirty();
                        spline.SendSplineModificationEvent(SplineModification.Default);
                    }

                    SplineCacheUtility.ClearCache();
                }
                SceneView.RepaintAll();
            }
        }

        int GetSplineIndexFromLabel(string label)
        {
            var regexExpr = "[0-9]+";
            var regex = new Regex(regexExpr, RegexOptions.Compiled);
            var match = regex.Match(label);
            if (match != null)
                return int.Parse(match.Value);

            return -1;
        }
    }
}