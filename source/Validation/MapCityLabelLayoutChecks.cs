using System;
using EasternDynasty;
internal static class MapCityLabelLayoutChecks
{
    static int count;
    static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
    static bool Contains(MapHudNavigationLayout.Box a,MapHudNavigationLayout.Box b){return b.X>=a.X&&b.Y>=a.Y&&b.X+b.W<=a.X+a.W+.001f&&b.Y+b.H<=a.Y+a.H+.001f;}
    static void Main()
    {
        foreach(float x in new[]{184f,720f,1014.25f})foreach(float y in new[]{170f,400f,701.333f})foreach(float text in new[]{0f,32f,114f,120f,233.9f,234f,235f,320f,448f})
        {
            var p=MapCityLabelLayout.Panel(x,y,text);var image=MapCityLabelLayout.Image(p);var name=MapCityLabelLayout.Name(p);var detail=MapCityLabelLayout.Detail(p);
            Check(p.W>=184&&p.W<=304&&p.H>=44,"Marker lost touch width/height bounds");
            Check(Math.Abs(p.X+p.W/2-x)<.001f&&Math.Abs(p.Y+p.H-(y-12))<.001f,"Marker no longer anchors above city");
            Check(Contains(p,image)&&Contains(p,name)&&Contains(p,detail)&&Contains(p,MapCityLabelLayout.Crest(p)),"Image/name/status clips");
            Check(image.X+image.W<=name.X&&name.Y+name.H<=detail.Y,"Name/status obscures city image");
            Check(text<=name.W?p.H==52:p.H==72,"Long city name failed to reserve a second line");
        }
        Console.WriteLine("PASS: "+count+" city marker geometry checks; real fonts and clicks await publisher.");
    }
}
