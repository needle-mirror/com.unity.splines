using System.Collections.Generic;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Implement ISplineProvider on a MonoBehaviour to enable Spline tools in the Editor.
    /// </summary>
    public interface ISplineProvider
    {
        /// <summary>
        /// A collection of Splines contained on this MonoBehaviour.
        /// </summary>
        IEnumerable<Spline> Splines { get; }
    }
}
