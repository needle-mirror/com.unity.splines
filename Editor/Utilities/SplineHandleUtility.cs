using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor.SettingsManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using Unity.Mathematics;
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
        [UserSetting]
        static UserSetting<Color> s_LineNormalFrontColor = new UserSetting<Color>(PathSettings.instance, "Handles.CurveNormalInFrontColor", new Color(0f, 0f, 0f, 1.0f), SettingsScope.User);

        [UserSetting]
        static UserSetting<Color> s_LineNormalBehindColor = new UserSetting<Color>(PathSettings.instance, "Handles.CurveNormalBehindColor", new Color(0f, 0f, 0f, 0.4f), SettingsScope.User);

#if !UNITY_2022_2_OR_NEWER
        [UserSetting]
        static UserSetting<Color> s_KnotColor = new UserSetting<Color>(PathSettings.instance, "Handles.KnotDefaultColor", new Color(0f, 224f / 255f, 1f, 1f), SettingsScope.User);
#endif

        [UserSetting]
        static UserSetting<Color> s_TangentColor = new UserSetting<Color>(PathSettings.instance, "Handles.TangentDefaultColor", Color.black, SettingsScope.User);

        [UserSettingBlock("Handles")]
        static void HandleColorPreferences(string searchContext)
        {
            s_LineNormalFrontColor.value = SettingsGUILayout.SettingsColorField("Curve Color", s_LineNormalFrontColor, searchContext);
            s_LineNormalBehindColor.value = SettingsGUILayout.SettingsColorField("Curve Color Behind Surface", s_LineNormalBehindColor, searchContext);
#if !UNITY_2022_2_OR_NEWER
            s_KnotColor.value = SettingsGUILayout.SettingsColorField("Knot Color", s_KnotColor, searchContext);
#endif
            s_TangentColor.value = SettingsGUILayout.SettingsColorField("Tangent Color", s_TangentColor, searchContext);
        }

        internal static Color lineBehindColor => s_LineNormalBehindColor;
        internal static Color lineColor => s_LineNormalFrontColor;
#if !UNITY_2022_2_OR_NEWER
        internal static Color knotColor => s_KnotColor;
#endif
        internal static Color tangentColor => s_TangentColor;

#if UNITY_2022_2_OR_NEWER
        static Color s_DefaultElementColor = Handles.elementColor;
        static Color s_DefaultElementPreselectionColor = Handles.elementPreselectionColor;
        static Color s_DefaultElementSelectionColor = Handles.elementSelectionColor;
#else
        static Color s_DefaultElementColor = SplineHandleUtility.knotColor;
        static Color s_DefaultElementPreselectionColor = Handles.preselectionColor;
        static Color s_DefaultElementSelectionColor = Handles.selectedColor;
#endif
        
        internal static Color elementColor = s_DefaultElementColor;
        internal static Color elementPreselectionColor = s_DefaultElementPreselectionColor;
        internal static Color elementSelectionColor = s_DefaultElementSelectionColor;
        
        internal const float pickingDistance = 8f;
        internal const float handleWidth = 4f;
        internal const float aliasedLineSizeMultiplier = 0.5f;
        internal const float sizeFactor = 0.15f;
        internal const float knotDiscRadiusFactorDefault = 0.06f;
        internal const float knotDiscRadiusFactorHover = 0.07f;
        internal const float knotDiscRadiusFactorSelected = 0.085f;

        internal static readonly Texture2D denseLineAATex = Resources.Load<Texture2D>(k_TangentLineAATexPath);

        const string k_TangentLineAATexPath = "Textures/TangentLineAATex";
        const int k_MaxDecimals = 15;
        const int k_SegmentsPointCount = 30;
        static readonly Vector3[] s_ClosestPointArray = new Vector3[k_SegmentsPointCount];
        static readonly Vector3[] s_AAWireDiscBuffer = new Vector3[18];
        const float k_KnotPickingDistance = 18f;

        static readonly Vector3[] s_LineBuffer = new Vector3[2];
        
        internal static bool canDrawOnCurves = false;

        internal  static ISelectableElement lastHoveredElement { get; private set; }
        internal  static int lastHoveredElementId { get; private set; }

        //Settings min and max ids used by handles when drawing curves/knots/tangents 
        //This helps to determine if nearest control is a spline element or a built-in tool
        static Vector2Int s_ElementIdRange = Vector2Int.zero;
        internal static int minElementId
        {
            get => s_ElementIdRange.x;
            set => s_ElementIdRange.x = value;
        }
        internal static int maxElementId
        {
            get => s_ElementIdRange.y;
            set => s_ElementIdRange.y = value;
        }

        internal static void UpdateElementColors()
        {
#if UNITY_2022_2_OR_NEWER
            elementColor = Handles.elementColor;
            elementPreselectionColor = Handles.elementPreselectionColor;
            elementSelectionColor = Handles.elementSelectionColor;
#else
            elementColor = SplineHandleUtility.knotColor;
            elementPreselectionColor = Handles.preselectionColor;
            elementSelectionColor = Handles.selectedColor;
#endif
        }

        internal static bool ShouldShowTangent(SelectableTangent tangent)
        {
            if (!SplineSelectionUtility.IsSelectable(tangent) || Mathf.Approximately(math.length(tangent.LocalDirection), 0f))
                return false;

            if (SplineHandleSettings.ShowAllTangents)
                return true;

            return SplineSelection.IsSelectedOrAdjacentToSelected(tangent);
        }

        internal static void ResetLastHoveredElement()
        {
            lastHoveredElementId = -1;
            lastHoveredElement = null;
        }
        
        internal static bool IsLastHoveredElement<T>(T element)
        {
            return element.Equals(lastHoveredElement);
        }

        internal static void SetLastHoveredElement<T>(T element, int controlId) where T : ISelectableElement
        {
            lastHoveredElementId = controlId;
            lastHoveredElement = element;
        }

        internal static bool IsElementHovered(int controlId)
        {
            //Hovering the element itself
            var isElementHovered = (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlId);
            // starting (Mouse down) or performing direct manip on that element
            var isDirectManipElement = GUIUtility.hotControl == controlId;
            return isElementHovered || isDirectManipElement;
        }
        
        //Check if the nearest control is one belonging to the spline elements
        internal static bool IsHoverAvailableForSplineElement()
        {
            return GUIUtility.hotControl == 0 && (!canDrawOnCurves || (HandleUtility.nearestControl > minElementId && HandleUtility.nearestControl < maxElementId)) 
                || GUIUtility.hotControl > minElementId && GUIUtility.hotControl < maxElementId;
        }

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
#if UNITY_2022_2_OR_NEWER
            return EditorSnapSettings.gridSnapActive ?
                   Snapping.Snap(position, EditorSnapSettings.gridSize) :
                   position;
#else
            GameObject tmp = new GameObject();
            tmp.hideFlags = HideFlags.HideAndDontSave;
            var trs = tmp.transform;
            trs.position = position;
            Handles.SnapToGrid(new[] { trs });
            var snapped = trs.position;
            Object.DestroyImmediate(tmp);

            return snapped;
#endif
        }

        internal static bool GetPointOnSurfaces(Vector2 mousePosition, out Vector3 point, out Vector3 normal)
        {
#if UNITY_2020_1_OR_NEWER
            if (HandleUtility.PlaceObject(mousePosition, out point, out normal))
            {
                if (EditorSnapSettings.gridSnapEnabled)
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
            var constraint = new Plane(SceneView.lastActiveSceneView.in2DMode ? Vector3.back : Vector3.up, Vector3.zero); //This should be in the direction of the current grid
            if (constraint.Raycast(ray, out float distance))
            {
                normal = constraint.normal;
                point = ray.origin + ray.direction * distance;

                if (EditorSnapSettings.gridSnapEnabled)
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

        public static float DistanceToCircle(Vector3 point, float radius)
        {
            Vector3 screenPos = HandleUtility.WorldToGUIPointWithDepth(point);
            if (screenPos.z < 0)
                return float.MaxValue;

            return Mathf.Max(0, Vector2.Distance(screenPos, Event.current.mousePosition) - radius);
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

        internal static void GetNearestPointOnCurve(BezierCurve curve, out Vector3 position, out float t)
        {
            GetNearestPointOnCurve(curve, out position, out t, out _);
        }

        internal static void GetNearestPointOnCurve(BezierCurve curve, out Vector3 position, out float t, out float distance)
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
            distance = closestDist;
        }

        static void GetCurveSegments(BezierCurve curve, Vector3[] results)
        {
            float segmentPercentage = 1f / (results.Length - 1);
            for (int i = 0; i < k_SegmentsPointCount; ++i)
            {
                results[i] = CurveUtility.EvaluatePosition(curve, i * segmentPercentage);
            }
        }

        internal static void DrawAAWireDisc(Vector3 position, Vector3 normal, float radius, float thickness)
        {
            // Right vector calculation here is identical to Handles.DrawWireDisc
            Vector3 right = Vector3.Cross(normal, Vector3.up);
            if ((double)right.sqrMagnitude < 1.0 / 1000.0)
                right = Vector3.Cross(normal, Vector3.right);

            var angleStep = 360f / (s_AAWireDiscBuffer.Length - 1);
            for (int i = 0; i < s_AAWireDiscBuffer.Length - 1; i++)
            {
                s_AAWireDiscBuffer[i] = position + right * radius;
                right = Quaternion.AngleAxis(angleStep, normal) * right;
            }

            s_AAWireDiscBuffer[s_AAWireDiscBuffer.Length - 1] = s_AAWireDiscBuffer[0];

            var tex = thickness > 2f ? denseLineAATex : null;
            Handles.DrawAAPolyLine(tex, thickness, s_AAWireDiscBuffer);
        }
    }
}
