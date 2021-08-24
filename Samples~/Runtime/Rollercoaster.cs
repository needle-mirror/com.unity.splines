using System.Collections.Generic;
using Unity.Splines.Examples;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Rollercoaster : MonoBehaviour, ISplineProvider
{
	[SerializeField]
	RollercoasterTrack m_Track;

	public IEnumerable<Spline> Splines => new[] { m_Track };
}
