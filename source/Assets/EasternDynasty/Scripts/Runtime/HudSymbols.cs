using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D[] resourceSymbols;
        private void ResourceIcon(Rect rect, int kind)
        {
            if(ResourceReferenceIcon(rect,kind))return;
            if(resourceSymbols==null)
            {
                resourceSymbols=new Texture2D[6];
                Color[] tones={C("65C5A2"),C("EDC56D"),C("A8B5CC"),C("86BCD2"),C("F0B75D"),C("73D8CA")};
                for(int k=0;k<6;k++)
                {
                    var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                    var pixels=new Color[32*32];
                    for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                    {
                        float dx=x-15.5f,dy=y-15.5f;bool fill=false,highlight=false;
                        if(k==0){fill=(y>=5&&y<=11&&x>=5&&x<=25)||(y>=14&&y<=20&&x>=8&&x<=28)||(y>=23&&y<=28&&x>=4&&x<=22);highlight=x%9<2;}
                        else if(k==1){fill=x>=14&&x<=17&&y>=3&&y<=28;for(int n=0;n<3;n++){float h=9+n*7;fill|=(Mathf.Pow((x-9f)/6,2)+Mathf.Pow((y-h-2)/3,2)<1)||(Mathf.Pow((x-22f)/6,2)+Mathf.Pow((y-h)/3,2)<1);}highlight=x>15;}
                        else if(k==2){fill=Mathf.Abs(dx)*.8f+Mathf.Abs(dy)<13&&y<26;highlight=x>15&&y>14;}
                        else if(k==3){fill=y>=8&&y<=23&&Mathf.Abs(dx)<9+(y-8)*.27f;highlight=y>18;}
                        else if(k==4){fill=dx*dx+dy*dy<150;highlight=dx*dx+dy*dy>94;fill&=!(Mathf.Abs(dx)<3&&Mathf.Abs(dy)<4);}
                        else {fill=Mathf.Abs(dx)*1.15f+Mathf.Abs(dy)*.8f<13;highlight=x<15&&y>14;}
                        pixels[y*32+x]=fill?(highlight?Color.Lerp(tones[k],Color.white,.34f):tones[k]):Color.clear;
                    }
                    texture.SetPixels(pixels);texture.Apply();resourceSymbols[k]=texture;
                }
            }
            GUI.DrawTexture(rect,resourceSymbols[Mathf.Clamp(kind,0,5)],ScaleMode.ScaleToFit,true);
        }
    }
}
