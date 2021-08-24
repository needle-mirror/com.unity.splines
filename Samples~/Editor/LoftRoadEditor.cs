using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Splines.Examples.Editor
{
    [CustomEditor(typeof(LoftRoadBehaviour))]
    class RoadTool : UnityEditor.Editor
    {
        List<LoftRoadBehaviour> m_Roads;

        static List<T> GetFiltered<T>(IEnumerable collection)
        {
            List<T> filtered = new List<T>();
            foreach (var obj in collection)
                if (obj is T cast)
                    filtered.Add(cast);
            return filtered;
        }

        void OnEnable()
        {
            m_Roads = GetFiltered<LoftRoadBehaviour>(targets);

            foreach(var road in m_Roads)
            {
                if (road == null || road.spline == null)
                    continue;

                EditorSplineUtility.RegisterSplineDataChanged<float>(OnAfterSplineDataWasModified);
                Undo.undoRedoPerformed += road.Loft;
            }

            EditorSplineUtility.afterSplineWasModified += OnAfterSplineWasModified;
        }

        void OnDisable()
        {
            foreach(var road in m_Roads)
            {
                if (road == null || road.spline == null)
                    continue;
                
                
                EditorSplineUtility.UnregisterSplineDataChanged<float>(OnAfterSplineDataWasModified);
                Undo.undoRedoPerformed -= road.Loft;
            }
            
            EditorSplineUtility.afterSplineWasModified -= OnAfterSplineWasModified;
        }


        void OnAfterSplineDataWasModified(SplineData<float> splineData)
        {
            var road = m_Roads.Find((road) => road.width == splineData);
            if (road != null)
                road.Loft();
        }
        
        void OnAfterSplineWasModified(Spline spline)
        {
            var road = m_Roads.Find((road) => road.spline == spline);

            if (road != null)
                road.Loft();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
        
            base.OnInspectorGUI();
        
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var road in m_Roads)
                    road.Loft();
            }
        }
    }
}
