using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using EasternDynasty;

internal static class CityArmyLayoutChecks
{
    private static int checks;
    private static List<Asset> portraits;

    [DataContract] private sealed class Manifest
    { [DataMember] public Asset[] entries { get; set; } }

    [DataContract] private sealed class Asset
    {
        [DataMember] public string id { get; set; }
        [DataMember] public string status { get; set; }
        [DataMember] public int width { get; set; }
        [DataMember] public int height { get; set; }
    }

    private static void Check(bool pass, string name)
    { if (!pass) throw new Exception(name); checks++; }

    private static void Near(float actual, float expected, string name)
    { Check(Math.Abs(actual - expected) < .002f, name + ": " + actual + " != " + expected); }

    private static bool Contains(CityPanoramaBounds parent, CityPanoramaBounds child)
    {
        return child.Width > 0 && child.Height > 0 && child.X >= parent.X - .002f && child.Y >= parent.Y - .002f
            && child.X + child.Width <= parent.X + parent.Width + .002f
            && child.Y + child.Height <= parent.Y + parent.Height + .002f;
    }

    private static bool Separated(CityPanoramaBounds a, CityPanoramaBounds b)
    {
        return a.X + a.Width <= b.X + .002f || b.X + b.Width <= a.X + .002f
            || a.Y + a.Height <= b.Y + .002f || b.Y + b.Height <= a.Y + .002f;
    }

    private static int Hit(float width, float height, float x, float y)
    {
        for (int i = 0; i < 3; i++)
        {
            var card = CityArmyLayout.Card(width, height, i);
            if (x >= card.X && x < card.X + card.Width && y >= card.Y && y < card.Y + card.Height) return i;
        }
        return -1;
    }

    private static void Layout(float width, float availableHeight)
    {
        string name = width + "x" + availableHeight;
        float height = CityArmyLayout.Height(width, availableHeight);
        var parent = new CityPanoramaBounds { Width = width, Height = height };
        Check(height <= availableHeight + .002f && height >= 360, name + " compact rail stays inside its original area");
        Check(height < availableHeight - 2, name + " removes actual unused height");
        var blocks = new List<CityPanoramaBounds>();
        float oldArtWidth = (width - 44) / 3 - 4;
        float oldArtHeight = availableHeight - 235;
        for (int i = 0; i < 3; i++)
        {
            var card = CityArmyLayout.Card(width, availableHeight, i);
            var art = CityArmyLayout.Portrait(card);
            var identity = CityArmyLayout.Identity(card);
            Check(Contains(parent, card) && Contains(card, art) && Contains(card, identity), name + " complete card " + i);
            Check(Separated(art, identity), name + " portrait never covers name/level " + i);
            Near(identity.Height, 42, name + " two-line identity " + i);
            Check(art.Height >= 138 && art.Width >= 95, name + " minimum complete painting " + i);
            Near(art.Width, oldArtWidth, name + " old and new art windows retain the same width " + i);
            Near(art.Height / art.Width, 1.45f, name + " portrait window matches S1 source ratio " + i);
            Near(art.Y - card.Y, 4, name + " no excess upper letterbox " + i);
            Near(identity.Y - (art.Y + art.Height), 1, name + " short identity separation " + i);
            // Filled and empty seats both route through the same whole-card geometry;
            // the image and name have independent sample points inside that hit area.
            Check(Hit(width, availableHeight, art.X + art.Width / 2, art.Y + art.Height / 2) == i,
                name + " portrait/empty-seat image hits its full card " + i);
            Check(Hit(width, availableHeight, identity.X + identity.Width / 2, identity.Y + identity.Height / 2) == i,
                name + " name/empty-seat caption hits its full card " + i);
            if (i < 2) Check(Hit(width, availableHeight, card.X + card.Width + 4, card.Y + 10) == -1,
                name + " card gutter does not hit a neighbour " + i);
            foreach (var source in portraits)
            {
                float before = Math.Min(oldArtWidth / source.width, oldArtHeight / source.height);
                float after = Math.Min(art.Width / source.width, art.Height / source.height);
                float drawnWidth = source.width * after, drawnHeight = source.height * after;
                Check(drawnWidth <= art.Width + .002f && drawnHeight <= art.Height + .002f,
                    name + " full uncropped portrait " + source.id);
                Near(drawnHeight / drawnWidth, source.height / (float)source.width, name + " source aspect " + source.id);
                Check(after + .00001f >= before, name + " actual S1 portrait size does not shrink " + source.id);
            }
            blocks.Add(card);
        }
        var strength = CityArmyLayout.Strength(width, availableHeight);
        var supply = CityArmyLayout.Supply(width, availableHeight);
        Check(Contains(parent, strength) && Contains(parent, supply), name + " strength and real costs fit");
        Near(strength.Height, 28, name + " strength height");
        Near(supply.Height, 40, name + " supply height");
        blocks.Add(strength); blocks.Add(supply);
        for (int i = 0; i < 3; i++)
        {
            var action = CityArmyLayout.Action(width, availableHeight, i);
            Check(Contains(parent, action) && action.Width >= 44, name + " command remains inside rail " + i);
            Near(action.Height, 44, name + " command touch target " + i);
            blocks.Add(action);
        }
        for (int i = 0; i < blocks.Count; i++)
        for (int j = i + 1; j < blocks.Count; j++)
            Check(Separated(blocks[i], blocks[j]), name + " independent sections never overlap " + i + "/" + j);
        Near(blocks[7].Y + blocks[7].Height, height - 6, name + " final command clears bottom edge");
    }

    private static void Screen(float[] screen)
    {
        var layout = MobileScreenLayout.Calculate(screen[0], screen[1], screen[2], screen[3], screen[4], screen[5], screen[6] > 0);
        var content = CityOverviewLayout.Content(layout.LogicalWidth, layout.LogicalHeight);
        float width = CityOverviewLayout.ArmyRailWidth(content.Height), availableHeight = content.Height - 230;
        Layout(width, availableHeight);
        float originX = content.X + content.Width - width, originY = content.Y + 230;
        var logicalRail = new CityPanoramaBounds { X = originX, Y = originY, Width = width, Height = CityArmyLayout.Height(width, availableHeight) };
        Check(Contains(content, logicalRail), "army rail fits production city content after safe-area scaling");
        var safe = new CityPanoramaBounds { X = layout.X, Y = layout.Y, Width = layout.Width, Height = layout.Height };
        var pixels = new CityPanoramaBounds { X = layout.X + originX * layout.Scale, Y = layout.Y + originY * layout.Scale,
            Width = width * layout.Scale, Height = logicalRail.Height * layout.Scale };
        Check(Contains(safe, pixels), "all army content stays inside physical screen safe area");
        Console.WriteLine("CITY_ARMY_LAYOUT screen={0}x{1} mobile={2} rail={3:0.##}x{4:0.##} originalHeight={5:0.##}",
            screen[0], screen[1], screen[6] > 0, width, logicalRail.Height, availableHeight);
    }

    private static void Main(string[] args)
    {
        string root = args.Length == 1 ? args[0] : Directory.GetCurrentDirectory();
        Manifest manifest;
        using (var stream = File.OpenRead(Path.Combine(root, "Assets/EasternDynasty/Resources/asset-manifest.json")))
            manifest = (Manifest)new DataContractJsonSerializer(typeof(Manifest)).ReadObject(stream);
        portraits = new List<Asset>();
        foreach (var entry in manifest.entries)
            if (entry.id.StartsWith("portrait.s1_", StringComparison.Ordinal) && entry.status == "ready")
            {
                Check(entry.width > 0 && entry.height > 0, "real S1 portrait dimensions " + entry.id);
                Check(entry.height / (float)entry.width <= 1.45f + .00001f, "manifest portrait fits chosen complete-art ratio " + entry.id);
                portraits.Add(entry);
            }
        Check(portraits.Count >= 245, "all existing S1 catalog paintings participate");
        foreach (float width in new[] { 342f, 342.5f, 354f, 390.25f, 406f })
        foreach (float height in new[] { 410f, 450f, 490f }) Layout(width, height);
        foreach (var screen in new[] {
            new[] {1440f,900f,0f,0f,1440f,900f,0f}, new[] {1280f,720f,0f,0f,1280f,720f,0f},
            new[] {1920f,1080f,0f,0f,1920f,1080f,0f}, new[] {1600f,720f,56f,18f,1488f,684f,1f},
            new[] {1280f,720f,0f,0f,1280f,720f,1f}, new[] {2400f,1080f,80f,20f,2240f,1040f,1f},
            new[] {1920f,1200f,0f,0f,1920f,1200f,1f}, new[] {1440f,1080f,0f,0f,1440f,1080f,0f},
            new[] {2048f,1536f,0f,0f,2048f,1536f,1f} }) Screen(screen);
        foreach (int index in new[] { -1, 3 })
        {
            bool cardRejected = false, actionRejected = false;
            try { CityArmyLayout.Card(342, 410, index); } catch (ArgumentOutOfRangeException) { cardRejected = true; }
            try { CityArmyLayout.Action(342, 410, index); } catch (ArgumentOutOfRangeException) { actionRejected = true; }
            Check(cardRejected && actionRejected, "invalid seat/command index cannot spill into another section");
        }
        Console.WriteLine("City army layout checks passed: " + checks + "; " + portraits.Count + " actual S1 portrait sizes; geometry only, runtime font/input/screenshot checks remain required.");
    }
}
