using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    struct ColorScope : IDisposable
    {
        readonly Color m_PrevColor;

        public ColorScope(Color color)
        {
            m_PrevColor = Handles.color;
            Handles.color = color;
        }

        public void Dispose()
        {
            Handles.color = m_PrevColor;
        }
    }
    
    struct ZTestScope : IDisposable
    {
        readonly CompareFunction m_Original;

        public ZTestScope(CompareFunction function)
        {
            m_Original = Handles.zTest;
            Handles.zTest = function;
        }
        
        public void Dispose()
        {
            Handles.zTest = m_Original;
        }
    }
        
    static class SplineHandleUtility
    {
        const int k_MaxDecimals = 15;
        const int k_SegmentsPointCount = 30;
        static readonly Vector3[] s_ClosestPointArray = new Vector3[k_SegmentsPointCount];
        const float k_KnotPickingDistance = 18f;
        
        static readonly Vector3[] s_LineBuffer = new Vector3[2]; 
        
        internal static Ray TransformRay(Ray ray, Matrix4x4 matrix)
        {
            return new Ray(matrix.MultiplyPoint3x4(ray.origin), matrix.MultiplyVector(ray.direction));
        }

        internal static Vector3 DoIncrementSnap(Vector3 position, Vector3 previousPosition)
        {
            var delta = position - previousPosition;

            var right = Tools.handleRotation * Vector3.right;
            var up = Tools.handleRotation * Vector3.up;
            var forward = Tools.handleRotation * Vector3.forward;
            
            var snappedDelta = 
                Snapping.Snap(Vector3.Dot(delta, right), EditorSnapSettings.move[0]) * right + 
                Snapping.Snap(Vector3.Dot(delta, up), EditorSnapSettings.move[1]) * up + 
                Snapping.Snap(Vector3.Dot(delta, forward), EditorSnapSettings.move[2]) * forward;
            return previousPosition + snappedDelta;
        }
        
        static Vector3 SnapToGrid(Vector3 position)
        {
            //todo Temporary version, waiting for a trunk PR to land to move to the commented version:
//#if UNITY_2022_2_OR_NEWER
            // if(EditorSnapSettings.gridSnapActive)
            //     return Snapping.Snap(position, EditorSnapSettings.gridSize, SnapAxis.All);
//#else
            GameObject tmp = new GameObject();
            tmp.hideFlags = HideFlags.HideAndDontSave;
            var trs = tmp.transform;
            trs.position = position;
            Handles.SnapToGrid(new []{trs});
            var snapped = trs.position;
            Object.DestroyImmediate(tmp);

            return snapped;
//#endif
        }
        
        internal static bool GetPointOnSurfaces(Vector2 mousePosition, out Vector3 point, out Vector3 normal)
        {
#if UNITY_2020_1_OR_NEWER
            if(HandleUtility.PlaceObject(mousePosition, out point, out normal))
            {
                if(EditorSnapSettings.gridSnapEnabled)
                    point = SnapToGrid(point);
                return true;
            }
#endif

            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);

#if !UNITY_2020_1_OR_NEWER
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                point = hit.point;
                normal = hit.normal;
                return true;
            }
#endif

            //Backup if couldn't find a surface
            var constraint = new Plane(Vector3.up, Vector3.zero); //This should be in the direction of the current grid
            if (constraint.Raycast(ray, out float distance))
            {
                normal = constraint.normal;
                point = ray.origin + ray.direction * distance;
                
                if(EditorSnapSettings.gridSnapEnabled)
                    point = SnapToGrid(point);
                
                return true;
            }

            point = normal = Vector3.zero;
            return false;
        }

        internal static void DrawLineWithWidth(Vector3 a, Vector3 b, float width, Texture2D lineAATex = null)
        {
            s_LineBuffer[0] = a;
            s_LineBuffer[1] = b;
         
            Handles.DrawAAPolyLine(lineAATex, width, s_LineBuffer);
        }

        public static float DistanceToKnot(Vector3 position)
        {
            return DistanceToCircle(position, k_KnotPickingDistance); 
        }
        
        public static float DistanceToCircle(Vector3 point, float radius)
        {
            Vector3 screenPos = HandleUtility.WorldToGUIPointWithDepth(point);
            if (screenPos.z < 0)
                return float.MaxValue;

            return Mathf.Max(0, Vector2.Distance(screenPos, Event.current.mousePosition) - radius);
        }
        
        internal static Vector3 RoundBasedOnMinimumDifference(Vector3 position)
        {
            var minDiff = GetMinDifference(position);
            position.x = RoundBasedOnMinimumDifference(position.x, minDiff.x);
            position.y = RoundBasedOnMinimumDifference(position.y, minDiff.y);
            position.z = RoundBasedOnMinimumDifference(position.z, minDiff.z);
            return position;
        }
        
        internal static Vector3 GetMinDifference(Vector3 position)
        {
            return Vector3.one * (HandleUtility.GetHandleSize(position) / 80f);
        }
        
        internal static float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
        {
            var numberOfDecimals = Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, k_MaxDecimals);
            return (float)Math.Round(valueToRound, numberOfDecimals, MidpointRounding.AwayFromZero);
        }
        
        public static void GetNearestPointOnCurve(CurveData curve, out Vector3 position, out float t)
        {
            Vector3 closestA = Vector3.zero;
            Vector3 closestB = Vector3.zero;
            float closestDist = float.MaxValue;
            int closestSegmentFirstPoint = -1;

            GetCurveSegments(curve, s_ClosestPointArray);
            for (int j = 0; j < s_ClosestPointArray.Length - 1; ++j)
            {
                Vector3 a = s_ClosestPointArray[j];
                Vector3 b = s_ClosestPointArray[j + 1];
                float dist = HandleUtility.DistanceToLine(a, b);

                if (dist < closestDist)
                {
                    closestA = a;
                    closestB = b;
                    closestDist = dist;
                    closestSegmentFirstPoint = j;
                }
            }

            //Calculate position
            Vector2 screenPosA = HandleUtility.WorldToGUIPoint(closestA);
            Vector2 screenPosB = HandleUtility.WorldToGUIPoint(closestB);
            Vector2 relativePoint = Event.current.mousePosition - screenPosA;
            Vector2 lineDirection = screenPosB - screenPosA;
            float length = lineDirection.magnitude;
            float dot = Vector3.Dot(lineDirection, relativePoint);
            if (length > .000001f)
                dot /= length * length;
            dot = Mathf.Clamp01(dot);
            position = Vector3.Lerp(closestA, closestB, dot);

            //Calculate percent on curve's segment
            float percentPerSegment = 1.0f / (k_SegmentsPointCount - 1);
            float percentA = closestSegmentFirstPoint * percentPerSegment;
            float lengthAB = (closestB - closestA).magnitude;
            float lengthAToClosest = (position - closestA).magnitude;
            t = percentA + percentPerSegment * (lengthAToClosest / lengthAB);
        }
        
        internal static void GetCurveSegments(CurveData curve, Vector3[] results)
        {
            if (!curve.IsValid())
                throw new ArgumentException(nameof(curve));

            if (results == null)
                throw new ArgumentNullException(nameof(results));

            if (results.Length < 2)
                throw new ArgumentException("Get curve segments requires a results array of at least two points", nameof(results));

            var segmentCount = results.Length - 1;
            float segmentPercentage = 1f / segmentCount;
            var path = curve.a.spline;
            for (int i = 0; i <= segmentCount; ++i)
            {
                results[i] = path.GetPointOnCurve(curve, i * segmentPercentage);
            }
        }
    }
}
