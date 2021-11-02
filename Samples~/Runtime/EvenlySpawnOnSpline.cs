using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor.Splines;
using UnityEngine.Splines;
using Random = Unity.Mathematics.Random;

public class EvenlySpawnOnSpline : MonoBehaviour
{
    public enum RotationMode
    {
        None,
        Randomize,
        AlignWithTangent
    }

    public SplineContainer m_Container;
    public List<GameObject> m_PrefabsToSpawn = new List<GameObject>();
    public float m_DistanceBetweenElements = 2f;
    public RotationMode m_RotationMode = RotationMode.Randomize;

    [SerializeField]
    List<GameObject> m_Instances = new List<GameObject>();

    void OnEnable()
    {
        InitContainer();
        UpdateSplineElements();
    }

    void InitContainer()
    {
        if(m_Container == null)
            m_Container = GetComponent<SplineContainer>();

#if UNITY_EDITOR
        EditorSplineUtility.afterSplineWasModified += delegate(Spline spline)
        {
            if(spline == m_Container.Spline)
                UpdateSplineElements();
        };
#endif
    }

    public void UpdateSplineElements()
    {
        for(int i = m_Instances.Count - 1; i >= 0; --i)
        {
            if(m_Instances[i] != null)
                DestroyImmediate(m_Instances[i]);
        }
        m_Instances.Clear();

        if(m_Container == null)
            InitContainer();

        if(m_Container == null || m_PrefabsToSpawn.Count == 0)
            return;

        float currentDist = 0f;
        float splineLength = m_Container.CalculateLength();
        Random rand = new Random((uint)GetInstanceID());
        while(currentDist < splineLength)
        {
            var instance = Instantiate(m_PrefabsToSpawn[rand.NextInt(0, m_PrefabsToSpawn.Count)], this.transform);
            var splineTime = currentDist / splineLength;
            instance.transform.position = m_Container.EvaluatePosition(splineTime);

            switch (m_RotationMode)
            {
                case RotationMode.Randomize:
                    instance.transform.rotation = Quaternion.AngleAxis(rand.NextFloat(0, 360f), Vector3.up);
                    break;

                case RotationMode.AlignWithTangent:
                    m_Container.Evaluate(splineTime, out var _, out var direction, out var up);
                    instance.transform.rotation = Quaternion.LookRotation(direction, up);
                    break;
                default:
                    instance.transform.rotation = quaternion.identity;
                    break;
            }

            m_Instances.Add(instance);
            currentDist += m_DistanceBetweenElements;
        }
    }
}
