using System;
using System.Collections.Generic;
using EasternDynasty;
internal static class MapHudNavigationChecks
{
    static int count;
    static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
    static bool Contains(MapHudNavigationLayout.Box a,MapHudNavigationLayout.Box b){return b.X>=a.X&&b.Y>=a.Y&&b.X+b.W<=a.X+a.W&&b.Y+b.H<=a.Y+a.H;}
    static bool Overlap(MapHudNavigationLayout.Box a,MapHudNavigationLayout.Box b){return a.X<b.X+b.W&&a.X+a.W>b.X&&a.Y<b.Y+b.H&&a.Y+a.H>b.Y;}
    static MapHudNavigationLayout.Box RecordedPixels(MapHudNavigationLayout.Box logical,MobileScreenLayout layout)
    {
        // Match GuiRectToPixels, RecordReviewTarget, then MapHudReviewRect. The
        // center/size round-trip exposed the 1600x720 mobile publication failure.
        float x=layout.X+logical.X*layout.Scale,y=layout.Y+logical.Y*layout.Scale;
        float w=logical.W*layout.Scale,h=logical.H*layout.Scale;
        float centerX=x+w/2,centerY=y+h/2;
        return new MapHudNavigationLayout.Box(centerX-w/2,centerY-h/2,w,h);
    }
    static void DesignBox(MapHudNavigationLayout.Box actual,float x,float y,float w,float h,string name)
    { Check(actual.X==x&&actual.Y==y&&actual.W==w&&actual.H==h,"Region differs from the inspected SVG contract: "+name); }
    static void Main()
    {
        Check(MapHudNavigationLayout.Style=="youzhou-reference-image-captions-v2","Navigation style does not identify the inspected horizontal-caption design");
        Check(MapHudNavigationLayout.RecruitStyle=="youzhou-recruit-pedestal-v3","Map recruitment must identify its inspected independent seal design");
        Check(MapHudAtlasLayout.Style=="youzhou-atlas-sidebar-v3","Atlas style does not identify the inspected full-height terrain/sidebar design");
        foreach(var size in new[]{new[]{1440f,900f},new[]{1280f,720f},new[]{1600f,720f},new[]{1920f,1080f},new[]{1024f,768f},new[]{2340f,1080f}})
        foreach(bool mobile in new[]{false,true})
        {
            // Use the exact -edMobileReview safe area, including non-integer X.
            var layout=MobileScreenLayout.Calculate(size[0],size[1],mobile?size[0]*.035f:0,mobile?24:0,mobile?size[0]*.94f:size[0],size[1]-(mobile?36:0),mobile);
            var footer=new MapHudNavigationLayout.Box(0,layout.LogicalHeight-78,layout.LogicalWidth,78);
            var controls=new List<MapHudNavigationLayout.Box>();
            for(int i=0;i<6;i++)
            {
                var button=MapHudNavigationLayout.Entry(layout.LogicalWidth,layout.LogicalHeight,i);
                var art=MapHudNavigationLayout.Art(button,i==5);var caption=MapHudNavigationLayout.Caption(button,i==5);
                Check(Contains(footer,button),"Navigation leaves safe footer");
                Check(Contains(button,art)&&Contains(button,caption)&&!Overlap(art,caption),"Art/title clipping or overlap");
                Check(button.W>=44&&button.H>=44,"Lost touch size");
                Check(art.Y+art.H<=caption.Y,"Navigation image must remain above its horizontal caption");
                DesignBox(art,button.X+(i==5?46:4),button.Y+(i==5?7:3),i==5?40:100,i==5?38:42,"navigation image "+i);
                DesignBox(caption,button.X+(i==5?8:4),button.Y+(i==5?47:46),i==5?116:100,22,"horizontal navigation caption "+i);
                Check(caption.W>caption.H&&caption.W>=100,"Navigation reverted to a narrow vertical title");
                controls.Add(button);
            }
            controls.Add(MapHudNavigationLayout.Utility(layout.LogicalWidth,layout.LogicalHeight,false));
            controls.Add(MapHudNavigationLayout.Utility(layout.LogicalWidth,layout.LogicalHeight,true));
            float utilityX=Math.Max(620,(layout.LogicalWidth-270)/2);
            DesignBox(controls[6],utilityX,layout.LogicalHeight-61,82,49,"save utility");
            DesignBox(controls[7],utilityX+90,layout.LogicalHeight-61,180,49,"speed utility");
            Check(controls[7].X-(controls[6].X+controls[6].W)==8,"Save/time controls lost the inspected eight-unit separation");
            for(int i=0;i<controls.Count;i++)
            {
                Check(Contains(footer,controls[i]),"Utility clipped by safe area");
                var pixels=RecordedPixels(controls[i],layout);
                Check(pixels.W/layout.Scale>=43.99f&&pixels.H/layout.Scale>=43.99f,"Recorded footer control lost its touch size");
                for(int j=0;j<i;j++)
                {
                    Check(!Overlap(controls[i],controls[j]),"Logical footer commands overlap");
                    Check(!Overlap(pixels,RecordedPixels(controls[j],layout)),"Recorded footer commands overlap after screen/safe-area/center transform: "+size[0]+"x"+size[1]+" mobile="+mobile+" controls="+i+"/"+j);
                }
            }
            var header=new MapHudNavigationLayout.Box(0,0,layout.LogicalWidth,70);
            // The raised map-only control must be checked independently of the
            // unchanged compact footer used by city pages and selected land.
            var safe=new MapHudNavigationLayout.Box(0,0,layout.LogicalWidth,layout.LogicalHeight);
            var recruit=MapHudNavigationLayout.RecruitEntry(layout.LogicalWidth,layout.LogicalHeight);
            var recruitArt=MapHudNavigationLayout.RecruitArt(recruit);
            var recruitCaption=MapHudNavigationLayout.RecruitCaption(recruit);
            DesignBox(recruit,layout.LogicalWidth-180,layout.LogicalHeight-144,160,140,"map-only recruitment seal");
            DesignBox(recruitArt,recruit.X+50,recruit.Y+10,60,88,"complete raised recruitment portrait");
            DesignBox(recruitCaption,recruit.X+10,recruit.Y+114,140,24,"separate raised recruitment title");
            Check(Contains(safe,recruit)&&!Contains(footer,recruit)&&footer.Y-recruit.Y==66,"Recruitment must rise 66 units while remaining inside the real safe area");
            Check(Contains(recruit,recruitArt)&&Contains(recruit,recruitCaption)&&!Overlap(recruitArt,recruitCaption),"Raised recruitment image/title overlap or clip");
            Check(recruitArt.Y+recruitArt.H<=recruitCaption.Y&&recruitArt.Y+recruitArt.H*.25f<footer.Y,"Raised portrait must remain above its title and visibly extend into the map");
            for(int i=0;i<controls.Count;i++)if(i!=5)
            {
                Check(!Overlap(recruit,controls[i]),"Raised recruitment overlaps unchanged footer controls");
                Check(!Overlap(RecordedPixels(recruit,layout),RecordedPixels(controls[i],layout)),"Raised recruitment overlaps after real safe-area transform");
            }
            var camera=MapHudHeaderLayout.Camera(layout.LogicalWidth,layout.LogicalHeight);
            Check(Contains(camera,header)&&camera.Y+camera.H==footer.Y,"Live terrain must cover header but stop at footer");
            var input=MapHudHeaderLayout.Input(layout.LogicalWidth,layout.LogicalHeight);
            var atlas=MapHudAtlasLayout.Panel(layout.LogicalWidth);
            var terrain=MapHudAtlasLayout.Terrain(atlas);var locator=MapHudAtlasLayout.Locate(atlas);
            var atlasParts=new[]{MapHudAtlasLayout.Title(atlas),locator,terrain,MapHudAtlasLayout.Legend(atlas)};
            Check(Contains(input,atlas),"Atlas leaves mobile/desktop safe map input");
            DesignBox(atlas,layout.LogicalWidth-302,90,282,204,"atlas panel");
            DesignBox(terrain,atlas.X,atlas.Y,204,204,"unchanged square atlas resolution");
            DesignBox(MapHudAtlasLayout.Title(atlas),atlas.X+210,atlas.Y,72,54,"atlas sidebar title and coordinates");
            DesignBox(locator,atlas.X+216,atlas.Y+64,60,60,"atlas touch locator");
            DesignBox(MapHudAtlasLayout.Pin(atlas),atlas.X+236,atlas.Y+70,20,28,"atlas locator image");
            DesignBox(MapHudAtlasLayout.Caption(atlas),atlas.X+216,atlas.Y+101,60,20,"atlas locator caption");
            DesignBox(MapHudAtlasLayout.Legend(atlas),atlas.X+210,atlas.Y+134,72,70,"atlas sidebar legend");
            Check(locator.W>=44&&locator.H>=44,"Atlas locator lost its full touch target");
            Check(terrain.Y==atlas.Y&&terrain.H==atlas.H,"Atlas terrain must occupy the complete panel height");
            Check(MapHudAtlasLayout.Title(atlas).X-(terrain.X+terrain.W)==6,"Atlas sidebar lost its inspected six-unit separation from terrain");
            for(int i=0;i<atlasParts.Length;i++)
            {
                Check(Contains(atlas,atlasParts[i]),"Atlas content escapes protected panel");
                for(int j=0;j<i;j++)Check(!Overlap(atlasParts[i],atlasParts[j]),"Atlas live terrain/title/legend/home overlap");
            }
            Check(Contains(locator,MapHudAtlasLayout.Pin(atlas))&&Contains(locator,MapHudAtlasLayout.Caption(atlas)),"Atlas image/caption leaves touch button");
            Check(!Overlap(MapHudAtlasLayout.Pin(atlas),MapHudAtlasLayout.Caption(atlas)),"Atlas image covers live caption");
            Check(!Overlap(atlas,MapHudQuestLayout.Panel)&&!Overlap(atlas,MapHudArmyLayout.Area(layout.LogicalHeight)),"Atlas overlaps primary HUD content");
            Check(Contains(camera,input)&&!Overlap(input,header)&&input.Y+input.H==footer.Y,"Input must exclude transparent header");
            Check(MapPointerGesture.EdgeAxis((input.Y*layout.Scale)+3,input.Y*layout.Scale,(input.Y+input.H)*layout.Scale,26)<0,"Top edge scrolling must remain reachable below blocked header");
            var quest=MapHudQuestLayout.Panel;
            Check(Contains(input,quest),"Quest must remain in the map safe area");
            var questParts=new[]{MapHudQuestLayout.Open,MapHudQuestLayout.Progress,MapHudQuestLayout.Action};
            for(int i=0;i<questParts.Length;i++)
            {
                Check(Contains(quest,questParts[i]),"Quest layer leaves its actual blocked panel");
                for(int j=0;j<i;j++)Check(!Overlap(questParts[i],questParts[j]),"Task navigation/progress/action overlap");
                Check(questParts[i].W>=44&&questParts[i].H>=44,"Quest chapter/action lost full touch area");
            }
            Check(Contains(MapHudQuestLayout.Open,MapHudQuestLayout.Banner)&&!Overlap(MapHudQuestLayout.Banner,MapHudQuestLayout.Chapter)&&!Overlap(MapHudQuestLayout.Banner,MapHudQuestLayout.Title)&&!Overlap(MapHudQuestLayout.Banner,MapHudQuestLayout.Objective),"Book must belong to the chapter control without covering text");
            Check(Contains(MapHudQuestLayout.Open,MapHudQuestLayout.Body)&&!Overlap(MapHudQuestLayout.Banner,MapHudQuestLayout.Body)
                &&Contains(MapHudQuestLayout.Body,MapHudQuestLayout.Chapter)&&Contains(MapHudQuestLayout.Body,MapHudQuestLayout.Title)
                &&Contains(MapHudQuestLayout.Body,MapHudQuestLayout.Objective),"Detached task book and information sleeve must not share a surface or clip live text");
            Check(Contains(MapHudQuestLayout.Open,MapHudQuestLayout.Chapter)&&Contains(MapHudQuestLayout.Open,MapHudQuestLayout.Title)
                &&Contains(MapHudQuestLayout.Open,MapHudQuestLayout.Objective),"Chapter/title/objective clip in their chapter entry");
            Check(!Overlap(MapHudQuestLayout.Chapter,MapHudQuestLayout.Title)&&!Overlap(MapHudQuestLayout.Title,MapHudQuestLayout.Objective),"Task text layers overlap");
            Check(!Overlap(quest,new MapHudNavigationLayout.Box(20,278,268,40)),"Task hides the existing land objective return");
            var army=MapHudArmyLayout.Area(layout.LogicalHeight);
            Check(!Overlap(quest,army),"Task overlaps army on the smallest safe layout");
            Check(Contains(input,army),"Army region leaves the map safe area");
            DesignBox(army,20,layout.LogicalHeight-396,354,302,"area");
            var armyHeader=MapHudArmyLayout.Header(army);var armyStatus=MapHudArmyLayout.Status(army);
            var reinforce=MapHudArmyLayout.Action(army,false);var command=MapHudArmyLayout.Action(army,true);
            DesignBox(armyHeader,20,layout.LogicalHeight-396,354,28,"header");
            DesignBox(armyStatus,20,layout.LogicalHeight-160,354,20,"status");
            DesignBox(reinforce,20,layout.LogicalHeight-138,171,44,"reinforcement");
            DesignBox(command,203,layout.LogicalHeight-138,171,44,"formation or recall");
            var armyParts=new List<MapHudNavigationLayout.Box>{armyHeader,armyStatus,reinforce,command};
            for(int i=0;i<3;i++)
            {
                var card=MapHudArmyLayout.Card(army,i);var art=MapHudArmyLayout.Art(card);
                var name=MapHudArmyLayout.Name(card);var level=MapHudArmyLayout.Identity(card);
                DesignBox(card,20,layout.LogicalHeight-364+i*68,354,64,"row "+i);
                DesignBox(art,24,layout.LogicalHeight-363+i*68,44,62,"portrait "+i);
                DesignBox(name,82,layout.LogicalHeight-352+i*68,164,40,"horizontal name "+i);
                DesignBox(level,262,layout.LogicalHeight-352+i*68,102,40,"identity "+i);
                Check(Contains(card,art)&&Contains(card,name)&&Contains(card,level),"Army portrait/name/identity leaves its actual card");
                Check(!Overlap(art,name)&&!Overlap(art,level)&&!Overlap(name,level),"Army name or identity covers its portrait");
                Check(card.W>=44&&card.H>=44,"Army general card lost its touch area");
                Check(art.X+art.W<=name.X&&name.X+name.W<=level.X,"Army portrait/name/identity lost horizontal reading order");
                var recordedRow=RecordedPixels(card,layout);
                Check(recordedRow.W/layout.Scale>=43.99f&&recordedRow.H/layout.Scale>=63.99f,"Recorded army row lost its complete touch region");
                if(i>0)Check(card.Y-(MapHudArmyLayout.Card(army,i-1).Y+64)==4,"Army rows lost the inspected four-unit gap");
                armyParts.Add(card);
            }
            for(int i=0;i<armyParts.Count;i++)
            {
                Check(Contains(army,armyParts[i]),"Army layer leaves the actual blocked region");
                for(int j=0;j<i;j++)Check(!Overlap(armyParts[i],armyParts[j]),"Army header/cards/status/actions overlap");
            }
            Check(reinforce.W>=44&&reinforce.H>=44&&command.W>=44&&command.H>=44,"Army action lost 44-unit touch geometry");
            // Both dynamic entries can coexist after a recorded battle while a
            // later training order is active. Reserve both, even in 720px preview.
            var trainingEntry=new MapHudNavigationLayout.Box(army.X,army.Y-51,army.W,44);
            var battleEntry=new MapHudNavigationLayout.Box(army.X,army.Y-102,army.W,44);
            var questReturn=new MapHudNavigationLayout.Box(20,278,268,40);
            foreach(var entry in new[]{trainingEntry,battleEntry})
            {
                Check(Contains(input,entry)&&!Overlap(entry,quest)&&!Overlap(entry,questReturn)&&!Overlap(entry,army),"Dynamic training/watch entry obscures task, objective return or army");
                Check(entry.Y>=questReturn.Y+questReturn.H+3.99f,"Dynamic watch entry lost the smallest safe-layout gap above objective return");
                var pixels=RecordedPixels(entry,layout);
                Check(pixels.W/layout.Scale>=43.99f&&pixels.H/layout.Scale>=43.99f,"Dynamic entry lost recorded 44-unit touch size");
            }
            Check(!Overlap(trainingEntry,battleEntry),"Training and battle watch entries overlap");
            // Old task card occupied through y264. The freed band must be genuine map input.
            Check(!Overlap(quest,new MapHudNavigationLayout.Box(20,222,354,42)),"Task docket must leave the remaining original charter band free");
            var identity=MapHudHeaderLayout.Identity;
            DesignBox(identity,16,4,246,62,"header identity");
            DesignBox(MapHudHeaderLayout.Crest,16,4,62,62,"header crest");
            Check(Contains(MapHudHeaderLayout.Crest,MapHudHeaderLayout.IdentityArt),"Complete city image must fit its crest");
            Check(Contains(header,identity)&&identity.W>=44&&identity.H>=44,"City touch area leaves header");
            foreach(bool wrapped in new[]{false,true})
            {
                var name=MapHudHeaderLayout.Ruler(wrapped);var detail=MapHudHeaderLayout.IdentityDetail(wrapped);
                Check(Contains(identity,name)&&Contains(identity,detail),"Long ruler or identity detail clips");
                Check(!Overlap(name,detail)&&!Overlap(name,MapHudHeaderLayout.IdentityArt),"Two-line ruler overlaps detail or city image");
            }
            var resources=new List<MapHudNavigationLayout.Box>{identity};
            for(int i=0;i<6;i++)
            {
                var cell=MapHudHeaderLayout.Resource(layout.LogicalWidth,i);
                var icon=MapHudHeaderLayout.Icon(cell,i);var stock=MapHudHeaderLayout.Stock(cell,i);var detail=MapHudHeaderLayout.Detail(cell,i);
                Check(Contains(header,cell)&&Contains(cell,icon)&&Contains(cell,stock)&&Contains(cell,detail),"Resource layers clipped in safe area");
                Check(!Overlap(icon,stock)&&!Overlap(icon,detail)&&!Overlap(stock,detail),"Resource image and text overlap");
                Check(stock.W>=100&&cell.H>=44,"Resource stock lost readable width or blocked height");
                Check(stock.Y==30&&stock.H==26&&detail.Y==6&&detail.H==23,"All six inventories must share the lower baseline below their rates");
                if(i<4)Check(icon.W<=38&&icon.H<=30,"Historical resource icons must stay below native source size");
                if(i==4)Check(Math.Abs(cell.X-(MapHudHeaderLayout.Production(layout.LogicalWidth).X+MapHudHeaderLayout.Production(layout.LogicalWidth).W)-16)<.01,"Currency must follow the production group without the old ultrawide gap");
                foreach(var other in resources)Check(!Overlap(cell,other),"Identity/production/currency groups overlap");
                resources.Add(cell);
            }
        }
        Console.WriteLine("PASS: "+count+" map navigation geometry checks across desktop/mobile safe areas; runtime visual/click review still required.");
    }
}
