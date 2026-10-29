using System;
using System.Collections;
using System.Collections.Generic;

// This scheduler executes the extracted production capture coroutine. It models
// Unity's Update -> coroutine -> OnGUI -> EndOfFrame sequence, not game pixels.
namespace UnityEngine
{
    public sealed class WaitForEndOfFrame { }
    public static class Application { public static string persistentDataPath = "synthetic-no-files"; }
    public static class JsonUtility { public static string ToJson(object value) { return value.ToString(); } }
    public static class ScreenCapture
    {
        public static Action<string> Capture;
        public static void CaptureScreenshot(string path) { Capture(path); }
    }
}

namespace EasternDynasty
{
    using UnityEngine;
    public sealed partial class EasternDynastyGame
    {
        private sealed class Land { }
        private sealed class World
        {
            public int revision;
            public List<Land> lands = new List<Land> { new Land() };
            public override string ToString() { return revision.ToString(); }
        }
        private sealed class Map { public int selected = 0; }
        private readonly World world = new World();
        private readonly Map map = new Map();
        private readonly Dictionary<string,int> reviewTargets = new Dictionary<string,int>();
        private bool reviewMode, mobileMode, moving, atEndOfFrame;
        private float speed = 2, targetX = 760;
        private float width, height;
        private int frame, drawnFrame = -1, assertionFrame = -1, captureFrame = -1, assertions, captures;
        private string fault;
        private LandSelectionLayout.Placement drawn;
        private string LandVisualState(Land land) { return "neutral"; }

        private void DrawFrame()
        {
            atEndOfFrame = true;
            reviewTargets.Clear();
            drawnFrame = frame;
            drawn = LandSelectionLayout.Place(width, height, targetX, 280, true, "neutral");
            if (fault == "displaced") drawn.Facts.X += 3;
            if (reviewMode && fault != "missing") reviewTargets.Add("land-panel", frame);
            if (fault == "campaign") world.revision++;
        }

        private void AdvanceUpdate()
        {
            // The previous frame has already drawn before the next Update starts.
            DrawFrame(); frame++; atEndOfFrame = false;
            if (moving) targetX += 1.25f;
        }

        private void AssertLandVisualTargets()
        {
            assertions++;
            if (!reviewTargets.ContainsKey("land-panel")) throw new Exception("Visible land target missing");
            if (moving && drawnFrame != frame)
                throw new Exception("Stale target observation after camera Update");
            var expected = LandSelectionLayout.Place(width, height, targetX, 280, true, "neutral");
            if (Math.Abs(drawn.Facts.X - expected.Facts.X) > .1f
                || Math.Abs(drawn.Facts.Y - expected.Facts.Y) > .1f)
                throw new Exception("Actual drawn land position differs from selected tile projection");
            assertionFrame = frame;
        }

        // The old wrapper called this actual scheduling pattern and asserted only
        // after its final null yield. The mutation run below must reproduce failure.
        private IEnumerator CaptureState(string filename)
        {
            for (int i = 0; i < 5; i++) yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(filename);
            yield return null;
        }

        private void Drain(IEnumerator routine)
        {
            try
            {
                while (routine.MoveNext())
                {
                    var nested = routine.Current as IEnumerator;
                    if (nested != null) Drain(nested);
                    else if (routine.Current is WaitForEndOfFrame) DrawFrame();
                    else AdvanceUpdate();
                }
            }
            finally
            {
                var disposable = routine as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
        }

        private static void RunCase(bool initialReview, int screenWidth, int screenHeight,
            bool moving, bool screenshot, string fault)
        {
            var game = new EasternDynastyGame { reviewMode = initialReview, width = screenWidth,
                height = screenHeight, mobileMode = screenWidth == 1600, moving = moving, fault = fault };
            ScreenCapture.Capture = path =>
            {
                if (fault == "capture") throw new Exception("Synthetic screenshot failure");
                if (!game.atEndOfFrame || game.assertionFrame != game.frame)
                    throw new Exception("Screenshot lacks same-frame geometry validation");
                game.captures++; game.captureFrame = game.frame;
            };
            string error = null;
            try { game.Drain(game.CaptureLandVisualState(screenshot ? "smoke-land.png" : null)); }
            catch (Exception failure) { error = failure.Message; }
            string expected = fault == "missing" ? "Visible land target missing"
                : fault == "displaced" ? "drawn land position differs"
                : fault == "campaign" ? "changed the campaign"
                : fault == "capture" ? "Synthetic screenshot failure" : null;
            if (expected == null && error != null || expected != null && (error == null || !error.Contains(expected)))
                throw new Exception("Unexpected capture result: review=" + initialReview + " size=" + screenWidth
                    + " moving=" + moving + " screenshot=" + screenshot + " fault=" + fault + ": " + error);
            if (game.reviewMode != initialReview || game.speed != 2)
                throw new Exception("Capture leaked observation mode or campaign speed");
            if (!initialReview && game.reviewTargets.Count != 0)
                throw new Exception("Inactive review retained observation targets");
            if (expected == null && (game.assertions != 1 || game.captures != (screenshot ? 1 : 0)
                || screenshot && game.captureFrame != game.assertionFrame))
                throw new Exception("Capture did not assert exactly once at its screenshot frame");
            if ((fault == "missing" || fault == "displaced") && (error == null || !error.Contains("capture=") || !error.Contains("state=neutral")))
                throw new Exception("Failed observation lost capture/state context");
        }

        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 1 && args[0] == "--require-stale-observation")
                {
                    var game = new EasternDynastyGame { width = 1280, height = 720, moving = true, fault = "" };
                    bool reproduced = false;
                    try { game.Drain(game.CaptureLandVisualState(null)); }
                    catch (Exception error) { reproduced = error.Message.Contains("Stale target observation after camera Update"); }
                    if (!reproduced) throw new Exception("Old scheduling mutation did not reproduce the stale camera/target observation.");
                    Console.WriteLine("LAND_VISUAL_OLD_SCHEDULE_REPRODUCED: moving camera resumes before new OnGUI observations at 1280x720; assertion remains strict.");
                    return 0;
                }
                int count = 0;
                foreach (bool review in new[] { false, true })
                foreach (int screenWidth in new[] { 1280, 1440, 1600 })
                foreach (bool moving in new[] { false, true })
                foreach (bool screenshot in new[] { false, true })
                foreach (string fault in new[] { "", "missing", "displaced", "campaign", "capture" })
                {
                    if (!screenshot && fault == "capture") continue;
                    RunCase(review, screenWidth, screenWidth == 1440 ? 900 : 720, moving, screenshot, fault); count++;
                }
                Console.WriteLine("LAND_VISUAL_DIAGNOSTICS_OK: " + count + " pure frame-order cases; moving/static camera, three viewport budgets, capture/assert-only, missing/displaced targets, campaign mutation and screenshot failure; no Unity or pixel claim.");
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
    }
}
