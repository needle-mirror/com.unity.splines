using System;
using UnityEngine;

namespace UnityEngine.Splines
{
    /// <summary>
    /// SplineDataHandleAttribute can be use to add custom handles to <see cref="SplineData{T}"/>.
    /// The custom drawer class must inherit from SplineDataHandle and override one of the Draw static method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class SplineDataHandleAttribute : Attribute {}
}