using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EvenlySpawnOnSpline))]
public class EvenlySpawnOnSplineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Update placement"))
        {
            ( (EvenlySpawnOnSpline)target ).UpdateSplineElements();
        }
    }
}
