using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Splines
{
    static class MathUtility
    {
        // Transforms a direction by this matrix - float4x4 equivalent of Matrix4x4.MultiplyVector.
        public static float3 MultiplyVector(float4x4 matrix, float3 vector)
        {
            float3 res;
            res.x = matrix.c0.x * vector.x + matrix.c1.x * vector.y + matrix.c2.x * vector.z;
            res.y = matrix.c0.y * vector.x + matrix.c1.y * vector.y + matrix.c2.y * vector.z;
            res.z = matrix.c0.z * vector.x + matrix.c1.z * vector.y + matrix.c2.z * vector.z;
            return res;
        }
    }
}
