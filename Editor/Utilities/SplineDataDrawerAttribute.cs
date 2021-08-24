using UnityEngine;

namespace UnityEditor.Splines
{
    /// <summary>
    /// SplineDataDrawerAttribute can be use to add custom handles to SplineData<T>.
    /// The custom drawer class must inherit from SplineDataDrawer and override one of the Draw static method.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class SplineDataDrawerAttribute : System.Attribute
    {
        public System.Type drawerType;

        /// <summary>
        /// Attribute constructor
        /// </summary>
        /// <param name="type">custom drawer type, must inherit from SplineDataDrawer</param>
        public SplineDataDrawerAttribute(System.Type type)
        {
            if((type.BaseType != null) 
                && type.BaseType.IsGenericType 
                && type.BaseType.GetGenericTypeDefinition() == typeof(SplineDataDrawer<>))
                drawerType = type;
            else
                Debug.LogError("Use of type "+type.Name+" with SplineDataDrawerAttribute: " +
                               "A type inheriting from SplineDataDrawer must be used with the SplineDataDrawerAttribute");
        }
    }
}