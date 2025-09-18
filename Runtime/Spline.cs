using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Mathematics;
using UObject = UnityEngine.Object;

namespace UnityEngine.Splines
{
    /// <summary>
    /// The Spline class is a collection of <see cref="BezierKnot"/>, the closed/open state, and editing representation.
    /// </summary>
    [Serializable]
    public class Spline : ISpline, IList<BezierKnot>
    {
        const TangentMode k_DefaultTangentMode = TangentMode.Broken;
        const BezierTangent k_DefaultMainTangent = BezierTangent.Out;
        const int k_BatchModification = -1;

        [Serializable]
        sealed class MetaData
        {
            public TangentMode Mode;
            public float Tension;

            DistanceToInterpolation[] m_DistanceToInterpolation = new DistanceToInterpolation[k_CurveDistanceLutResolution];

            public DistanceToInterpolation[] DistanceToInterpolation
            {
                get
                {
                    if (m_DistanceToInterpolation == null || m_DistanceToInterpolation.Length != k_CurveDistanceLutResolution)
                    {
                        m_DistanceToInterpolation = new DistanceToInterpolation[k_CurveDistanceLutResolution];
                        InvalidateCache();
                    }

                    return m_DistanceToInterpolation;
                }
            }

            float3[] m_UpVectors = new float3[k_CurveDistanceLutResolution];
            public float3[] UpVectors
            {
                get
                {
                    if (m_UpVectors == null || m_UpVectors.Length != k_CurveDistanceLutResolution)
                    {
                        m_UpVectors = new float3[k_CurveDistanceLutResolution];
                        InvalidateCache();
                    }

                    return m_UpVectors;
                }
            }

            public MetaData()
            {
                Mode = k_DefaultTangentMode;
                Tension = SplineUtility.CatmullRomTension;
                InvalidateCache();
            }

            public MetaData(MetaData toCopy)
            {
                Mode = toCopy.Mode;
                Tension = toCopy.Tension;
                Array.Copy(toCopy.DistanceToInterpolation, DistanceToInterpolation, DistanceToInterpolation.Length);
                Array.Copy(toCopy.UpVectors, UpVectors, UpVectors.Length);
            }

            public void InvalidateCache()
            {
                DistanceToInterpolation[0] = Splines.DistanceToInterpolation.Invalid;
                UpVectors[0] = Vector3.zero;
            }
        }

        const int k_CurveDistanceLutResolution = 30;

        [SerializeField, Obsolete, HideInInspector]
#pragma warning disable CS0618
        SplineType m_EditModeType = SplineType.Bezier;
#pragma warning restore CS0618

        [SerializeField]
        List<BezierKnot> m_Knots = new List<BezierKnot>();

        float m_Length = -1f;

        [SerializeField, HideInInspector]
        List<MetaData> m_MetaData = new List<MetaData>();

        [SerializeField]
        bool m_Closed;

        [SerializeField]
        SplineDataDictionary<int> m_IntData = new SplineDataDictionary<int>();

        [SerializeField]
        SplineDataDictionary<float> m_FloatData = new SplineDataDictionary<float>();

        [SerializeField]
        SplineDataDictionary<float4> m_Float4Data = new SplineDataDictionary<float4>();

        [SerializeField]
        SplineDataDictionary<UObject> m_ObjectData = new SplineDataDictionary<UObject>();

        IEnumerable<ISplineModificationHandler> embeddedSplineData
        {
            get
            {
                foreach (var data in m_IntData) yield return data.Value;
                foreach (var data in m_FloatData) yield return data.Value;
                foreach (var data in m_Float4Data) yield return data.Value;
                foreach (var data in m_ObjectData) yield return data.Value;
            }
        }

        /// <summary>
        /// Retrieve a <see cref="SplineData{T}"/> reference for <paramref name="key"/> if it exists.
        /// Note that this is a reference to the stored <see cref="SplineData{T}"/>, not a copy. Any modifications to
        /// this collection will affect the <see cref="Spline"/> data.
        /// </summary>
        /// <param name="key">The string key value to search for. Only one instance of a key value can exist in an
        /// embedded <see cref="SplineData{T}"/> collection, however keys are unique to each data type. The same key
        /// can be re-used to store float data and Object data.</param>
        /// <param name="data">The output <see cref="SplineData{T}"/> if the key is found.</param>
        /// <returns>True if the key and type combination are found, otherwise false.</returns>
        public bool TryGetFloatData(string key, out SplineData<float> data) => m_FloatData.TryGetValue(key, out data);

        /// <inheritdoc cref="TryGetFloatData"/>
        public bool TryGetFloat4Data(string key, out SplineData<float4> data) => m_Float4Data.TryGetValue(key, out data);

        /// <inheritdoc cref="TryGetFloatData"/>
        public bool TryGetIntData(string key, out SplineData<int> data) => m_IntData.TryGetValue(key, out data);

        /// <inheritdoc cref="TryGetFloatData"/>
        public bool TryGetObjectData(string key, out SplineData<UObject> data) => m_ObjectData.TryGetValue(key, out data);

        /// <summary>
        /// Returns a <see cref="SplineData{T}"/> for <paramref name="key"/>. If an instance matching the key and
        /// type does not exist, a new entry is appended to the internal collection and returned.
        /// Note that this is a reference to the stored <see cref="SplineData{T}"/>, not a copy. Any modifications to
        /// this collection will affect the <see cref="Spline"/> data.
        /// </summary>
        /// <param name="key">The string key value to search for. Only one instance of a key value can exist in an
        /// embedded <see cref="SplineData{T}"/> collection, however keys are unique to each data type. The same key
        /// can be re-used to store float data and Object data.</param>
        /// <returns>A <see cref="SplineData{T}"/> of the requested type.</returns>
        public SplineData<float> GetOrCreateFloatData(string key) => m_FloatData.GetOrCreate(key);

        /// <inheritdoc cref="GetOrCreateFloatData"/>
        public SplineData<float4> GetOrCreateFloat4Data(string key) => m_Float4Data.GetOrCreate(key);

        /// <inheritdoc cref="GetOrCreateFloatData"/>
        public SplineData<int> GetOrCreateIntData(string key) => m_IntData.GetOrCreate(key);

        /// <inheritdoc cref="GetOrCreateFloatData"/>
        public SplineData<UObject> GetOrCreateObjectData(string key) => m_ObjectData.GetOrCreate(key);

        /// <summary>
        /// Remove a <see cref="SplineData{T}"/> value.
        /// </summary>
        /// <param name="key">The string key value to search for. Only one instance of a key value can exist in an
        /// embedded <see cref="SplineData{T}"/> collection, however keys are unique to each data type. The same key
        /// can be re-used to store float data and Object data.</param>
        /// <returns>Returns true if a matching <see cref="SplineData{T}"/> key value pair was found and removed, or
        /// false if no match was found.</returns>
        public bool RemoveFloatData(string key) => m_FloatData.Remove(key);

        /// <inheritdoc cref="RemoveFloatData"/>
        public bool RemoveFloat4Data(string key) => m_Float4Data.Remove(key);

        /// <inheritdoc cref="RemoveFloatData"/>
        public bool RemoveIntData(string key) => m_IntData.Remove(key);

        /// <inheritdoc cref="RemoveFloatData"/>
        public bool RemoveObjectData(string key) => m_ObjectData.Remove(key);

        /// <summary>
        /// Get a collection of the keys of embedded <see cref="SplineData{T}"/> for this type.
        /// </summary>
        /// <returns>An enumerable list of keys present for the requested type.</returns>
        public IEnumerable<string> GetFloatDataKeys() => m_FloatData.Keys;

        /// <inheritdoc cref="GetFloatDataKeys"/>
        public IEnumerable<string> GetFloat4DataKeys() => m_Float4Data.Keys;

        /// <inheritdoc cref="GetFloatDataKeys"/>
        public IEnumerable<string> GetIntDataKeys() => m_IntData.Keys;

        /// <inheritdoc cref="GetFloatDataKeys"/>
        public IEnumerable<string> GetObjectDataKeys() => m_ObjectData.Keys;

        /// <inheritdoc cref="GetFloatDataKeys"/>
        public IEnumerable<string> GetSplineDataKeys(EmbeddedSplineDataType type)
        {
            switch (type)
            {
                case EmbeddedSplineDataType.Float: return m_FloatData.Keys;
                case EmbeddedSplineDataType.Float4: return m_Float4Data.Keys;
                case EmbeddedSplineDataType.Int: return m_IntData.Keys;
                case EmbeddedSplineDataType.Object: return m_ObjectData.Keys;
                default: throw new InvalidEnumArgumentException();
            }
        }

        /// <summary>
        /// Get a collection of the <see cref="SplineData{T}"/> values for this type.
        /// </summary>
        /// <returns>An enumerable list of values present for the requested type.</returns>
        public IEnumerable<SplineData<float>> GetFloatDataValues() => m_FloatData.Values;

        /// <inheritdoc cref="GetFloatDataValues"/>
        public IEnumerable<SplineData<float4>> GetFloat4DataValues() => m_Float4Data.Values;

        /// <inheritdoc cref="GetFloatDataValues"/>
        public IEnumerable<SplineData<int>> GetIntDataValues() => m_IntData.Values;

        /// <inheritdoc cref="GetFloatDataValues"/>
        public IEnumerable<SplineData<Object>> GetObjectDataValues() => m_ObjectData.Values;

        /// <summary>
        /// Set the <see cref="SplineData{T}"/> for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The string key value to search for. Only one instance of a key value can exist in an
        /// embedded <see cref="SplineData{T}"/> collection, however keys are unique to each data type. The same key
        /// can be re-used to store float data and Object data.</param>
        /// <param name="value">The <see cref="SplineData{T}"/> to set. This value will be copied.</param>
        public void SetFloatData(string key, SplineData<float> value) => m_FloatData[key] = value;

        /// <inheritdoc cref="SetFloatData"/>
        public void SetFloat4Data(string key, SplineData<float4> value) => m_Float4Data[key] = value;

        /// <inheritdoc cref="SetFloatData"/>
        public void SetIntData(string key, SplineData<int> value) => m_IntData[key] = value;

        /// <inheritdoc cref="SetFloatData"/>
        public void SetObjectData(string key, SplineData<UObject> value) => m_ObjectData[key] = value;

        /// <summary>
        /// Return the number of knots.
        /// </summary>
        public int Count => m_Knots.Count;

        /// <summary>
        /// Returns true if this Spline is read-only, false if it is mutable.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Invoked in the editor any time a spline property is modified.
        /// </summary>
        /// <remarks>
        /// In the editor this can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.AfterSplineWasModified"/> when
        /// working with splines in the editor.
        /// </remarks>
        [Obsolete("Deprecated, use " + nameof(Changed) + " instead.")]
        public event Action changed;

        /// <summary>
        /// Invoked any time a spline is modified.
        /// </summary>
        /// <remarks>
        /// First parameter is the target Spline that the event is raised for, second parameter is
        /// the knot index and the third parameter represents the type of change that occurred.
        /// If the event does not target a specific knot, the second parameter will have the value of -1.
        ///
        /// In the editor this callback can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.AfterSplineWasModified"/> when
        /// working with splines in the editor.
        /// </remarks>
        /// <seealso cref="SplineModification"/>
        public static event Action<Spline, int, SplineModification> Changed;

#if UNITY_EDITOR
        internal static Action<Spline> afterSplineWasModified;
        internal static Action<Spline> afterSplineWasModifiedSceneLoop;
        [NonSerialized]
        bool m_QueueAfterSplineModifiedCallback;
#endif

        (float curve0, float curve1) m_LastKnotChangeCurveLengths;

        internal void SetDirtyNoNotify()
        {
            EnsureMetaDataValid();
            m_Length = -1f;
            for (int i = 0, c = m_MetaData.Count; i < c; ++i)
                m_MetaData[i].InvalidateCache();
        }

        internal void SetDirty(SplineModification modificationEvent, int knotIndex = k_BatchModification)
        {
            SetDirtyNoNotify();

#pragma warning disable 618
            changed?.Invoke();
#pragma warning restore 618

            OnSplineChanged();

            foreach (var data in embeddedSplineData)
                data.OnSplineModified(new SplineModificationData(this, modificationEvent, knotIndex, m_LastKnotChangeCurveLengths.curve0, m_LastKnotChangeCurveLengths.curve1));

            Changed?.Invoke(this, knotIndex, modificationEvent);

#if UNITY_EDITOR
            if (m_QueueAfterSplineModifiedCallback)
                return;

            m_QueueAfterSplineModifiedCallback = true;

            UnityEditor.SceneView.duringSceneGui += OnAfterSplineWasModifiedSceneLoop;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                m_QueueAfterSplineModifiedCallback = false;
                afterSplineWasModified?.Invoke(this);
            };
#endif
        }

#if UNITY_EDITOR
        private void OnAfterSplineWasModifiedSceneLoop(UnityEditor.SceneView sceneView)
        {
            UnityEditor.SceneView.duringSceneGui -= OnAfterSplineWasModifiedSceneLoop;
            afterSplineWasModifiedSceneLoop?.Invoke(this);
        }
#endif

        /// <summary>
        /// Invoked any time a spline property is modified.
        /// </summary>
        /// <remarks>
        /// In the editor this can be invoked many times per-frame.
        /// Prefer to use <see cref="UnityEditor.Splines.EditorSplineUtility.AfterSplineWasModified"/> when working
        /// with splines in the editor.
        /// </remarks>
        protected virtual void OnSplineChanged()
        {
        }

        void EnsureMetaDataValid()
        {
            while(m_MetaData.Count < m_Knots.Count)
                m_MetaData.Add(new MetaData());
        }

        /// <summary>
        /// Ensure that a <see cref="BezierKnot"/> has the correct tangent and rotation values to match it's
        /// <see cref="TangentMode"/> and tension. This can be necessary if knot data is modified outside of the Spline
        /// class (ex, manually setting the <see cref="Knots"/> array without taking care to also set the tangent
        /// modes).
        /// </summary>
        /// <param name="index">The knot index to set tangent and rotation values for.</param>
        public void EnforceTangentModeNoNotify(int index) => EnforceTangentModeNoNotify(new SplineRange(index, 1));

        /// <summary>
        /// Ensure that a <see cref="BezierKnot"/> has the correct tangent and rotation values to match it's
        /// <see cref="TangentMode"/> and tension. This can be necessary if knot data is modified outside of the Spline
        /// class (ex, manually setting the <see cref="Knots"/> array without taking care to also set the tangent
        /// modes).
        /// </summary>
        /// <param name="range">The <see cref="SplineRange"/> range of knot indices to set tangent and rotation values
        /// for.</param>
        public void EnforceTangentModeNoNotify(SplineRange range)
        {
            for(int i = range.Start; i <= range.End; ++i)
                ApplyTangentModeNoNotify(i);
        }

        /// <summary>
        /// Gets the <see cref="TangentMode"/> for a knot index.
        /// </summary>
        /// <param name="index">The index to retrieve <see cref="TangentMode"/> data for.</param>
        /// <returns>A <see cref="TangentMode"/> for the knot at index.</returns>
        public TangentMode GetTangentMode(int index)
        {
            EnsureMetaDataValid();
            return m_MetaData.Count > 0 ? m_MetaData[index].Mode : k_DefaultTangentMode;
        }

        /// <summary>
        /// Sets the <see cref="TangentMode"/> for all knots on this spline.
        /// </summary>
        /// <param name="mode">The <see cref="TangentMode"/> to apply to each knot.</param>
        public void SetTangentMode(TangentMode mode)
        {
            SetTangentMode(new SplineRange(0, Count), mode);
        }

        /// <summary>
        /// Sets the <see cref="TangentMode"/> for a knot, and ensures that the rotation and tangent values match the
        /// behavior of the tangent mode.
        /// This function can modify the contents of the <see cref="BezierKnot"/> at the specified index.
        /// </summary>
        /// <param name="index">The index of the knot to set.</param>
        /// <param name="mode">The mode to set.</param>
        /// <param name="main">The tangent direction to align both the In and Out tangent when assigning Continuous
        /// or Mirrored tangent mode.</param>
        public void SetTangentMode(int index, TangentMode mode, BezierTangent main = k_DefaultMainTangent)
        {
            if (GetTangentMode(index) == mode)
                return;

            // In the case of an open spline, changing knot mode to a mirrored mode will change the shape of
            // the spline as the considered tangent is the out tangent by default. To avoid this, we change
            // the considered tangent to the in tangent when the knot is the last knot of an open spline.
            if (index == Count - 1 && !Closed)
                main = BezierTangent.In;

            SetTangentMode(new SplineRange(index, 1), mode, main);
        }

        /// <summary>
        /// Sets the <see cref="TangentMode"/> for a series of knots, and ensures that the rotation and tangent values
        /// match the behavior of the tangent mode.
        /// This function can modify the contents of the <see cref="BezierKnot"/> at the specified indices.
        /// </summary>
        /// <param name="range">The range of knot indices to set.</param>
        /// <param name="mode">The mode to set.</param>
        /// <param name="main">The tangent direction to align both the In and Out tangent with when Continuous or
        /// Mirrored tangent mode is assigned .</param>
        public void SetTangentMode(SplineRange range, TangentMode mode, BezierTangent main = k_DefaultMainTangent)
        {
            foreach (var index in range)
            {
                CacheKnotOperationCurves(index);
                SetTangentModeNoNotify(index, mode, main);
                SetDirty(SplineModification.KnotModified, index);
            }
        }

        /// <summary>
        /// Sets the <see cref="TangentMode"/> for a knot, and ensures that the rotation and tangent values match the
        /// behavior of the tangent mode. No changed callbacks will be invoked.
        /// This function can modify the contents of the <see cref="BezierKnot"/> at the specified index.
        /// </summary>
        /// <param name="index">The index of the knot to set.</param>
        /// <param name="mode">The mode to set.</param>
        /// <param name="main">The tangent direction to align both the In and Out tangent when assigning Continuous
        /// or Mirrored tangent mode.</param>
        public void SetTangentModeNoNotify(int index, TangentMode mode, BezierTangent main = k_DefaultMainTangent)
        {
            EnsureMetaDataValid();

            var knot = m_Knots[index];

            // If coming from a tangent mode where tangents are locked to 0 length, preset the Bezier tangents with a
            // likely non-zero length.
            if (m_MetaData[index].Mode == TangentMode.Linear && mode >= TangentMode.Mirrored)
            {
                knot.TangentIn = SplineUtility.GetExplicitLinearTangent(knot, this.Previous(index));
                knot.TangentOut = SplineUtility.GetExplicitLinearTangent(knot, this.Next(index));
            }

            m_MetaData[index].Mode = mode;
            m_Knots[index] = knot;
            ApplyTangentModeNoNotify(index, main);
        }

        /// <summary>
        /// Ensures that the tangents at an index conform to the tangent mode.
        /// </summary>
        /// <remarks>
        /// This function updates the tangents, but does not set the tangent mode.
        /// </remarks>
        /// <param name="index">The index of the knot to set tangent values for.</param>
        /// <param name="main">The tangent direction to align the In and Out tangent to when assigning Continuous
        /// or Mirrored tangent mode.</param>
        void ApplyTangentModeNoNotify(int index, BezierTangent main = k_DefaultMainTangent)
        {
            var knot = m_Knots[index];
            var mode = GetTangentMode(index);

            switch(mode)
            {
                case TangentMode.Continuous:
                    knot = knot.BakeTangentDirectionToRotation(false, main);
                    break;

                case TangentMode.Mirrored:
                    knot = knot.BakeTangentDirectionToRotation(true, main);
                    break;

                case TangentMode.Linear:
                    knot.TangentIn = float3.zero;
                    knot.TangentOut = float3.zero;
                    break;

                case TangentMode.AutoSmooth:
                    knot = SplineUtility.GetAutoSmoothKnot(knot.Position,
                        this.Previous(index).Position,
                        this.Next(index).Position,
                        math.mul(knot.Rotation, math.up()),
                        m_MetaData[index].Tension);
                    break;
            }

            m_Knots[index] = knot;
            SetDirtyNoNotify();
        }

        /// <summary>
        /// Gets the tension value for the requested index.
        /// </summary>
        /// <param name="index">The knot index to get a tension value for.</param>
        /// <returns>Returns the tension value for the requested index.</returns>
        public float GetAutoSmoothTension(int index) => m_MetaData[index].Tension;

        /// <summary>
        /// Sets the tension that is used to calculate the magnitude of tangents when the <see cref="TangentMode"/> is
        /// <see cref="TangentMode.AutoSmooth"/>. Valid values are between 0 and 1.
        /// A lower value results in sharper curves, whereas higher values appear more rounded.
        /// </summary>
        /// <param name="index">The knot index to set a tension value for.</param>
        /// <param name="tension">Set the length of the tangent vectors.</param>
        public void SetAutoSmoothTension(int index, float tension)
        {
            SetAutoSmoothTension(new SplineRange(index, 1), tension);
        }

        /// <summary>
        /// Sets the tension that is used to calculate the magnitude of tangents when the <see cref="TangentMode"/> is
        /// <see cref="TangentMode.AutoSmooth"/>. Valid values are between 0 and 1.
        /// A lower value results in sharper curves, whereas higher values appear more rounded.
        /// </summary>
        /// <param name="range">The range of knot indices to set a tension value for.</param>
        /// <param name="tension">Set the length of the tangent vectors.</param>
        public void SetAutoSmoothTension(SplineRange range, float tension)
        {
            SetAutoSmoothTensionInternal(range, tension, true);
        }

        /// <summary>
        /// Sets the tension that is used to calculate the magnitude of tangents when the <see cref="TangentMode"/> is
        /// <see cref="TangentMode.AutoSmooth"/>. Valid values are between 0 and 1.
        /// A lower value results in sharper curves, whereas higher values appear more rounded.
        /// No changed callbacks will be invoked.
        /// </summary>
        /// <param name="index">The knot index to set a tension value for.</param>
        /// <param name="tension">Set the length of the tangent vectors for a knot set to <see cref="TangentMode.AutoSmooth"/>.</param>
        public void SetAutoSmoothTensionNoNotify(int index, float tension)
        {
            SetAutoSmoothTensionInternal(new SplineRange(index, 1), tension, false);
        }

        /// <summary>
        /// Set the tension that is used to calculate the magnitude of tangents when the <see cref="TangentMode"/> is
        /// <see cref="TangentMode.AutoSmooth"/>. Valid values are between 0 and 1.
        /// A lower value results in sharper curves, whereas higher values appear more rounded.
        /// No changed callbacks will be invoked.
        /// </summary>
        /// <param name="range">The range of knot indices to set a tension value for.</param>
        /// <param name="tension">Set the length of the tangent vectors for a knot set to <see cref="TangentMode.AutoSmooth"/>.</param>
        public void SetAutoSmoothTensionNoNotify(SplineRange range, float tension)
        {
            SetAutoSmoothTensionInternal(range, tension, false);
        }

        void SetAutoSmoothTensionInternal(SplineRange range, float tension, bool setDirty)
        {
            for (int i = 0, c = range.Count; i < c; ++i)
            {
                var index = range[i];
                CacheKnotOperationCurves(index);
                m_MetaData[index].Tension = tension;
                if(m_MetaData[index].Mode == TangentMode.AutoSmooth)
                    ApplyTangentModeNoNotify(index);

                if (setDirty)
                    SetDirty(SplineModification.KnotModified, index);
            }
        }

        /// <summary>
        /// The SplineType that this spline should be presented as to the user.
        /// </summary>
        /// <remarks>
        /// Internally all splines are stored as a collection of Bezier knots, and when editing converted or displayed
        /// with the handles appropriate to the editable type.
        /// </remarks>
        [Obsolete("Use GetTangentMode and SetTangentMode.")]
        public SplineType EditType
        {
            get => m_EditModeType;
            set
            {
                if (m_EditModeType == value)
                    return;
                m_EditModeType = value;
                var mode = value.GetTangentMode();
                for(int i = 0; i < Count; ++i)
                    SetTangentModeNoNotify(i, mode);
                SetDirty(SplineModification.Default);
            }
        }

        /// <summary>
        /// A collection of <see cref="BezierKnot"/>.
        /// </summary>
        public IEnumerable<BezierKnot> Knots
        {
            get => m_Knots;
            set
            {
                m_Knots = new List<BezierKnot>(value);
                m_MetaData = new List<MetaData>(m_Knots.Count);
                SetDirty(SplineModification.Default);
            }
        }

        /// <summary>
        /// Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).
        /// </summary>
        public bool Closed
        {
            get => m_Closed;
            set
            {
                if (m_Closed == value)
                    return;
                m_Closed = value;

                CheckAutoSmoothExtremityKnots();
                SetDirty(SplineModification.ClosedModified);
            }
        }

        internal void CheckAutoSmoothExtremityKnots()
        {
            if (GetTangentMode(0) == TangentMode.AutoSmooth)
                ApplyTangentModeNoNotify(0);
            if (Count > 2 && GetTangentMode(Count - 1) == TangentMode.AutoSmooth)
                ApplyTangentModeNoNotify(Count - 1);
        }

        /// <summary>
        /// Return the first index of an element matching item.
        /// </summary>
        /// <param name="item">The knot to locate.</param>
        /// <returns>The zero-based index of the knot, or -1 if not found.</returns>
        public int IndexOf(BezierKnot item) => m_Knots.IndexOf(item);

        /// <summary>
        /// Insert a <see cref="BezierKnot"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index to insert the new element.</param>
        /// <param name="knot">The <see cref="BezierKnot"/> to insert.</param>
        public void Insert(int index, BezierKnot knot) =>
            Insert(index, knot, k_DefaultTangentMode, SplineUtility.CatmullRomTension);

        /// <summary>
        /// Inserts a <see cref="BezierKnot"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index to insert the new element.</param>
        /// <param name="knot">The <see cref="BezierKnot"/> to insert.</param>
        /// <param name="mode">The <see cref="TangentMode"/> to apply to this knot. Tangent modes are enforced
        /// when a knot value is set.</param>
        public void Insert(int index, BezierKnot knot, TangentMode mode) =>
            Insert(index, knot, mode, SplineUtility.CatmullRomTension);

        /// <summary>
        /// Adds a <see cref="BezierKnot"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index to insert the new element.</param>
        /// <param name="knot">The <see cref="BezierKnot"/> to insert.</param>
        /// <param name="mode">The <see cref="TangentMode"/> to apply to this knot. Tangent modes are enforced
        /// when a knot value is set.</param>
        /// <param name="tension">The modifier value that is used to calculate the magnitude of tangents when the
        /// <see cref="TangentMode"/> is <see cref="TangentMode.AutoSmooth"/>. Valid values are between 0 and 1.
        /// A lower value results in sharper curves, whereas higher values appear more rounded.
        /// </param>
        public void Insert(int index, BezierKnot knot, TangentMode mode, float tension)
        {
            CacheKnotOperationCurves(index);
            InsertNoNotify(index, knot, mode, tension);
            SetDirty(SplineModification.KnotInserted, index);
        }

        void InsertNoNotify(int index, BezierKnot knot, TangentMode mode, float tension)
        {
            EnsureMetaDataValid();

            m_Knots.Insert(index, knot);
            m_MetaData.Insert(index, new MetaData() { Mode = mode, Tension = tension });

            var previousIndex = this.PreviousIndex(index);
            if (previousIndex != index)
                ApplyTangentModeNoNotify(previousIndex);

            ApplyTangentModeNoNotify(index);

            var nextIndex = this.NextIndex(index);
            if (nextIndex != index)
                ApplyTangentModeNoNotify(nextIndex);
        }

        /// <summary>
        /// Creates a <see cref="BezierKnot"/> at the specified <paramref name="index"/> with a <paramref name="curveT"/> normalized offset.
        /// </summary>
        /// <param name="index">The zero-based index to insert the new element at.</param>
        /// <param name="curveT">The normalized offset along the curve.</param>
        /// </param>
        internal void InsertOnCurve(int index, float curveT)
        {
            var previousIndex = SplineUtility.PreviousIndex(index, Count, Closed);
            var previous = m_Knots[previousIndex];
            var next = m_Knots[index];

            var curveToSplit = new BezierCurve(previous, m_Knots[index]);
            CurveUtility.Split(curveToSplit, curveT, out var leftCurve, out var rightCurve);

            if (GetTangentMode(previousIndex) == TangentMode.Mirrored)
                SetTangentMode(previousIndex, TangentMode.Continuous);

            if (GetTangentMode(index) == TangentMode.Mirrored)
                SetTangentMode(index, TangentMode.Continuous);

            if (SplineUtility.AreTangentsModifiable(GetTangentMode(previousIndex)))
                previous.TangentOut = math.mul(math.inverse(previous.Rotation), leftCurve.Tangent0);
            if (SplineUtility.AreTangentsModifiable(GetTangentMode(index)))
                next.TangentIn = math.mul(math.inverse(next.Rotation), rightCurve.Tangent1);

            var up = CurveUtility.EvaluateUpVector(curveToSplit, curveT, math.rotate(previous.Rotation, math.up()), math.rotate(next.Rotation, math.up()));
            var rotation = quaternion.LookRotationSafe(math.normalizesafe(rightCurve.Tangent0), up);
            var inverseRotation = math.inverse(rotation);

            SetKnotNoNotify(previousIndex, previous);
            SetKnotNoNotify(index, next);

            // Inserting the knot at the right position to compute correctly auto-smooth tangents
            var bezierKnot = new BezierKnot(leftCurve.P3, math.mul(inverseRotation, leftCurve.Tangent1), math.mul(inverseRotation, rightCurve.Tangent0), rotation);
            Insert(index, bezierKnot);
        }

        /// <summary>
        /// Removes the knot at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            EnsureMetaDataValid();
            CacheKnotOperationCurves(index);
            m_Knots.RemoveAt(index);
            m_MetaData.RemoveAt(index);
            var next = Mathf.Clamp(index, 0, Count-1);

            if (Count > 0)
            {
                ApplyTangentModeNoNotify(this.PreviousIndex(next));
                ApplyTangentModeNoNotify(next);
            }

            SetDirty(SplineModification.KnotRemoved, index);
        }

        /// <summary>
        /// Get or set the knot at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        public BezierKnot this[int index]
        {
            get => m_Knots[index];
            set => SetKnot(index, value);
        }

        /// <summary>
        /// Sets the value of a knot at index.
        /// </summary>
        /// <param name="index">The index of the <see cref="BezierKnot"/> to set.</param>
        /// <param name="value">The <see cref="BezierKnot"/> to set.</param>
        /// <param name="main">The tangent to prioritize if the tangents are modified to conform with the
        /// <see cref="TangentMode"/> set for this knot.</param>
        public void SetKnot(int index, BezierKnot value, BezierTangent main = k_DefaultMainTangent)
        {
            CacheKnotOperationCurves(index);
            SetKnotNoNotify(index, value, main);
            SetDirty(SplineModification.KnotModified, index);
        }

        /// <summary>
        /// Sets the value of a knot index without invoking any change callbacks.
        /// </summary>
        /// <param name="index">The index of the <see cref="BezierKnot"/> to set.</param>
        /// <param name="value">The <see cref="BezierKnot"/> to set.</param>
        /// <param name="main">The tangent to prioritize if the tangents are modified to conform with the
        /// <see cref="TangentMode"/> set for this knot.</param>
        public void SetKnotNoNotify(int index, BezierKnot value, BezierTangent main = k_DefaultMainTangent)
        {
            m_Knots[index] = value;
            ApplyTangentModeNoNotify(index, main);

            // setting knot position affects the tangents of neighbor auto-smooth (catmull-rom) knots
            int p = this.PreviousIndex(index), n = this.NextIndex(index);
            if(m_MetaData[p].Mode == TangentMode.AutoSmooth)
                ApplyTangentModeNoNotify(p, main);
            if(m_MetaData[n].Mode == TangentMode.AutoSmooth)
                ApplyTangentModeNoNotify(n, main);
        }

        /// <summary>
        /// Default constructor creates a spline with no knots, not closed.
        /// </summary>
        public Spline() { }

        /// <summary>
        /// Create a spline with a pre-allocated knot capacity.
        /// </summary>
        /// <param name="knotCapacity">The capacity of the knot collection.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        public Spline(int knotCapacity, bool closed = false)
        {
            m_Knots = new List<BezierKnot>(knotCapacity);
            m_Closed = closed;
        }

        /// <summary>
        /// Create a spline from a collection of <see cref="BezierKnot"/>.
        /// </summary>
        /// <param name="knots">A collection of <see cref="BezierKnot"/>.</param>
        /// <param name="closed">Whether the spline is open (has a start and end point) or closed (forms an unbroken loop).</param>
        public Spline(IEnumerable<BezierKnot> knots, bool closed = false)
        {
            m_Knots = knots.ToList();
            m_Closed = closed;
        }

        /// <summary>
        /// Create a spline from a collection of <see cref="float3"/> knot positions.
        /// The knot positions are converted to <see cref="BezierKnot"/>.
        /// When the tangent mode is set to Mirrored, Continuous, Broken, or Linear, the final tangent values are obtained from tangents initially computed using Autosmooth to ensure accuracy.
        /// </summary>
        /// <param name="knotPositions">The range of knot positions to add to the spline.</param>
        /// <param name="tangentMode">The <see cref="TangentMode"/> to apply to this range of knot positions. The default value is Autosmooth.</param>
        /// <param name="closed">Whether the spline is open or closed. Open splines have a start and end point and closed splines form an unbroken loop. </param>
        public Spline(IEnumerable<float3> knotPositions, TangentMode tangentMode = TangentMode.AutoSmooth, bool closed = false)
        {
            InsertRangeNoNotify(Count, knotPositions, tangentMode);
            m_Closed = closed;
        }

        /// <summary>
        /// Create a copy of a spline.
        /// </summary>
        /// <param name="spline">The spline to copy in that new instance.</param>
        public Spline(Spline spline)
        {
            m_Knots = spline.Knots.ToList();
            m_Closed = spline.Closed;

            //Deep copy of the 4 embedded SplineData
            foreach (var data in spline.m_IntData)
                m_IntData[data.Key] = data.Value;
            foreach (var data in spline.m_FloatData)
                m_FloatData[data.Key] = data.Value;
            foreach (var data in spline.m_Float4Data)
                m_Float4Data[data.Key] = data.Value;
            foreach (var data in spline.m_ObjectData)
                m_ObjectData[data.Key] = data.Value;
        }

        /// <summary>
        /// Get a <see cref="BezierCurve"/> from a knot index.
        /// </summary>
        /// <param name="index">The knot index that serves as the first control point for this curve.</param>
        /// <returns>
        /// A <see cref="BezierCurve"/> formed by the knot at index and the next knot.
        /// </returns>
        public BezierCurve GetCurve(int index)
        {
            int next = m_Closed ? (index + 1) % m_Knots.Count : math.min(index + 1, m_Knots.Count - 1);
            return new BezierCurve(m_Knots[index], m_Knots[next]);
        }

        /// <summary>
        /// Return the length of a curve.
        /// </summary>
        /// <param name="index">The knot index that serves as the first control point for this curve.</param>
        /// <seealso cref="Warmup"/>
        /// <seealso cref="GetLength"/>
        /// <returns>Returns the length of the <see cref="BezierCurve"/> formed by the knot at index and the next knot.</returns>
        public float GetCurveLength(int index)
        {
            EnsureMetaDataValid();
            var cumulativeCurveLengths = m_MetaData[index].DistanceToInterpolation;
            if(cumulativeCurveLengths[0].Distance < 0f)
                CurveUtility.CalculateCurveLengths(GetCurve(index), cumulativeCurveLengths);

            return cumulativeCurveLengths.Length > 0 ? cumulativeCurveLengths[cumulativeCurveLengths.Length - 1].Distance : 0f;
        }

        /// <summary>
        /// Return the sum of all curve lengths, accounting for <see cref="Closed"/> state.
        /// Note that this value is not accounting for transform hierarchy. If you require length in world space, use <see cref="SplineContainer.CalculateLength"/>.
        /// </summary>
        /// <remarks>
        /// This value is cached. It is recommended to call this once in a non-performance critical path to ensure that
        /// the cache is valid.
        /// </remarks>
        /// <seealso cref="Warmup"/>
        /// <seealso cref="GetCurveLength"/>
        /// <returns>
        /// Returns the sum length of all curves composing this spline, accounting for closed state.
        /// </returns>
        public float GetLength()
        {
            if (m_Length < 0f)
            {
                m_Length = 0f;
                for (int i = 0, c = Closed ? Count : Count - 1; i < c; ++i)
                    m_Length += GetCurveLength(i);
            }

            return m_Length;
        }

        DistanceToInterpolation[] GetCurveDistanceLut(int index)
        {
            if (m_MetaData[index].DistanceToInterpolation[0].Distance < 0f)
                CurveUtility.CalculateCurveLengths(GetCurve(index), m_MetaData[index].DistanceToInterpolation);

            return m_MetaData[index].DistanceToInterpolation;
        }

        /// <summary>
        /// Return the normalized interpolation (t) corresponding to a distance on a <see cref="BezierCurve"/>.
        /// </summary>
        /// <param name="curveIndex"> The zero-based index of the curve.</param>
        /// <param name="curveDistance">The curve-relative distance to convert to an interpolation ratio (also referred to as 't').</param>
        /// <returns>  The normalized interpolation ratio associated to distance on the designated curve.</returns>
        public float GetCurveInterpolation(int curveIndex, float curveDistance)
            => CurveUtility.GetDistanceToInterpolation(GetCurveDistanceLut(curveIndex), curveDistance);


        void WarmUpCurveUps()
        {
            EnsureMetaDataValid();
            for (int i = 0, c = Closed ? Count : Count - 1; i < c; ++i)
                this.EvaluateUpVectorsForCurve(i, m_MetaData[i].UpVectors);
        }

        /// <summary>
        /// Return the up vector for a t ratio on the curve.
        /// </summary>
        /// <param name="index">The index of the curve for which the length needs to be retrieved.</param>
        /// <param name="t">A value between 0 and 1 representing the ratio along the curve.</param>
        /// <returns>
        /// Returns the up vector at the t ratio of the curve of index 'index'.
        /// </returns>
        public float3 GetCurveUpVector(int index, float t)
        {
            EnsureMetaDataValid();
            var ups = m_MetaData[index].UpVectors;

            if (math.all(ups[0] == float3.zero))
                this.EvaluateUpVectorsForCurve(index, ups);

            var offset = 1f / (float)(ups.Length - 1);
            var curveT = 0f;
            for (int i = 0; i < ups.Length; i++)
            {
                if (t <= curveT + offset)
                    return Vector3.Lerp(ups[i], ups[i + 1], (t - curveT) / offset);

                curveT += offset;
            }

            return ups[ups.Length - 1];
        }

        /// <summary>
        /// Ensure that all caches contain valid data. Call this to avoid unexpected performance costs when accessing
        /// spline data. Caches remain valid until any part of the spline state is modified.
        /// </summary>
        public void Warmup()
        {
            var _ = GetLength();
            WarmUpCurveUps();
        }

        /// <summary>
        /// Change the size of the <see cref="BezierKnot"/> list.
        /// </summary>
        /// <param name="newSize">The new size of the knots collection.</param>
        public void Resize(int newSize)
        {
            int originalSize = Count;
            newSize = math.max(0, newSize);

            if (newSize == originalSize)
                return;

            if (newSize > originalSize)
            {
                while (m_Knots.Count < newSize)
                    Add(new BezierKnot());

            }
            else if (newSize < originalSize)
            {
                while(newSize < Count)
                    RemoveAt(Count-1);
                var last = newSize - 1;
                if(last > -1 && last < m_Knots.Count)
                    ApplyTangentModeNoNotify(last);
            }
        }

        /// <summary>
        /// Create an array of spline knots.
        /// </summary>
        /// <returns>Return a new array copy of the knots collection.</returns>
        public BezierKnot[] ToArray()
        {
            return m_Knots.ToArray();
        }

        /// <summary>
        /// Copy the values from <paramref name="copyFrom"/> to this spline.
        /// </summary>
        /// <param name="copyFrom">The spline to copy property data from.</param>
        public void Copy(Spline copyFrom)
        {
            if (copyFrom == this)
                return;

            m_Closed = copyFrom.Closed;
            m_Knots.Clear();
            m_Knots.AddRange(copyFrom.m_Knots);
            m_MetaData.Clear();
            for (int i = 0; i < copyFrom.m_MetaData.Count; ++i)
                m_MetaData.Add(new MetaData(copyFrom.m_MetaData[i]));

            SetDirty(SplineModification.Default);
        }

        /// <summary>
        /// Get an enumerator that iterates through the <see cref="BezierKnot"/> collection.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        public IEnumerator<BezierKnot> GetEnumerator() => m_Knots.GetEnumerator();

        /// <summary>
        /// Gets an enumerator that iterates through the <see cref="BezierKnot"/> collection.
        /// </summary>
        /// <returns>An IEnumerator that is used to iterate the <see cref="BezierKnot"/> collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => m_Knots.GetEnumerator();

        /// <summary>
        /// Adds a knot to the spline.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to add.</param>
        public void Add(BezierKnot item) => Add(item, k_DefaultTangentMode);

        /// <summary>
        /// Creates a <see cref="BezierKnot"/> from a <see cref="float3"/> knot position and adds it to the spline.
        /// When the tangent mode is set to Mirrored, Continuous, Broken, or Linear, the final tangent values are obtained from tangents initially computed using Autosmooth to ensure accuracy.
        /// </summary>
        /// <param name="knotPosition">The knot position to convert to a <see cref="BezierKnot"/> and add to the spline.</param>
        /// <param name="tangentMode">The <see cref="TangentMode"/> to apply to the new element. The default value is Autosmooth.</param>
        public void Add(float3 knotPosition, TangentMode tangentMode = TangentMode.AutoSmooth) =>
            Insert(Count, knotPosition, tangentMode);

        /// <summary>
        /// Creates <see cref="BezierKnot"/> from a range of <see cref="float3"/> knot positions and adds them to the spline.
        /// When the tangent mode is set to Mirrored, Continuous, Broken, or Linear, the final tangent values are obtained from tangents initially computed using Autosmooth to ensure accuracy.
        /// </summary>
        /// <param name="knotPositions">The range of knot positions to add to the spline.</param>
        /// <param name="tangentMode">The <see cref="TangentMode"/> to apply to this range of knot positions. The default value is Autosmooth.</param>
        public void AddRange(IEnumerable<float3> knotPositions, TangentMode tangentMode = TangentMode.AutoSmooth) =>
            InsertRange(Count, knotPositions, tangentMode);

        /// <summary>
        /// Creates a <see cref="BezierKnot"/> from a <see cref="float3"/> knot position and inserts it at the specified <paramref name="index"/>.
        /// When the tangent mode is set to Mirrored, Continuous, Broken, or Linear, the final tangent values are obtained from tangents initially computed using Autosmooth to ensure accuracy.
        /// </summary>
        /// <param name="index">The zero-based index to insert the new element at.</param>
        /// <param name="knotPosition">The knot position to convert to a <see cref="BezierKnot"/> and insert in the spline.</param>
        /// <param name="tangentMode">The <see cref="TangentMode"/> to apply to the new element. The default value is Autosmooth.</param>
        public void Insert(int index, float3 knotPosition, TangentMode tangentMode = TangentMode.AutoSmooth)
        {
            if (tangentMode == TangentMode.AutoSmooth)
            {
                Insert(index, new BezierKnot(knotPosition), tangentMode);
            }
            else
            {
                // Knots with a tangent mode other than Autosmooth require a two-phase insertion process.
                // The first phase involves standard insertion using the Autosmooth tangent mode in order to calculate the tangents.
                // The second phase involves converting the knots to the specified tangent mode.
                // Tangent modes should only be set after all knots have been inserted into the spline to ensure proper computation of the tangents.
                CacheKnotOperationCurves(index);
                InsertNoNotify(index, new BezierKnot(knotPosition), TangentMode.AutoSmooth, SplineUtility.DefaultTension);
                SetTangentModeNoNotify(index, tangentMode);
                SetDirty(SplineModification.KnotInserted, index); // Ensure that KnotInserted is sent, and not KnotModified.
            }
        }

        /// <summary>
        /// Creates <see cref="BezierKnot"/> from a range of <see cref="float3"/> knot positions and inserts them at the specified <paramref name="index"/>.
        /// When the tangent mode is set to Mirrored, Continuous, Broken, or Linear, the final tangent values are obtained from tangents initially computed using Autosmooth to ensure accuracy.
        /// </summary>
        /// <param name="index">The zero-based index to insert the new elements at.</param>
        /// <param name="knotPositions">The range of knot positions to insert in the spline.</param>
        /// <param name="tangentMode">The <see cref="TangentMode"/> to apply to this range of knot positions. The default value is Autosmooth.</param>
        public void InsertRange(int index, IEnumerable<float3> knotPositions, TangentMode tangentMode = TangentMode.AutoSmooth)
        {
            InsertRangeNoNotify(index, knotPositions, tangentMode, true);
            SetDirty(SplineModification.KnotInserted); // Ensure that KnotInserted is sent, and not KnotModified.
        }

        void InsertRangeNoNotify(int index, IEnumerable<float3> knotPositions, TangentMode tangentMode = TangentMode.AutoSmooth, bool cacheCurves = false)
        {
            var currentIndex = 0;

            foreach (var pos in knotPositions)
            {
                var knotIndex = index + currentIndex;

                if (cacheCurves)
                    CacheKnotOperationCurves(knotIndex);

                InsertNoNotify(knotIndex, new BezierKnot(pos), TangentMode.AutoSmooth, SplineUtility.DefaultTension);
                currentIndex++;
            }

            if (tangentMode != TangentMode.AutoSmooth)
            {
                currentIndex = 0;

                // Knots with a tangent mode other than Autosmooth require a two-phase insertion process.
                // The first phase involves standard insertion using the Autosmooth tangent mode in order to calculate the tangents.
                // The second phase involves converting the knots to the specified tangent mode.
                // Tangent modes should only be set after all knots have been inserted into the spline to ensure proper computation of the tangents.
                foreach (var pos in knotPositions)
                {
                    var knotIndex = index + currentIndex;
                    SetTangentModeNoNotify(knotIndex, tangentMode);
                    currentIndex++;
                }
            }
        }

        /// <summary>
        /// Adds a knot to the spline.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to add.</param>
        /// <param name="mode">The tangent mode for this knot.</param>
        public void Add(BezierKnot item, TangentMode mode)
        {
            Insert(Count, item, mode);
        }

        /// <summary>
        /// Adds a knot to the spline.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to add.</param>
        /// <param name="mode">The tangent mode for this knot.</param>
        /// <param name="tension">The modifier value that is used to calculate the magnitude of tangents when the
        /// <see cref="TangentMode"/> is <see cref="TangentMode.AutoSmooth"/>. Valid values are between 0 and 1.
        /// A lower value results in sharper curves, whereas higher values appear more rounded.
        /// </param>
        public void Add(BezierKnot item, TangentMode mode, float tension)
        {
            Insert(Count, item, mode, tension);
        }

        /// <summary>
        /// Adds all knots from a given spline to this spline.
        /// </summary>
        /// <param name="spline">The source spline of the knots to add.
        /// </param>
        public void Add(Spline spline)
        {
            for (int i = 0; i < spline.Count; ++i)
                Insert(Count, spline[i], spline.GetTangentMode(i), spline.GetAutoSmoothTension(i));
        }

        /// <summary>
        /// Remove all knots from the spline.
        /// </summary>
        public void Clear()
        {
            m_Knots.Clear();
            m_MetaData.Clear();
            SetDirty(SplineModification.KnotRemoved);
        }

        /// <summary>
        /// Return true if a knot is present in the spline.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to locate.</param>
        /// <returns>Returns true if the knot is found, false if it is not present.</returns>
        public bool Contains(BezierKnot item) => m_Knots.Contains(item);

        /// <summary>
        /// Copies the contents of the knot list to an array starting at an index.
        /// </summary>
        /// <param name="array">The destination array to place the copied item in.</param>
        /// <param name="arrayIndex">The zero-based index to copy.</param>
        public void CopyTo(BezierKnot[] array, int arrayIndex) => m_Knots.CopyTo(array, arrayIndex);

        /// <summary>
        /// Removes the first matching knot.
        /// </summary>
        /// <param name="item">The <see cref="BezierKnot"/> to locate and remove.</param>
        /// <returns>Returns true if a matching item was found and removed, false if no match was discovered.</returns>
        public bool Remove(BezierKnot item)
        {
            var index = m_Knots.IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove any unused embedded <see cref="SplineData{T}"/> entries.
        /// </summary>
        /// <seealso cref="GetOrCreateFloatData"/>
        /// <seealso cref="GetOrCreateFloat4Data"/>
        /// <seealso cref="GetOrCreateIntData"/>
        /// <seealso cref="GetOrCreateObjectData"/>
        internal void RemoveUnusedSplineData()
        {
            m_FloatData.RemoveEmpty();
            m_Float4Data.RemoveEmpty();
            m_IntData.RemoveEmpty();
            m_ObjectData.RemoveEmpty();
        }

        internal void CacheKnotOperationCurves(int index)
        {
            if (Count <= 1)
                return;

            m_LastKnotChangeCurveLengths.curve0 = GetCurveLength(this.PreviousIndex(index));
            if (index < Count)
                m_LastKnotChangeCurveLengths.curve1 = GetCurveLength(index);
        }
    }
}
