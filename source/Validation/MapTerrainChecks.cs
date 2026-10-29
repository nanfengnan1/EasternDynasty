using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace EasternDynasty
{
    public static class MapTerrainChecks
    {
        private static int checks;
        private static void Require(bool condition,string message)
        { checks++; if(!condition)throw new Exception(message); }
        private static byte[] State(World world)
        { using(var s=new MemoryStream()){new DataContractJsonSerializer(typeof(World)).WriteObject(s,world);return s.ToArray();} }
        private static World Reload(byte[] bytes)
        { using(var s=new MemoryStream(bytes))return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(s); }
        private static bool Same(YouzhouTerrainLayout.Patch a,YouzhouTerrainLayout.Patch b)
        { return a.image==b.image&&a.xMin==b.xMin&&a.xMax==b.xMax&&a.zMin==b.zMin&&a.zMax==b.zMax; }
        private static void CheckWorld(World world)
        {
            byte[] before=State(world);var reloaded=Reload(before);var widths=new HashSet<int>();int count=0;
            foreach(var land in world.lands)
            {
                YouzhouTerrainLayout.Patch p,q;
                bool drawn=YouzhouTerrainLayout.TryPatch(world,land,out p);
                bool resource=land.kind==LandKind.Grain||land.kind==LandKind.Pasture||land.kind==LandKind.Stone||land.kind==LandKind.Iron;
                Require(drawn==(resource&&!land.home&&!land.capital&&!land.bridge&&!world.IsCityDistrict(land.id)),"Missing resource or artwork drawn over a city/river/mountain.");
                if(!drawn)continue;
                count++;
                Require(p.xMin>land.x-.5f&&p.xMax<land.x+.5f&&p.zMin>land.z-.5f&&p.zMax<land.z+.5f,"Artwork escapes its actual selectable cell.");
                Require(p.xMin<land.x&&p.xMax>land.x&&p.zMin<land.z&&p.zMax>land.z,"Artwork no longer represents its own cell centre.");
                float area=(p.xMax-p.xMin)*(p.zMax-p.zMin);
                Require(area>.48f&&area<.72f,"Map patch loses all readable detail or fills the cell again.");
                int expected=land.kind==LandKind.Grain?0:land.kind==LandKind.Pasture?1:land.kind==LandKind.Stone?2:3;
                Require(p.image==expected,"Resource artwork identity changed.");
                Require(YouzhouTerrainLayout.TryPatch(reloaded,reloaded.TryGetLand(land.x,land.z),out q)&&Same(p,q),"Reload changes landscape composition.");
                float meadow=YouzhouTerrainLayout.MeadowX(p,land.x);
                Require((meadow<p.xMin||meadow>p.xMax)&&Math.Abs(meadow-land.x)<.461f&&(int)Math.Floor(meadow+.5f)==land.x,"Exposed meadow does not pick the original logical cell with a pixel-rounding margin.");
                widths.Add((int)Math.Round((p.xMax-p.xMin)*1000));
            }
            Require(count>1000&&widths.Count>50,"Resources lost or repeated full-size stamp layout returned.");
            Require(before.SequenceEqual(State(world)),"Rendering layout changed the complete World.");
        }
        public static int Main()
        {
            try
            {
                for(int start=0;start<3;start++)CheckWorld(World.CreateYouzhou(start));
                var world=World.CreateYouzhou(0);var target=world.lands.First(t=>world.AttackProblem(t.id).Length==0);
                YouzhouTerrainLayout.Patch before,after;
                Require(YouzhouTerrainLayout.TryPatch(world,target,out before),"Real attack target lacks a resource patch.");
                string problem=world.March(target.id);Require(world.order!=null,"Failed to issue the actual capture order: "+problem);
                world.Tick(300);Require(target.owned&&world.order==null,"Capture/return fixture failed.");
                Require(YouzhouTerrainLayout.TryPatch(world,target,out after)&&Same(before,after),"Capturing land shifts its artwork or falsely changes type.");
                CheckWorld(world);
                target.level+=1;world.elapsed+=100;target.name="changed display label";
                Require(YouzhouTerrainLayout.TryPatch(world,target,out after)&&Same(before,after),"Time, name or level changes invent new resource imagery.");
                YouzhouTerrainLayout.Patch empty;
                Require(!YouzhouTerrainLayout.TryPatch(null,target,out empty)&&!YouzhouTerrainLayout.TryPatch(world,null,out empty),"Null input created a patch.");
                Console.WriteLine("MAP_TERRAIN_CHECKS_OK: "+checks+" geometry/identity/reload/capture/meadow-selection assertions; no Unity, shader execution or visual acceptance.");return 0;
            }
            catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        }
    }
}
