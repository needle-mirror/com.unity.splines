using Unity.Mathematics;

namespace UnityEngine.Splines.Interpolators
{
    /// <summary>
    /// Linearly interpolate between two values a and b by ratio t. 
    /// </summary>
    public struct LerpFloat : IInterpolator<float>
    {
        /// <summary>
        /// Linearly interpolates between a and b by t.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public float Interpolate(float a, float b, float t)
        {
            return math.lerp(a, b, t);
        }
    }
    
    /// <summary>
    /// Linearly interpolate between two values a and b by ratio t. 
    /// </summary>
    public struct LerpFloat2 : IInterpolator<float2>
    {
        /// <summary>
        /// Linearly interpolates between a and b by t.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public float2 Interpolate(float2 a, float2 b, float t)
        {
            return math.lerp(a, b, t);
        }
    }
    
    /// <summary>
    /// Linearly interpolate between two values a and b by ratio t. 
    /// </summary>
    public struct LerpFloat3 : IInterpolator<float3>
    {
        /// <summary>
        /// Linearly interpolates between a and b by t.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public float3 Interpolate(float3 a, float3 b, float t)
        {
            return math.lerp(a, b, t);
        }
    }

    /// <summary>
    /// Linearly interpolate between two values a and b by ratio t. 
    /// </summary>
    public struct LerpFloat4 : IInterpolator<float4>
    {
        /// <summary>
        /// Linearly interpolates between a and b by t.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public float4 Interpolate(float4 a, float4 b, float t)
        {
            return math.lerp(a, b, t);
        }
    }
    
    /// <summary>
    /// Spherically interpolate between two values a and b by ratio t. 
    /// </summary>
    public struct SlerpFloat2 : IInterpolator<float2>
    {
        /// <summary>
        /// Spherically interpolates between a and b by t.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The spherically interpolated result between the two values.</returns>
        public float2 Interpolate(float2 a, float2 b, float t)
        {
            // Using Vector3 API as Mathematics does not provide Slerp for float2.
            var result = Vector3.Slerp(new Vector3(a.x, a.y, 0f), new Vector3(b.x, b.y, 0f), t);
            return new float2(result.x, result.y);
        }
    }
    
    /// <summary>
    /// Spherically interpolate between two values a and b by ratio t. 
    /// </summary>
    public struct SlerpFloat3 : IInterpolator<float3>
    {
        /// <summary>
        /// Spherically interpolates between a and b by t.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The spherically interpolated result between the two values.</returns>
        public float3 Interpolate(float3 a, float3 b, float t)
        {
            // Using Vector3 API as Mathematics does not provide Slerp for float3.
            return Vector3.Slerp(a, b, t);
        }
    }
    
    /// <summary>
    /// Linearly interpolate between two values a and b by ratio t. 
    /// </summary>
    public struct LerpQuaternion : IInterpolator<quaternion>
    {
        /// <summary>
        /// Linearly interpolates between a and b by t.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public quaternion Interpolate(quaternion a, quaternion b, float t)
        {
            return math.nlerp(a, b, t);
        }
    }

    /// <summary>
    /// Linearly interpolate between two values a and b by ratio t. 
    /// </summary>
    public struct LerpColor : IInterpolator<Color>
    {
        /// <summary>
        /// Linearly interpolates between a and b by t.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public Color Interpolate(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, t);
        }
    }

    /// <summary>
    /// Interpolate between two values a and b by ratio t with smoothing at the start and end. 
    /// </summary>
    public struct SmoothStepFloat : IInterpolator<float>
    {
        /// <summary>
        /// Interpolates between a and b by ratio t with smoothing at the limits.
        /// This function interpolates between min and max in a similar way to Lerp. However, the interpolation will
        /// gradually speed up from the start and slow down toward the end. This is useful for creating natural-looking
        /// animation, fading and other transitions.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public float Interpolate(float a, float b, float t)
        {
            return math.smoothstep(a, b, t);
        }
    }

    /// <summary>
    /// Interpolate between two values a and b by ratio t with smoothing at the start and end. 
    /// </summary>
    public struct SmoothStepFloat2 : IInterpolator<float2>
    {
        /// <summary>
        /// Interpolates between a and b by ratio t with smoothing at the limits.
        /// This function interpolates between min and max in a similar way to Lerp. However, the interpolation will
        /// gradually speed up from the start and slow down toward the end. This is useful for creating natural-looking
        /// animation, fading and other transitions.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public float2 Interpolate(float2 a, float2 b, float t)
        {
            return math.smoothstep(a, b, t);
        }
    }
    
    /// <summary>
    /// Interpolate between two values a and b by ratio t with smoothing at the start and end. 
    /// </summary>
    public struct SmoothStepFloat3 : IInterpolator<float3>
    {
        /// <summary>
        /// Interpolates between a and b by ratio t with smoothing at the limits.
        /// This function interpolates between min and max in a similar way to Lerp. However, the interpolation will
        /// gradually speed up from the start and slow down toward the end. This is useful for creating natural-looking
        /// animation, fading and other transitions.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public float3 Interpolate(float3 a, float3 b, float t)
        {
            return math.smoothstep(a, b, t);
        }
    }
    
    /// <summary>
    /// Interpolate between two values a and b by ratio t with smoothing at the start and end. 
    /// </summary>
    public struct SmoothStepFloat4 : IInterpolator<float4>
    {
        /// <summary>
        /// Interpolates between a and b by ratio t with smoothing at the limits.
        /// This function interpolates between min and max in a similar way to Lerp. However, the interpolation will
        /// gradually speed up from the start and slow down toward the end. This is useful for creating natural-looking
        /// animation, fading and other transitions.
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns> The interpolated result between the two values.</returns>
        public float4 Interpolate(float4 a, float4 b, float t)
        {
            return math.smoothstep(a, b, t);
        }
    }
    
    /// <summary>
    /// Spherically interpolates between quaternions a and b by ratio t. The parameter t is clamped b the range [0, 1]. 
    /// </summary>
    public struct SlerpQuaternion : IInterpolator<quaternion>
    {
        /// <summary>
        /// Spherically interpolates between quaternions a and b by ratio t. The parameter t is clamped b the range [0, 1].
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Interpolation ratio.</param>
        /// <returns>A quaternion spherically interpolated between quaternions a and b.</returns>
        public quaternion Interpolate(quaternion a, quaternion b, float t)
        {
            return math.slerp(a, b, t);
        }
    }
}
