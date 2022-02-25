using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    //This drawer is used to draw the actual type of the spline (editor version) and not the stored spline which is always bezier
    [CustomPropertyDrawer(typeof(Spline), true)]
    class SplineDrawer : PropertyDrawer
    {
        const string k_MultiSplineEditMessage = "Multi-selection is not supported for Splines";
        const string k_SplineFoldoutTitle = "Advanced";
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if(!property.isExpanded || property.serializedObject.isEditingMultipleObjects)
                return height;

            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_EditModeType"));

            var proxy = SplineUIManager.instance.GetProxyFromProperty(property);
            
            var it = proxy.SerializedObject.FindProperty("Spline").Copy();
            it.NextVisible(true);
            do
            {
                height += EditorGUI.GetPropertyHeight(it);
            } while(it.NextVisible(false));
                
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.LabelField(position,k_MultiSplineEditMessage, EditorStyles.helpBox);
                return;
            }

            label.text =  L10n.Tr(k_SplineFoldoutTitle);
            property.isExpanded = EditorGUI.Foldout(SplineUIManager.ReserveSpace(EditorGUIUtility.singleLineHeight, ref position), property.isExpanded, label);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                var proxy = SplineUIManager.instance.GetProxyFromProperty(property);

                var editTypeProperty = property.FindPropertyRelative("m_EditModeType"); 
                EditorGUI.PropertyField(SplineUIManager.ReserveSpace(EditorGUI.GetPropertyHeight(editTypeProperty), ref position), editTypeProperty);

                var pathProperty = proxy.SerializedObject.FindProperty("Spline");

                // HACK to get around the fact that array size change isn't an actual change when applying (bug)
                var knotsProperty = pathProperty.FindPropertyRelative("m_Knots");
                var arraySize = knotsProperty.arraySize;

                EditorGUI.BeginChangeCheck();

                var it = pathProperty.Copy();
                it.NextVisible(true);
                do
                {
                    EditorGUI.PropertyField(SplineUIManager.ReserveSpace(EditorGUI.GetPropertyHeight(it), ref position), it, true);
                } while(it.NextVisible(false));


                if(EditorGUI.EndChangeCheck() || arraySize != knotsProperty.arraySize)
                {
                    SplineUIManager.instance.ApplyProxyToProperty(proxy, property);
                }
                
                EditorGUI.indentLevel--;
            }
        }
    }
}
