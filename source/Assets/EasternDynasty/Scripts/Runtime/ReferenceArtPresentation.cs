using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        // Pixel regions refer to the complete, unretouched historical reference
        // images recorded in Docs/Resource-Update-20261025.md. They deliberately
        // exclude baked resource amounts and the residence's old level label.
        private static readonly Rect[] resourceArtRegions = {
            new Rect(202,9,38,30), new Rect(722,9,41,31),
            new Rect(548,9,41,31), new Rect(374,9,38,31)
        };

        private bool ReferenceArt(Rect bounds, string id, Rect topLeftPixels)
        {
            var texture=GameAssets.LoadTexture(id,false);
            if(texture==null)return false;
            // Region coordinates are in source-image pixels, before Unity may
            // resize the texture for a particular platform.
            float sourceWidth=id=="reference.youzhou.resource_hud"?894:690;
            float sourceHeight=id=="reference.youzhou.resource_hud"?74:389;
            float ratio=topLeftPixels.width/topLeftPixels.height;
            float w=Mathf.Min(bounds.width,bounds.height*ratio),h=w/ratio;
            var destination=new Rect(bounds.center.x-w*.5f,bounds.center.y-h*.5f,w,h);
            var uv=new Rect(topLeftPixels.x/sourceWidth,
                1-(topLeftPixels.y+topLeftPixels.height)/sourceHeight,
                topLeftPixels.width/sourceWidth,topLeftPixels.height/sourceHeight);
            GUI.DrawTextureWithTexCoords(destination,texture,uv,true);
            return true;
        }

        private bool ResourceReferenceIcon(Rect bounds,int kind)
        {
            return kind>=0&&kind<resourceArtRegions.Length&&
                ReferenceArt(bounds,"reference.youzhou.resource_hud",resourceArtRegions[kind]);
        }

        private bool CityReferenceIcon(Rect bounds,CityBuilding building)
        {
            return building==CityBuilding.Residence&&ReferenceArt(bounds,
                "reference.youzhou.residence_detail",new Rect(133,152,77,59));
        }
    }
}
