using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [Obsolete("Use SplineDataHandles.DataPointHandles instead and EditorTools to interact with SplineData.", false)]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CustomSplineDataHandle : Attribute
    {
        internal Type m_Type;

        public CustomSplineDataHandle(Type type)
        {
            m_Type = type;
        }
    }

#pragma warning disable 618
    interface ISplineDataHandle
    {
        public SplineDataHandleAttribute attribute
        { get; }

        void SetAttribute(SplineDataHandleAttribute attribute);
    }
#pragma warning restore 618

    /// <summary>
    /// SplineDataHandle is a base class to override in order to enable custom handles for spline data.
    /// The Drawer needs to inherit from this class and override the method corresponding to the correct splineData type.
    /// Either one of the method or both can be overriden regarding the user needs.
    /// </summary>
    /// <typeparam name="T">
    /// The type parameter of the <see cref="SplineData{T}"/> that this drawer targets.
    /// </typeparam>
    [Obsolete("Use SplineDataHandles.DataPointHandles instead and EditorTools to interact with SplineData.", false)]
    public abstract class SplineDataHandle<T> : ISplineDataHandle
    {
        internal int[] m_ControlIDs;

        SplineDataHandleAttribute m_Attribute;
        public SplineDataHandleAttribute attribute => m_Attribute;

        /// <summary>
        /// Array of reserved control IDs used for <see cref="SplineData{T}"/> handles.
        /// </summary>
        public int[] controlIDs => m_ControlIDs;

        void ISplineDataHandle.SetAttribute(SplineDataHandleAttribute attribute)
        {
            m_Attribute = attribute;
        }

        /// <summary>
        /// Override this method to create custom handles for <see cref="SplineData{T}"/>,
        /// this method is called before DrawKeyframe in the render loop.
        /// </summary>
        /// <param name="splineData">The <see cref="SplineData{T}"/> for which the method is drawing handles.</param>
        /// <param name="spline">The target Spline associated to the SplineData for the drawing.</param>
        /// <param name="localToWorld">The spline localToWorld Matrix.</param>
        /// <param name="color">The color defined in the SplineData scene interface.</param>
        public virtual void DrawSplineData(SplineData<T> splineData, Spline spline, Matrix4x4 localToWorld, Color color)
        {}

        /// <summary>
        /// Override this method to create custom handles for a <see cref="DataPoint{T}"/>in <see cref="SplineData{T}"/>,
        /// 'position' and 'direction' are given in the Spline-space basis.
        /// This method is called after DrawSplineData in the render loop.
        /// </summary>
        /// <param name="controlID">A control ID from <see cref="controlIDs"/> that represents this handle.</param>
        /// <param name="position">The position of the keyframe data in spline space.</param>
        /// <param name="direction">The direction of the spline at the current keyframe.</param>
        /// <param name="upDirection">The up vector orthogonal to the spline direction at the current keyframe regarding knot rotation.</param>
        /// <param name="splineData">The <see cref="SplineData{T}"/> for which the method is drawing handles.</param>
        /// <param name="dataPointIndex">The index of the current keyframe to handle.</param>
        public virtual void DrawDataPoint(
            int controlID,
            Vector3 position,
            Vector3 direction,
            Vector3 upDirection,
            SplineData<T> splineData,
            int dataPointIndex)
        {}
    }
}