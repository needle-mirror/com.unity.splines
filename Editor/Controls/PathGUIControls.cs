using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Splines
{
    static class PathGUIControls
    {
        internal static readonly List<Vector3> pointsBuffer = new List<Vector3>();

        public static void MultiEditVector3Field(GUIContent label, IList<Vector3> points)
        {
            if (!EditorGUIUtility.wideMode)
                GUILayout.Label(label);

            EditorGUILayout.BeginHorizontal();

            if (EditorGUIUtility.wideMode)
                EditorGUILayout.PrefixLabel(label);

            var prevMixedValue = EditorGUI.showMixedValue;
            var result = GetMultiEditValue(points, out Vector3 value);

            EditorGUI.showMixedValue = result.xMixed;
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("X");
            float x = EditorGUILayout.FloatField(value.x);
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < points.Count; ++i)
                {
                    var oldValue = points[i];
                    oldValue.x = x;
                    points[i] = oldValue;
                }

                GUI.changed = true;
            }

            EditorGUI.showMixedValue = result.yMixed;
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Y");
            float y = EditorGUILayout.FloatField(value.y);
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < points.Count; ++i)
                {
                    var oldValue = points[i];
                    oldValue.y = y;
                    points[i] = oldValue;
                }

                GUI.changed = true;
            }

            EditorGUI.showMixedValue = result.zMixed;
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Z");
            float z = EditorGUILayout.FloatField(value.z);
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < points.Count; ++i)
                {
                    var oldValue = points[i];
                    oldValue.z = z;
                    points[i] = oldValue;
                }

                GUI.changed = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.showMixedValue = prevMixedValue;
        }

        //Returns true if the value has multiple values
        static (bool xMixed, bool yMixed, bool zMixed) GetMultiEditValue(IList<Vector3> points, out Vector3 value)
        {
            value = points[0];
            bool xMixed = false;
            bool yMixed = false;
            bool zMixed = false;
            for (int i = 1; i < points.Count; ++i)
            {
                Vector3 point = points[i];
                xMixed |= !Mathf.Approximately(value.x, point.x);
                yMixed |= !Mathf.Approximately(value.y, point.y);
                zMixed |= !Mathf.Approximately(value.z, point.z);

                if (xMixed && yMixed && zMixed)
                    break;
            }

            return (xMixed, yMixed, zMixed);
        }

        /// <summary>
        /// Get the mixed value for a field
        /// </summary>
        /// <typeparam name="T">The target type</typeparam>
        /// <param name="values">A list of all the values</param>
        /// <param name="value">The value that should be used for the field</param>
        /// <returns>True if has mixed values</returns>
        public static bool GetMixedValue<T>(IList<T> values, out T value)
            where T : IEquatable<T>
        {
            value = values[0];
            for (int i = 1; i < values.Count; ++i)
            {
                if (!value.Equals(values[i]))
                    return true;
            }

            return false;
        }
    }
}
