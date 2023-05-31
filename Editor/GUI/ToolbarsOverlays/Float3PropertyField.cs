using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.UIElements;
#if !UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace UnityEditor.Splines
{
    class Float3PropertyField<T> : Vector3Field
        where T : ISelectableElement
    {
        static readonly List<float3> s_Float3Buffer = new List<float3>();
        static readonly SplineGUIUtility.EqualityComparer<float3> s_ComparerX = (a, b) => a.x.Equals(b.x);
        static readonly SplineGUIUtility.EqualityComparer<float3> s_ComparerY = (a, b) => a.y.Equals(b.y);
        static readonly SplineGUIUtility.EqualityComparer<float3> s_ComparerZ = (a, b) => a.z.Equals(b.z);

        readonly FloatField m_X;
        readonly FloatField m_Y;
        readonly FloatField m_Z;

        readonly Func<T, float3> m_Get;
        readonly Action<T, float3> m_Set;

        IReadOnlyList<T> m_Elements = new List<T>(0);

        public event Action changed;

        public Float3PropertyField(string label, Func<T, float3> get, Action<T, float3> set) : base(label)
        {
            m_Get = get;
            m_Set = set;

            m_X = this.Q<FloatField>("unity-x-input");
            m_Y = this.Q<FloatField>("unity-y-input");
            m_Z = this.Q<FloatField>("unity-z-input");

            m_X.RegisterValueChangedCallback(ApplyX);
            m_Y.RegisterValueChangedCallback(ApplyY);
            m_Z.RegisterValueChangedCallback(ApplyZ);
        }

        public void Update(IReadOnlyList<T> elements)
        {
            m_Elements = elements;

            s_Float3Buffer.Clear();
            for (int i = 0; i < elements.Count; ++i)
                s_Float3Buffer.Add(m_Get.Invoke(elements[i]));

            var value = s_Float3Buffer.Count > 0 ? s_Float3Buffer[0] : 0;
            m_X.showMixedValue = SplineGUIUtility.HasMultipleValues(s_Float3Buffer, s_ComparerX);
            if (!m_X.showMixedValue)
                m_X.SetValueWithoutNotify(value[0]);

            m_Y.showMixedValue = SplineGUIUtility.HasMultipleValues(s_Float3Buffer, s_ComparerY);
            if (!m_Y.showMixedValue)
                m_Y.SetValueWithoutNotify(value[1]);

            m_Z.showMixedValue = SplineGUIUtility.HasMultipleValues(s_Float3Buffer, s_ComparerZ);
            if (!m_Z.showMixedValue)
                m_Z.SetValueWithoutNotify(value[2]);
        }

        void ApplyX(ChangeEvent<float> evt)
        {
            EditorSplineUtility.RecordObjects(m_Elements, SplineInspectorOverlay.SplineChangeUndoMessage);

            ElementInspector.ignoreKnotCallbacks = true;
            for (int i = 0; i < m_Elements.Count; ++i)
            {
                var value = m_Get.Invoke(m_Elements[i]);
                value.x = evt.newValue;
                m_Set.Invoke(m_Elements[i], value);
            }

            m_X.showMixedValue = false;
            m_X.SetValueWithoutNotify(evt.newValue);
            changed?.Invoke();
            ElementInspector.ignoreKnotCallbacks = false;
        }

        void ApplyY(ChangeEvent<float> evt)
        {
            EditorSplineUtility.RecordObjects(m_Elements, SplineInspectorOverlay.SplineChangeUndoMessage);

            ElementInspector.ignoreKnotCallbacks = true;
            for (int i = 0; i < m_Elements.Count; ++i)
            {
                var value = m_Get.Invoke(m_Elements[i]);
                value.y = evt.newValue;
                m_Set.Invoke(m_Elements[i], value);
            }
            m_Y.showMixedValue = false;
            m_Y.SetValueWithoutNotify(evt.newValue);
            changed?.Invoke();
            ElementInspector.ignoreKnotCallbacks = false;
        }

        void ApplyZ(ChangeEvent<float> evt)
        {
            EditorSplineUtility.RecordObjects(m_Elements, SplineInspectorOverlay.SplineChangeUndoMessage);

            ElementInspector.ignoreKnotCallbacks = true;
            for (int i = 0; i < m_Elements.Count; ++i)
            {
                var value = m_Get.Invoke(m_Elements[i]);
                value.z = evt.newValue;
                m_Set.Invoke(m_Elements[i], value);
            }
            m_Z.showMixedValue = false;
            m_Z.SetValueWithoutNotify(evt.newValue);
            changed?.Invoke();
            ElementInspector.ignoreKnotCallbacks = false;
        }
    }
}
