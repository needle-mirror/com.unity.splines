using System;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Editor utility functions for working with <see cref="Spline"/> and <see cref="SplineData{T}"/>.
    /// </summary>
    public static class EditorSplineUtility
    {
        /// <summary>
        /// Invoked once per-frame if a spline property has been modified.
        /// </summary>
        public static event Action<Spline> afterSplineWasModified;
        
        static EditorSplineUtility()
        {
            Spline.afterSplineWasModified += (spline) =>
            {
                 afterSplineWasModified?.Invoke(spline);
            };
        }
        
        /// <summary>
        /// Use this function to register a callback that gets invoked
        /// once per-frame if any <see cref="SplineData{T}"/> changes occur.
        /// </summary>
        /// <param name="action">The callback to register.</param>
        /// <typeparam name="T">
        /// The type parameter of <see cref="SplineData{T}"/>.
        /// </typeparam>
        public static void RegisterSplineDataChanged<T>(Action<SplineData<T>> action)
        {
            SplineData<T>.afterSplineDataWasModified += action;
        }

        /// <summary>
        /// Use this function to unregister <see cref="SplineData{T}"/> change callback.
        /// </summary>
        /// <param name="action">The callback to unregister.</param>
        /// <typeparam name="T">
        /// The type parameter of <see cref="SplineData{T}"/>.
        /// </typeparam>
        public static void UnregisterSplineDataChanged<T>(Action<SplineData<T>> action)
        {
            SplineData<T>.afterSplineDataWasModified -= action;
        }
    }
}