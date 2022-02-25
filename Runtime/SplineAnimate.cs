using System;
using UnityEngine.Splines;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Splines
{
    /// <summary>
    /// A component for animating an object along a Spline.
    /// </summary>
    [AddComponentMenu("Splines/Spline Animate")]
    public class SplineAnimate : SplineComponent
    {
        /// <summary>
        /// Describes the different methods that may be used to animated the object along the Spline.
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
            /// <summary> Traverse the Spline once and stop at the end. </summary>
            [InspectorName("Once")]
            Once,
            /// <summary> Traverse the Spline continously without stopping. </summary>
            [InspectorName("Loop Continous")]
            Loop,
            /// <summary> Traverse the Spline continously without stopping. If <see cref="SplineAnimate.easingMode"/> is set to <see cref="SplineAnimate.EasingMode.EaseIn"/> or
            /// <see cref="SplineAnimate.EasingMode.EaseInOut"/> then only ease in is applied and only during the first loop. Otherwise, no easing is applied when using this loop mode.
            /// </summary>
            [InspectorName("Ease In Then Continous")]
            LoopEaseInOnce,
            /// <summary> Traverse the Spline continously without stopping and reverse movement direction uppon reaching end or beginning of the Spline. </summary>
            [InspectorName("Ping Pong")]
            PingPong
        }

        /// <summary>
        /// Describes the different ways the object's animation along the Spline can be eased.
        /// </summary>
        public enum EasingMode
        {
            /// <summary> Apply no easing - animation will be linear. </summary>
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
        /// Describes the ways the object can be aligned when animating along the Spline.
        /// </summary>
        public enum AlignmentMode
        {
            /// <summary> No aligment is done and object's rotation is unaffected. </summary>
            [InspectorName("None")]
            None,
            /// <summary> Object's forward and up axes align to the Spline's tangent and up vectors. </summary>
            [InspectorName("Spline Element")]
            SplineElement,
            /// <summary> Object's forward and up axes align to the Spline tranform's Z and Y axes. </summary>
            [InspectorName("Spline Object")]
            SplineObject,
            /// <summary> Object's forward and up axes align to to the World's Z and Y axes. </summary>
            [InspectorName("World")]
            World
        }
        
        [SerializeField, Tooltip("The target Spline to follow.")]
        SplineContainer m_Target; 
        
        [SerializeField, Tooltip("If true, transform will automatically start following the target Spline on awake.")]
        bool m_PlayOnAwake = true;

        [SerializeField, Tooltip("The loop mode used when animating object along the Spline.\n" +
                                 "Once - Traverse the Spline once and stop at the end.\n" + 
                                 "Loop Continous - Traverse the Spline continously without stopping.\n" +
                                 "Ease In Then Continous - Traverse the Spline repeatedly without stopping. If ease in easing is enabled, apply it only for the first loop.\n" +
                                 "Ping Pong - Traverse the Spline continously without stopping and reverse movement direction upon reaching end or beginning of the Spline.\n")] 
        LoopMode m_LoopMode = LoopMode.Loop;

        [SerializeField, Tooltip("The method used to animate object along the Spline.\n" +
                                 "Time - spline will be traversed in given amount of seconds.\n" +
                                 "Speed - spline will be traversed at a given maximum speed.")]
        Method m_Method = Method.Time;

        [SerializeField, Tooltip("Amount of time (in seconds) that the spline will be traversed in.")]
        float m_Duration = 1f;

        [SerializeField, Tooltip("Speed (in meters/second) at which the spline will be traversed.")] 
        float m_MaxSpeed = 10f;
        
        [SerializeField, Tooltip("Easing mode used when the object along the Spline.\n" +
                                 "None - Apply no easing. Animation will be linear.\n" +
                                 "Ease In Only - Apply easing to the beginning of animation.\n" +
                                 "Ease Out Only - Apply easing to the end of animation.\n" +
                                 "Ease In-Out - Apply easing to the beginning and end of animation.\n")] 
        EasingMode m_EasingMode = EasingMode.None;
        
        [SerializeField, Tooltip("The coordinate space to which the object's up and forward axes should align to.")]
        AlignmentMode m_AlignmentMode = AlignmentMode.SplineElement;

        [SerializeField, Tooltip("Which axis of the object should be treated as the forward axis.")]
        AlignAxis m_ObjectForwardAxis = AlignAxis.ZAxis;

        [SerializeField, Tooltip("Which axis of the object should be treated as the up axis.")]
        AlignAxis m_ObjectUpAxis = AlignAxis.YAxis;

        Spline m_Spline;
        float m_SplineLength = -1;
        bool m_Playing;
        float m_NormalizedTime;
        float m_ElapsedTime;
#if UNITY_EDITOR
        double m_LastEditorUpdateTime;
#endif

        /// <summary>The target Spline to follow.</summary>
        public SplineContainer splineContainer
        {
            get => m_Target;
            set
            {
                if (enabled && m_Target != null && m_Target.Spline != null)
                    m_Target.Spline.changed -= OnSplineChange;

                m_Target = value;

                if (enabled && m_Target != null && m_Target.Spline != null)
                {
                    m_Target.Spline.changed += OnSplineChange;
                    CalculateSplineLength();
                    OnSplineChange();
                }
            }
        }

        /// <summary>If true, transform will automatically start following the target Spline on awake.</summary>
        public bool playOnAwake
        {
            get => m_PlayOnAwake;
            set => m_PlayOnAwake = value;
        }

        /// <summary>The way the Spline should be looped. See <see cref="LoopMode"/> for details.</summary>
        public LoopMode loopMode
        {
            get => m_LoopMode;
            set => m_LoopMode = value;
        }
        
        /// <summary> The method used to traverse the Spline. See <see cref="Method"/> for details. </summary>
        public Method method
        {
            get => m_Method;
            set => m_Method = value;
        }

        /// <summary> The time (in seconds) it takes to traverse the Spline once. </summary>
        /// <remarks>
        /// When animation method is set to <see cref="Method.Time"/> this setter will set the <see cref="duration"/> value and automatically recalculate <see cref="maxSpeed"/>,
        /// otherwise, it will have no effect.
        /// </remarks>
        public float duration
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
        /// When animation method is set to <see cref="Method.Speed"/> this setter will set the <see cref="maxSpeed"/> value and automatically recalculate <see cref="duration"/>,
        /// otherwise, it will have no effect.
        /// </remarks>
        public float maxSpeed
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
        public EasingMode easingMode
        {
            get => m_EasingMode;
            set => m_EasingMode = value;
        }

        /// <summary> The way the object should align when animating along the Spline. See <see cref="AlignmentMode"/> for details. </summary>
        public AlignmentMode alignmentMode
        {
            get => m_AlignmentMode;
            set => m_AlignmentMode = value;
        }

        /// <summary> Object space axis that should be considered as the object's forward vector. </summary>
        public AlignAxis objectForwardAxis
        {
            get => m_ObjectForwardAxis;
            set => m_ObjectUpAxis = SetObjectAlignAxis(value, ref m_ObjectForwardAxis, m_ObjectUpAxis);
        }

        /// <summary> Object space axis that should be considered as the object's up vector. </summary>
        public AlignAxis objectUpAxis
        {
            get => m_ObjectUpAxis;
            set => m_ObjectForwardAxis = SetObjectAlignAxis(value, ref m_ObjectUpAxis, m_ObjectForwardAxis);
        }

        /// <summary>
        /// Normalized time of the Spline's traversal. The integer part is the number of times the Spline has been traversed.
        /// The fractional part is the % (0-1) of progress in the current loop.
        /// </summary>
        public float normalizedTime
        {
            get => m_NormalizedTime;
            set
            {
                m_NormalizedTime = value;
                m_ElapsedTime = m_Duration * m_NormalizedTime;
                UpdateTransform();
            }
        }
        
        /// <summary> Total time (in seconds) since the start of Spline's traversal. </summary>
        public float elapsedTime
        {
            get => m_ElapsedTime;
            set
            {
                m_ElapsedTime = value;
                CalculateNormalizedTime(0f);
                UpdateTransform();
            }
        }

        /// <summary> Returns true if object is currently animating along the Spline. </summary>
        public bool isPlaying => m_Playing;

        /// <summary> Invoked each time object's animation along the Spline is updated.</summary>
        public event Action<Vector3, Quaternion> onUpdated;

        void Awake()
        {
            CalculateSplineLength();
            Restart(m_PlayOnAwake);
        }

        void OnEnable()
        {
            if (m_Target != null && m_Target.Spline != null)
                m_Target.Spline.changed += OnSplineChange;
        }

        void OnDisable()
        {
            if (m_Target != null && m_Target.Spline != null)
                m_Target.Spline.changed -= OnSplineChange;
        }
        
        void OnValidate()
        {
            switch (m_Method)
            {
                case Method.Time:
                    m_Duration = Mathf.Max(0f, m_Duration);
                    CalculateMaxSpeed();
                    break;
                
                case Method.Speed:
                    m_MaxSpeed = Mathf.Max(0f, m_MaxSpeed);
                    CalculateDuration();
                    break;
                
                default:
                    Debug.Log($"{m_Method} animation method is not supported!");
                    break;
            }
        }

        bool CheckForNullContainerOrSpline()
        {
            if (m_Target == null || m_Target.Spline == null)
            {
                Debug.LogError("Spline Follow does not have a valid SplineContainer set.");
                return true;
            }
            
            return false;
        }
        
        
        /// <summary> Begin animating object along the Spline. </summary>
        public void Play()
        {
            if (CheckForNullContainerOrSpline())
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
            if (CheckForNullContainerOrSpline())
                return;
            
            m_Playing = false;
            m_ElapsedTime = 0f;
            normalizedTime = 0f;

            switch (m_Method)
            {
                case Method.Time:
                    CalculateMaxSpeed();
                    break;
                    
                case Method.Speed:
                    CalculateDuration();
                    break;

                default:
                    Debug.Log($"{m_Method} animation method is not supported!");
                    break;
            }
            UpdateTransform();

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
                    break;

                case LoopMode.LoopEaseInOnce:
                    /* If the first loop had an ease in, then our velocity is double that of linear traversal.
                     Therefore time to traverse subsequent loops should be half of the first loop. */
                    if ((m_EasingMode == EasingMode.EaseIn || m_EasingMode == EasingMode.EaseInOut) && 
                        m_ElapsedTime >= currentDuration)
                        currentDuration *= 0.5f;
                    t = m_ElapsedTime % currentDuration;
                    break;
                
                case LoopMode.PingPong:
                    t = Mathf.PingPong(m_ElapsedTime, currentDuration);
                    break;

                default:
                    Debug.Log($"{m_LoopMode} animation loop mode is not supported!");
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

            m_NormalizedTime = Mathf.Floor(m_NormalizedTime) + t;
            if (m_NormalizedTime >= 1f && m_LoopMode == LoopMode.Once)
                m_Playing = false;
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
        }
        
        void EvaluatePositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            var t = GetLoopInterpolation();

            position = m_Target.EvaluatePosition(t);
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
                        forward = Vector3.Normalize(m_Target.EvaluateTangent(t));
                        up = m_Target.EvaluateUpVector(t);
                        break;
                    
                    case AlignmentMode.SplineObject:
                        var objectRotation = m_Target.transform.rotation;
                        forward = objectRotation * forward;
                        up = objectRotation * up;
                        break;
                    
                    default:
                        Debug.Log($"{m_AlignmentMode} animation aligment mode is not supported!");
                        break;
                }

                rotation = Quaternion.LookRotation(forward, up) * axisRemapRotation;
            }
            else
                rotation = axisRemapRotation;
        }
        
        void CalculateDuration()
        {
            if (m_SplineLength < 0f)
                CalculateSplineLength();

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
                        Debug.Log($"{m_EasingMode} animation easing mode is not supported!");
                        break;
                }
            }
        }

        void CalculateMaxSpeed()
        {
            if (m_SplineLength <= 0f)
                CalculateSplineLength();

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
                        Debug.Log($"{m_EasingMode} animation easing mode is not supported!");
                        break;
                }
            }
        }
        
        void CalculateSplineLength()
        {
            if (m_Target != null)
                m_SplineLength = m_Target.CalculateLength();
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

        void OnSplineChange()
        {
            CalculateSplineLength();
            switch (m_Method)
            {
                case Method.Time:
                    CalculateMaxSpeed();
                    break;

                case Method.Speed:
                    CalculateDuration();
                    break;

                default:
                    Debug.Log($"{m_Method} animation method is not supported!");
                    break;
            }
        }

        internal float GetLoopInterpolation()
        {
            var t = 0f;
            if (Mathf.Floor(normalizedTime) == normalizedTime)
                t = Mathf.Clamp01(normalizedTime);
            else
                t = normalizedTime % 1f;

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