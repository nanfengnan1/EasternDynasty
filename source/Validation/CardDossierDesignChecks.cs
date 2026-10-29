using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using EasternDynasty;
using Box = EasternDynasty.CardDossierDesignLayout.Box;

internal static class CardDossierDesignChecks
{
    private static int checks;
    [DataContract] private sealed class Manifest { [DataMember] public Asset[] entries { get; set; } }
    [DataContract] private sealed class Source { [DataMember] public string reference { get; set; } }
    [DataContract] private sealed class Asset
    {
        [DataMember] public string id { get; set; }
        [DataMember] public string status { get; set; }
        [DataMember] public string resourcePath { get; set; }
        [DataMember] public string sha256 { get; set; }
        [DataMember] public int width { get; set; }
        [DataMember] public int height { get; set; }
        [DataMember] public bool hasAlpha { get; set; }
        [DataMember] public bool hasTransparency { get; set; }
        [DataMember] public Source source { get; set; }
    }
    [DataContract] private sealed class Receipt
    {
        [DataMember] public string relativeSource { get; set; }
        [DataMember] public string sourceSha256 { get; set; }
        [DataMember] public string toolVersion { get; set; }
        [DataMember] public Export[] images { get; set; }
    }
    [DataContract] private sealed class Export
    {
        [DataMember] public string relativePath { get; set; }
        [DataMember] public string sha256 { get; set; }
        [DataMember] public int width { get; set; }
        [DataMember] public int height { get; set; }
    }
    private static void Check(bool pass, string message)
    { if (!pass) throw new Exception(message); checks++; }
    private static bool Contains(Box outer, Box inner)
    { return inner.W > 0 && inner.H > 0 && inner.X >= outer.X - .002f && inner.Y >= outer.Y - .002f && inner.X + inner.W <= outer.X + outer.W + .002f && inner.Y + inner.H <= outer.Y + outer.H + .002f; }
    private static bool Separate(Box a, Box b)
    { return a.X + a.W <= b.X + .002f || b.X + b.W <= a.X + .002f || a.Y + a.H <= b.Y + .002f || b.Y + b.H <= a.Y + .002f; }
    private static T Read<T>(string path)
    { using (var stream=File.OpenRead(path)) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream); }
    private static string Hash(string path)
    { using(var stream=File.OpenRead(path)) using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", ""); }
    private static string Local(string root,string relative)
    {
        string path=Path.GetFullPath(Path.Combine(root,relative));
        Check(path.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase), "asset or receipt path stays in this project");
        return path;
    }
    private static void Assets(string root)
    {
        var manifest=Read<Manifest>(Path.Combine(root,"Assets/EasternDynasty/Resources/asset-manifest.json"));
        foreach(string name in new[]{"dossier_attack","dossier_siege","dossier_defense","dossier_speed","dossier_strategy","dossier_skill_ring","nav_book","portrait_base"})
        {
            string id="ui.youzhou."+name;
            var matches=manifest.entries.Where(a=>a.id==id).ToArray();
            Check(matches.Length==1,"one stable identity for "+id);
            var asset=matches[0];
            Check(asset.status=="ready"&&asset.hasAlpha,"controlled PNG is ready with alpha: "+id);
            int expectedWidth=name=="dossier_skill_ring"?144:name=="nav_book"?66:name=="portrait_base"?1040:64;
            int expectedHeight=name=="nav_book"?36:name=="portrait_base"?136:expectedWidth;
            Check(asset.width==expectedWidth&&asset.height==expectedHeight,"controlled source dimensions for "+id);
            string path=Local(root,"Assets/EasternDynasty/Resources/"+asset.resourcePath+".png");
            Check(string.Equals(Hash(path),asset.sha256,StringComparison.OrdinalIgnoreCase),"actual PNG matches manifest SHA: "+id);
            using(var bitmap=new Bitmap(path))
            {
                Check(bitmap.Width==expectedWidth&&bitmap.Height==expectedHeight,"actual image dimensions: "+id);
                bool clear=false,drawn=false;
                for(int y=0;y<bitmap.Height;y++) for(int x=0;x<bitmap.Width;x++)
                { byte alpha=bitmap.GetPixel(x,y).A;clear|=alpha==0;drawn|=alpha>0; }
                Check(drawn,"export contains visible artwork: "+id);
                if(name.StartsWith("dossier_",StringComparison.Ordinal)) Check(clear&&asset.hasTransparency,"standalone symbol keeps transparent surroundings: "+id);
            }
            var receipt=Read<Receipt>(Local(root,asset.source.reference));
            Check(receipt.toolVersion.StartsWith("Inkscape ",StringComparison.Ordinal),"real Inkscape export receipt: "+id);
            Check(string.Equals(Hash(Local(root,receipt.relativeSource)),receipt.sourceSha256,StringComparison.OrdinalIgnoreCase),"editable source matches its receipt: "+id);
            var exports=receipt.images.Where(e=>string.Equals(e.sha256,asset.sha256,StringComparison.OrdinalIgnoreCase)).ToArray();
            Check(exports.Length==1&&exports[0].width==asset.width&&exports[0].height==asset.height,"import is the actual exported component: "+id);
            Check(string.Equals(Hash(Local(root,exports[0].relativePath)),asset.sha256,StringComparison.OrdinalIgnoreCase),"exported PNG matches controlled asset: "+id);
        }
    }
    private static void Screen(float[] screen)
    {
        var layout=MobileScreenLayout.Calculate(screen[0],screen[1],screen[2],screen[3],screen[4],screen[5],screen[6]>0);
        float panelWidth=Math.Min(1280,layout.LogicalWidth-64),panelHeight=Math.Min(820,layout.LogicalHeight-28);
        float panelX=(layout.LogicalWidth-panelWidth)/2,panelY=(layout.LogicalHeight-panelHeight)/2;
        float artRight=panelX+20+panelWidth*.43f,infoX=artRight+28,infoWidth=panelX+panelWidth-infoX-24;
        var viewport=new Box(infoX,panelY+198,infoWidth,panelHeight-282);
        float width=infoWidth-22;
        var content=new Box(0,0,width,CardDossierDesignLayout.ContentHeight);
        var attributes=CardDossierDesignLayout.Attributes(width);
        var status=CardDossierDesignLayout.Status(width);
        var troops=CardDossierDesignLayout.Troops(width);
        Check(CardDossierDesignLayout.ContentHeight<=viewport.H,"complete attribute and both skill actions fit the first viewport");
        Check(Contains(content,attributes)&&Contains(attributes,status)&&Contains(attributes,troops),"status and advanced troops remain in the attribute group");
        Check(status.Y>=56&&status.H>=38,"level/experience has its own line below the heading and source caption");
        var rows=Enumerable.Range(0,5).Select(i=>CardDossierDesignLayout.Stat(width,i)).ToArray();
        for(int i=0;i<rows.Length;i++)
        {
            Check(Contains(attributes,rows[i])&&Separate(rows[i],status)&&Separate(rows[i],troops),"attribute clears level, troops and group edges: "+i);
            for(int j=0;j<i;j++) Check(Separate(rows[i],rows[j]),"attribute cells do not overlap: "+i+"/"+j);
            var parts=new[]{CardDossierDesignLayout.Icon(width,i),CardDossierDesignLayout.Label(width,i),CardDossierDesignLayout.Value(width,i),CardDossierDesignLayout.Growth(width,i)};
            for(int j=0;j<parts.Length;j++)
            {
                Check(Contains(rows[i],parts[j]),"icon, label, value or growth fits its real column: "+i+"/"+j);
                for(int k=0;k<j;k++) Check(Separate(parts[j],parts[k]),"attribute icon and factual text regions remain disjoint: "+i+"/"+j+"/"+k);
            }
            Check(parts[0].W==32&&parts[0].H==32&&parts[1].W>=44&&parts[2].W>=78&&parts[3].W>=100,"inspectable symbol plus Chinese label, base value and growth width retained");
        }
        var skills=new[]{CardDossierDesignLayout.Skill(width,0),CardDossierDesignLayout.Skill(width,1)};
        Check(Separate(skills[0],skills[1])&&Math.Abs(skills[0].Y-skills[1].Y)<.002f,"two skills form one horizontal row with no overlap");
        for(int i=0;i<skills.Length;i++)
        {
            var slot=skills[i];var image=CardDossierDesignLayout.SkillImage(slot);var title=CardDossierDesignLayout.SkillTitle(slot);var name=CardDossierDesignLayout.SkillName(slot);
            Check(Contains(content,slot)&&Separate(slot,attributes)&&slot.W>=44&&slot.H>=44,"skill action remains a full touch target below attributes");
            Check(Contains(slot,image)&&Contains(slot,title)&&Contains(slot,name)&&Separate(image,title)&&Separate(image,name)&&Separate(title,name),"skill image, type and actual name have distinct visible regions");
            Check(image.W==72&&image.H==72&&name.H>=54&&name.W>=190,"skill image and two-line name retain usable area");
            var physical=new Box(layout.X+(viewport.X+slot.X)*layout.Scale,layout.Y+(viewport.Y+slot.Y)*layout.Scale,slot.W*layout.Scale,slot.H*layout.Scale);
            Check(Contains(new Box(layout.X,layout.Y,layout.Width,layout.Height),physical),"actual skill action stays in the physical mobile/desktop safe area");
        }
        Console.WriteLine("CARD_DOSSIER_DESIGN_SCREEN: {0}x{1}; mobile={2}; content={3:0.##}x{4}; viewport={5:0.##}",screen[0],screen[1],screen[6]>0,width,CardDossierDesignLayout.ContentHeight,viewport.H);
    }
    private static void Main(string[] args)
    {
        string root=args[0];
        Check(CardDossierDesignLayout.Style=="inkscape-dossier-inline-v1","design revision is explicit");
        Assets(root);
        foreach(var screen in new[]{
            new[]{1440f,900f,0f,0f,1440f,900f,0f},new[]{1920f,1080f,0f,0f,1920f,1080f,0f},
            new[]{1280f,720f,0f,0f,1280f,720f,0f},new[]{1600f,720f,0f,0f,1600f,720f,0f},
            new[]{1440f,1080f,0f,0f,1440f,1080f,0f},new[]{1600f,720f,56f,18f,1488f,684f,1f},
            new[]{1600f,720f,56f,24f,1504f,684f,1f},new[]{1600f,720f,0f,0f,1600f,720f,1f},
            new[]{1280f,720f,0f,0f,1280f,720f,1f},new[]{2400f,1080f,80f,20f,2240f,1040f,1f},
            new[]{1920f,1200f,0f,0f,1920f,1200f,1f},new[]{2048f,1536f,0f,0f,2048f,1536f,1f}}) Screen(screen);
        Console.WriteLine("CARD_DOSSIER_DESIGN_CHECKS_OK: {0} assertions; 12 desktop/mobile layouts; 8 controlled PNGs with editable-source/export/import SHA. Actual Unity font fit, screenshots and image/name input remain publisher checks.",checks);
    }
}
