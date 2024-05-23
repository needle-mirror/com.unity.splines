using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Splines
{
    /// <summary>
    /// A component to animate an object along a spline.
    /// </summary>
    [AddComponentMenu("Splines/Spline Animate")]
    [ExecuteInEditMode]
    public class SplineAnimate : SplineComponent
    {
        /// <summary>
        /// Describes the different methods that can be used to animated an object along a spline.
        /// </summary>
        public enum Method
        {
            /// <summary> Spline will be traversed in the given amount of seconds. </summary>
            Time,
            /// <summary> Spline will be traversed at a given maximum speed. </summary>
            Speed
        }

        /// <summary>
        /// Describes the different ways the object's animation along the Spline can be looped.
        /// </summary>
        public enum LoopMode
        {
            /// <summary> Traverse the spline once and stop at the end. </summary>
            [InspectorName("Once")]
            Once,
            /// <summary> Traverse the spline continuously without stopping. </summary>
            [InspectorName("Loop Continuous")]
            Loop,
            /// <summary> Traverse the spline continuously without stopping. If <see cref="SplineAnimate.Easing"/> is set to <see cref="SplineAnimate.EasingMode.EaseIn"/> or
            /// <see cref="SplineAnimate.EasingMode.EaseInOut"/> then easing is only applied to the first loop of the animation. Otherwise, no easing is applied with this loop mode.
            /// </summary>
            [InspectorName("Ease In Then Continuous")]
            LoopEaseInOnce,
            /// <summary> Traverse the spline and then reverse direction at the end of the spline. The animation plays repeatedly. </summary>
            [InspectorName("Ping Pong")]
            PingPong
        }

        /// <summary>
        /// Describes the different ways the object's animation along the spline can be eased.
        /// </summary>
        public enum EasingMode
        {
            /// <summary> Apply no easing. The animation speed is linear.</summary>
            [InspectorName("None")]
            None,
            /// <summary> Apply easing to the beginning of animation. </summary>
            [InspectorName("Ease In Only")]
            EaseIn,
            /// <summary> Apply easing to the end of animation. </summary>
            [InspectorName("Ease Out Only")]
            EaseOut,
            /// <summary> Apply easing to the beginning and end of animation. </summary>
            [InspectorName("Ease In-Out")]
            EaseInOut
        }

        /// <summary>
        /// Describes the ways the object can be aligned when animating along the spline.
        /// </summary>
        public enum AlignmentMode
        {
            /// <summary> No aligment is done and object's rotation is unaffected. </summary>
            [InspectorName("None")]
            None,
            /// <summary> The object's forward and up axes align to the spline's tangent and up vectors. </summary>
            [InspectorName("Spline Element")]
            SplineElement,
            /// <summary> The object's forward and up axes align to the spline tranform's z-axis and y-axis. </summary>
            [InspectorName("Spline Object")]
            SplineObject,
            /// <summary> The object's forward and up axes align to to the world's z-axis and y-axis. </summary>
            [InspectorName("World Space")]
            World
        }

        [SerializeField, Tooltip("The target spline to follow.")]
        SplineContainer m_Target;

        [SerializeField, Tooltip("Enable to have the animation start when the GameObject first loads.")]
        bool m_PlayOnAwake = true;

        [SerializeField, Tooltip("The loop mode that the animation uses. Loop modes cause the animation to repeat after it finishes. The following loop modes are available:.\n" +
                                 "Once - Traverse the spline once and stop at the end.\n" +
                                 "Loop Continuous - Traverse the spline continuously without stopping.\n" +
                                 "Ease In Then Continuous - Traverse the spline repeatedly without stopping. If Ease In easing is enabled, apply easing to the first loop only.\n" +
                                 "Ping Pong - Traverse the spline continuously without stopping and reverse direction after an end of the spline is reached.\n")]
        LoopMode m_LoopMode = LoopMode.Loop;

        [SerializeField, Tooltip("The method used to animate the GameObject along the spline.\n" +
                                 "Time - The spline is traversed in a given amount of seconds.\n" +
                                 "Speed - The spline is traversed at a given maximum speed.")]
        Method m_Method = Method.Time;

        [SerializeField, Tooltip("The period of time that it takes for the GameObject to complete its animation along the spline.")]
        float m_Duration = 1f;

        [SerializeField, Tooltip("The speed in meters/second that the GameObject animates along the spline at.")]
        float m_MaxSpeed = 10f;

        [SerializeField, Tooltip("The easing mode used when the GameObject animates along the spline.\n" +
                                 "None - Apply no easing to the animation. The animation speed is linear.\n" +
                                 "Ease In Only - Apply easing to the beginning of animation.\n" +
                                 "Ease Out Only - Apply easing to the end of animation.\n" +
                                 "Ease In-Out - Apply easing to the beginning and end of animation.\n")]
        EasingMode m_EasingMode = EasingMode.None;

        [SerializeField, Tooltip("The coordinate space that the GameObject's up and forward axes align to.")]
        AlignmentMode m_AlignmentMode = AlignmentMode.SplineElement;

        [SerializeField, Tooltip("Which axis of the GameObject is treated as the forward axis.")]
        AlignAxis m_ObjectForwardAxis = AlignAxis.ZAxis;

        [SerializeField, Tooltip("Which axis of the GameObject is treated as the up axis.")]
        AlignAxis m_ObjectUpAxis = AlignAxis.YAxis;

        [SerializeField, Tooltip("Normalized distance [0;1] offset along the spline at which the GameObject should be placed when the animation begins.")]
        float m_StartOffset;
        [NonSerialized]
        float m_StartOffsetT;

        float m_SplineLength = -1;
        bool m_Playing;
        float m_NormalizedTime;
        float m_ElapsedTime;
#if UNITY_EDITOR
        double m_LastEditorUpdateTime;
#endif
        SplinePath<Spline> m_SplinePath;

        /// <summary>The target container of the splines to follow.</summary>
        [Obsolete("Use Container instead.", false)]
        public SplineContainer splineContainer => Container;
        /// <summary>The target container of the splines to follow.</summary>
        public SplineContainer Container
        {
            get => m_Target;
            set
            {
                m_Target = value;

                if (enabled && m_Target != null && m_Target.Splines != null)
                {
                    for (int i = 0; i < m_Target.Splines.Count; i++)
                        OnSplineChange(m_Target.Splines[i], -1, SplineModification.Default);
                }

                UpdateStartOffsetT();
            }
        }

        /// <summary>If true, transform will automatically start following the target Spline on awake.</summary>
        [Obsolete("Use PlayOnAwake instead.", false)]
        public bool playOnAwake => PlayOnAwake;
        /// <summary>If true, transform will automatically start following the target Spline on awake.</summary>
        public bool PlayOnAwake
        {
            get => m_PlayOnAwake;
            set => m_PlayOnAwake = value;
        }

        /// <summary>The way the Spline should be looped. See <see cref="LoopMode"/> for details.</summary>
        [Obsolete("Use Loop instead.", false)]
        public LoopMode loopMode => Loop;
        /// <summary>The way the Spline should be looped. See <see cref="LoopMode"/> for details.</summary>
        public LoopMode Loop
        {
            get => m_LoopMode;
            set => m_LoopMode = value;
        }

        /// <summary> The method used to traverse the Spline. See <see cref="Method"/> for details. </summary>
        [Obsolete("Use AnimationMethod instead.", false)]
        public Method method => AnimationMethod;
        /// <summary> The method used to traverse the Spline. See <see cref="Method"/> for details. </summary>
        public Method AnimationMethod
        {
            get => m_Method;
            set => m_Method = value;
        }

        /// <summary> The time (in seconds) it takes to traverse the Spline once. </summary>
        /// <remarks>
        /// When animation method is set to <see cref="Method.Time"/> this setter will set the <see cref="Duration"/> value and automatically recalculate <see cref="MaxSpeed"/>,
        /// otherwise, it will have no effect.
        /// </remarks>
        [Obsolete("Use Duration instead.", false)]
        public float duration => Duration;
        /// <summary> The time (in seconds) it takes to traverse the Spline once. </summary>
        /// <remarks>
        /// When animation method is set to <see cref="Method.Time"/> this setter will set the <see cref="Duration"/> value and automatically recalculate <see cref="MaxSpeed"/>,
        /// otherwise, it will have no effect.
        /// </remarks>
        public float Duration
        {
            get => m_Duration;
            set
            {
                if (m_Method == Method.Time)
                {
                    m_Duration = Mathf.Max(0f, value);
                    CalculateMaxSpeed();
                }
            }
        }

        /// <summary> The maxSpeed speed (in Unity units/second) that the Spline traversal will advance in. </summary>
        /// <remarks>
        /// If <see cref="EasingMode"/> is to <see cref="EasingMode.None"/> then the Spline will be traversed at MaxSpeed throughout its length.
        /// Otherwise, the traversal speed will range from 0 to MaxSpeed throughout the Spline's length depending on the easing mode set.
        /// When animation method is set to <see cref="Method.Speed"/> this setter will set the <see cref="MaxSpeed"/> value and automatically recalculate <see cref="Duration"/>,
        /// otherwise, it will have no effect.
        /// </remarks>
        [Obsolete("Use MaxSpeed instead.", false)]
        public float maxSpeed => MaxSpeed;
        /// <summary> The maxSpeed speed (in Unity units/second) that the Spline traversal will advance in. </summary>
        /// <remarks>
        /// If <see cref="EasingMode"/> is to <see cref="EasingMode.None"/> then the Spline will be traversed at MaxSpeed throughout its length.
        /// Otherwise, the traversal speed will range from 0 to MaxSpeed throughout the Spline's length depending on the easing mode set.
        /// When animation method is set to <see cref="Method.Speed"/> this setter will set the <see cref="MaxSpeed"/> value and automatically recalculate <see cref="Duration"/>,
        /// otherwise, it will have no effect.
        /// </remarks>
        public float MaxSpeed
        {
            get => m_MaxSpeed;
            set
            {
                if (m_Method == Method.Speed)
                {
                    m_MaxSpeed = Mathf.Max(0f, value);
                    CalculateDuration();
                }
            }
        }

        /// <summary> Easing mode used when animating the object along the Spline. See <see cref="EasingMode"/> for details. </summary>
        [Obsolete("Use Easing instead.", false)]
        public EasingMode easingMode => Easing;
        /// <summary> Easing mode used when animating the object along the Spline. See <see cref="EasingMode"/> for details. </summary>
        public EasingMode Easing
        {
            get => m_EasingMode;
            set => m_EasingMode = value;
        }

        /// <summary> The way the object should align when animating along the Spline. See <see cref="AlignmentMode"/> for details. </summary>
        [Obsolete("Use Alignment instead.", false)]
        public AlignmentMode alignmentMode => Alignment;
        /// <summary> The way the object should align when animating along the Spline. See <see cref="AlignmentMode"/> for details. </summary>
        public AlignmentMode Alignment
        {
            get => m_AlignmentMode;
            set => m_AlignmentMode = value;
        }

        /// <summary> Object space axis that should be considered as the object's forward vector. </summary>
        [Obsolete("Use ObjectForwardAxis instead.", false)]
        public AlignAxis objectForwardAxis => ObjectForwardAxis;
        /// <summary> Object space axis that should be considered as the object's forward vector. </summary>
        public AlignAxis ObjectForwardAxis
        {
            get => m_ObjectForwardAxis;
            set => m_ObjectUpAxis = SetObjectAlignAxis(value, ref m_ObjectForwardAxis, m_ObjectUpAxis);
        }

        /// <summary> Object space axis that should be considered as the object's up vector. </summary>
        [Obsolete("Use ObjectUpAxis instead.", false)]
        public AlignAxis objectUpAxis => ObjectUpAxis;
        /// <summary> Object space axis that should be considered as the object's up vector. </summary>
        public AlignAxis ObjectUpAxis
        {
            get => m_ObjectUpAxis;
            set => m_ObjectForwardAxis = SetObjectAlignAxis(value, ref m_ObjectUpAxis, m_ObjectForwardAxis);
        }

        /// <summary>
        /// Normalized time of the Spline's traversal. The integer part is the number of times the Spline has been traversed.
        /// The fractional part is the % (0-1) of progress in the current loop.
        /// </summary>
        [Obsolete("Use NormalizedTime instead.", false)]
        public float normalizedTime => NormalizedTime;
        /// <summary>
        /// Normalized time of the Spline's traversal. The integer part is the number of times the Spline has been traversed.
        /// The fractional part is the % (0-1) of progress in the current loop.
        /// </summary>
        public float NormalizedTime
        {
            get => m_NormalizedTime;
            set
            {
                m_NormalizedTime = value;
                if (m_LoopMode == LoopMode.PingPong)
                {
                    var currentDirection = (int)(m_ElapsedTime / m_Duration);
                    m_ElapsedTime = m_Duration * m_NormalizedTime + ((currentDirection % 2 == 1) ? m_Duration : 0f);
                }
                else
                    m_ElapsedTime = m_Duration * m_NormalizedTime;
                
                UpdateTransform();
            }
        }

        /// <summary> Total time (in seconds) since the start of Spline's traversal. </summary>
        [Obsolete("Use ElapsedTime instead.", false)]
        public float elapsedTime => ElapsedTime;
        /// <summary> Total time (in seconds) since the start of Spline's traversal. </summary>
        public float ElapsedTime
        {
            get => m_ElapsedTime;
            set
            {
                m_ElapsedTime = value;
                CalculateNormalizedTime(0f);
                UpdateTransform();
            }
        }

        /// <summary> Normalized distance [0;1] offset along the spline at which the object should be placed when the animation begins. </summary>
        public float StartOffset
        {
            get => m_StartOffset;
            set
            {
                if (m_SplineLength < 0f)
                    RebuildSplinePath();

                m_StartOffset = Mathf.Clamp01(value);
                UpdateStartOffsetT();
            }
        }

        internal float StartOffsetT => m_StartOffsetT;

        /// <summary> Returns true if object is currently animating along the Spline. </summary>
        [Obsolete("Use IsPlaying instead.", false)]
        public bool isPlaying => IsPlaying;
        /// <summary> Returns true if object is currently animating along the Spline. </summary>
        public bool IsPlaying => m_Playing;

        /// <summary> Invoked each time object's animation along the Spline is updated.</summary>
        [Obsolete("Use Updated instead.", false)]
        public event Action<Vector3, Quaternion> onUpdated;
        /// <summary> Invoked each time object's animation along the Spline is updated.</summary>
        public event Action<Vector3, Quaternion> Updated;

        private bool m_EndReached = false;
        /// <summary> Invoked every time the object's animation reaches the end of the Spline.
        /// In case the animation loops, this event is called at the end of each loop.</summary>
        public event Action Completed; 

        void Awake()
        {
#if UNITY_EDITOR      
            if(EditorApplication.isPlaying)
#endif
            Restart(m_PlayOnAwake);
#if UNITY_EDITOR
            else // Place the animated object back at the animation start position.
                Restart(false);
#endif
        }

        void OnEnable()
        {
            RecalculateAnimationParameters();
            Spline.Changed += OnSplineChange;
        }

        void OnDisable()
        {
            Spline.Changed -= OnSplineChange;
        }

        void OnValidate()
        {
            m_Duration = Mathf.Max(0f, m_Duration);
            m_MaxSpeed = Mathf.Max(0f, m_MaxSpeed);
            RecalculateAnimationParameters();
        }
        
        internal void RecalculateAnimationParameters()
        {
            RebuildSplinePath();

            switch (m_Method)
            {
                case Method.Time:
                    CalculateMaxSpeed();
                    break;

                case Method.Speed:
                    CalculateDuration();
                    break;

                default:
                    Debug.Log($"{m_Method} animation method is not supported!", this);
                    break;
            }
        }
     
        internal static readonly string k_EmptyContainerError = "SplineAnimate does not have a valid SplineContainer set.";
        bool IsNullOrEmptyContainer()
        {
            if (m_Target == null || m_Target.Splines.Count == 0)
            {
                if(Application.isPlaying)
                    Debug.LogError(k_EmptyContainerError, this);
                return true;
            }
            return false;
        }

        /// <summary> Begin animating object along the Spline. </summary>
        public void Play()
        {
            if (IsNullOrEmptyContainer())
                return;

            m_Playing = true;
#if UNITY_EDITOR
            m_LastEditorUpdateTime = EditorApplication.timeSinceStartup;
#endif
        }

        /// <summary> Pause object's animation along the Spline. </summary>
        public void Pause()
        {
            m_Playing = false;
        }

        /// <summary> Stop the animation and place the object at the beginning of the Spline. </summary>
        /// <param name="autoplay"> If true, the animation along the Spline will start over again. </param>
        public void Restart(bool autoplay)
        {
            // [SPLB-269]: Early exit if the container is null to remove log error when initializing the spline animate object from code
            if (Container == null)
                return;
            
            if(IsNullOrEmptyContainer())
                return;

            m_Playing = false;
            m_ElapsedTime = 0f;
            NormalizedTime = 0f;

            switch (m_Method)
            {
                case Method.Time:
                    CalculateMaxSpeed();
                    break;

                case Method.Speed:
                    CalculateDuration();
                    break;

                default:
                    Debug.Log($"{m_Method} animation method is not supported!", this);
                    break;
            }
            UpdateTransform();
            UpdateStartOffsetT();

            if (autoplay)
                Play();
        }

        /// <summary>
        /// Evaluates the animation along the Spline based on deltaTime.
        /// </summary>
        public void Update()
        {
            if (!m_Playing || (m_LoopMode == LoopMode.Once && m_NormalizedTime >= 1f))
                return;

            var dt = Time.deltaTime;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                dt = (float)(EditorApplication.timeSinceStartup - m_LastEditorUpdateTime);
                m_LastEditorUpdateTime = EditorApplication.timeSinceStartup;
            }
#endif
            CalculateNormalizedTime(dt);
            UpdateTransform();
        }

        void CalculateNormalizedTime(float deltaTime)
        {
            var previousElapsedTime = m_ElapsedTime;
            m_ElapsedTime += deltaTime;
            var currentDuration = m_Duration;

            var t = 0f;
            switch (m_LoopMode)
            {
                case LoopMode.Once:
                    t = Mathf.Min(m_ElapsedTime, currentDuration);
                    break;

                case LoopMode.Loop:
                    t = m_ElapsedTime % currentDuration;
                    UpdateEndReached(previousElapsedTime, currentDuration);
                    break;

                case LoopMode.LoopEaseInOnce:
                    /* If the first loop had an ease in, then our velocity is double that of linear traversal.
                     Therefore time to traverse subsequent loops should be half of the first loop. */
                    if ((m_EasingMode == EasingMode.EaseIn || m_EasingMode == EasingMode.EaseInOut) &&
                        m_ElapsedTime >= currentDuration)
                        currentDuration *= 0.5f;
                    t = m_ElapsedTime % currentDuration;
                    UpdateEndReached(previousElapsedTime, currentDuration);
                    break;

                case LoopMode.PingPong:
                    t = Mathf.PingPong(m_ElapsedTime, currentDuration);
                    UpdateEndReached(previousElapsedTime, currentDuration);
                    break;

                default:
                    Debug.Log($"{m_LoopMode} animation loop mode is not supported!", this);
                    break;
            }
            t /= currentDuration;

            if (m_LoopMode == LoopMode.LoopEaseInOnce)
            {
                // Apply ease in for the first loop and continue linearly for remaining loops
                if ((m_EasingMode == EasingMode.EaseIn || m_EasingMode == EasingMode.EaseInOut) &&
                    m_ElapsedTime < currentDuration)
                    t = EaseInQuadratic(t);
            }
            else
            {
                switch (m_EasingMode)
                {
                    case EasingMode.EaseIn:
                        t = EaseInQuadratic(t);
                        break;

                    case EasingMode.EaseOut:
                        t = EaseOutQuadratic(t);
                        break;

                    case EasingMode.EaseInOut:
                        t = EaseInOutQuadratic(t);
                        break;
                }
            }

            // forcing reset to 0 if the m_NormalizedTime reach the end of the spline previously (1).
            m_NormalizedTime = t == 0 ? 0f : Mathf.Floor(m_NormalizedTime) + t;
            if (m_NormalizedTime >= 1f && m_LoopMode == LoopMode.Once)
            {
                m_EndReached = true;
                m_Playing = false;
            }
        }
        
        void UpdateEndReached(float previousTime, float currentDuration)
        {
            m_EndReached = Mathf.FloorToInt(previousTime/currentDuration) < Mathf.FloorToInt(m_ElapsedTime/currentDuration);
        }

        void UpdateStartOffsetT()
        {
            if (m_SplinePath != null)
                m_StartOffsetT = m_SplinePath.ConvertIndexUnit(m_StartOffset * m_SplineLength, PathIndexUnit.Distance, PathIndexUnit.Normalized);
        }

        void UpdateTransform()
        {
            if (m_Target == null)
                return;

            EvaluatePositionAndRotation(out var position, out var rotation);

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
#endif
                transform.position = position;
                if (m_AlignmentMode != AlignmentMode.None)
                    transform.rotation = rotation;

#if UNITY_EDITOR
            }
#endif
            onUpdated?.Invoke(position, rotation);
            Updated?.Invoke(position, rotation);

            if (m_EndReached)
            {
                m_EndReached = false;
                Completed?.Invoke();
            }
        }

        void EvaluatePositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            var t = GetLoopInterpolation(true);
            position = m_Target.EvaluatePosition(m_SplinePath, t);
            rotation = Quaternion.identity;

            // Correct forward and up vectors based on axis remapping parameters
            var remappedForward = GetAxis(m_ObjectForwardAxis);
            var remappedUp = GetAxis(m_ObjectUpAxis);
            var axisRemapRotation = Quaternion.Inverse(Quaternion.LookRotation(remappedForward, remappedUp));

            if (m_AlignmentMode != AlignmentMode.None)
            {
                var forward = Vector3.forward;
                var up = Vector3.up;

                switch (m_AlignmentMode)
                {
                    case AlignmentMode.SplineElement:
                        forward = m_Target.EvaluateTangent(m_SplinePath, t);
                        if (Vector3.Magnitude(forward) <= Mathf.Epsilon)
                        {
                            if (t < 1f)
                                forward = m_Target.EvaluateTangent(m_SplinePath, Mathf.Min(1f, t + 0.01f));
                            else
                                forward = m_Target.EvaluateTangent(m_SplinePath, t - 0.01f);
                        }
                        forward.Normalize();
                        up = m_Target.EvaluateUpVector(m_SplinePath, t);
                        break;

                    case AlignmentMode.SplineObject:
                        var objectRotation = m_Target.transform.rotation;
                        forward = objectRotation * forward;
                        up = objectRotation * up;
                        break;

                    default:
                        Debug.Log($"{m_AlignmentMode} animation alignment mode is not supported!", this);
                        break;
                }

                rotation = Quaternion.LookRotation(forward, up) * axisRemapRotation;
            }
            else
                rotation = transform.rotation;
        }

        void CalculateDuration()
        {
            if (m_SplineLength < 0f)
                RebuildSplinePath();

            if (m_SplineLength >= 0f)
            {
                switch (m_EasingMode)
                {
                    case EasingMode.None:
                        m_Duration = m_SplineLength / m_MaxSpeed;
                        break;

                    case EasingMode.EaseIn:
                    case EasingMode.EaseOut:
                    case EasingMode.EaseInOut:
                        m_Duration = (2f * m_SplineLength) / m_MaxSpeed;
                        break;

                    default:
                        Debug.Log($"{m_EasingMode} animation easing mode is not supported!", this);
                        break;
                }
            }
        }

        void CalculateMaxSpeed()
        {
            if (m_SplineLength < 0f)
                RebuildSplinePath();

            if (m_SplineLength >= 0f)
            {
                switch (m_EasingMode)
                {
                    case EasingMode.None:
                        m_MaxSpeed = m_SplineLength / m_Duration;
                        break;

                    case EasingMode.EaseIn:
                    case EasingMode.EaseOut:
                    case EasingMode.EaseInOut:
                        m_MaxSpeed = (2f * m_SplineLength) / m_Duration;
                        break;

                    default:
                        Debug.Log($"{m_EasingMode} animation easing mode is not supported!", this);
                        break;
                }
            }
        }

        void RebuildSplinePath()
        {
            if (m_Target != null)
            {
                m_SplinePath = new SplinePath<Spline>(m_Target.Splines);
                m_SplineLength = m_SplinePath != null ? m_SplinePath.GetLength() : 0f;
            }
        }

        AlignAxis SetObjectAlignAxis(AlignAxis newValue, ref AlignAxis targetAxis, AlignAxis otherAxis)
        {
            // Swap axes if the new value matches that of the other axis
            if (newValue == otherAxis)
            {
                otherAxis = targetAxis;
                targetAxis = newValue;
            }
            // Do not allow configuring object's forward and up axes as opposite
            else if ((int) newValue % 3 != (int) otherAxis % 3)
                targetAxis = newValue;

            return otherAxis;
        }

        void OnSplineChange(Spline spline, int knotIndex, SplineModification modificationType)
        {
            RecalculateAnimationParameters();
        }

        internal float GetLoopInterpolation(bool offset)
        {
            var t = 0f;
            var normalizedTimeWithOffset = NormalizedTime + (offset ? m_StartOffsetT : 0f);
            if (Mathf.Floor(normalizedTimeWithOffset) == normalizedTimeWithOffset)
                t = Mathf.Clamp01(normalizedTimeWithOffset);
            else
                t = normalizedTimeWithOffset % 1f;
            
            return t;
        }

        float EaseInQuadratic(float t)
        {
            return t * t;
        }

        float EaseOutQuadratic(float t)
        {
            return t * (2f - t);
        }

        float EaseInOutQuadratic(float t)
        {
            var eased = 2f * t * t;
            if (t > 0.5f)
                eased = 4f * t - eased - 1f;
            return eased;
        }
    }
}
