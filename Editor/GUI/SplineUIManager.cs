using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    sealed class SplineUIProxy : ScriptableObject
    {
        [NonSerialized] public SerializedObject SerializedObject;
        [SerializeReference] public IEditableSpline Spline;
        [NonSerialized] public int LastFrameCount;
    }

    class SplineUIManager : ScriptableSingleton<SplineUIManager>
    {
        List<BezierKnot> m_KnotsBuffer = new List<BezierKnot>();
        Dictionary<Spline, SplineUIProxy> m_Proxies = new Dictionary<Spline, SplineUIProxy>();
        List<Spline> m_ProxiesToDestroy = new List<Spline>();
        
        void OnEnable()
        {
            Selection.selectionChanged += VerifyProxiesAreValid;
#if UNITY_EDITOR
            EditorSplineUtility.afterSplineWasModified += OnSplineUpdated;
            Undo.undoRedoPerformed += ClearProxies;
#endif
        }

        void OnDisable()
        {
            Selection.selectionChanged -= VerifyProxiesAreValid;
#if UNITY_EDITOR
            EditorSplineUtility.afterSplineWasModified -= OnSplineUpdated;
            Undo.undoRedoPerformed -= ClearProxies;
#endif
        }

        public SplineUIProxy GetProxyFromProperty(SerializedProperty splineProperty)
        {
            var targetSpline = GetSplineValue(splineProperty.serializedObject.targetObject, splineProperty.propertyPath);
            
            if (targetSpline == null || !m_Proxies.TryGetValue(targetSpline, out SplineUIProxy proxy))
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
                    m_KnotsBuffer.Clear();
                    for (int i = 0; i < knotsProperty.arraySize; ++i)
                    {
                        var knot = knotsProperty.GetArrayElementAtIndex(i);
                        m_KnotsBuffer.Add(new BezierKnot(
                            GetFloat3FromProperty(knot.FindPropertyRelative("Position")),
                            GetFloat3FromProperty(knot.FindPropertyRelative("TangentIn")),
                            GetFloat3FromProperty(knot.FindPropertyRelative("TangentOut")),
                            GetQuaternionFromProperty(knot.FindPropertyRelative("Rotation"))));
                    }

                    spline.FromBezier(m_KnotsBuffer);
                    proxy.Spline = spline;

                    var conversionData = spline;
                    conversionData.isDirty = false;
                    conversionData.ValidateData();
                }

                proxy.SerializedObject = new SerializedObject(proxy);
                if(targetSpline != null)
                    m_Proxies.Add(targetSpline, proxy);
            }

            proxy.LastFrameCount = Time.frameCount;
            return proxy;
        }

        public void ApplyProxyToProperty(SplineUIProxy proxy, SerializedProperty property)
        {
            proxy.SerializedObject.ApplyModifiedPropertiesWithoutUndo();
            
            var path = proxy.Spline;
            path.ValidateData();
            property.FindPropertyRelative("m_EditModeType").enumValueIndex = (int)EditableSplineUtility.GetSplineType(path);
            property.FindPropertyRelative("m_Closed").boolValue = proxy.SerializedObject.FindProperty("Spline.m_Closed").boolValue;
            var knotsProperty = property.FindPropertyRelative("m_Knots");

            m_KnotsBuffer.Clear();
            path.ToBezier(m_KnotsBuffer);
            knotsProperty.arraySize = m_KnotsBuffer.Count;
            for (int i = 0; i < m_KnotsBuffer.Count; ++i)
            {
                var knotProperty = knotsProperty.GetArrayElementAtIndex(i);
                var knot = m_KnotsBuffer[i];
                SetFloat3Property(knotProperty.FindPropertyRelative("Position"), knot.Position);
                SetFloat3Property(knotProperty.FindPropertyRelative("TangentIn"), knot.TangentIn);
                SetFloat3Property(knotProperty.FindPropertyRelative("TangentOut"), knot.TangentOut);

                if(math.length(knot.Rotation.value) == 0)
                {
                    //Update knot rotation with a valid value
                    knot.Rotation = quaternion.identity;
                    m_KnotsBuffer[i] = knot;
                    //Updating proxy
                    path.FromBezier(m_KnotsBuffer);
                }

                SetQuaternionFromProperty(knotProperty.FindPropertyRelative("Rotation"), knot.Rotation);
            }

            var targetSpline = GetSplineValue(property.serializedObject.targetObject, property.propertyPath);
            targetSpline?.SetDirty();
            property.FindPropertyRelative("m_Length").floatValue = -1;

            EditorApplication.delayCall += () => SplineConversionUtility.UpdateEditableSplinesForTarget(property.serializedObject.targetObject);
        }

        Spline GetSplineValue(UnityEngine.Object targetObject, string propertyPath)
        {
            if(targetObject == null)
                return null;
                    
            var fieldInfo = targetObject.GetType().GetField(propertyPath, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (Spline)fieldInfo?.GetValue(targetObject);;
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

        void ClearProxies()
        {
            foreach(var kvp in m_Proxies)
            {
                //Forcing inspector update on spline changes in the scene view
                DestroyImmediate(kvp.Value);
            }
            m_Proxies.Clear();
        }
        
        void OnSplineUpdated(Spline spline)
        {
            if(m_Proxies.TryGetValue(spline, out SplineUIProxy proxy))
            {
                //Forcing inspector update on spline changes in the scene view
                DestroyImmediate(proxy);
                m_Proxies.Remove(spline);
            }
        }
        
        void VerifyProxiesAreValid()
        {
            if(m_Proxies.Count == 0)
                return;
            
            m_ProxiesToDestroy.Clear();
            var currentTime = Time.frameCount;

            const int frameCountBeforeProxyRemoval = 5;
            foreach(var kvp in m_Proxies)
            {
                if(currentTime - kvp.Value.LastFrameCount > frameCountBeforeProxyRemoval)
                    m_ProxiesToDestroy.Add(kvp.Key);
            }

            foreach(var keyToDestroy in m_ProxiesToDestroy)
            {
                if (m_Proxies.TryGetValue(keyToDestroy, out SplineUIProxy proxy))
                {
                    DestroyImmediate(proxy);
                    m_Proxies.Remove(keyToDestroy);
                }
            }
        }
        
        internal static Rect ReserveSpace(float height, ref Rect total)
        {
            Rect current = total;
            current.height = height;
            total.y += height;
            return current;
        }
    }
}
