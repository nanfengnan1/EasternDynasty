using System;
using EasternDynasty;
using Box = EasternDynasty.CardPortraitDesignLayout.Box;

internal static class CardChromeChecks
{
    private static int checks;
    private static void Check(bool pass, string message)
    { if (!pass) throw new Exception(message); checks++; }
    private static bool Contains(Box outer, Box inner)
    { return inner.W > 0 && inner.H > 0 && inner.X >= outer.X-.002f && inner.Y >= outer.Y-.002f && inner.X+inner.W <= outer.X+outer.W+.002f && inner.Y+inner.H <= outer.Y+outer.H+.002f; }
    private static bool Separate(Box a, Box b)
    { return a.X+a.W <= b.X+.002f || b.X+b.W <= a.X+.002f || a.Y+a.H <= b.Y+.002f || b.Y+b.H <= a.Y+.002f; }
    private static bool Same(Box a, Box b)
    { return Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)+Math.Abs(a.W-b.W)+Math.Abs(a.H-b.H)<.005f; }
    private static Box Physical(MobileScreenLayout layout, Box box)
    { return new Box(layout.X+box.X*layout.Scale,layout.Y+box.Y*layout.Scale,box.W*layout.Scale,box.H*layout.Scale); }

    private static void Screen(float[] screen)
    {
        var layout=MobileScreenLayout.Calculate(screen[0],screen[1],screen[2],screen[3],screen[4],screen[5],screen[6]>0);
        float panelWidth=Math.Min(1280,layout.LogicalWidth-64),panelHeight=Math.Min(820,layout.LogicalHeight-28);
        float panelX=(layout.LogicalWidth-panelWidth)/2,panelY=(layout.LogicalHeight-panelHeight)/2;
        var portrait=new Box(panelX+20,panelY+14,panelWidth*.43f,panelHeight-28);
        float x=portrait.X+portrait.W+28,width=panelX+panelWidth-x-24;
        var right=new Box(x,panelY,width,panelHeight);
        var nav=CardChromeLayout.Nav(right);var identity=CardChromeLayout.Identity(right);
        var viewport=CardChromeLayout.Viewport(right);var close=CardChromeLayout.Close(right);
        var back=CardChromeLayout.Return(right);var manage=CardChromeLayout.Manage(right);
        var note=CardChromeLayout.FooterNote(right);
        var safe=new Box(layout.X,layout.Y,layout.Width,layout.Height);
        Check(Same(nav,new Box(x,panelY+24,width-66,54)),"navigation occupies the inspected single top row");
        Check(Same(identity,new Box(x,panelY+90,width,40)),"identity has its complete 40-high band below navigation");
        Check(Same(viewport,new Box(x,panelY+142,width,panelHeight-226)),"content begins at the inspected 142-unit anchor");
        Check(Same(close,new Box(x+width-54,panelY+24,54,54)),"close occupies a separate square after the three tabs");
        Check(Same(back,new Box(x+width-180,panelY+panelHeight-62,180,54)),"return retains the independent lower-right action");
        Check(Same(manage,new Box(x,panelY+panelHeight-62,Math.Min(240,width-196),54)),"owned manage retains the separate lower-left action");
        Check(Same(note,new Box(x,panelY+panelHeight-58,width-196,44)),"preview status retains two-line text space outside the return action");
        var oldViewport=new Box(x,panelY+198,width,panelHeight-282);
        Check(!Same(viewport,oldViewport)&&Math.Abs(viewport.Y-oldViewport.Y+56)<.002f&&Math.Abs(viewport.H-oldViewport.H-56)<.002f,"old tall header is rejected and exactly 56 logical pixels return to visible content");
        Check(viewport.H>=CardDossierDesignLayout.ContentHeight,"both actual skill actions and all attributes fit the first viewport");
        Check(Separate(note,back)&&Separate(note,viewport)&&Contains(right,note),"preview status cannot cover content or return");
        var parts=new[]{CardChromeLayout.Tab(right,0),CardChromeLayout.Tab(right,1),CardChromeLayout.Tab(right,2),close,identity,viewport,back,manage};
        for(int i=0;i<parts.Length;i++)
        {
            Check(Contains(right,parts[i])&&Contains(safe,Physical(layout,parts[i])),"chrome region fits actual desktop/mobile safe area: "+i);
            Check(Separate(portrait,parts[i]),"chrome never occludes the unchanged full portrait: "+i);
            for(int j=0;j<i;j++) Check(Separate(parts[i],parts[j]),"visible chrome and touch regions cannot overlap: "+i+"/"+j);
            if(i!=4&&i!=5) Check(parts[i].W>=44&&parts[i].H>=44,"chrome action retains at least 44 logical pixels: "+i);
        }
        for(int i=0;i<3;i++)
        {
            var tab=parts[i];
            Check(Contains(nav,tab)&&Same(tab,new Box(nav.X+i*nav.W/3,nav.Y,nav.W/3,nav.H)),"three complete tabs partition the exported navigation surface: "+i);
        }
        Check(Math.Abs(close.X-(nav.X+nav.W)-12)<.002f,"close keeps a 12-unit gap from the tabs");
        Console.WriteLine("CARD_CHROME_SCREEN: {0}x{1}; mobile={2}; viewport={3:0.##}x{4:0.##}; contentGain=56",screen[0],screen[1],screen[6]>0,viewport.W,viewport.H);
    }

    private static void Main()
    {
        Check(CardChromeLayout.Style=="inkscape-card-chrome-v1","explicit inspected chrome style");
        foreach(var screen in new[]{
            new[]{1440f,900f,0f,0f,1440f,900f,0f},new[]{1920f,1080f,0f,0f,1920f,1080f,0f},
            new[]{1280f,720f,0f,0f,1280f,720f,0f},new[]{1600f,720f,0f,0f,1600f,720f,0f},
            new[]{1440f,1080f,0f,0f,1440f,1080f,0f},new[]{1600f,720f,56f,18f,1488f,684f,1f},
            new[]{1600f,720f,56f,24f,1504f,684f,1f},new[]{1600f,720f,0f,0f,1600f,720f,1f},
            new[]{1280f,720f,0f,0f,1280f,720f,1f},new[]{2400f,1080f,80f,20f,2240f,1040f,1f},
            new[]{1920f,1200f,0f,0f,1920f,1200f,1f},new[]{2048f,1536f,0f,0f,2048f,1536f,1f}}) Screen(screen);
        Console.WriteLine("CARD_CHROME_CHECKS_OK: {0} assertions; 12 desktop/mobile layouts, complete touch actions, unchanged portrait, larger viewport and old-header rejection. Actual font metrics, clicks and visual acceptance remain publisher checks.",checks);
    }
}
