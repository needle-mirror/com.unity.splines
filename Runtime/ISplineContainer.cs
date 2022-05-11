using System.Collections.Generic;

namespace UnityEngine.Splines
{
    public interface ISplineContainer
    {
        IReadOnlyList<Spline> Splines { get; set; }
        KnotLinkCollection KnotLinkCollection { get; }
    }
}