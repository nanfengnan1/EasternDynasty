using System;
using EasternDynasty;
using Box = EasternDynasty.MapHudNavigationLayout.Box;

internal static class LandSelectionLayoutChecks
{
    private static int checks, flanking, edge;
    private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    private static bool Contains(Box a, Box b) { return b.W > 0 && b.H > 0 && b.X >= a.X - .001f && b.Y >= a.Y - .001f && b.X + b.W <= a.X + a.W + .001f && b.Y + b.H <= a.Y + a.H + .001f; }
    private static bool Overlaps(Box a, Box b) { return a.X < b.X+b.W && b.X < a.X+a.W && a.Y < b.Y+b.H && b.Y < a.Y+a.H; }
    private static bool Point(Box a,float x,float y) { return x>=a.X && x<a.X+a.W && y>=a.Y && y<a.Y+a.H; }
    private static Box At(Box p,float x,float y,float w,float h) {return new Box(p.X+x,p.Y+y,w,h);}
    private static bool Same(Box a,Box b) { return Math.Abs(a.X-b.X)<.001f&&Math.Abs(a.Y-b.Y)<.001f&&a.W==b.W&&a.H==b.H; }
    private static Box Pixels(Box b,MobileScreenLayout s) {
        float w=b.W*s.Scale,h=b.H*s.Scale,cx=s.X+b.X*s.Scale+w/2,cy=s.Y+b.Y*s.Scale+h/2;
        return new Box(cx-w/2,cy-h/2,w,h);
    }
    private static void Case(MobileScreenLayout screen,float tx,float ty,bool visible,string state)
    {
        float w=screen.LogicalWidth,h=screen.LogicalHeight;
        var p=LandSelectionLayout.Place(w,h,tx,ty,visible,state);var fact=p.Facts;var commands=p.Commands;
        var safe=new Box(0,0,w,h);var physical=new Box(screen.X,screen.Y,screen.Width,screen.Height);
        Check(Contains(safe,fact)&&Contains(safe,commands),"Both independent cards must remain inside safe area.");
        Check(!Overlaps(fact,commands)&&!Overlaps(Pixels(fact,screen),Pixels(commands,screen)),"Cards overlap after recorded-pixel round trip.");
        Check(fact.X>=394&&commands.X>=394&&fact.Y>=100&&commands.Y>=100,"Cards obscure left HUD or top resources.");
        Check(fact.Y+fact.H<=h-100&&commands.Y+commands.H<=h-100,"Cards obscure footer/toast navigation.");
        Check(fact.W==280&&fact.H==228&&commands.W==240&&commands.H==LandCommandMenuLayout.Height(state),"Inspected compact facts and state-specific command composition changed.");
        Check(!visible||!Point(fact,tx,ty)&&!Point(commands,tx,ty),"Cards obscure actual selected target center.");
        if(p.Flanking){flanking++;Check(fact.X+fact.W<=tx-44&&commands.X>=tx+72,"Selected tile loses central clearance.");}
        else {edge++;Check(fact.X==w-560&&commands.X==w-260,"Off-screen/edge selection must use the right dock.");}
        var header=LandSelectionLayout.Header(fact);var art=LandSelectionLayout.Art(fact);
        var image=LandSelectionLayout.Image(fact);var identity=LandSelectionLayout.Identity(fact);
        var summary=LandSelectionLayout.Summary(fact);var actions=LandSelectionLayout.Actions(commands);var close=LandSelectionLayout.Close(fact);
        var defense=LandSelectionLayout.Defense(fact);var yieldFact=LandSelectionLayout.Yield(fact);
        Check(Same(header,At(fact,0,0,280,148))&&Same(art,At(fact,12,64,256,84)),"Header and illustration must follow the inspected compact facts surface.");
        Check(Same(image,At(fact,12,68,156,78))&&Same(identity,At(fact,178,72,90,70)),"Full image and separate identity no longer follow the inspected design.");
        Check(Same(summary,At(fact,16,156,248,60))&&Same(close,At(fact,224,12,44,44)),"Compact summary and independent close geometry changed.");
        Check(Contains(fact,header)&&Contains(header,art)&&Contains(art,image)&&Contains(art,identity),"Complete terrain and identity escape visible header.");
        Check(!Overlaps(image,identity)&&image.W==156&&image.H==78&&identity.X-image.X-image.W==10,"Complete enlarged image/identity separation lost.");
        Check(Contains(header,close)&&!Overlaps(close,art)&&close.W>=44&&close.H>=44,"Close no longer has an independent full touch area.");
        Check(Contains(fact,summary)&&!Overlaps(header,summary)&&summary.Y-header.Y-header.H==8,"Facts no longer separated by the inspected gap.");
        Check(Same(defense,At(fact,16,156,248,28))&&Same(yieldFact,At(fact,16,188,248,28)),"Facts must be two complete 248-by-28 rows in the inspected order.");
        Check(Contains(summary,defense)&&Contains(summary,yieldFact)&&!Overlaps(defense,yieldFact)&&Math.Abs(yieldFact.Y-defense.Y-defense.H-4)<.001f,"Compact fact rows overlap or leave their summary region.");
        Check(Contains(commands,actions)&&actions.W==208&&actions.H==commands.H-24,"Command content escapes its own visible surface.");
        var information=LandCommandMenuLayout.Information(commands,state);
        Check(Contains(commands,information)&&information.H==LandCommandMenuLayout.InformationHeight(state),"Information surface escapes menu.");
        foreach(var b in new[]{fact,commands,header,art,image,identity,summary,defense,yieldFact,actions,close})Check(Contains(physical,Pixels(b,screen)),"Recorded drawing leaves physical safe area.");
        Box previous=close;
        string[] ids=state=="owned"?new[]{"army-garrison","army-farm","land-training"}:state=="busy"?new[]{"army-recall","land-training"}:state=="city"?new[]{"land-enter-city"}:new[]{"army-attack"};
        foreach(string id in ids){
            var button=LandCommandMenuLayout.Command(actions,state,id);var icon=LandSelectionLayout.CommandIcon(button);var label=LandSelectionLayout.CommandLabel(button);
            Check(button.Y>=information.Y+information.H+12,"Command must clear the entire factual note.");
            Check(Contains(actions,button)&&button.W>=44&&button.H>=44&&!Overlaps(button,previous),"A retained action is clipped, too small or overlapping.");
            Check(Contains(button,icon)&&Contains(button,label)&&!Overlaps(icon,label),"Rendered command image and text overlap.");
            Check(icon.W==34&&icon.H==34&&label.W==156&&label.H==36,"Four/five character actions lose the mobile font budget.");
            Check(!Overlaps(Pixels(icon,screen),Pixels(label,screen))&&!Overlaps(Pixels(button,screen),Pixels(previous,screen)),"Actual pixel recording introduces overlap.");
            previous=button;
        }
    }
    public static void Main()
    {
        Check(LandSelectionLayout.Style=="land-compact-facts-v5","Compact facts renderer must expose the current inspected design identity.");
        foreach(var size in new[]{new[]{1280f,720f},new[]{1440f,900f},new[]{1600f,720f},new[]{1920f,1080f},new[]{1024f,768f},new[]{3440f,1440f}})
        foreach(bool mobile in new[]{false,true}){
            var s=MobileScreenLayout.Calculate(size[0],size[1],size[0]*.035f,24,size[0]*.94f,size[1]-36,mobile);
            foreach(float x in new[]{-100f,0f,300f,717.99f,718f,s.LogicalWidth/2,s.LogicalWidth-332,s.LogicalWidth-331.99f,s.LogicalWidth,s.LogicalWidth+100})
            foreach(float y in new[]{-100f,0f,143.99f,144f,s.LogicalHeight/2,s.LogicalHeight-124,s.LogicalHeight-123.99f,s.LogicalHeight})
                foreach(string state in new[]{"owned","busy","city","neutral","blocked"})
                    Case(s,x,y,x>=0&&x<=s.LogicalWidth&&y>=0&&y<=s.LogicalHeight,state);
        }
        Check(flanking>0&&edge>0,"Both placement branches must be exercised.");
        Console.WriteLine("PASS: "+checks+" land placement, full image, independent command and safe-area assertions; "+flanking+" target-relative and "+edge+" dock cases. Runtime appearance/clicks remain publisher checks.");
    }
}

