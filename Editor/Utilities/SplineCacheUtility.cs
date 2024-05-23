using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class SplineCacheUtility
    {
        const int k_SegmentsCount = 32;
        
        static Dictionary<Spline, Vector3[]> s_SplineCacheTable = new Dictionary<Spline, Vector3[]>();

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            Spline.Changed += ClearCache;
            Undo.undoRedoPerformed +=  ClearAllCache;
            PrefabStage.prefabStageClosing += _ => ClearAllCache();
            
#if UNITY_2022_3_OR_NEWER
            PrefabUtility.prefabInstanceReverting += _ => ClearAllCache();
#else
            // PrefabUtility.prefabInstanceReverting is unfortunately not backported yet.
            ObjectChangeEvents.changesPublished += (ref ObjectChangeEventStream stream) =>
            {
                bool cacheCleared = false;
                for (int i = 0; i < stream.length; ++i)
                {
                    if (cacheCleared)
                        break;

                    int instanceID = 0;
                    switch (stream.GetEventType(i))
                    {
                        // Called after prefab instance override Revert All.
                        case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                            stream.GetChangeGameObjectStructureHierarchyEvent(i, out var changeGameObjectStructureHierarchyEvt);
                            instanceID = changeGameObjectStructureHierarchyEvt.instanceId;
                            break;
                        // Called after prefab instance override Revert on field or component.
                        case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                            stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var changeGameObjectOrComponentPropertiesEvt);
                            instanceID = changeGameObjectOrComponentPropertiesEvt.instanceId;
                            break;
                    }

                    if (instanceID != 0)
                    {
                        var obj = EditorUtility.InstanceIDToObject(instanceID);
                        if(obj == null)
                            continue;

                        if (PrefabUtility.GetPrefabInstanceStatus(obj) == PrefabInstanceStatus.Connected)
                        {
                            SplineContainer splineContainer = null;
                            GameObject splineGO = obj as GameObject;
                            
                            if (splineGO != null)
                                splineContainer = splineGO.GetComponent<SplineContainer>();
                            else
                                splineContainer = obj as SplineContainer;
                          
                            if (splineContainer != null)
                            {
                                foreach (var spline in splineContainer.Splines)
                                {
                                    if (s_SplineCacheTable.ContainsKey(spline))
                                    {
                                        ClearAllCache();
                                        cacheCleared = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            };
#endif
        }
        
        internal static void ClearAllCache()
        {
            s_SplineCacheTable.Clear();
            s_CurvesBuffers?.Clear();
            s_TangentsCache?.Clear();
        }

        public static void GetCachedPositions(Spline spline, out Vector3[] positions)
        {
            if(!s_SplineCacheTable.ContainsKey(spline))
                s_SplineCacheTable.Add(spline, null);

            int count = spline.Closed ? spline.Count : spline.Count - 1;

            if(s_SplineCacheTable[spline] == null)
            {
                s_SplineCacheTable[spline] = new Vector3[count * k_SegmentsCount];
                CacheCurvePositionsTable(spline, count);
            }
            positions = s_SplineCacheTable[spline];
        }
       
        static void CacheCurvePositionsTable(Spline spline, int curveCount)
        {
            float inv = 1f / (k_SegmentsCount - 1);
            for(int i = 0; i < curveCount; ++i)
            {
                var curve = spline.GetCurve(i);
                var startIndex = i * k_SegmentsCount;
                for(int n = 0; n < k_SegmentsCount; n++)
                    (s_SplineCacheTable[spline])[startIndex + n] = CurveUtility.EvaluatePosition(curve, n * inv);
            }
        }

        internal struct CurveBufferData
        {
            internal Vector3[] positions;
            internal (Vector3[] positions, Matrix4x4 trs) flowArrow;
        }
        
        //internal for tests
        internal static Dictionary<BezierCurve, CurveBufferData> s_CurvesBuffers = null;
        
        //internal for tests
        internal static Dictionary<SelectableTangent, (float3 position, quaternion rotation)> s_TangentsCache = null;

        const int k_CurveDrawResolution = 64;
        internal static int CurveDrawResolution => k_CurveDrawResolution;
        
        static void ClearCache(Spline spline, int knotIndex, SplineModification modificationType)
        {
            if (knotIndex == -1 || modificationType != SplineModification.KnotModified)
            {
                s_CurvesBuffers?.Clear();
                s_SplineCacheTable.Remove(spline);
            }
            else // If Knot modified by the tools
            {
                // If knots are in auto mode they can influence up to the 4 curves around them
                var knotIndexBefore = SplineUtility.PreviousIndex(knotIndex, spline.Count, spline.Closed);
                var knotIndexBeforeBefore = SplineUtility.PreviousIndex(knotIndexBefore, spline.Count, spline.Closed);
                var knotIndexAfter = SplineUtility.NextIndex(knotIndex, spline.Count, spline.Closed);

                UpdateCachePosition(spline, knotIndexBeforeBefore);
                UpdateCachePosition(spline, knotIndexBefore);
                UpdateCachePosition(spline, knotIndex);
                UpdateCachePosition(spline, knotIndexAfter);
            }
            
            //We don't have access to the spline container here so we cannot detect which specific SelectableTangents are impacted
            s_TangentsCache?.Clear();
        }

        static void UpdateCachePosition(Spline spline, int curveIndex)
        {
            if (curveIndex > spline.Count)
                return;
            
            var curve = spline.GetCurve(curveIndex);
            if (s_CurvesBuffers != null)
            {
                // Update curve cache
                if (s_CurvesBuffers.ContainsKey(curve))
                    s_CurvesBuffers.Remove(curve);
            }

            if (s_SplineCacheTable == null || !s_SplineCacheTable.ContainsKey(spline) || s_SplineCacheTable[spline] == null)
                return;
                
            // Update position cache
            float inv = 1f / (k_SegmentsCount - 1);
            var startIndex = curveIndex * k_SegmentsCount;
            for (int n = 0; n < k_SegmentsCount; n++)
            {
                if(startIndex + n < (s_SplineCacheTable[spline]).Length)
                    (s_SplineCacheTable[spline])[startIndex + n] = CurveUtility.EvaluatePosition(curve, n * inv);
            }
        }

        internal static void InitializeCache()
        {
            s_CurvesBuffers = new Dictionary<BezierCurve, CurveBufferData>();
            s_TangentsCache = new Dictionary<SelectableTangent, (float3, quaternion)>();
        }
        
        internal static void ClearCache()
        {
            s_TangentsCache?.Clear();
            s_CurvesBuffers?.Clear();
            s_TangentsCache = null;
            s_CurvesBuffers = null;
        }

        static (float3, quaternion) InitTangentEntry(SelectableTangent tangent)
        {
            var pos = tangent.Position;
            var rot = EditorSplineUtility.GetElementRotation(math.length(tangent.LocalPosition) > 0 ? (ISelectableElement)tangent : tangent.Owner);

            s_TangentsCache.Add(tangent, (pos, rot));
            return (pos, rot);
        }

        internal static (float3 position, quaternion rotation) GetTangentPositionAndRotation(SelectableTangent tangent)
        {
            if (s_TangentsCache == null)
                return (tangent.Position, EditorSplineUtility.GetElementRotation(math.length(tangent.LocalPosition) > 0 ? (ISelectableElement)tangent : tangent.Owner));
            
            if (!s_TangentsCache.TryGetValue(tangent, out var tuple))
                tuple = InitTangentEntry(tangent);

            return tuple;
        }
        
        internal static quaternion GetTangentRotation(SelectableTangent tangent)
        {
            if (s_TangentsCache == null)
                return EditorSplineUtility.GetElementRotation(math.length(tangent.LocalPosition) > 0 ? (ISelectableElement)tangent : tangent.Owner);
            
            if (!s_TangentsCache.TryGetValue(tangent, out var tuple))
                tuple = InitTangentEntry(tangent);

            return tuple.rotation;
        }
        
        internal static float3 GetTangentPosition(SelectableTangent tangent)
        {
            if (s_TangentsCache == null)
                return tangent.Position;
            
            if (!s_TangentsCache.TryGetValue(tangent, out var tuple))
                tuple = InitTangentEntry(tangent);

            return tuple.position;
        }

        internal static void GetCurvePositions(BezierCurve curve, Vector3[] buffer)
        {
            if (s_CurvesBuffers == null)
            {
                ComputeCurveBuffer(curve, buffer);
                return;
            }

            if (!s_CurvesBuffers.TryGetValue(curve, out var curveBufferData))
            {
                curveBufferData = new CurveBufferData() { positions = null, flowArrow = new(null, new Matrix4x4()) };
                s_CurvesBuffers.Add(curve, curveBufferData);
            }

            if(curveBufferData.positions == null)
            {
                curveBufferData.positions = new Vector3[buffer.Length];
                ComputeCurveBuffer(curve, curveBufferData.positions);
                
                s_CurvesBuffers[curve] = curveBufferData;
            }
            
            Array.Copy(curveBufferData.positions, buffer, curveBufferData.positions.Length);
        }

        internal static void ComputeCurveBuffer(BezierCurve curve, Vector3[] buffer)
        {
            const float segmentPercentage = 1f / k_CurveDrawResolution;
            for (int i = 0; i <= k_CurveDrawResolution; ++i)
            {
                buffer[i] = CurveUtility.EvaluatePosition(curve, i * segmentPercentage);
            }
        }
        
        internal static (Vector3[] positions, Matrix4x4 trs) GetCurveArrow(ISpline spline, int curveIndex, BezierCurve curve)
        {
            if (s_CurvesBuffers == null)
                return ComputeArrowPositions(spline, curveIndex, curve);

            if (!s_CurvesBuffers.TryGetValue(curve, out var curveBufferData))
            {
                curveBufferData = new CurveBufferData() { positions = null, flowArrow = new(null, new Matrix4x4()) };
                s_CurvesBuffers.Add(curve, curveBufferData);
            }

            if (curveBufferData.flowArrow.positions == null)
            {
                var arrow = ComputeArrowPositions(spline, curveIndex, curve);
                curveBufferData.flowArrow.positions = arrow.positions;
                curveBufferData.flowArrow.trs = arrow.trs;
                
                s_CurvesBuffers[curve] = curveBufferData;
            }

            return curveBufferData.flowArrow;
        }
        
        internal static (Vector3[] positions, Matrix4x4 trs) ComputeArrowPositions(ISpline spline, int curveIndex, BezierCurve curve)
        {
            var t = EditorSplineUtility.GetCurveMiddleInterpolation(curve, spline, curveIndex);
            
            var position = (Vector3)CurveUtility.EvaluatePosition(curve, t);
            var tangent = ((Vector3)CurveUtility.EvaluateTangent(curve, t)).normalized;
            var up = spline.GetCurveUpVector(curveIndex, t);
            var rotation = Quaternion.LookRotation(tangent, up);

            var arrowMaxSpline = .05f * CurveUtility.ApproximateLength(curve);
            var size = HandleUtility.GetHandleSize(position) * .5f;

            tangent = new Vector3(0, 0, .1f) * size;
            var right = new Vector3(0.075f, 0, 0) * size;
            var magnitude = tangent.magnitude;

            if(magnitude > arrowMaxSpline)
            {
                var ratio = arrowMaxSpline / magnitude;
                tangent *= ratio;
                right *= ratio;
            }

            var a = tangent;
            var b = -tangent + right;
            var c = -tangent - right;
            var positions = new[] { a, b, c};

            return (positions, Matrix4x4.TRS(position, rotation, Vector3.one));
        }
        

    }
}
