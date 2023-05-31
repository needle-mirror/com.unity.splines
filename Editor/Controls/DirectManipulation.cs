using Unity.Mathematics;
using UnityEditor.SettingsManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Splines
{
    static class DirectManipulation
    {
        [UserSetting("Tweak Mode", "Plane Color")]
        static readonly Pref<Color> s_GuidePlaneColor = new Pref<Color>("Handles.DirectManipulation.PlaneColor", new Color(1f, 1f, 1f, 5f/255f));

        [UserSetting("Tweak Mode", "Snap to Guide Enabled")]
        static readonly Pref<bool> s_SnapToGuide = new Pref<bool>("Handles.DirectManipulation.SnapToGuide", true);

        [UserSetting("Tweak Mode", "Snap to Guide Distance")]
        static readonly Pref<float> s_SnapToGuideDistance = new Pref<float>("Handles.DirectManipulation.SnapToGuideDistance", 7f);
        
        static readonly Vector3[] s_VertexBuffer = new Vector3[4];

        public static bool IsDragging => s_IsDragging;

        static readonly Vector3 k_GuidePlaneZTestOffset = new Vector3(0.001f, 0.001f, 0.001f);
        static Vector3 s_InitialPosition;
        static Quaternion s_InitialRotation;
        static Vector2 s_InitialMousePosition;
        static bool s_IsDragging;

#if UNITY_2022_2_OR_NEWER
        static bool IncrementalSnapActive => EditorSnapSettings.incrementalSnapActive;
#else
        static bool IncrementalSnapActive => false;
#endif

        const float k_HandleColorAlphaFactor = 0.3f;

        static bool ShouldMoveOnNormal(int controlId) => GUIUtility.hotControl == controlId && Event.current.alt;

        public static void BeginDrag(Vector3 position, Quaternion rotation)
        {
            s_InitialPosition = position;
            s_InitialRotation = rotation;
            s_InitialMousePosition = Event.current.mousePosition;
        }

        public static Vector3 UpdateDrag(int controlId)
        {
            var position = ShouldMoveOnNormal(controlId)
                ? MoveOnNormal(Event.current.mousePosition, s_InitialPosition, s_InitialRotation)
                : MoveOnPlane(Event.current.mousePosition, s_InitialPosition, s_InitialRotation, IncrementalSnapActive);

            s_IsDragging = true;
            return position;
        }

        public static void EndDrag()
        {
            s_IsDragging = false;
        }

        public static void DrawHandles(int controlId, Vector3 position)
        {
            if (GUIUtility.hotControl != controlId || !s_IsDragging)
                return;

            EditorGUIUtility.AddCursorRect(new Rect(0, 0, 100000, 10000), MouseCursor.MoveArrow);

            if (ShouldMoveOnNormal(controlId))
            {
                var yDir = s_InitialRotation * Vector3.up;
                DrawGuideAxis(s_InitialPosition, yDir, Handles.yAxisColor);
            }
            else
            {
                var zDir = s_InitialRotation * Vector3.forward;
                var xDir = s_InitialRotation * Vector3.right;

                DrawGuidePlane(s_InitialPosition, xDir, zDir, position);
                DrawGuideDottedLine(s_InitialPosition, zDir, position);
                DrawGuideDottedLine(s_InitialPosition, xDir, position);

                DrawGuideAxis(s_InitialPosition, zDir, Handles.zAxisColor);
                DrawGuideAxis(s_InitialPosition, xDir, Handles.xAxisColor);
            }
        }

        static (Vector3 projection, float distance) GetSnapToGuideData(Vector3 current, Vector3 origin, Vector3 axis)
        {
            var projection = Vector3.Project(current - origin, axis);
            var screenPos = HandleUtility.WorldToGUIPoint(origin + projection);
            var distance = Vector2.Distance(screenPos, Event.current.mousePosition);
            return (projection, distance);
        }

        static Vector3 MoveOnPlane(Vector2 mousePosition, Vector3 origin, Quaternion rotation, bool snapping)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var manipPlane = new Plane(rotation * Vector3.up, origin);
            var position = manipPlane.Raycast(ray, out float distance)
                ? ray.origin + ray.direction * distance
                : origin;

            var dir = position - origin;
            var forward = GetSnapToGuideData(position, origin, rotation * Vector3.forward);
            var right = GetSnapToGuideData(position, origin, rotation * Vector3.right);

            if (!snapping && s_SnapToGuide)
            {
                if (forward.distance < s_SnapToGuideDistance || right.distance < s_SnapToGuideDistance)
                {
                    var snapToForward = forward.distance < right.distance;
                    var axis = (snapToForward ? forward : right).projection;
                    return origin + axis;
                }
            }

            if(Mathf.Approximately(dir.magnitude, 0f))
                dir = Vector3.forward;
            
            var translation = Handles.SnapValue(Quaternion.Inverse(rotation) * dir, new Vector3(EditorSnapSettings.move.x, 0, EditorSnapSettings.move.z));
            return origin + rotation * translation;
        }
        
        static Vector3 MoveOnNormal(Vector2 mousePosition, Vector3 origin, Quaternion rotation)
        {
            var upAxis = rotation * Vector3.up;
            var translation = upAxis * Handles.SnapValue(HandleUtility.CalcLineTranslation(s_InitialMousePosition, mousePosition, origin, upAxis), EditorSnapSettings.move.y);
            return origin + translation;
        }

        static void DrawGuideAxis(Vector3 origin, Vector3 axis, Color color)
        {
            var start = origin - axis.normalized * 10000f;
            var end = origin + axis.normalized * 10000f;
            
            using (new ZTestScope(CompareFunction.Less))
                using (new Handles.DrawingScope(color))
                {
                    Handles.DrawLine(origin, start, 0f);
                    Handles.DrawLine(origin, end, 0f);
                }

            color = new Color(color.r, color.g, color.b, color.a * k_HandleColorAlphaFactor);

            using (new ZTestScope(CompareFunction.Greater))
                using (new Handles.DrawingScope(color))
                {
                    Handles.DrawLine(origin, start, 0f);
                    Handles.DrawLine(origin, end, 0f);
                }
        }

        static void DrawGuidePlane(Vector3 origin, Vector3 axisX, Vector3 axisZ, Vector3 position)
        {
            var xAxisProjection = Vector3.Project(position - origin, axisX);
            var zAxisProjection = Vector3.Project(position - origin, axisZ);
            var cross = math.cross(xAxisProjection, zAxisProjection);
            var normal = math.normalizesafe(cross);
            var scaledOffset = k_GuidePlaneZTestOffset * HandleUtility.GetHandleSize(origin);
            var calculatedOffset = new Vector3(scaledOffset.x * normal.x, scaledOffset.y * normal.y, scaledOffset.z * normal.z);
           
            position += calculatedOffset;
            origin += calculatedOffset;

            s_VertexBuffer[0] = origin;
            s_VertexBuffer[1] = origin + Vector3.Project(position - origin, axisX);
            s_VertexBuffer[2] = position;
            s_VertexBuffer[3] = origin + Vector3.Project(position - origin, axisZ);

            DrawGuidePlane(Matrix4x4.identity);
        }

        static void DrawGuidePlane(Matrix4x4 matrix)
        {
            var color = s_GuidePlaneColor.value;

            using (new ZTestScope(CompareFunction.Less))
                using (new Handles.DrawingScope(matrix))
                    Handles.DrawSolidRectangleWithOutline(s_VertexBuffer, color, Color.clear);

            color = new Color(s_GuidePlaneColor.value.r, s_GuidePlaneColor.value.g, s_GuidePlaneColor.value.b,
                s_GuidePlaneColor.value.a * k_HandleColorAlphaFactor);

            using (new ZTestScope(CompareFunction.Greater))
                using (new Handles.DrawingScope(matrix))
                    Handles.DrawSolidRectangleWithOutline(s_VertexBuffer, color, Color.clear);
        }

        static void DrawGuideDottedLine(Vector3 origin, Vector3 axis, Vector3 position)
        {
            using (new ZTestScope(CompareFunction.Less))
                Handles.DrawDottedLine(origin + Vector3.Project(position - origin, axis), position, 3f);

            var color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, Handles.color.a * k_HandleColorAlphaFactor);

            using (new ZTestScope(CompareFunction.Greater))
                using (new Handles.DrawingScope(color))
                    Handles.DrawDottedLine(origin + Vector3.Project(position - origin, axis), position, 3f);
        }
    }
}