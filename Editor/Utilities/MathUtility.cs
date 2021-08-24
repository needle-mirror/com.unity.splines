using UnityEngine;
using Unity.Mathematics;

namespace UnityEditor.Splines
{
    static class MathUtility
    {
        public static float GetRollAroundAxis(quaternion q, float3 axis)
        {
            axis = math.normalize(axis);
            var orthoAxis = math.cross(axis, axis.Equals(math.up()) ? math.right() : math.up());
            var rotatedOrtho = math.rotate(q, orthoAxis);
            var flattened = math.normalize(Vector3.ProjectOnPlane(rotatedOrtho, axis));
            var angle = Mathf.Deg2Rad * Vector3.SignedAngle(orthoAxis, flattened, axis);

            return angle;
        }
    }
}