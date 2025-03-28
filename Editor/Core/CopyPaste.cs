using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Object = UnityEngine.Object;

namespace UnityEditor.Splines
{
    static class CopyPaste
    {
        // JSONUtility needs a root object to serialize
        [Serializable]
        class CopyPasteBuffer
        {
            public SerializedSpline[] Splines;
            public SerializedLink[] Links;
        }

        [Serializable]
        struct SerializedKnot
        {
            public BezierKnot Knot;
            public TangentMode Mode;
            public float Tension;

            public SerializedKnot(SelectableKnot knot)
            {
                Knot = knot.GetBezierKnot(false);
                Mode = knot.Mode;
                Tension = knot.Tension;
            }
        }

        [Serializable]
        class SerializedSpline
        {
            public float4x4 Transform;
            public bool Closed;
            public SerializedKnot[] Knots;
        }

        [Serializable]
        class SerializedLink
        {
            public SplineKnotIndex[] Indices;
        }

        public static bool IsSplineCopyBuffer(string contents)
        {
            if (string.IsNullOrEmpty(contents))
                return false;


            var buffer = new CopyPasteBuffer();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(contents, buffer);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return buffer.Splines?.Length > 0;
        }

        static int CompareKnot(SelectableKnot a, SelectableKnot b)
        {
            var compareTarget = (int)math.sign(a.SplineInfo.Object.GetInstanceID() - b.SplineInfo.Object.GetInstanceID());
            if (compareTarget != 0)
                return compareTarget;

            var compareSpline = (int)math.sign(a.SplineInfo.Index - b.SplineInfo.Index);
            if (compareSpline != 0)
                return compareSpline;

            return (int)math.sign(a.KnotIndex - b.KnotIndex);
        }

        public static string Copy(IEnumerable<SelectableKnot> selection)
        {
            SerializedKnot[] ToArray(SelectableKnot[] original)
            {
                var result = new SerializedKnot[original.Length];
                for (int i = 0; i < result.Length; ++i)
                    result[i] = new SerializedKnot(original[i]);
                return result;
            }

            void Flatten(List<SelectableKnot[]> arrays, List<SelectableKnot> results)
            {
                results.Clear();
                foreach (var knotArray in arrays)
                    results.AddRange(knotArray);
            }

            Dictionary<SelectableKnot, SplineKnotIndex> knotToSerializedIndex = new Dictionary<SelectableKnot, SplineKnotIndex>();
            List<SerializedSpline> splines = new List<SerializedSpline>();
            List<SelectableKnot> originalKnots = new List<SelectableKnot>(selection);

            var connectedKnots = GetConnectedKnots(originalKnots);
            foreach (var connectedKnotArray in connectedKnots)
            {
                // Skip Orphan Knots
                if (connectedKnotArray.Length < 2)
                    continue;

                var splineInfo = connectedKnotArray[0].SplineInfo;
                splines.Add(new SerializedSpline
                {
                    Closed = splineInfo.Spline.Closed && connectedKnotArray.Length == splineInfo.Spline.Count,
                    Knots = ToArray(connectedKnotArray),
                    Transform = splineInfo.LocalToWorld
                });

                for (int i = 0; i < connectedKnotArray.Length; ++i)
                    knotToSerializedIndex.Add(connectedKnotArray[i], new SplineKnotIndex(splines.Count - 1, i));
            }

            // Add the links
            List<SplineKnotIndex> indices = new List<SplineKnotIndex>();
            List<SerializedLink> links = new List<SerializedLink>();
            List<SelectableKnot> knots = new List<SelectableKnot>();

            // Update the original knots array with the removal of orphan knots
            Flatten(connectedKnots, originalKnots);

            foreach (var originalKnot in originalKnots)
            {
                EditorSplineUtility.GetKnotLinks(originalKnot, knots);
                indices.Clear();
                foreach (var knot in knots)
                {
                    if (knotToSerializedIndex.TryGetValue(knot, out var index))
                    {
                        indices.Add(index);

                        //Remove the pair to ensure we don't get duplicates for every knot in the same link
                        knotToSerializedIndex.Remove(knot);
                    }
                }

                // Only serialized the link if at least 2 copied knots were linked together
                if (indices.Count >= 2)
                    links.Add(new SerializedLink {Indices = indices.ToArray()});
            }

            if (splines.Count == 0)
                return string.Empty;

            CopyPasteBuffer buffer = new CopyPasteBuffer
            {
                Splines = splines.ToArray(),
                Links = links.ToArray(),
            };

            return EditorJsonUtility.ToJson(buffer);
        }

        static List<SelectableKnot[]> GetConnectedKnots(List<SelectableKnot> knots)
        {
            if (knots.Count == 0)
                return new List<SelectableKnot[]>();

            knots.Sort(CompareKnot);

            List<SelectableKnot[]> results = new List<SelectableKnot[]>();
            List<SelectableKnot> connected = new List<SelectableKnot> { knots[0] };

            for (int i = 1; i < knots.Count; ++i)
            {
                var previous = connected[^1];
                var current = knots[i];

                // Check if adjacent and on the same spline as previous
                if (!previous.SplineInfo.Equals(current.SplineInfo)
                    || previous.KnotIndex + 1 != current.KnotIndex)
                {
                    results.Add(connected.ToArray());
                    connected.Clear();
                }

                connected.Add(current);
            }

            results.Add(connected.ToArray());

            // Merge ends if the spline is closed and first and last knots are connected
            for (int i = 0; i < results.Count; ++i)
            {
                var firstKnot = results[i][0];
                if (firstKnot.KnotIndex == 0 && firstKnot.SplineInfo.Spline.Closed)
                {
                    // Look for the last knot on the same spline
                    for (int j = i + 1; j < results.Count; ++j)
                    {
                        var lastKnot = results[j][^1];

                        // Early exit if not on the same spline
                        if (!lastKnot.SplineInfo.Equals(firstKnot.SplineInfo))
                            break;

                        if (lastKnot.KnotIndex == lastKnot.SplineInfo.Spline.Count - 1)
                        {
                            // combine both arrays
                            var a = results[j];
                            var b = results[i];

                            var newArray = new SelectableKnot[a.Length + b.Length];
                            Array.Copy(a, newArray, a.Length);
                            Array.Copy(b, 0, newArray, a.Length, b.Length);
                            results[i] = newArray;
                            results.RemoveAt(j);
                            break;
                        }
                    }
                }
            }

            return results;
        }

        // Paste will create all new splines in the first active ISplineContainer in the selection.
        // Duplicate will try to create new splines in the same container that the knots were copied from.
        public static void Paste(string copyPasteBuffer)
        {
            ISplineContainer target = Selection.GetFiltered<ISplineContainer>(SelectionMode.TopLevel).FirstOrDefault() ??
                                      ObjectFactory.CreateGameObject("New Spline", typeof(SplineContainer)).GetComponent<SplineContainer>();

            Paste(copyPasteBuffer, target);
        }

        public static void Paste(string copyPasteBuffer, ISplineContainer target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (string.IsNullOrEmpty(copyPasteBuffer))
                return;

            var buffer = new CopyPasteBuffer();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(copyPasteBuffer, buffer);
            }
            catch (ArgumentException)
            {
                //If the copy buffer wasn't for a spline copy buffer, we just don't do anything
                return;
            }

            var selection = new List<SelectableKnot>();

            var inverse = (target is Component component)
                ? component.transform.localToWorldMatrix.inverse
                : Matrix4x4.identity;

            var branches = new List<Spline>(target.Splines);
            int splineIndexOffset = branches.Count;

            foreach (var serialized in buffer.Splines)
            {
                var knots = serialized.Knots;
                var spline = new Spline(knots.Length);
                spline.Closed = serialized.Closed;
                var trs = serialized.Transform;
                var index = branches.Count;
                var info = new SplineInfo(target, index);
                branches.Add(spline);
                for (int i = 0, c = knots.Length; i < c; ++i)
                {
                    spline.Add(knots[i].Knot.Transform(math.mul(inverse, trs)), knots[i].Mode, knots[i].Tension);
                    selection.Add(new SelectableKnot(info, i));
                }
            }

            if (target is Object obj)
                Undo.RecordObject(obj, "Paste Knots");

            target.Splines = branches;

            foreach (var link in buffer.Links)
            {
                var firstIndex = link.Indices[0];
                firstIndex.Spline += splineIndexOffset;

                for (int i = 1; i < link.Indices.Length; ++i)
                {
                    var indexPair = link.Indices[i];
                    indexPair.Spline += splineIndexOffset;
                    target.KnotLinkCollection.Link(firstIndex, indexPair);
                }
            }

            SplineSelection.Clear();
            SplineSelection.AddRange(selection);
            SceneView.RepaintAll();
        }
    }
}
