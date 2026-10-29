using System;
using EasternDynasty;

internal static class CityPanoramaCameraChecks
{
    private static int checks;

    private static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception(name);
        checks++;
    }

    private static void Near(double actual, double expected, string name)
    {
        Check(Math.Abs(actual - expected) <= Math.Max(.001d, Math.Abs(expected) * .000001d), name);
    }

    private static void Finite(float value, string name)
    {
        Check(!float.IsNaN(value) && !float.IsInfinity(value), name);
    }

    private static void Bounds(CityPanoramaCamera camera, float width, float height, string name)
    {
        var bounds = camera.Fit(width, height);
        Finite(bounds.X, name + " finite x");
        Finite(bounds.Y, name + " finite y");
        Finite(bounds.Width, name + " finite width");
        Finite(bounds.Height, name + " finite height");
        Near(bounds.Width / 1000d, bounds.Height / 440d, name + " source aspect ratio");
        Axis(bounds.X, bounds.Width, width, name + " horizontal");
        Axis(bounds.Y, bounds.Height, height, name + " vertical");
    }

    private static void Axis(float origin, float contentSize, float viewportSize, string name)
    {
        if (contentSize <= viewportSize)
            Near(origin, (viewportSize - (double)contentSize) * .5d, name + " letterbox centered");
        else
        {
            Check(origin <= .001f, name + " no leading blank edge");
            Check((double)origin + contentSize >= viewportSize - .001d, name + " no trailing blank edge");
        }
    }

    private static void Viewport(float width, float height, string name)
    {
        var camera = new CityPanoramaCamera();
        Near(camera.Zoom, 1, name + " default zoom");
        var original = camera.Fit(width, height);
        Check(original.Width <= width + .001f && original.Height <= height + .001f, name + " contains entire original scene");
        Check(Math.Abs(original.Width - width) <= .001f || Math.Abs(original.Height - height) <= .001f, name + " fits one viewport edge");
        Bounds(camera, width, height, name + " default");
        camera.Pan(8000f, -8000f);
        Bounds(camera, width, height, name + " unzoomed drag");
        Near(camera.PanX, 0, name + " unzoomed horizontal drag constrained");
        Near(camera.PanY, 0, name + " unzoomed vertical drag constrained");
        foreach (float zoom in new[] { 1.25f, 1.5f, 2f, 2.25f })
        {
            camera.SetZoom(zoom);
            camera.Pan(1000000, -1000000);
            Bounds(camera, width, height, name + " zoom " + zoom + " bottom-left");
            camera.Pan(-2000000, 2000000);
            Bounds(camera, width, height, name + " zoom " + zoom + " top-right");
            var transformed = camera.Fit(width, height);
            foreach (float sourceX in new[] { 0f, 83f, 500f, 921f, 1000f })
                Near(((transformed.X + sourceX * transformed.Width / 1000d) - transformed.X) * 1000d / transformed.Width,
                    sourceX, name + " horizontal source coordinate roundtrip");
            foreach (float sourceY in new[] { 0f, 73f, 220f, 417f, 440f })
                Near(((transformed.Y + sourceY * transformed.Height / 440d) - transformed.Y) * 440d / transformed.Height,
                    sourceY, name + " vertical source coordinate roundtrip");
        }
        camera.Reset();
        var reset = camera.Fit(width, height);
        Near(camera.Zoom, 1, name + " reset zoom");
        Near(camera.PanX, 0, name + " reset pan x");
        Near(camera.PanY, 0, name + " reset pan y");
        Near(reset.X, original.X, name + " reset scene x");
        Near(reset.Y, original.Y, name + " reset scene y");
        Near(reset.Width, original.Width, name + " reset scene width");
        Near(reset.Height, original.Height, name + " reset scene height");
    }

    private static void Main()
    {
        OverviewDock();
        // The interactive viewport is unchanged; its original landscape must also
        // cover the new painted canvas (60 above, 30 below, one pixel each side).
        foreach(var panel in new[]{new[]{974f,660f},new[]{1038f,660f},new[]{1392f,660f},
            new[]{1050f,580f},new[]{1385f,580f},new[]{1741f,580f}})
        foreach(float zoom in new[]{1f,1.25f,1.5f,2f,2.25f})
        foreach(float direction in new[]{-1f,0f,1f})
        {
            var backdropCamera=new CityPanoramaCamera();backdropCamera.SetZoom(zoom);
            backdropCamera.Pan(direction*100000,-direction*100000);
            var town=backdropCamera.Fit(panel[0]-2,panel[1]-90);
            float s=town.Width/1000f;
            float x=1+town.X-400*s,y=60+town.Y-220*s;
            Check(x<=.01f&&y<=.01f&&x+1800*s>=panel[0]-.01f&&y+880*s>=panel[1]-.01f,
                "full landscape canvas covered after zoom/drag "+panel[0]+"x"+panel[1]+" zoom="+zoom+" direction="+direction);
        }
        // Exact current panel geometry after logical UI scaling, plus adverse aspect ratios.
        Viewport(1440, 900, "desktop 1440");
        Viewport(1920, 1080, "desktop 1920");
        Viewport(980, 440, "city card");
        Viewport(1390, 582, "1440 desktop expanded city viewport");
        Viewport(1130, 382, "1440 desktop compact city viewport");
        Viewport(1550, 582, "1920 desktop expanded city viewport after 1.2 UI scale");
        Viewport(1290, 382, "1920 desktop compact city viewport after 1.2 UI scale");
        float mobileScale = 684f / 820f;
        float mobileLogicalWidth = 1504f / mobileScale;
        Viewport(mobileLogicalWidth - 50, 502, "1600 phone preview expanded city viewport");
        Viewport(mobileLogicalWidth - 310, 302, "1600 phone preview compact city viewport");
        Viewport(2246, 1048, "landscape phone safe viewport");
        Viewport(1703, 594, "landscape phone logical city viewport");
        Viewport(360, 780, "narrow portrait viewport");
        Viewport(1600, 240, "very wide viewport");
        Viewport(73, 31, "small viewport");
        Viewport(.1f, .044f, "fractional viewport");

        var camera = new CityPanoramaCamera();
        camera.SetZoom(2);
        camera.Pan(40, -30);
        var moved = camera.Fit(1000, 440);
        Near(moved.X, -460, "drag moves the rendered scene right");
        Near(moved.Y, -250, "drag moves the rendered scene up");
        Near(moved.Width, 2000, "double zoom has exact scene width");
        Near(moved.Height, 880, "double zoom has exact scene height");
        camera.Pan(10000, -10000);
        moved = camera.Fit(1000, 440);
        Near(moved.X, 0, "drag right stops at scene left edge");
        Near(moved.Y, -440, "drag up stops at scene bottom edge");
        camera.Pan(-20000, 20000);
        moved = camera.Fit(1000, 440);
        Near(moved.X, -1000, "drag left stops at scene right edge");
        Near(moved.Y, 0, "drag down stops at scene top edge");
        camera.Reset();
        camera.SetZoom(1.5f);
        camera.Pan(80, -40);
        var before = camera.Fit(1000, 440);
        double sourceCenterX = (500d - before.X) * 1000d / before.Width;
        double sourceCenterY = (220d - before.Y) * 440d / before.Height;
        camera.SetZoom(2.25f);
        Near(camera.PanX, 120, "zoom scales horizontal pan");
        Near(camera.PanY, -60, "zoom scales vertical pan");
        var after = camera.Fit(1000, 440);
        Near((500d - after.X) * 1000d / after.Width, sourceCenterX, "zoom preserves center source x");
        Near((220d - after.Y) * 440d / after.Height, sourceCenterY, "zoom preserves center source y");
        camera.SetZoom(1.5f);
        after = camera.Fit(1000, 440);
        Near(after.X, before.X, "zoom out restores x");
        Near(after.Y, before.Y, "zoom out restores y");

        camera.SetZoom(100);
        Near(camera.Zoom, 2.25, "zoom upper clamp");
        camera.SetZoom(-100);
        Near(camera.Zoom, 1, "zoom lower clamp");
        Bounds(camera, 1000, 440, "zoom out re-clamps offsets");
        Near(camera.PanX, 0, "zoom out horizontal centered");
        Near(camera.PanY, 0, "zoom out vertical centered");
        camera.SetZoom(2);
        camera.Pan(100, -50);
        camera.SetZoom(float.NaN);
        camera.SetZoom(float.PositiveInfinity);
        camera.SetZoom(float.NegativeInfinity);
        Near(camera.Zoom, 2, "invalid zoom rejected");
        Near(camera.PanX, 100, "invalid zoom retains pan x");
        Near(camera.PanY, -50, "invalid zoom retains pan y");
        camera.Pan(float.NaN, float.PositiveInfinity);
        Near(camera.PanX, 100, "invalid drag x ignored");
        Near(camera.PanY, -50, "invalid drag y ignored");
        camera.Pan(10, float.NaN);
        Near(camera.PanX, 110, "finite drag axis still applies");
        camera.Pan(float.MaxValue, -float.MaxValue);
        camera.Pan(float.MaxValue, -float.MaxValue);
        Finite(camera.PanX, "accumulated huge drag x finite");
        Finite(camera.PanY, "accumulated huge drag y finite");
        Bounds(camera, 1000, 440, "huge drag clamped");
        Bounds(camera, 320, 900, "resize wide to narrow");
        Bounds(camera, 1900, 180, "resize narrow to wide");
        Bounds(camera, 1, 1, "resize to tiny");
        Bounds(camera, 1368, 674, "resize back to expanded");

        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            var empty = camera.Fit(invalid, 440);
            Near(empty.Width, 0, "invalid width gives empty bounds");
            Near(empty.Height, 0, "invalid width gives empty height");
            empty = camera.Fit(1000, invalid);
            Near(empty.Width, 0, "invalid height gives empty width");
            Near(empty.Height, 0, "invalid height gives empty bounds");
            Near(camera.PanX, 0, "invalid layout resets x");
            Near(camera.PanY, 0, "invalid layout resets y");
        }
        Bounds(camera, 1368, 674, "valid layout recovers after invalid input");
        var huge = camera.Fit(float.MaxValue, float.MaxValue);
        Finite(huge.X, "huge layout x finite");
        Finite(huge.Y, "huge layout y finite");
        Finite(huge.Width, "huge layout width finite");
        Finite(huge.Height, "huge layout height finite");
        var tiny = camera.Fit(float.Epsilon, float.Epsilon);
        Finite(tiny.X, "subnormal layout x finite");
        Finite(tiny.Y, "subnormal layout y finite");
        Finite(tiny.Width, "subnormal layout width finite");
        Finite(tiny.Height, "subnormal layout height finite");
        Console.WriteLine("City panorama camera checks passed: " + checks);
    }

    private static bool Contains(CityPanoramaBounds parent, CityPanoramaBounds child)
    { return child.Width>0&&child.Height>0&&child.X>=parent.X-.01f&&child.Y>=parent.Y-.01f&&child.X+child.Width<=parent.X+parent.Width+.01f&&child.Y+child.Height<=parent.Y+parent.Height+.01f; }

    private static void OverviewDock()
    {
        // Production safe-area mapping and production dock geometry are exercised
        // together. These are logical checks, never substituted for new screenshots.
        foreach(var screen in new[]{new[]{1440f,900f,0f,0f,1440f,900f,0f},new[]{1920f,1080f,0f,0f,1920f,1080f,0f},
            new[]{1600f,720f,56f,18f,1488f,684f,1f},new[]{1280f,720f,0f,0f,1280f,720f,1f},
            new[]{2400f,1080f,80f,20f,2240f,1040f,1f},new[]{1920f,1200f,0f,0f,1920f,1200f,1f},
            new[]{1440f,1080f,0f,0f,1440f,1080f,0f},new[]{2048f,1536f,0f,0f,2048f,1536f,1f}})
        {
            var layout=MobileScreenLayout.Calculate(screen[0],screen[1],screen[2],screen[3],screen[4],screen[5],screen[6]>0);
            float width=layout.LogicalWidth,height=layout.LogicalHeight;
            var safe=new CityPanoramaBounds{Width=width,Height=height};
            var dock=CityOverviewLayout.Dock(width,height);var content=CityOverviewLayout.Content(width,height);
            Check(Contains(safe,dock)&&Contains(safe,content),"overview dock/content remain in safe area");
            Check(dock.X+dock.Width+12<=content.X+.01f,"navigation and scene are separated by a real gutter");
            Check(content.Y>=70&&content.Y+content.Height<=height-78,"overview clears global header/footer");
            Check(content.Y<142&&content.Height>height-240,"overview genuinely reclaims old heading height");
            var back=CityOverviewLayout.Back(width,height);
            Check(Contains(dock,back)&&back.Width>=44&&back.Height>=44,"return stays visible and touchable");
            float previousBottom=dock.Y;
            for(int index=0;index<5;index++)
            {
                var button=CityOverviewLayout.Navigation(width,height,index);
                Check(Contains(dock,button)&&button.Width>=44&&button.Height>=44,"all five navigation entries fit dock");
                Check(button.Y>=previousBottom&&button.Y+button.Height<back.Y,"navigation never overlaps next action or return");
                previousBottom=button.Y+button.Height;
            }
            float rail=CityOverviewLayout.ArmyRailWidth(content.Height);
            float sceneWidth=content.Width-rail-12;
            var actualTown=new CityPanoramaCamera().FitCanvas(sceneWidth-2,content.Height-90,60,30);
            Console.WriteLine("CITY_DOCK_LAYOUT screen={0}x{1} mobile={2} logical={3:0.##}x{4:0.##} scene={5:0.##}x{6:0.##} town={7:0.##}x{8:0.##} rail={9}",
                screen[0],screen[1],screen[6]>0,width,height,sceneWidth,content.Height,actualTown.Width,actualTown.Height,rail);
            Check(sceneWidth>=content.Width*.6f,"panorama remains dominant beside actual army rail");
            // The title, five camera actions and 44px building plaques must still
            // fit after the horizontal tradeoff; full-body art remains proportional.
            Check(sceneWidth-500>=300,"scene caption has room for the full long-name fixture");
            foreach (float toolbarWidth in new[] { sceneWidth-20, content.Width-20 }) {
                var toolbar = new CityPanoramaBounds { X=content.X+10, Y=content.Y+2, Width=toolbarWidth, Height=56 };
                var identity = CityPanoramaToolbarLayout.Title(toolbar);
                Check(Contains(toolbar,identity) && identity.Width<=320, "city identity is independent and capped");
                Check(Contains(identity,CityPanoramaToolbarLayout.TitleArt(toolbar)) && Contains(identity,CityPanoramaToolbarLayout.TitleText(toolbar)), "complete city image and text fit title");
                float previousRight=identity.X+identity.Width;
                for (int action=0;action<5;action++) {
                    var button=CityPanoramaToolbarLayout.Action(toolbar,action);
                    Check(Contains(toolbar,button) && button.Width>=44 && button.Height>=44, "panorama controls remain touchable inside band");
                    Check(button.X>=previousRight+6-.01f, "independent controls never overlap");
                    Check(button.Y+button.Height<=content.Y+60, "toolbar never covers building viewport");
                    if(action!=1&&action!=2) {
                        var art=CityPanoramaToolbarLayout.Art(button);var caption=CityPanoramaToolbarLayout.Caption(button);
                        Check(Contains(button,art)&&Contains(button,caption)&&art.Y+art.Height<caption.Y, "complete image and caption have separate visible regions");
                    }
                    previousRight=button.X+button.Width;
                }
            }
            // The publisher's masonry close-up must expose the entire gate roof,
            // passage and approach using the same real camera as the player.
            // The 44-unit plaque below the southernmost facility must remain
            // visible, while an actual click inside the gate stays clear of it.
            var southernCamera = new CityPanoramaCamera();
            southernCamera.SetZoom(1.5f); southernCamera.Pan(0, -10000);
            var southernViewport = new CityPanoramaBounds { Width = content.Width - 2, Height = content.Height - 90 };
            var expandedTown = new CityPanoramaCamera().FitCanvas(southernViewport.Width, southernViewport.Height, 60, 30);
            float expandedScale = expandedTown.Width / 1000;
            // Real click probes used by Check-Experience on a new city must be visible,
            // and outside the hall's complete body plus fixed-height name plaque.
            float hallX = expandedTown.X + 410 * expandedScale;
            float hallRight = expandedTown.X + 590 * expandedScale;
            float hallTop = expandedTown.Y + 116 * expandedScale;
            float hallBottom = expandedTown.Y + 260 * expandedScale + 26;
            hallX = Math.Min(hallX, expandedTown.X + 500 * expandedScale - 71);
            hallRight = Math.Max(hallRight, expandedTown.X + 500 * expandedScale + 71);
            foreach (var artPoint in new[] { new[] { 520f, 405f }, new[] { 260f, 291f }, new[] { 500f, 315f } })
            {
                var point = new CityPanoramaBounds { X = expandedTown.X + artPoint[0] * expandedScale,
                    Y = expandedTown.Y + artPoint[1] * expandedScale, Width = expandedScale, Height = expandedScale };
                Check(Contains(southernViewport, point), "actual gate/wall/paving click probes remain visible in reset expanded city");
                Check(point.X < hallX || point.X > hallRight || point.Y < hallTop || point.Y > hallBottom,
                    "gate/wall/paving scenery click cannot accidentally open the real hall");
            }
            var southernTown = southernCamera.FitCanvas(southernViewport.Width, southernViewport.Height, 60, 30);
            float gateScale = southernTown.Width / 1000;
            var gate = new CityPanoramaBounds { X = southernTown.X + 437 * gateScale, Y = southernTown.Y + 318 * gateScale,
                Width = 126 * gateScale, Height = 110 * gateScale };
            var passage = new CityPanoramaBounds { X = southernTown.X + 520 * gateScale, Y = southernTown.Y + 405 * gateScale,
                Width = gateScale, Height = gateScale };
            var lastPlaque = new CityPanoramaBounds { X = southernTown.X + 500 * gateScale - 63,
                Y = southernTown.Y + 376 * gateScale - 18, Width = 126, Height = 44 };
            Check(Contains(southernViewport, gate), "masonry close-up shows the complete gate silhouette inside the real viewport");
            Check(Contains(gate, passage), "gate passage click is actually inside its visible gate artwork");
            Check(Contains(southernViewport, lastPlaque), "masonry close-up retains a complete real southern facility plaque");
            Check(passage.Y > lastPlaque.Y + lastPlaque.Height + 4,
                "gate passage click remains below the southernmost real facility and its expanded label hit area");
            Console.WriteLine("CITY_GATE_VIEW screen={0}x{1} mobile={2}; gate=({3:0.##},{4:0.##},{5:0.##},{6:0.##}); viewport={7:0.##}x{8:0.##}; passageFacilityGap={9:0.##}",
                screen[0],screen[1],screen[6]>0,gate.X,gate.Y,gate.Width,gate.Height,southernViewport.Width,southernViewport.Height,
                passage.Y-lastPlaque.Y-lastPlaque.Height);
            foreach(bool expanded in new[]{false,true})
            foreach(float zoom in new[]{1f,1.25f,2.25f})
            foreach(float pan in new[]{-100000f,0f,100000f})
            {
                float panelWidth=expanded?content.Width:sceneWidth;
                var camera=new CityPanoramaCamera();camera.SetZoom(zoom);camera.Pan(pan,-pan);
                var town=camera.FitCanvas(panelWidth-2,content.Height-90,60,30);
                float s=town.Width/1000;
                float x=1+town.X-400*s,y=60+town.Y-220*s;
                Check(x<=.01f&&y<=.01f&&x+1800*s>=panelWidth-.01f&&y+880*s>=content.Height-.01f,"new city canvas has continuous scenery after zoom/drag");
                Near(town.Width/1000,town.Height/440,"new city retains artwork aspect ratio");
            }
        }
        bool rejected=false;try{CityOverviewLayout.Navigation(1440,900,5);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,"nonexistent sixth city entry cannot acquire a target");
    }
}
