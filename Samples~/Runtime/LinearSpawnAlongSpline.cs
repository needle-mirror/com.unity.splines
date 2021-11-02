using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode]
public class LinearSpawnAlongSpline : MonoBehaviour
{
    public SplineContainer m_Container;
    public GameObject m_PrefabToSpawn;

    [Range(0f,1f)]
    public float m_StartTime = 0f;
    public float m_Distance = 2f;
    
    [SerializeField]
    List<GameObject> m_Instances = new List<GameObject>();

    void OnEnable()
    {
        InitContainer();
        UpdateSplineElements();
    }
    
    void OnValidate()
    {
#if UNITY_EDITOR
        if(EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += () =>  UpdateSplineElements();
#endif
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
        
        if(m_Container == null || m_PrefabToSpawn == null)
            return;
        
        var points = GetPointsWithLinearDistance(m_Container.Spline.ToNativeSpline(m_Container.transform.localToWorldMatrix), m_Distance);
        
        for(int pIndex = 0; pIndex < points.Count; pIndex++)
        {
            var p = points[pIndex];
            var forward = pIndex < points.Count - 1 ? points[pIndex+1] - p : m_Container.EvaluatePosition(1f) - p;
            if(forward.Equals(float3.zero))
                forward = m_Container.EvaluateTangent(1f);
            
            var instance = GameObject.Instantiate(m_PrefabToSpawn, transform);
            instance.transform.position = p;
            instance.transform.rotation = Quaternion.LookRotation(forward);
            
            m_Instances.Add(instance);
        }
    }
    
    public List<float3> GetPointsWithLinearDistance(NativeSpline spline,
        float distance)
    {
        List<float3> points = new List<float3>();
    
        float time = m_StartTime;
        float3 point = spline.EvaluatePosition(m_StartTime);
        points.Add(point);
        while((distance > 0 && time < 1f) || (distance < 0 && time > 0f))
        {
            point = spline.GetPointAtLinearDistance(time, distance, out time);
            
            if(math.distance(point, points[points.Count - 1]) < 0.95f * distance || time >= 1f)
                break;
            
            points.Add(point);
        }
        
        return points;
    }
}
