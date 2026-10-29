using System.Collections.Generic;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D minimapTexture;
        private World minimapWorld;
        private readonly List<MinimapViewportGeometry.Point> minimapViewport = new List<MinimapViewportGeometry.Point>();
        private GUIStyle atlasCaption, atlasCityLabel, atlasTitle, atlasHomeCaption;
        private Texture2D atlasStrip, atlasLocate, atlasLocateHover, atlasPin;
        private bool atlasTextFits;
        private bool AtlasDesignReady { get { return atlasStrip!=null
            &&atlasLocate!=null&&atlasLocate.width==120&&atlasLocate.height==120
            &&atlasLocateHover!=null&&atlasLocateHover.width==120&&atlasLocateHover.height==120
            &&atlasPin!=null&&atlasPin.width==40&&atlasPin.height==56; } }

        private void MiniMap()
        {
            InitMapHudStyles();
            if(atlasCaption==null)
            {
                atlasCaption=new GUIStyle(hudSmall){fontSize=12,alignment=TextAnchor.MiddleLeft,wordWrap=false};
                atlasCaption.normal.textColor=C("eee7d4");
                atlasCityLabel=new GUIStyle(tiny){fontSize=12,alignment=TextAnchor.MiddleCenter,wordWrap=false};
                atlasCityLabel.normal.textColor=C("f6e6bd");
                atlasTitle=new GUIStyle(atlasCaption){fontSize=15,alignment=TextAnchor.MiddleCenter};
                atlasHomeCaption=new GUIStyle(atlasCaption){fontSize=13,alignment=TextAnchor.MiddleCenter};
                atlasStrip=GameAssets.LoadTexture("ui.youzhou.atlas_strip");
                atlasLocate=GameAssets.LoadTexture("ui.youzhou.atlas_locate_dark");
                atlasLocateHover=GameAssets.LoadTexture("ui.youzhou.atlas_locate_dark_hover");
                atlasPin=GameAssets.LoadTexture("ui.youzhou.atlas_pin_light");
            }
            atlasTextFits=true;
            var layout=MapHudAtlasLayout.Panel(width);
            Rect panel=HudDesignRect(layout),title=HudDesignRect(MapHudAtlasLayout.Title(layout));
            Block(panel);RecordMapHudTarget("minimap-panel",panel);
            GUI.DrawTexture(title,atlasStrip,ScaleMode.StretchToFill,true);
            DrawAtlasText(new Rect(title.x,title.y,72,28),"幽州舆图",atlasTitle);
            DrawAtlasText(new Rect(title.x,title.y+28,72,24),
                "("+Mathf.RoundToInt(map.focus.x)+", "+Mathf.RoundToInt(map.focus.z)+")",new GUIStyle(atlasCaption){fontSize=13,alignment=TextAnchor.MiddleCenter});
            RecordMapHudTarget("minimap-title",title);
            Rect locate=HudDesignRect(MapHudAtlasLayout.Locate(layout));
            Rect pin=HudDesignRect(MapHudAtlasLayout.Pin(layout)),caption=HudDesignRect(MapHudAtlasLayout.Caption(layout));
            bool interactive=GUI.enabled&&!BlocksMapInput;
            GUI.DrawTexture(locate,interactive&&locate.Contains(Event.current.mousePosition)?atlasLocateHover:atlasLocate,ScaleMode.StretchToFill,true);
            GUI.DrawTexture(pin,atlasPin,ScaleMode.ScaleToFit,true);
            DrawAtlasText(caption,"归城",atlasHomeCaption);
            if(interactive)
            {
                RecordReviewTarget("map-home",locate);
                RecordReviewTarget("map-home-art",pin);
                RecordReviewTarget("map-home-caption",caption);
            }
            if(ContentTileClicked(locate,"minimap-home",-1))map.CenterHome();
            EnsureMinimapAtlas();
            Rect area=HudDesignRect(MapHudAtlasLayout.Terrain(layout));
            if(interactive)RecordReviewTarget("minimap-area",area);
            Color previousColor=GUI.color;
            GUI.color=previousColor*new Color(1,1,1,MapHudAtlasLayout.TerrainOpacity);
            GUI.DrawTexture(area,minimapTexture,ScaleMode.StretchToFill,true);GUI.color=previousColor;
            // The atlas is a drawing of the actual 40 x 40 map. Ownership and
            // camera position stay separate from terrain so neither can go stale.
            foreach(var t in world.lands)
            {
                if(!t.owned||t.home||t.capital)continue;
                Vector2 p=AtlasPoint(area,t.x,t.z);
                RectFill(new Rect(p.x-2,p.y-2,4,4),C("8dd6b0"));
            }
            DrawAtlasViewport(area);
            foreach(var t in world.lands)
            {
                if(!t.capital&&!t.home)continue;
                Vector2 p=AtlasPoint(area,t.x,t.z);
                DrawAtlasSettlement(p,t.home?Gold:t.owned?C("8dd6b0"):C("d99871"),t.home);
            }
            var home=world.lands[world.homeId];Vector2 hp=AtlasPoint(area,home.x,home.z);
            Rect homeLabel=new Rect(Mathf.Clamp(hp.x-22,area.x+1,area.xMax-45),
                Mathf.Clamp(hp.y+7,area.y+1,area.yMax-18),44,17);
            RectFill(homeLabel,new Color(.09f,.13f,.12f,.86f));DrawAtlasText(homeLabel,"主城",atlasCityLabel);
            // North and the legend are fixed furniture, never extra map hit targets.
            Rect north=new Rect(area.xMax-20,area.y+3,17,20);
            RectFill(north,new Color(.08f,.13f,.12f,.85f));DrawAtlasText(north,"北",atlasCityLabel);
            Rect legend=HudDesignRect(MapHudAtlasLayout.Legend(layout));
            GUI.DrawTexture(legend,atlasStrip,ScaleMode.StretchToFill,true);
            RectFill(new Rect(legend.x+7,legend.y+9,5,5),C("8dd6b0"));
            DrawAtlasText(new Rect(legend.x+20,legend.y,48,22),"领地",atlasCaption);
            Outline(new Rect(legend.x+5,legend.y+32,9,7),Paper);
            DrawAtlasText(new Rect(legend.x+20,legend.y+24,48,22),"视野",atlasCaption);
            DrawAtlasText(new Rect(legend.x,legend.y+48,72,22),"S1 40×40",new GUIStyle(atlasCaption){fontSize=11,alignment=TextAnchor.MiddleCenter});
            RecordMapHudTarget("minimap-legend",legend);
            if(interactive&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&area.Contains(Event.current.mousePosition))
            {
                Vector2 p=Event.current.mousePosition;
                map.focus=new Vector3(Mathf.Clamp((p.x-area.x)/area.width*40-.5f,0,39),0,
                    Mathf.Clamp(39.5f-(p.y-area.y)/area.height*40,0,39));
                RecordMinimapNavigation(p);
                Event.current.Use();
            }
        }

        private void DrawAtlasText(Rect bounds,string text,GUIStyle style)
        {
            var fitted=new GUIStyle(style);var content=new GUIContent(text);
            while(fitted.fontSize>10&&!HeaderTextFits(bounds,content,fitted,false))fitted.fontSize--;
            atlasTextFits&=HeaderTextFits(bounds,content,fitted,false);
            GUI.Label(bounds,content,fitted);
        }

        private void EnsureMinimapAtlas()
        {
            if(ReferenceEquals(minimapWorld,world)&&minimapTexture!=null)return;
            int[] pixels=MinimapAtlasRaster.Render(world);var colors=new Color32[pixels.Length];
            for(int i=0;i<pixels.Length;i++)
            {uint argb=unchecked((uint)pixels[i]);colors[i]=new Color32((byte)(argb>>16),(byte)(argb>>8),(byte)argb,255);}
            if(minimapTexture==null)
            {
                minimapTexture=new Texture2D(MinimapAtlasRaster.Size,MinimapAtlasRaster.Size,TextureFormat.RGBA32,false);
                minimapTexture.filterMode=FilterMode.Bilinear;minimapTexture.wrapMode=TextureWrapMode.Clamp;
            }
            minimapTexture.SetPixels32(colors);minimapTexture.Apply();minimapWorld=world;
        }

        private static Vector2 AtlasPoint(Rect area,float x,float z)
        {return new Vector2(area.x+(x+.5f)/40*area.width,area.y+(39.5f-z)/40*area.height);}

        private void DrawAtlasSettlement(Vector2 point,Color color,bool home)
        {
            float size=home?10:7;
            Rect r=new Rect(point.x-size/2,point.y-size/2,size,size);
            RectFill(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),C("26342d"));
            RectFill(r,color);RectFill(new Rect(r.x+size*.35f,r.y+size*.45f,size*.3f,size*.55f),C("26342d"));
            RectFill(new Rect(r.x-1,r.y-2,2,3),color);RectFill(new Rect(r.xMax-1,r.y-2,2,3),color);
        }

        private void DrawAtlasViewport(Rect area)
        {
            minimapViewport.Clear();if(map.viewCamera==null)return;
            // Project all four actual camera viewport corners onto the same ground
            // plane as land picking. Clip the polygon, not individual corner values.
            Vector2[] corners={new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            foreach(var corner in corners)
            {
                Ray ray=map.viewCamera.ViewportPointToRay(new Vector3(corner.x,corner.y,0));float distance;
                if(!new Plane(Vector3.up,Vector3.zero).Raycast(ray,out distance))return;
                Vector3 p=ray.GetPoint(distance);minimapViewport.Add(new MinimapViewportGeometry.Point((p.x+.5f)/40,(39.5f-p.z)/40));
            }
            MinimapViewportGeometry.ClipToMap(minimapViewport);
            for(int i=0;i<minimapViewport.Count;i++)
            {
                var a=minimapViewport[i];var b=minimapViewport[(i+1)%minimapViewport.Count];
                HudLine(area,a.x,a.y,b.x,b.y,new Color(.13f,.19f,.17f,.9f),3);
                HudLine(area,a.x,a.y,b.x,b.y,Paper,1.25f);
            }
            Vector2 focus=AtlasPoint(area,map.focus.x,map.focus.z);
            RectFill(new Rect(focus.x-2,focus.y-2,4,4),Paper);
        }

    }
}
