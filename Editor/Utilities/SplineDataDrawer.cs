using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// SplineDataDrawer is a base class to override in order to enable custom handles for spline data.
    /// The Drawer needs to inherit from this class and override the method corresponding to the correct splineData type.
    /// Either one of the method or both can be overriden regarding the user needs.
    /// </summary>
    public abstract class SplineDataDrawer<T>
    {
        int[] m_IDs;

        public int[] controlIDs
        {
            get => m_IDs;
            set => m_IDs = value;
        }
        
        /// <summary>
        /// Override this method to create custom handles for a SplineData<T>,
        /// this method is called before DrawKeyframe in the render loop.
        /// </summary>
        /// <param name="splineData">The SplineData<T> for which the method is drawing handles.</param>
        /// <param name="spline">The target Spline associated to the SplineData for the drawing.</param>
        /// <param name="localToWorld">The spline localToWorld Matrix.</param>
        /// <param name="color">The color defined in the SplineData scene interface.</param>
        public virtual void DrawSplineData(SplineData<T> splineData, Spline spline, Matrix4x4 localToWorld, Color color)
        {}

        /// <summary>
        /// Override this method to create custom handles for a Keyframe<T> in SplineData<T>,
        /// 'position' and 'direction' are given in the Spline-space basis.
        /// This method is called after DrawSplineData in the render loop.
        /// </summary>
        /// <param name="controlID">The control ID for the handle.</param>
        /// <param name="position">The position of the keyframe data in spline space.</param>
        /// <param name="direction">The direction of the spline at the current keyframe.</param> 
        /// <param name="upDirection">The up vector orthogonal to the spline direction at the current keyframe regarding knot rotation.</param>
        /// <param name="splineData">The SplineData<T> for which the method is drawing handles.</param>
        /// <param name="keyframeIndex">The index of the current keyframe to handle.</param>
        public virtual void DrawKeyframe(
            int controlID, 
            Vector3 position, 
            Vector3 direction,
            Vector3 upDirection,
            SplineData<T> splineData, 
            int keyframeIndex) 
        {}
    }
}