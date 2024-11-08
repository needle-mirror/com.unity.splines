using Unity.Mathematics;

namespace UnityEngine.Splines
{
    /// <summary>
    /// Base class for SplineInstantiate and SplineExtrude, contains common elements to both of these Components
    /// </summary>
    public abstract class SplineComponent : MonoBehaviour
    {
        /// <summary>
        /// Describes the different types of object alignment axes.
        /// </summary>
        public enum AlignAxis
        {
            /// <summary> Object space X axis. </summary>
            [InspectorName("Object X+")]
            XAxis,
            /// <summary> Object space Y axis. </summary>
            [InspectorName("Object Y+")]
            YAxis,
            /// <summary> Object space Z axis. </summary>
            [InspectorName("Object Z+")]
            ZAxis,
            /// <summary> Object space negative X axis. </summary>
            [InspectorName("Object X-")]
            NegativeXAxis,
            /// <summary> Object space negative Y axis. </summary>
            [InspectorName("Object Y-")]
            NegativeYAxis,
            /// <summary> Object space negative Z axis. </summary>
            [InspectorName("Object Z-")]
            NegativeZAxis
        }
                
        readonly float3[] m_AlignAxisToVector = new float3[] {math.right(), math.up(), math.forward(), math.left(), math.down(), math.back()};

        /// <summary>
        /// Transform a AlignAxis to the associated float3 direction. 
        /// </summary>
        /// <param name="axis">The AlignAxis to transform</param>
        /// <returns>Returns the corresponding <see cref="float3"/> direction for the specified <see cref="AlignAxis"/>.</returns>
        protected float3 GetAxis(AlignAxis axis)
        {
            return m_AlignAxisToVector[(int) axis];
        }
    }
}
