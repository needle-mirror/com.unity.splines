using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// InterpolatorUtility provides easy access to all the different IInterpolator implementations.
    /// </summary>
    public static class InterpolatorUtility
    {
        static readonly IInterpolator<float> s_LerpFloat = new Interpolators.LerpFloat();
        static readonly IInterpolator<float2> s_LerpFloat2 = new Interpolators.LerpFloat2();
        static readonly IInterpolator<float3> s_LerpFloat3 = new Interpolators.LerpFloat3();
        static readonly IInterpolator<float4> s_LerpFloat4 = new Interpolators.LerpFloat4();

        static readonly IInterpolator<float2> s_SlerpFloat2 = new Interpolators.SlerpFloat2();
        static readonly IInterpolator<float3> s_SlerpFloat3 = new Interpolators.SlerpFloat3();

        static readonly IInterpolator<quaternion> s_LerpQuaternion = new Interpolators.LerpQuaternion();

        static readonly IInterpolator<Color> s_LerpColor = new Interpolators.LerpColor();

        static readonly IInterpolator<float> s_SmoothStepFloat = new Interpolators.SmoothStepFloat();
        static readonly IInterpolator<float2> s_SmoothStepFloat2 = new Interpolators.SmoothStepFloat2();
        static readonly IInterpolator<float3> s_SmoothStepFloat3 = new Interpolators.SmoothStepFloat3();
        static readonly IInterpolator<float4> s_SmoothStepFloat4 = new Interpolators.SmoothStepFloat4();

        static readonly IInterpolator<quaternion> s_SlerpQuaternion = new Interpolators.SlerpQuaternion();

        /// <summary>
        /// Linearly interpolate between two values a and b by ratio t.
        /// </summary>
        public static IInterpolator<float> LerpFloat => s_LerpFloat;

        /// <summary>
        /// Linearly interpolate between two values a and b by ratio t.
        /// </summary>
        public static IInterpolator<float2> LerpFloat2 => s_LerpFloat2;

        /// <summary>
        /// Linearly interpolate between two values a and b by ratio t.
        /// </summary>
        public static IInterpolator<float3> LerpFloat3 => s_LerpFloat3;

        /// <summary>
        /// Linearly interpolate between two values a and b by ratio t.
        /// </summary>
        public static IInterpolator<float4> LerpFloat4 => s_LerpFloat4;

        /// <summary>
        /// Spherically interpolate between two values a and b by ratio t.
        /// </summary>
        public static IInterpolator<float2> SlerpFloat2 => s_SlerpFloat2;

        /// <summary>
        /// Spherically interpolate between two values a and b by ratio t.
        /// </summary>
        public static IInterpolator<float3> SlerpFloat3 => s_SlerpFloat3;

        /// <summary>
        /// Linearly interpolate between two values a and b by ratio t.
        /// </summary>
        public static IInterpolator<quaternion> LerpQuaternion => s_LerpQuaternion;

        /// <summary>
        /// Linearly interpolate between two values a and b by ratio t.
        /// </summary>
        public static IInterpolator<Color> LerpColor => s_LerpColor;

        /// <summary>
        /// Interpolate between two values a and b by ratio t with smoothing at the start and end.
        /// </summary>
        public static IInterpolator<float> SmoothStepFloat => s_SmoothStepFloat;

        /// <summary>
        /// Interpolate between two values a and b by ratio t with smoothing at the start and end.
        /// </summary>
        public static IInterpolator<float2> SmoothStepFloat2 => s_SmoothStepFloat2;

        /// <summary>
        /// Interpolate between two values a and b by ratio t with smoothing at the start and end.
        /// </summary>
        public static IInterpolator<float3> SmoothStepFloat3 => s_SmoothStepFloat3;

        /// <summary>
        /// Interpolate between two values a and b by ratio t with smoothing at the start and end.
        /// </summary>
        public static IInterpolator<float4> SmoothStepFloat4 => s_SmoothStepFloat4;

        /// <summary>
        /// Spherically interpolates between quaternions a and b by ratio t. The parameter t is clamped b the range [0, 1].
        /// </summary>
        public static IInterpolator<quaternion> SlerpQuaternion => s_SlerpQuaternion;
    }
}
