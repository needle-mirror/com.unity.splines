using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [CustomPropertyDrawer(typeof(Spline))]
    class SplinePropertyDrawer : PropertyDrawer
    {
        static readonly string[] k_SplineData = new string[]
        {
            "m_IntData",
            "m_FloatData",
            "m_Float4Data",
            "m_ObjectData"
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return SplineGUIUtility.lineHeight;
            float height = SplineGUIUtility.lineHeight * 2;
            height += KnotReorderableList.Get(property).GetHeight();
            height += EditorGUIUtility.standardVerticalSpacing;
            for (int i = 0, c = k_SplineData.Length; i < c; ++i)
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(k_SplineData[i]));
            return height;
        }

        // Important note - if this is inspecting a Spline that is not part of an ISplineContainer, callbacks will not
        // invoked. That means the Scene View won't reflect changes made in the Inspector without additional code to
        // fire the modified callbacks. The easiest way to handle this is to implement ISplineContainer. Alternatively,
        // write a custom editor for your class and call Spline.EnforceTangentModeNoNotify() & Spline.SetDirty() after
        // any changes.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.isExpanded = EditorGUI.Foldout(
                SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position),
                property.isExpanded, label);

            if(property.isExpanded)
            {
                var closedProperty = property.FindPropertyRelative("m_Closed");

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(SplineGUIUtility.ReserveSpace(SplineGUIUtility.lineHeight, ref position), closedProperty);
                if (EditorGUI.EndChangeCheck() && SerializedPropertyUtility.TryGetSpline(property, out var spline))
                {
                    property.serializedObject.ApplyModifiedProperties();
                    spline.SetDirty(SplineModification.ClosedModified);
                    property.serializedObject.Update();
                }

                var knots = KnotReorderableList.Get(property);
                knots.DoList(position);
                position.y += knots.GetHeight() + EditorGUIUtility.standardVerticalSpacing;

                for (int i = 0, c = k_SplineData.Length; i < c; ++i)
                {
                    var prop = property.FindPropertyRelative(k_SplineData[i]);
                    var height = EditorGUI.GetPropertyHeight(prop);
                    var rect = position;
                    rect.height = height;
                    position.y += rect.height;
                    EditorGUI.PropertyField(rect, prop, true);
                }
            }
        }
    }
}
