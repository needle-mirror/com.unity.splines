using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    static class CurveHandles
    {
        const int k_CurveDrawResolution = 32;
        const float k_CurveLineWidth = 5f;
        const float k_PreviewCurveOpacity = 0.5f;

        static readonly Vector3[] s_CurveDrawingBuffer = new Vector3[k_CurveDrawResolution + 1];

        public static void DrawPreview(BezierCurve curve)
        {
            if(Event.current.type == EventType.Repaint)
                Draw(-1, curve, true, true);
        }

        public static void Draw(BezierCurve curve, bool activeSpline)
        {
            if(Event.current.type == EventType.Repaint)
                Draw(0, curve, true, activeSpline);
        }

        public static void DrawWithHighlight(int controlID, BezierCurve curve, SelectableKnot a, SelectableKnot b, bool activeSpline)
        {
            var evt = Event.current;
            switch(evt.GetTypeForControl(controlID))
            {
                case EventType.Layout:
                    Draw(controlID, curve, true, activeSpline);
                    break;

                case EventType.Repaint:
                    Draw(controlID, curve, true, activeSpline);
                    if(HandleUtility.nearestControl == controlID)
                    {
                        SplineHandleUtility.GetNearestPointOnCurve(curve, out _, out var t);
                        using(new ColorScope(Handles.preselectionColor))
                            DoCurveHighlightCap(t <= .5f ? a : b);
                    }
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == controlID)
                    {
                        //Clicking a knot selects it
                        if (evt.button != 0)
                            break;

                        GUIUtility.hotControl = controlID;
                        evt.Use();

                        SplineHandleUtility.GetNearestPointOnCurve(curve, out _, out var t);
                        SplineSelectionUtility.HandleSelection(t <= .5f ? a : b, (EditorGUI.actionKey || evt.modifiers == EventModifiers.Shift), false);
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }

                    break;
            }

        }

        static void Draw(int controlID, BezierCurve curve, bool preview, bool activeSpline)
        {
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                    {
                       var dist = DistanceToCurve(curve);
                        HandleUtility.AddControl(controlID, Mathf.Max(0, dist - SplineHandleUtility.pickingDistance));
                    }
                    break;

                case EventType.Repaint:
                    
                    //We attenuate the spline display if a spline can be controlled (id != -1) and
                    //if it's not the current active spline
                    var attenuate = controlID != -1 && !activeSpline;
                    var prevColor = Handles.color;

                    var color = SplineHandleUtility.lineColor;
                    if (attenuate)
                        color = Handles.secondaryColor;
                    if (preview)
                        color.a *= k_PreviewCurveOpacity;

                    FillCurveDrawingBuffer(curve);

                    Handles.color = color;

                    using (new ZTestScope(CompareFunction.Less))
                    {
                        Handles.DrawAAPolyLine(k_CurveLineWidth, s_CurveDrawingBuffer);
                    }

                    color = SplineHandleUtility.lineBehindColor;
                    if (attenuate)
                        color = Handles.secondaryColor;
                    if (preview)
                        color.a *= k_PreviewCurveOpacity;

                    Handles.color = color;

                    using (new ZTestScope(CompareFunction.Greater))
                    {
                        Handles.DrawAAPolyLine(k_CurveLineWidth, s_CurveDrawingBuffer);
                    }

                    Handles.color = prevColor;
                    break;
            }
        }

        public static void CurveHandleCap(
            int controlID,
            BezierCurve curve,
            float size,
            EventType evt)
        {
            switch (evt)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    HandleUtility.AddControl(controlID, DistanceToCurve(curve));
                    break;
                case EventType.Repaint:
                    FillCurveDrawingBuffer(curve);
                    Handles.DrawAAPolyLine(size, s_CurveDrawingBuffer);
                    break;
            }
        }

        static void FillCurveDrawingBuffer(BezierCurve curve)
        {
            const float segmentPercentage = 1f / k_CurveDrawResolution;
            for (int i = 0; i <= k_CurveDrawResolution; ++i)
            {
                s_CurveDrawingBuffer[i] = CurveUtility.EvaluatePosition(curve, i * segmentPercentage);
            }
        }

        internal static float DistanceToCurve(BezierCurve curve)
        {
            FillCurveDrawingBuffer(curve);
            return DistanceToCurve();
        }

        static float DistanceToCurve()
        {
            float dist = float.MaxValue;
            for (var i = 0; i < s_CurveDrawingBuffer.Length - 1; ++i)
            {
                var a = s_CurveDrawingBuffer[i];
                var b = s_CurveDrawingBuffer[i + 1];
                dist = Mathf.Min(HandleUtility.DistanceToLine(a, b), dist);
            }

            return dist;
        }

        internal static void DoCurveHighlightCap(SelectableKnot knot)
        {
            if(Event.current.type != EventType.Repaint)
                return;

            if(knot.IsValid())
            {
                var spline = knot.SplineInfo.Spline;
                var localToWorld = knot.SplineInfo.LocalToWorld;

                if(knot.KnotIndex > 0 || spline.Closed)
                {
                    var curve = spline.GetCurve(spline.PreviousIndex(knot.KnotIndex)).Transform(localToWorld);
                    DrawCurveHighlight(curve, 1f, 0.5f);
                }

                if(knot.KnotIndex < spline.Count - 1  || spline.Closed)
                {
                    var curve = spline.GetCurve(knot.KnotIndex).Transform(localToWorld);
                    DrawCurveHighlight(curve, 0f, 0.5f);
                }
            }
        }

        internal static void DrawCurveHighlight(BezierCurve curve, float startT, float endT)
        {
            FillCurveDrawingBuffer(curve);

            var prevColor = Handles.color;

            var growing = startT <= endT;
            var color = prevColor;
            color.a = growing ? 1f : 0f;

            Handles.color = color;
            using (new ZTestScope(CompareFunction.Less))
            {
                for(int i = 1; i <= k_CurveDrawResolution; ++i)
                {
                    Handles.DrawAAPolyLine(k_CurveLineWidth, new []{s_CurveDrawingBuffer[i-1], s_CurveDrawingBuffer[i]});
                    var current = ( (float)i / (float)k_CurveDrawResolution );
                    if(growing)
                    {
                        if(current > endT)
                            color.a = 0f;
                        else if(current > startT)
                            color.a = 1f - ( current - startT ) / ( endT - startT );
                    }
                    else
                    {
                        if(current > startT)
                            color.a = 0f;
                        else if(current > endT && current < startT)
                            color.a =  (current - endT) /  (startT - endT);
                    }

                    Handles.color = color;
                }
            }

            Handles.color = prevColor;
        }

        public static void DrawControlNet(BezierCurve curve)
        {
            Handles.color = Color.green;
            Handles.DotHandleCap(-1, curve.P0, Quaternion.identity, HandleUtility.GetHandleSize(curve.P0) * .04f, Event.current.type);
            Handles.color = Color.red;
            Handles.DotHandleCap(-1, curve.P1, Quaternion.identity, HandleUtility.GetHandleSize(curve.P1) * .04f, Event.current.type);
            Handles.color = Color.yellow;
            Handles.DotHandleCap(-1, curve.P2, Quaternion.identity, HandleUtility.GetHandleSize(curve.P2) * .04f, Event.current.type);
            Handles.color = Color.blue;
            Handles.DotHandleCap(-1, curve.P3, Quaternion.identity, HandleUtility.GetHandleSize(curve.P3) * .04f, Event.current.type);

            Handles.color = Color.gray;
            Handles.DrawDottedLine(curve.P0, curve.P1, 2f);
            Handles.DrawDottedLine(curve.P1, curve.P2, 2f);
            Handles.DrawDottedLine(curve.P2, curve.P3, 2f);
        }
    }
}