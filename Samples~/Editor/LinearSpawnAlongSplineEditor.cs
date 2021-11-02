using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LinearSpawnAlongSpline))]
public class LinearSpawnAlongSplineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Update placement"))
        {
            ( (LinearSpawnAlongSpline)target ).UpdateSplineElements();
        }
    }
}
