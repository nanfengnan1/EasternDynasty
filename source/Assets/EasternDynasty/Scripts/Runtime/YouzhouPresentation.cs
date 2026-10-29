using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private GUIStyle resourceBadge,ownedResourceBadge;

        private void YouzhouWelcome()
        {
            RectFill(new Rect(0,0,width,height),new Color(.055f,.065f,.05f,.79f));
            float x=(width-1180)/2,y=(height-720)/2,cx=x+522;
            var art=new Rect(x,y,480,720);Portrait(art,"s1_100039");
            GUI.DrawTexture(new Rect(x,y+400,480,320),shade);
            Label(x+32,y+472,420,90,"幽州初定",display);
            Label(x+39,y+579,405,42,"一城为始，山河待定。",heading);
            Label(x+39,y+674,405,24,"甲子 · 黄巾之乱    /    公元 184 年",small);
            Frame(new Rect(x+480,y,700,720),Ink);
            Label(cx,y+34,610,32,"E A S T E R N   D Y N A S T Y",heading);
            Label(cx,y+85,610,32,"幽州 · 方格战略地图",cardName);
            Label(cx,y+139,610,62,"开城建设 · 招募良将 · 出征拓土",small);
            RectFill(new Rect(cx,y+223,610,1),Border);
            Label(cx,y+251,610,30,"01    选择立足之地",body);
            for(int i=0;i<YouzhouMapDefinition.StartNames.Length;i++)
                if(Button(new Rect(cx+i*207,y+301,195,49),YouzhouMapDefinition.StartNames[i],youzhouStart==i))youzhouStart=i;
            Label(cx,y+380,610,61,"从一座城开始，经营你的幽州。",muted);
            Label(cx,y+465,135,28,"02    势力名称",body);
            ruler=GUI.TextField(new Rect(cx+160,y+457,450,44),ruler,12);
            if(Button(new Rect(cx,y+532,610,36),"旧版九州征程 · 读取原有存档"))useYouzhou=false;
            if(Button(new Rect(cx,y+588,300,53),"于"+YouzhouMapDefinition.StartNames[youzhouStart]+"起兵",true))
            {
                if(HasSave)Ask("建立新势力会替换幽州存档，并保留上一份备份。是否继续？",()=>StartWorld(true));
                else StartWorld(true);
            }
            if(Button(new Rect(cx+315,y+588,295,53),"继续幽州征程",false,HasSave))Load();
            Label(cx,y+667,610,35,string.IsNullOrEmpty(status)?"S1 · 幽州征程":status,small);
        }

        private void ResourceLabels()
        {
            if(!world.IsYouzhou||map.zoom>7.2f)return;
            if(resourceBadge==null){resourceBadge=new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter};resourceBadge.normal.textColor=Text;ownedResourceBadge=new GUIStyle(resourceBadge);ownedResourceBadge.normal.textColor=C("bfe4ce");}
            foreach(var t in world.lands)
            {
                if(t.capital||t.home||!t.Walkable||world.IsCityDistrict(t.id))continue;
                bool nearby=Mathf.Abs(t.x-map.focus.x)<=2&&Mathf.Abs(t.z-map.focus.z)<=2;
                if(!t.bridge&&!t.owned&&t.id!=map.selected&&!(map.zoom<=5.4f&&nearby))continue;
                Vector3 p=map.viewCamera.WorldToScreenPoint(map.SurfacePoint(t));
                if(p.z<0)continue;
                Vector2 logical=ScreenPointToGui(p);float x=logical.x,y=logical.y+16;
                Rect r=new Rect(x-13,y,26,19);
                if(r.x<0||r.xMax>width||r.y<200||r.yMax>height-122)continue;
                bool blocked=false;foreach(var block in hudBlocks)if(block.Overlaps(GuiRectToPixels(r))){blocked=true;break;}if(blocked)continue;
                bool overlapsCity=false;foreach(var label in cityLabels)if(label.Overlaps(r)){overlapsCity=true;break;}if(overlapsCity)continue;
                RectFill(r,new Color(.13f,.17f,.12f,.8f));
                GUI.Label(r,t.bridge?"桥":t.level.ToString(),t.owned?ownedResourceBadge:resourceBadge);
            }
        }
    }
}
