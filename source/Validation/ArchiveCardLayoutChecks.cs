using System;
using System.Linq;
using EasternDynasty;
using Box = EasternDynasty.ArchiveCardLayout.Box;

internal static class ArchiveCardLayoutChecks
{
    private static int checks;
    private static void Check(bool value,string message)
    { if(!value)throw new Exception(message);checks++; }
    private static bool Contains(Box a,Box b)
    {return b.W>0&&b.H>0&&b.X>=a.X-.01f&&b.Y>=a.Y-.01f&&b.X+b.W<=a.X+a.W+.01f&&b.Y+b.H<=a.Y+a.H+.01f;}
    private static bool Separate(Box a,Box b)
    {return a.X+a.W<=b.X+.01f||b.X+b.W<=a.X+.01f||a.Y+a.H<=b.Y+.01f||b.Y+b.H<=a.Y+.01f;}
    private static void Screen(float[] values)
    {
        var screen=MobileScreenLayout.Calculate(values[0],values[1],values[2],values[3],values[4],values[5],values[6]>0);
        float viewWidth=screen.LogicalWidth-200,viewHeight=screen.LogicalHeight-360;
        int columns=ArchiveCardLayout.Columns(viewWidth,viewHeight);
        float width=ArchiveCardLayout.Width(viewWidth,viewHeight),height=ArchiveCardLayout.Height(viewHeight),gap=ArchiveCardLayout.Gap;
        float left=ArchiveCardLayout.Left(viewWidth,viewHeight);
        Check(columns>=6,"supported landscape archive loses compact six-column density");
        Check(2*height+gap<=viewHeight+.01f,"two full rows do not fit the actual viewport");
        Check(left>=0&&left+columns*width+(columns-1)*gap<=viewWidth-24+.01f,"cards overlap scrollbar reserve");
        Check(height<=252&&height>=224&&width>=150,"portrait/card became too small for the inspected design");
        var card=new Box(0,0,width,height);
        var art=ArchiveCardLayout.Art(width,height);
        var footer=ArchiveCardLayout.Footer(width,height);
        var faction=ArchiveCardLayout.Faction(width,height);
        var stars=ArchiveCardLayout.Stars(width,height);
        var cost=ArchiveCardLayout.Cost(width,height);
        var troop=ArchiveCardLayout.Troop(width,height);
        var skill=ArchiveCardLayout.Skill(width,height);
        var scope=ArchiveCardLayout.Scope(width,height);
        Check(art.W>110&&art.H>=144&&Contains(card,art)&&Contains(card,footer),"complete portrait or factual footer escaped the card");
        Check(Separate(art,footer)&&Separate(art,faction)&&Separate(art,stars),"portrait is covered by permanent factual labels");
        Check(Contains(card,faction)&&Contains(card,stars)&&Separate(faction,stars),"faction and rarity overlap or clip");
        Check(Contains(card,cost)&&Separate(cost,faction)&&Separate(cost,stars)&&Separate(cost,art),"actual cost budget overlaps faction, stars or portrait");
        Check(Contains(card,troop)&&Separate(troop,art)&&Separate(troop,footer),"troop label overlaps portrait or footer");
        Check(Contains(footer,skill)&&Contains(footer,scope)&&Separate(skill,scope),"actual skill and acquisition rows overlap or escape the footer");
        foreach(var definition in Catalog.Generals.Where(g=>g.s1))
        {
            var name=ArchiveCardLayout.Name(width,height,definition.name.Length);
            float step=ArchiveCardLayout.NameStep(height,definition.name.Length);
            Check(Contains(card,name)&&Separate(name,art)&&Separate(name,footer)&&Separate(name,faction),"actual catalog name flag overlaps artwork or facts: "+definition.id);
            Check(step>=16&&name.H>=step*definition.name.Length,"actual longest catalog name cannot fit without clipping: "+definition.id);
            Check(Separate(name,troop),"actual catalog name ribbon overlaps the troop label: "+definition.id);
        }
        foreach(int count in new[]{1,columns,columns*2,245})
        {
            int rows=(count+columns-1)/columns;
            float content=ArchiveCardLayout.ContentHeight(viewHeight,count,columns),scroll=content-viewHeight;
            Check(Math.Abs(content-Math.Max(viewHeight,rows*(height+gap)-gap))<.01f,"runtime content height retains an unneeded trailing row gap");
            int last=count-1;
            var finalCard=new Box(left+last%columns*(width+gap),last/columns*(height+gap)-scroll,width,height);
            Check(Contains(new Box(0,0,viewWidth,viewHeight),finalCard),"last archive card cannot be fully viewed at maximum scroll");
        }
        Console.WriteLine("ARCHIVE_CARD_LAYOUT_SCREEN: {0}x{1} mobile={2}; logical={3:0.##}x{4:0.##}; columns={5}; card={6:0.##}x{7:0.##}; twoRows={8:0.##}/{9:0.##}",values[0],values[1],values[6]>0,screen.LogicalWidth,screen.LogicalHeight,columns,width,height,2*height+gap,viewHeight);
    }
    private static int Main()
    {
        try
        {
            foreach(var values in new[]{
                new[]{1440f,900f,0f,0f,1440f,900f,0f},new[]{1280f,720f,0f,0f,1280f,720f,0f},
                new[]{1920f,1080f,0f,0f,1920f,1080f,0f},new[]{1440f,1080f,0f,0f,1440f,1080f,0f},
                new[]{1600f,720f,56f,18f,1488f,684f,1f},new[]{1600f,720f,56f,24f,1504f,684f,1f},
                new[]{1280f,720f,0f,0f,1280f,720f,1f},new[]{2400f,1080f,80f,20f,2240f,1040f,1f},
                new[]{1920f,1200f,0f,0f,1920f,1200f,1f},new[]{2048f,1536f,0f,0f,2048f,1536f,1f}
            })Screen(values);
            Console.WriteLine("ARCHIVE_CARD_LAYOUT_CHECKS_OK: {0} assertions across 10 desktop/mobile safe areas; two full rows, image/name/troop/footer separation, cost/stars and footer text budgets, real roster name lengths and production content-height reachability. Pure geometry only; publisher must verify actual fonts, screenshots and input.",checks);
            return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
