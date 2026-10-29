using System;
using System.Collections.Generic;
using System.Reflection;
using EasternDynasty;

internal static class UiLineRasterChecks
{
    private static int checks, cases;
    private static void Check(bool passed, string message)
    { if (!passed) throw new Exception(message); checks++; }
    private static bool Near(double a, double b, double tolerance = .002d)
    { return Math.Abs(a - b) <= tolerance; }
    private static List<UiLineStrip> Draw(float ax, float ay, float bx, float by, float thickness)
    {
        var raster = new UiLineRaster(ax, ay, bx, by, thickness);
        var result = new List<UiLineStrip>();
        UiLineStrip strip;
        while (raster.MoveNext(out strip))
        {
            result.Add(strip);
            if (result.Count > UiLineRaster.MaximumStrips) throw new Exception("Unbounded scan loop");
        }
        Check(!raster.MoveNext(out strip), "Exhausted raster remains exhausted");
        return result;
    }
    private static void Same(UiLineStrip a, UiLineStrip b, float dx, float dy, string name)
    {
        Check(Near(a.X + dx, b.X) && Near(a.Y + dy, b.Y) &&
            Near(a.Width, b.Width) && Near(a.Height, b.Height), name + " expected " + (a.X + dx) + "," + (a.Y + dy) + "," + a.Width + "," + a.Height + " actual " + b.X + "," + b.Y + "," + b.Width + "," + b.Height);
    }
    private static double Distance(float px, float py, UiLineStrip s)
    {
        double dx = Math.Max(s.X - px, Math.Max(0d, px - s.X - s.Width));
        double dy = Math.Max(s.Y - py, Math.Max(0d, py - s.Y - s.Height));
        return Math.Sqrt(dx * dx + dy * dy);
    }
    private static void Stroke(float ax, float ay, float bx, float by, float thickness)
    {
        cases++;
        var strips = Draw(ax, ay, bx, by, thickness);
        var reversed = Draw(bx, by, ax, ay, thickness);
        // Parent groups, scroll offsets, and safe-area translations cannot alter
        // the local stroke's geometry. The renderer never changes their matrix.
        var shifted = Draw(ax + 601.25f, ay - 317.5f, bx + 601.25f, by - 317.5f, thickness);
        Check(strips.Count > 0 && strips.Count == reversed.Count && strips.Count == shifted.Count,
            "Stroke survives direction reversal and parent translation");
        double dx = bx - (double)ax, dy = by - (double)ay;
        double length = Math.Sqrt(dx * dx + dy * dy), half = thickness * .5d;
        double minX = Math.Min(ax, bx) - Math.Abs(dy / length * half);
        double maxX = Math.Max(ax, bx) + Math.Abs(dy / length * half);
        double minY = Math.Min(ay, by) - Math.Abs(dx / length * half);
        double maxY = Math.Max(ay, by) + Math.Abs(dx / length * half);
        double area = 0;
        for (int i = 0; i < strips.Count; i++)
        {
            var s = strips[i];
            Check(s.Width > 0 && s.Height > 0 && !float.IsInfinity(s.Width) && !float.IsInfinity(s.Height), "Finite positive geometry");
            Check(s.X >= minX - .002d && s.Y >= minY - .002d &&
                s.X + (double)s.Width <= maxX + .002d && s.Y + (double)s.Height <= maxY + .002d,
                "No strip escapes the stroke's projected bounds");
            double cx = s.X + s.Width * .5d - ax, cy = s.Y + s.Height * .5d - ay;
            Check(Math.Abs((cx * -dy + cy * dx) / length) <= half + .002d &&
                (cx * dx + cy * dy) / length >= -.002d &&
                (cx * dx + cy * dy) / length <= length + .002d,
                "Strip center belongs to the requested butt-capped stroke");
            Same(s, reversed[i], 0, 0, "Reversal preserves stroke pixels");
            Same(s, shifted[i], 601.25f, -317.5f, "Scroll/group translation preserves stroke shape");
            if (i > 0)
            {
                var p = strips[i - 1];
                Check(Near(p.Y + p.Height, s.Y) || Near(p.X + p.Width, s.X), "Adjacent scan strips meet without overdraw gaps");
            }
            area += s.Width * (double)s.Height;
        }
        // Midpoint strips approximate edge triangles; the total permitted error
        // is tied to the subpixel band, not proportional to endpoint coordinates.
        Check(Math.Abs(area - length * thickness) <= Math.Max(.6d, length * thickness * .06d),
            "Integrated stroke thickness remains within raster tolerance");
        foreach (float fraction in new[] { 0f, .1f, .25f, .5f, .75f, .9f, 1f })
        {
            float px = ax + (bx - ax) * fraction, py = ay + (by - ay) * fraction;
            double distance = double.PositiveInfinity;
            foreach (var s in strips) distance = Math.Min(distance, Distance(px, py, s));
            Check(distance <= .76d, "Both endpoints and centerline remain covered to subpixel tolerance");
        }
    }
    private static void Main()
    {
        var horizontal = Draw(7, 12, 39, 12, 4);
        Check(horizontal.Count == 1, "Horizontal line uses one rectangle");
        Same(horizontal[0], new UiLineStrip { X = 7, Y = 10, Width = 32, Height = 4 }, 0, 0, "Horizontal exact thickness and endpoints");
        var vertical = Draw(-3, 8, -3, -19, 2);
        Check(vertical.Count == 1, "Vertical line uses one rectangle");
        Same(vertical[0], new UiLineStrip { X = -4, Y = -19, Width = 2, Height = 27 }, 0, 0, "Vertical exact thickness and endpoints");
        foreach (float thickness in new[] { .35f, 1.5f, 2f, 5f })
        foreach (float length in new[] { 2f, 21f, 76f })
        for (int degrees = 0; degrees < 360; degrees += 15)
        {
            double angle = degrees * Math.PI / 180d;
            Stroke(-31.25f, 47.5f, -31.25f + (float)(Math.Cos(angle) * length), 47.5f + (float)(Math.Sin(angle) * length), thickness);
        }
        // Same icon geometry at desktop and mobile logical scales.
        foreach (float scale in new[] { .5f, .75f, 1f, 1.4f, 2f })
            Stroke(13 * scale, 19 * scale, 31 * scale, 54 * scale, 1.5f * scale);
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Check(Draw(invalid, 0, 1, 1, 1).Count == 0, "Invalid start X rejected");
            Check(Draw(0, invalid, 1, 1, 1).Count == 0, "Invalid start Y rejected");
            Check(Draw(0, 0, invalid, 1, 1).Count == 0, "Invalid end X rejected");
            Check(Draw(0, 0, 1, invalid, 1).Count == 0, "Invalid end Y rejected");
            Check(Draw(0, 0, 1, 1, invalid).Count == 0, "Invalid thickness rejected");
        }
        Check(Draw(3, 3, 3, 3, 2).Count == 0, "Coincident endpoints are empty");
        Check(Draw(0, 0, 20, 20, 0).Count == 0, "Zero thickness is empty");
        Check(Draw(0, 0, 20, 20, -2).Count == 0, "Negative thickness is empty");
        Check(Draw(0, 0, 1000000, 1000000, 3).Count <= UiLineRaster.MaximumStrips, "Extreme input has bounded work");
        Check(Draw(float.MaxValue, float.MaxValue, 0, 0, float.MaxValue).Count == 0, "Unrepresentable bounds rejected");
        Check(typeof(UiLineRaster).IsValueType && typeof(UiLineStrip).IsValueType, "Runtime iteration uses value types");
        foreach (var field in typeof(UiLineRaster).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            Check(field.FieldType.IsValueType, "Raster owns no per-frame heap collection");
        Console.WriteLine("PASS: UI line raster {0} checks; {1} directional/thickness/scale cases. Pure local geometry only; Unity clip, appearance and performance await publication screenshots.", checks, cases);
    }
}
