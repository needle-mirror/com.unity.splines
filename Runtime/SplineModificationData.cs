namespace UnityEngine.Splines
{
    /// <summary>
    /// Describes the different types of changes that can occur to a spline.
    /// </summary>
    public enum SplineModification
    {
        /// <summary>
        /// The default modification type. This is used when no other SplineModification types apply.
        /// </summary>
        Default,

        /// <summary>
        /// The spline's <see cref="Spline.Closed"/> property was modified.
        /// </summary>
        ClosedModified,

        /// <summary>
        /// A knot was modified.
        /// </summary>
        KnotModified,

        /// <summary>
        /// A knot was inserted.
        /// </summary>
        KnotInserted,

        /// <summary>
        /// A knot was removed.
        /// </summary>
        KnotRemoved,

        /// <summary>
        /// A knot was reordered.
        /// </summary>
        KnotReordered
    }

    struct SplineModificationData
    {
        public readonly Spline @Spline;
        public readonly SplineModification Modification;
        public readonly int KnotIndex;
        // Length of curve before the edited knot (if insert then length of the curve inserted into).
        public readonly float PrevCurveLength;
        // Length of the edited knot's curve (has no meaning if modification is insert).
        public readonly float NextCurveLength;

        public SplineModificationData(Spline spline, SplineModification modification, int knotIndex, float prevCurveLength, float nextCurveLength)
        {
            Spline = spline;
            Modification = modification;
            KnotIndex = knotIndex;
            PrevCurveLength = prevCurveLength;
            NextCurveLength = nextCurveLength;
        }
    }
}
