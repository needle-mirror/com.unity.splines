using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    sealed class SplineUIProxy : ScriptableObject
    {
        [NonSerialized] public SerializedObject SerializedObject;
        [SerializeReference] public IEditableSpline Spline;
    }

    static class SplineUIUtility
    {
        static readonly List<BezierKnot> s_KnotsBuffer = new List<BezierKnot>();
        static readonly Dictionary<SerializedProperty, SplineUIProxy> s_Proxies = new Dictionary<SerializedProperty, SplineUIProxy>();

        public static SplineUIProxy GetProxyFromProperty(SerializedProperty splineProperty)
        {
            if (!s_Proxies.TryGetValue(splineProperty, out SplineUIProxy proxy))
            {
                proxy = ScriptableObject.CreateInstance<SplineUIProxy>();
                var editType = splineProperty.FindPropertyRelative("m_EditModeType");

                if (splineProperty.hasMultipleDifferentValues)
                {
                    proxy.Spline = null;
                }
                else
                {
                    IEditableSpline spline = EditableSplineUtility.CreatePathOfType((SplineType) editType.enumValueIndex);
                    spline.closed = splineProperty.FindPropertyRelative("m_Closed").boolValue;

                    var knotsProperty = splineProperty.FindPropertyRelative("m_Knots");
                    s_KnotsBuffer.Clear();
                    for (int i = 0; i < knotsProperty.arraySize; ++i)
                    {
                        var knot = knotsProperty.GetArrayElementAtIndex(i);
                        s_KnotsBuffer.Add(new BezierKnot(
                            GetFloat3FromProperty(knot.FindPropertyRelative("Position")),
                            GetFloat3FromProperty(knot.FindPropertyRelative("TangentIn")),
                            GetFloat3FromProperty(knot.FindPropertyRelative("TangentOut")),
                            GetQuaternionFromProperty(knot.FindPropertyRelative("Rotation"))));
                    }

                    spline.FromBezier(s_KnotsBuffer);
                    proxy.Spline = spline;

                    var conversionData = (IEditableSplineConversionData) spline;
                    conversionData.isDirty = false;
                    conversionData.ValidateData();
                }

                proxy.SerializedObject = new SerializedObject(proxy);
            }

            return proxy;
        }

        public static void ApplyProxyToProperty(SplineUIProxy proxy, SerializedProperty property)
        {
            proxy.SerializedObject.ApplyModifiedPropertiesWithoutUndo();
            var path = proxy.Spline;
            ((IEditableSplineConversionData)path).ValidateData();
            property.FindPropertyRelative("m_EditModeType").enumValueIndex = (int)EditableSplineUtility.GetSplineType(path);
            property.FindPropertyRelative("m_Closed").boolValue = proxy.SerializedObject.FindProperty("Spline.m_Closed").boolValue;
            var knotsProperty = property.FindPropertyRelative("m_Knots");

            s_KnotsBuffer.Clear();
            path.ToBezier(s_KnotsBuffer);
            knotsProperty.arraySize = s_KnotsBuffer.Count;
            for (int i = 0; i < s_KnotsBuffer.Count; ++i)
            {
                var knotProperty = knotsProperty.GetArrayElementAtIndex(i);
                var knot = s_KnotsBuffer[i];
                SetFloat3Property(knotProperty.FindPropertyRelative("Position"), knot.Position);
                SetFloat3Property(knotProperty.FindPropertyRelative("TangentIn"), knot.TangentIn);
                SetFloat3Property(knotProperty.FindPropertyRelative("TangentOut"), knot.TangentOut);
                SetQuaternionFromProperty(knotProperty.FindPropertyRelative("Rotation"), knot.Rotation);
            }

            EditorApplication.delayCall += () => SplineConversionUtility.UpdateEditableSplinesForTarget(property.serializedObject.targetObject );
        }

        static float3 GetFloat3FromProperty(SerializedProperty property)
        {
            return new float3(
                property.FindPropertyRelative("x").floatValue,
                property.FindPropertyRelative("y").floatValue,
                property.FindPropertyRelative("z").floatValue);
        }

        static void SetFloat3Property(SerializedProperty property, float3 value)
        {
            property.FindPropertyRelative("x").floatValue = value.x;
            property.FindPropertyRelative("y").floatValue = value.y;
            property.FindPropertyRelative("z").floatValue = value.z;
        }
        
        static quaternion GetQuaternionFromProperty(SerializedProperty property)
        {
            return new quaternion(
                property.FindPropertyRelative("value.x").floatValue,
                property.FindPropertyRelative("value.y").floatValue,
                property.FindPropertyRelative("value.z").floatValue,
                property.FindPropertyRelative("value.w").floatValue);
        }
        
        static void SetQuaternionFromProperty(SerializedProperty property, quaternion quaternion)
        {
            property.FindPropertyRelative("value.x").floatValue = quaternion.value.x;
            property.FindPropertyRelative("value.y").floatValue = quaternion.value.y;
            property.FindPropertyRelative("value.z").floatValue = quaternion.value.z;
            property.FindPropertyRelative("value.w").floatValue = quaternion.value.w;
        }

        public static void ReleaseProxyForSplineProperty(SerializedProperty splineProperty)
        {
            if (s_Proxies.TryGetValue(splineProperty, out SplineUIProxy proxy))
            {
                Object.DestroyImmediate(proxy);
                s_Proxies.Remove(splineProperty);
            }
        }
    }
}
