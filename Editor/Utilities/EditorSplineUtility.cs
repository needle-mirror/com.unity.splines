using System;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    public static class EditorSplineUtility
    {
        public static event Action<Spline> afterSplineWasModified;
        
        static EditorSplineUtility()
        {
            Spline.afterSplineWasModified += (spline) =>
            {
                 afterSplineWasModified?.Invoke(spline);
            };
        }
        
        public static void RegisterSplineDataChanged<T>(Action<SplineData<T>> action)
        {
            SplineData<T>.afterSplineDataWasModified += action;
        }
        
        public static void UnregisterSplineDataChanged<T>(Action<SplineData<T>> action)
        {
            SplineData<T>.afterSplineDataWasModified -= action;
        }
    }
}