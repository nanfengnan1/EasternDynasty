using System;
using System.Collections.Generic;

namespace EasternDynasty
{
    /// <summary>Presentation geometry shared by the atlas and standalone checks; no campaign effects.</summary>
    public static class MinimapViewportGeometry
    {
        public struct Point
        {
            public float x, y;
            public Point(float x, float y) { this.x = x; this.y = y; }
        }

        // Keep the actual camera-ground polygon. Clamping each original corner would
        // fabricate diagonal borders where the visible footprint crosses a map edge.
        public static void ClipToMap(List<Point> polygon)
        {
            if (polygon == null) throw new ArgumentNullException("polygon");
            foreach (Point point in polygon)
                if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y))
                    throw new ArgumentException("Camera footprint coordinates must be finite.", "polygon");
            for (int edge = 0; edge < 4 && polygon.Count > 0; edge++)
            {
                Point[] input = polygon.ToArray(); polygon.Clear();
                Point a = input[input.Length - 1]; double da = EdgeDistance(a, edge);
                foreach (Point b in input)
                {
                    double db = EdgeDistance(b, edge);
                    if ((da >= 0) != (db >= 0)) polygon.Add(Intersection(a, b, da / (da - db), edge));
                    if (db >= 0) polygon.Add(b);
                    a = b; da = db;
                }
            }
        }

        private static double EdgeDistance(Point point, int edge)
        { return edge == 0 ? point.x : edge == 1 ? 1.0 - point.x : edge == 2 ? point.y : 1.0 - point.y; }

        private static Point Intersection(Point a, Point b, double t, int edge)
        {
            // The constrained component is exactly the map boundary. Interpolating
            // it with single-precision Lerp left tiny negative coordinates in real
            // navigation captures. Double precision keeps the other component on
            // its segment without altering any untouched camera vertex.
            return edge < 2
                ? new Point(edge == 0 ? 0 : 1, (float)(a.y + ((double)b.y - a.y) * t))
                : new Point((float)(a.x + ((double)b.x - a.x) * t), edge == 2 ? 0 : 1);
        }
    }
}
