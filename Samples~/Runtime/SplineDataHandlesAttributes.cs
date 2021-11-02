using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Splines.Examples
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BorderHandleAttribute : SplineDataHandleAttribute {}
    
    [AttributeUsage(AttributeTargets.Field)]
    public class WidthHandleAttribute : SplineDataHandleAttribute {}
    
    [AttributeUsage(AttributeTargets.Field)]
    public class DriftHandleAttribute : SplineDataHandleAttribute {}
    
    [AttributeUsage(AttributeTargets.Field)]
    public class PointHandleAttribute : SplineDataHandleAttribute {}

    [AttributeUsage(AttributeTargets.Field)]
    public class SpeedHandleAttribute : SplineDataHandleAttribute
    {
        public float maxSpeed;
        public SpeedHandleAttribute(float maxSpeed)
        {
            this.maxSpeed = maxSpeed;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class TiltHandleAttribute : SplineDataHandleAttribute {}
    
}