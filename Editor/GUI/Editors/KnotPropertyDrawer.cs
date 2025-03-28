using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(TangentMode))]
    class TangentModePropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent[] k_TangentModeLabels = new[]
        {
            new GUIContent("Auto", "The path from this knot to the preceding and following knots is a bezier curve that is automatically calculated from the neighboring knot positions."),
            new GUIContent("Linear", "The path from this knot to the preceding and following knots is a straight line."),
            new GUIContent("Bezier", "The path from this knot to the preceding and following knots is a bezier curve with manually defined tangents."),
        };

        static readonly string[] k_BezierModeLabels = new[]
        {
            "Mirrored",
            "Continuous",
            "Broken"
        };

        public static float GetPropertyHeight() => SplineGUIUtility.lineHeight * 2 + 2;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => GetPropertyHeight();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            int mode = property.enumValueIndex;
            int type = System.Math.Min(mode, (int) TangentMode.Mirrored);
            int bezier = System.Math.Max(mode, (int) TangentMode.Mirrored) - (int)TangentMode.Mirrored;
            bool mixed = EditorGUI.showMixedValue;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            type = GUI.Toolbar(EditorGUI.IndentedRect(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position)),
                type,
                k_TangentModeLabels);
            if (EditorGUI.EndChangeCheck())
            {
                // Continuous mode should be prefered instead of Mirrored when switching from AutoSmooth to Bezier
                // as Centripetal Catmull-Rom's tangents are not always of equal length.
                if (property.enumValueIndex == (int)TangentMode.AutoSmooth && type == (int)TangentMode.Mirrored)
                    type = (int)TangentMode.Continuous;

                property.enumValueIndex = type;
            }

            position.y += 2;
            position.height = SplineGUIUtility.lineHeight;

            EditorGUI.BeginDisabledGroup(mode < (int) TangentMode.Mirrored);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            bezier = EditorGUI.Popup(position, "Bezier", bezier, k_BezierModeLabels);
            if (EditorGUI.EndChangeCheck())
                property.enumValueIndex = bezier + (int)TangentMode.Mirrored;
            EditorGUI.EndDisabledGroup();

            EditorGUI.showMixedValue = mixed;
        }
    }

    /// <summary>
    /// Specialized UI for drawing a knot property drawer with additional data (ex, TangentMode from Spline.MetaData).
    /// Additionally supports inline fields a little longer than the regular inspector wide mode would allow.
    /// </summary>
    static class KnotPropertyDrawerUI
    {
        static readonly GUIContent k_Position = EditorGUIUtility.TrTextContent("Position");
        static readonly GUIContent k_Rotation = EditorGUIUtility.TrTextContent("Rotation");
        static readonly GUIContent k_TangentLengthContent = EditorGUIUtility.TrTextContent("Tangent Length");
        static readonly GUIContent k_TangentLengthContentIn = EditorGUIUtility.TrTextContent("Tangent In Length");
        static readonly GUIContent k_TangentLengthContentOut = EditorGUIUtility.TrTextContent("Tangent Out Length");

        const float k_IndentPad = 13f; // kIndentPerLevel - margin (probably)
        const int k_MinWideModeWidth = 230;
        const int k_WideModeInputFieldWidth = 212;

        static bool CanForceWideMode() => EditorGUIUtility.currentViewWidth > k_MinWideModeWidth;

        public static float GetPropertyHeight(SerializedProperty knot, SerializedProperty meta, GUIContent _)
        {
            // title
            float height = SplineGUIUtility.lineHeight;
            // position, rotation
            height += SplineGUIUtility.lineHeight * (CanForceWideMode() ? 2 : 4);
            // 1. { linear, auto, bezier }
            // 2. { broken, continuous, mirrored }
            height += meta == null ? 0 : TangentModePropertyDrawer.GetPropertyHeight();
            // 3. (optional) tangent in
            // 4. (optional) tangent out
            height += TangentGetPropertyHeight(meta);

            return knot.isExpanded ? height : SplineGUIUtility.lineHeight;
        }

        public static float TangentGetPropertyHeight(SerializedProperty meta)
        {
            var prop = meta?.FindPropertyRelative("Mode");
            var mode = meta == null ? TangentMode.Broken : (TangentMode) prop.enumValueIndex;

            switch (mode)
            {
                case TangentMode.AutoSmooth:
                case TangentMode.Linear:
                    return 0;

                case TangentMode.Mirrored:
                    return SplineGUIUtility.lineHeight;

                case TangentMode.Continuous:
                    return SplineGUIUtility.lineHeight * 2;

                case TangentMode.Broken:
                default:
                    return SplineGUIUtility.lineHeight * (CanForceWideMode() ? 2 : 4);
            }
        }

        public static void TangentOnGUI(ref Rect rect,
            SerializedProperty tangentIn,
            SerializedProperty tangentOut,
            TangentMode mode)
        {
            if (mode == TangentMode.Broken)
            {
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpaceForLine(ref rect), tangentIn);
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpaceForLine(ref rect), tangentOut);
                return;
            }

            // tangents are not serialized as vec3, they are a generic type
            var tin = tangentIn.FindPropertyRelative("z");
            var tout = tangentOut.FindPropertyRelative("z");

            if (mode == TangentMode.Mirrored)
            {
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref rect), tout, k_TangentLengthContent);
                tin.floatValue = -tout.floatValue;
            }
            else if (mode == TangentMode.Continuous)
            {
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref rect), tin, k_TangentLengthContentIn);
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref rect), tout, k_TangentLengthContentOut);
            }
        }

        public static bool OnGUI(Rect rect, SerializedProperty knot, SerializedProperty meta, GUIContent label)
        {
            bool wideMode = EditorGUIUtility.wideMode;

            if (!wideMode && CanForceWideMode())
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - k_WideModeInputFieldWidth;
            }
            else
                EditorGUIUtility.labelWidth = 0;

            var titleRect = SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref rect);
            titleRect.width = EditorGUIUtility.labelWidth;
            knot.isExpanded = EditorGUI.Foldout(titleRect, knot.isExpanded, label);
            var position = knot.FindPropertyRelative("Position");

            EditorGUI.BeginChangeCheck();
            if (knot.isExpanded)
            {
                var mode = meta?.FindPropertyRelative("Mode");
                bool modeAllowsRotationAndTangent = mode?.enumValueIndex > (int) TangentMode.Linear;
                var tangentMode = mode != null ? (TangentMode) mode.enumValueIndex : TangentMode.Broken;

                var rotation = knot.FindPropertyRelative("Rotation");
                var tangentIn = knot.FindPropertyRelative("TangentIn");
                var tangentOut = knot.FindPropertyRelative("TangentOut");

                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpaceForLine(ref rect), position, k_Position);
                EditorGUI.BeginDisabledGroup(!modeAllowsRotationAndTangent);
                SplineGUILayout.QuaternionField(SplineGUIUtility.ReserveSpaceForLine(ref rect), k_Rotation, rotation);
                EditorGUI.EndDisabledGroup();

                if (meta != null)
                {
                    EditorGUI.PropertyField(rect, mode);
                    rect.y += TangentModePropertyDrawer.GetPropertyHeight();
                }

                EditorGUI.BeginDisabledGroup(!modeAllowsRotationAndTangent);
                TangentOnGUI(ref rect, tangentIn, tangentOut, tangentMode);
                EditorGUI.EndDisabledGroup();
            }
            // When in wide mode, show the position field inline with the knot title if not expanded
            else if (EditorGUIUtility.wideMode)
            {
                var inlinePositionRect = titleRect;
                inlinePositionRect.x += titleRect.width - k_IndentPad * EditorGUI.indentLevel;
                inlinePositionRect.width = rect.width - (titleRect.width - k_IndentPad * EditorGUI.indentLevel);
                EditorGUI.PropertyField(inlinePositionRect, position, GUIContent.none);
            }

            EditorGUIUtility.wideMode = wideMode;
            EditorGUIUtility.labelWidth = 0;
            return EditorGUI.EndChangeCheck();
        }
    }
}
