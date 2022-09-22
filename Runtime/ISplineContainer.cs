using System;
using System.Collections.Generic;

namespace UnityEngine.Splines
{
    /// <summary>
    /// An interface that represents ISplineContainer on a MonoBehaviour to enable Spline tools in the Editor.
    /// </summary>
    public interface ISplineContainer
    {
        /// <summary>
        /// A collection of splines contained in this MonoBehaviour.
        /// </summary>
        IReadOnlyList<Spline> Splines { get; set; }

        /// <summary>
        /// A collection of KnotLinks to maintain valid links between knots.
        /// </summary>
        KnotLinkCollection KnotLinkCollection { get; }
        
#if UNITY_EDITOR
        internal static Action<ISplineContainer, int> SplineAdded;
        internal static Action<ISplineContainer, int> SplineRemoved;
#endif
    }
}