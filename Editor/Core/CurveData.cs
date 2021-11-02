using System;

namespace UnityEditor.Splines
{
    [Serializable]
    struct CurveData
    {
        public static readonly CurveData empty = new CurveData
        {
            a = null,
            b = null
        };

        public EditableKnot a { get; private set; }
        public EditableKnot b { get; private set; }

        public CurveData(EditableKnot firstKnot)
        {
            a = firstKnot;

            //If first knot is last knot of the spline, use index 0 for the closing curve
            var path = firstKnot.spline;
            int nextIndex = firstKnot.index + 1;
            if (nextIndex >= path.knotCount)
                nextIndex = 0;
                
            b = path.GetKnot(nextIndex);
        }
        
        public CurveData(EditableKnot firstKnot, EditableKnot lastKnot)
        {
            a = firstKnot;
            b = lastKnot;
        }

        public CurveData(IEditableSpline spline, int firstIndex) : this(spline.GetKnot(firstIndex)){}

        public bool IsValid()
        {
            return a != null && b != null;
        }
    }
}
