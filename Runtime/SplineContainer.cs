using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// A component that holds a list of <see cref="Spline"/> objects.
    /// </summary>
#if UNITY_2021_2_OR_NEWER
    [Icon(k_IconPath)]
#endif
    [AddComponentMenu("Splines/Spline Container")]
    [ExecuteInEditMode]
    public sealed class SplineContainer : MonoBehaviour, ISplineContainer, ISerializationCallbackReceiver
    {
        const string k_IconPath = "Packages/com.unity.splines/Editor/Resources/Icons/SplineComponent.png";

        // Keeping a main spline to be backwards compatible with older versions of the spline package
        [SerializeField, Obsolete, HideInInspector]
        Spline m_Spline;

        [SerializeField]
        Spline[] m_Splines = { new Spline() };

        [SerializeField]
        KnotLinkCollection m_Knots = new KnotLinkCollection();

        List<(int previousIndex, int newIndex)> m_ReorderedSplinesIndices = new List<(int, int)>();
        List<int> m_RemovedSplinesIndices = new List<int>();
        List<int> m_AddedSplinesIndices = new List<int>();

        /// <summary>
        /// Invoked any time a spline is added to the container.
        /// </summary>
        /// <remarks>
        /// The parameter corresponds to the spline index.
        /// </remarks>
        public static event Action<SplineContainer, int> SplineAdded;

        /// <summary>
        /// Invoked any time a spline is removed from the container.
        /// </summary>
        /// <remarks>
        /// The parameter corresponds to the spline index.
        /// </remarks>
        public static event Action<SplineContainer, int> SplineRemoved;

        /// <summary>
        /// Invoked any time a spline is reordered in the container.
        /// </summary>
        /// <remarks>
        /// The first parameter corresponds to the previous spline index,
        /// the second parameter corresponds to the new spline index.
        /// </remarks>
        public static event Action<SplineContainer, int, int> SplineReordered;

        ReadOnlyCollection<Spline> m_ReadOnlySplines;

        /// <summary>
        /// The list of all splines attached to that container.
        /// </summary>
        public IReadOnlyList<Spline> Splines
        {
            get => m_ReadOnlySplines ??= new ReadOnlyCollection<Spline>(m_Splines);
            set
            {
                if (value == null)
                {
                    m_Splines = Array.Empty<Spline>();
                    return;
                }

                m_ReorderedSplinesIndices.Clear();
                m_RemovedSplinesIndices.Clear();
                m_AddedSplinesIndices.Clear();

                for (var i = 0; i < m_Splines.Length; i++)
                {
                    var index = IndexOf(value, m_Splines[i]);
                    if (index == -1)
                        m_RemovedSplinesIndices.Add(i);
                    else if (index != i)
                        m_ReorderedSplinesIndices.Add((i, index));
                }

                for (var i = 0; i < value.Count; i++)
                {
                    var index = Array.FindIndex(m_Splines, spline => spline == value[i]);
                    if (index == -1)
                        m_AddedSplinesIndices.Add(i);
                }

                m_Splines = new Spline[value.Count];
                for (int i = 0; i < m_Splines.Length; ++i)
                    m_Splines[i] = value[i];

                m_ReadOnlySplines = new ReadOnlyCollection<Spline>(m_Splines);

                foreach (var removedIndex in m_RemovedSplinesIndices)
                    SplineRemoved?.Invoke(this, removedIndex);

                foreach (var addedIndex in m_AddedSplinesIndices)
                    SplineAdded?.Invoke(this, addedIndex);

                foreach (var reorderedSpline in m_ReorderedSplinesIndices)
                    SplineReordered?.Invoke(this, reorderedSpline.previousIndex, reorderedSpline.newIndex);
            }
        }

        static int IndexOf(IReadOnlyList<Spline> self, Spline elementToFind)
        {
            for (var i = 0; i < self.Count; i++)
            {
                var element = self[i];
                if (element == elementToFind)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// A collection of all linked knots. Linked knots can be on different splines. However, knots can
        /// only link to other knots within the same container. This collection is used to maintain
        /// the validity of the links when operations such as knot insertions or removals are performed on the splines.
        /// </summary>
        public KnotLinkCollection KnotLinkCollection => m_Knots;

        /// <summary>
        /// Gets or sets the <see cref="Spline"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public Spline this[int index] => m_Splines[index];

        void OnEnable()
        {
            Spline.Changed += OnSplineChanged;
        }

        void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
        }

        void OnSplineChanged(Spline spline, int index, SplineModification modificationType)
        {
            var splineIndex = Array.IndexOf(m_Splines, spline);
            if (splineIndex < 0)
                return;

            switch (modificationType)
            {
                case SplineModification.KnotModified:
                    this.SetLinkedKnotPosition(new SplineKnotIndex(splineIndex, index));
                    break;

                case SplineModification.KnotReordered:
                case SplineModification.KnotInserted:
                    m_Knots.KnotInserted(splineIndex, index);
                    break;

                case SplineModification.KnotRemoved:
                    m_Knots.KnotRemoved(splineIndex, index);
                    break;
            }
        }

        void OnKnotModified(Spline spline, int index)
        {
            var splineIndex = Array.IndexOf(m_Splines, spline);
            if (splineIndex >= 0)
                this.SetLinkedKnotPosition(new SplineKnotIndex(splineIndex, index));
        }

        bool IsScaled => transform.lossyScale != Vector3.one;

        /// <summary>
        /// The main <see cref="Spline"/> attached to this component.
        /// </summary>
        public Spline Spline
        {
            get => m_Splines.Length > 0 ? m_Splines[0] : null;
            set
            {
                if (m_Splines.Length > 0)
                    m_Splines[0] = value;
            }
        }

        /// <summary>
        /// Computes interpolated position, direction and upDirection at ratio t. Calling this method to get the
        /// 3 vectors is faster than calling independently EvaluateSplinePosition, EvaluateSplineTangent and EvaluateSplineUpVector
        /// for the same time t as it reduces some redundant computation.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <param name="position">The output variable for the float3 position at t.</param>
        /// <param name="tangent">The output variable for the float3 tangent at t.</param>
        /// <param name="upVector">The output variable for the float3 up direction at t.</param>
        /// <returns>Boolean value, true if a valid set of output variables as been computed.</returns>
        public bool Evaluate(float t, out float3 position, out float3 tangent, out float3 upVector)
            => Evaluate(0, t, out position, out tangent, out upVector);

        /// <summary>
        /// Computes the interpolated position, direction and upDirection at ratio t for the spline at index `splineIndex`. Calling this method to get the
        /// 3 vectors is faster than calling independently EvaluateSplinePosition, EvaluateSplineTangent and EvaluateSplineUpVector
        /// for the same time t as it reduces some redundant computation.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 that represents the ratio along the curve.</param>
        /// <param name="position">The output variable for the float3 position at t.</param>
        /// <param name="tangent">The output variable for the float3 tangent at t.</param>
        /// <param name="upVector">The output variable for the float3 up direction at t.</param>
        /// <returns>True if a valid set of output variables is computed and false otherwise.</returns>
        public bool Evaluate(int splineIndex, float t, out float3 position,  out float3 tangent,  out float3 upVector)
            => Evaluate(m_Splines[splineIndex], t, out position, out tangent, out upVector);

        /// <summary>
        /// Gets the interpolated position, direction, and upDirection at ratio t for a spline.  This method gets the three
        /// vectors faster than EvaluateSplinePosition, EvaluateSplineTangent and EvaluateSplineUpVector for the same
        /// time t, because it reduces some redundant computation.
        /// </summary>
        /// <typeparam name="T">The spline type.</typeparam>
        /// <param name="spline">The spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 that represents the ratio along the curve.</param>
        /// <param name="position">The output variable for the float3 position at t.</param>
        /// <param name="tangent">The output variable for the float3 tangent at t.</param>
        /// <param name="upVector">The output variable for the float3 up direction at t.</param>
        /// <returns>True if a valid set of output variables is computed and false otherwise.</returns>
        public bool Evaluate<T>(T spline, float t, out float3 position, out float3 tangent, out float3 upVector) where T : ISpline
        {
            if (spline == null)
            {
                position = float3.zero;
                tangent = new float3(0, 0, 1);
                upVector = new float3(0, 1, 0);
                return false;
            }

            if (IsScaled)
            {
                using var nativeSpline = new NativeSpline(spline, transform.localToWorldMatrix);
                return SplineUtility.Evaluate(nativeSpline, t, out position, out tangent, out upVector);
            }

            var evaluationStatus = SplineUtility.Evaluate(spline, t, out position, out tangent, out upVector);
            if (evaluationStatus)
            {
                position = transform.TransformPoint(position);
                tangent = transform.TransformVector(tangent);
                upVector = transform.TransformDirection(upVector);
            }

            return evaluationStatus;
        }

        /// <summary>
        /// Evaluates the position of a point, t, on a spline in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A tangent vector.</returns>
        public float3 EvaluatePosition(float t) => EvaluatePosition(0, t);

        /// <summary>
        /// Evaluates the position of a point, t, on a spline at an index, `splineIndex`, in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A world position along the spline.</returns>
        public float3 EvaluatePosition(int splineIndex, float t) => EvaluatePosition(m_Splines[splineIndex], t);

        /// <summary>
        /// Evaluates the position of a point, t, on a given spline, in world space.
        /// </summary>
        /// <typeparam name="T">The spline type.</typeparam>
        /// <param name="spline">The spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of the curve.</param>
        /// <returns>A world position along the spline.</returns>
        public float3 EvaluatePosition<T>(T spline, float t) where T : ISpline
        {
            if (spline== null)
                return float.PositiveInfinity;

            if (IsScaled)
            {
                using var nativeSpline = new NativeSpline(spline, transform.localToWorldMatrix);
                return SplineUtility.EvaluatePosition(nativeSpline, t);
            }

            return transform.TransformPoint(SplineUtility.EvaluatePosition(spline, t));
        }

        /// <summary>
        /// Evaluates the tangent vector of a point, t, on a spline in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed tangent vector.</returns>
        public float3 EvaluateTangent(float t) => EvaluateTangent(0, t);

        /// <summary>
        /// Evaluates the tangent vector of a point, t, on a spline at an index, `splineIndex`, in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed tangent vector.</returns>
        public float3 EvaluateTangent(int splineIndex, float t) => EvaluateTangent(m_Splines[splineIndex], t);

        /// <summary>
        /// Evaluates the tangent vector of a point, t, on a given spline, in world space.
        /// </summary>
        /// <typeparam name="T">The spline type.</typeparam>
        /// <param name="spline">The spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed tangent vector.</returns>
        public float3 EvaluateTangent<T>(T spline, float t) where T : ISpline
        {
            if (spline == null)
                return float.PositiveInfinity;

            if (IsScaled)
            {
                using var nativeSpline = new NativeSpline(spline, transform.localToWorldMatrix);
                return SplineUtility.EvaluateTangent(nativeSpline, t);
            }
            return transform.TransformVector(SplineUtility.EvaluateTangent(spline, t));
        }

        /// <summary>
        /// Evaluates the up vector of a point, t, on a spline in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed up direction.</returns>
        public float3 EvaluateUpVector(float t) => EvaluateUpVector(0, t);

        /// <summary>
        /// Evaluates the up vector of a point, t, on a spline at an index, `splineIndex`, in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the Spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed up direction.</returns>
        public float3 EvaluateUpVector(int splineIndex, float t) => EvaluateUpVector(m_Splines[splineIndex], t);

        /// <summary>
        /// Evaluates the up vector of a point, t, on a given spline, in world space.
        /// </summary>
        /// <typeparam name="T">The spline type.</typeparam>
        /// <param name="spline">The Spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed up direction.</returns>
        public float3 EvaluateUpVector<T>(T spline, float t) where T : ISpline
        {
            if (spline == null)
                return float3.zero;

            if (IsScaled)
            {
                using var nativeSpline = new NativeSpline(spline, transform.localToWorldMatrix, true);
                return SplineUtility.EvaluateUpVector(nativeSpline, t);
            }

            //Using TransformDirection as up direction is not sensible to scale.
            return transform.TransformDirection(SplineUtility.EvaluateUpVector(spline, t));
        }


        /// <summary>
        /// Evaluates the acceleration vector of a point, t, on a spline in world space.
        /// </summary>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed acceleration vector.</returns>
        public float3 EvaluateAcceleration(float t) => EvaluateAcceleration(0, t);

        /// <summary>
        /// Evaluates the acceleration vector of a point, t, on a spline at an index, `splineIndex,  in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed acceleration vector.</returns>
        public float3 EvaluateAcceleration(int splineIndex, float t) => EvaluateAcceleration(m_Splines[splineIndex], t);

        /// <summary>
        /// Evaluates the acceleration vector of a point, t, on a given Spline,  in world space.
        /// </summary>
        /// <typeparam name="T">The spline type.</typeparam>
        /// <param name="spline">The Spline to evaluate.</param>
        /// <param name="t">A value between 0 and 1 representing a percentage of entire spline.</param>
        /// <returns>The computed acceleration vector.</returns>
        public float3 EvaluateAcceleration<T>(T spline, float t) where T : ISpline
        {
            if (spline == null)
                return float3.zero;

            if (IsScaled)
            {
                using var nativeSpline = new NativeSpline(spline, transform.localToWorldMatrix);
                return SplineUtility.EvaluateAcceleration(nativeSpline, t);
            }

            return transform.TransformVector(SplineUtility.EvaluateAcceleration(spline, t));
        }

        /// <summary>
        /// Calculate the length of <see cref="Spline"/> in world space.
        /// </summary>
        /// <returns>The length of <see cref="Spline"/> in world space</returns>
        public float CalculateLength() => CalculateLength(0);

        /// <summary>
        /// Calculates the length of `Splines[splineIndex]` in world space.
        /// </summary>
        /// <param name="splineIndex">The index of the spline to evaluate.</param>
        /// <returns>The length of `Splines[splineIndex]` in world space</returns>
        public float CalculateLength(int splineIndex)
        {
            return SplineUtility.CalculateLength(m_Splines[splineIndex], transform.localToWorldMatrix);
        }

        /// <summary>
        /// See ISerializationCallbackReceiver.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// See ISerializationCallbackReceiver.
        /// </summary>
        public void OnAfterDeserialize()
        {
#pragma warning disable 612, 618
            if (m_Spline != null && m_Spline.Count > 0)
            {
                if (m_Splines == null || m_Splines.Length == 0 || m_Splines.Length == 1 && m_Splines[0].Count == 0)
                {
                    m_Splines = new[] { m_Spline };
                    m_ReadOnlySplines = new ReadOnlyCollection<Spline>(m_Splines);
                }

                m_Spline = new Spline(); //Clear spline
            }
#pragma warning restore 612, 618
        }
    }
}
