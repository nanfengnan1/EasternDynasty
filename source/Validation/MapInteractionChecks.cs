using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using EasternDynasty;

public static class MapInteractionChecks
{
    private static int checks;
    private static void Check(bool result, string message) { if (!result) throw new Exception(message); checks++; }
    private static void Near(double a, double b, string message) { Check(Math.Abs(a-b)<.000001, message+": "+a+" / "+b); }
    private static World FlatWorld()
    {
        var w = World.CreateYouzhou();
        foreach (var tile in w.lands) { tile.kind=LandKind.Grain; tile.bridge=false; }
        return w;
    }
    private static World Copy(World w)
    {
        var serializer=new DataContractJsonSerializer(typeof(World));
        using(var bytes=new MemoryStream()){serializer.WriteObject(bytes,w);bytes.Position=0;return (World)serializer.ReadObject(bytes);}
    }
    private static void Valid(World w) { string problem; Check(w.IsValid(out problem), problem); }
    private static void CheckPath(World w, List<int> path)
    {
        Check(path!=null && path.Count>0,"path missing");
        for(int i=1;i<path.Count;i++)Check(w.CanMarchStep(path[i-1],path[i],2),"path has illegal step");
    }
    public static int Main()
    {
        try
        {
            var gesture=new MapPointerGesture();
            gesture.Begin(0,100,100);Check(!gesture.Move(103,102,true),"small click motion starts drag");Check(gesture.Release(0,true),"small click did not select");
            gesture.Begin(0,100,100);Check(gesture.Move(110,100,true),"left drag did not start");Check(!gesture.Release(0,true),"drag also selected a tile");
            gesture.Begin(0,100,100);gesture.Move(115,100,true);gesture.Move(100,100,true);Check(!gesture.Release(0,true),"returning to down point converted drag into click");
            gesture.Begin(0,100,100);Check(!gesture.Move(130,100,false)&&gesture.Button==-1,"UI crossing did not cancel gesture");Check(!gesture.Release(0,true),"cancelled UI gesture selected");
            gesture.Begin(0,100,100);Check(!gesture.Release(0,false),"release outside map selected");
            for(int button=1;button<=2;button++){gesture.Begin(button,20,20);Check(gesture.Move(21,21,true),"right/middle drag unavailable");Check(!gesture.Release(button,true),"right/middle button selected");}
            gesture.Begin(0,10,10);Check(!gesture.Release(1,true)&&gesture.Button==0,"other button release stole gesture");gesture.Cancel();
            Near(MapPointerGesture.EdgeAxis(100,0,200,26),0,"centre drifts");Near(MapPointerGesture.EdgeAxis(0,0,200,26),-1,"left edge scroll");Near(MapPointerGesture.EdgeAxis(200,0,200,26),1,"right edge scroll");
            Near(MapPointerGesture.EdgeAxis(-1,0,200,26),0,"outside screen scrolls");Near(MapPointerGesture.EdgeAxis(201,0,200,26),0,"outside right scrolls");Near(MapPointerGesture.EdgeAxis(13,0,200,26),-.5,"edge acceleration");

            var w=FlatWorld();int centre=w.TryGetLand(10,10).id;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)Check(w.AreAdjacent(centre,w.TryGetLand(10+dx,10+dz).id)==(dx!=0||dz!=0),"eight-direction adjacency differs");
            Check(!w.AreAdjacent(centre,w.TryGetLand(12,10).id),"two-cell gap adjacent");Check(!w.AreAdjacent(-1,centre),"invalid tile adjacent");
            Check(w.lands.Count(t=>w.AreAdjacent(0,t.id))==3,"corner neighbors overflow");Check(w.lands.Count(t=>w.AreAdjacent(10,t.id))==5,"edge neighbors overflow");
            foreach(var t in w.lands)t.owned=false;w.lands[centre].owned=true;
            Check(w.HasAdjacentOwnedLand(w.TryGetLand(11,11).id),"diagonal territory not connected");Check(!w.HasAdjacentOwnedLand(w.TryGetLand(12,12).id),"distant territory connected");
            w=FlatWorld();var home=w.lands[w.homeId];foreach(var t in w.lands)t.owned=t.home;
            int diagonal=w.TryGetLand(home.x+1,home.z+1).id;
            Check(w.AttackProblem(diagonal)=="","diagonal target cannot be attacked");
            var legacy=World.Create(Homeland.Han,4,"Legacy");var oldHome=legacy.lands[legacy.homeId];
            Check(!legacy.AreAdjacent(legacy.homeId,legacy.TryGetLand(oldHome.x+1,oldHome.z+1).id),"legacy adjacency silently changed");

            w=FlatWorld();int start=w.TryGetLand(4,4).id,end=w.TryGetLand(7,6).id;
            var path=w.FindMarchPath(start,end);CheckPath(w,path);Near(w.MarchPathLength(path),1+Math.Sqrt(2)*2,"weighted shortest diagonal route");
            Check(path.First()==start&&path.Last()==end,"route endpoints differ");
            Check(w.FindMarchPath(-1,end)==null,"invalid from accepted");Check(w.FindMarchPath(start,1600)==null,"invalid to accepted");Check(w.FindMarchPath(start,start).SequenceEqual(new[]{start}),"same-tile route");
            int diagonalStep=w.TryGetLand(5,5).id;
            Check(w.CanMarchStep(start,diagonalStep,2),"open diagonal blocked");Check(!w.CanMarchStep(start,diagonalStep,1),"legacy route accepts diagonal");
            w.TryGetLand(5,4).kind=LandKind.Mountain;Check(!w.CanMarchStep(start,diagonalStep,2),"diagonal cuts blocked mountain corner");
            w.TryGetLand(5,4).kind=LandKind.Grain;w.TryGetLand(4,5).kind=LandKind.Water;Check(!w.CanMarchStep(start,diagonalStep,2),"diagonal cuts river corner");
            w.TryGetLand(4,5).kind=LandKind.Grain;w.TryGetLand(5,5).bridge=true;Check(!w.CanMarchStep(start,diagonalStep,2),"diagonal shortcut onto bridge");
            w.TryGetLand(5,5).bridge=false;w.TryGetLand(5,4).bridge=true;Check(!w.CanMarchStep(start,diagonalStep,2),"diagonal bypasses bridge corner");Check(w.CanMarchStep(start,w.TryGetLand(5,4).id,2),"orthogonal bridge entrance blocked");

            w=FlatWorld();for(int z=0;z<40;z++)w.TryGetLand(6,z).kind=LandKind.Water;
            var bridge=w.TryGetLand(6,10);bridge.kind=LandKind.Grain;bridge.bridge=true;
            start=w.TryGetLand(4,4).id;end=w.TryGetLand(8,4).id;path=w.FindMarchPath(start,end);CheckPath(w,path);
            Check(path.Contains(bridge.id),"river route did not use bridge");Check(path.Any(i=>w.lands[i].z==10),"route stayed on straight start-end line");
            bridge.kind=LandKind.Water;bridge.bridge=false;Check(w.FindMarchPath(start,end)==null,"disconnected river crossed");

            w=FlatWorld();home=w.lands[w.homeId];
            var first=w.TryGetLand(home.x+1,home.z);var last=w.TryGetLand(home.x+2,home.z+1);last.owned=true;
            w.order=new ArmyOrder{target=last.id,action=1,routeVersion=2,path=new List<int>{home.id,first.id,last.id},routeStart=0,routeEnd=1};
            double total=1+Math.Sqrt(2);w.order.duration=8+total*4;w.order.remaining=w.order.duration*(1-(1+Math.Sqrt(2)*.25)/total);
            double x,zPos;w.ArmyPosition(out x,out zPos);Near(x,home.x+1.25,"weighted path interpolation x");Near(zPos,home.z+.25,"weighted path interpolation z");Valid(w);
            w.RecallArmy();double recalledX,recalledZ;w.ArmyPosition(out recalledX,out recalledZ);Near(x,recalledX,"recall teleports x");Near(zPos,recalledZ,"recall teleports z");
            var copy=Copy(w);Valid(copy);copy.ArmyPosition(out recalledX,out recalledZ);Near(x,recalledX,"saved return changes x");Near(zPos,recalledZ,"saved return changes z");copy.Tick(1000);Check(copy.order==null,"return never completes");
            var corrupt=Copy(w);corrupt.order.routeVersion=1;string error;Check(!corrupt.IsValid(out error),"v1 diagonal save accepted");
            corrupt=Copy(w);corrupt.TryGetLand(home.x+2,home.z).kind=LandKind.Water;Check(!corrupt.IsValid(out error),"v2 saved corner cutting accepted");

            w=FlatWorld();home=w.lands[w.homeId];first=w.TryGetLand(home.x+1,home.z);last=w.TryGetLand(home.x+1,home.z+1);last.owned=true;
            w.order=new ArmyOrder{target=last.id,action=1,routeVersion=1,path=new List<int>{home.id,first.id,last.id},routeStart=0,routeEnd=1,duration=16,remaining=4};
            Valid(w);w.ArmyPosition(out x,out zPos);Near(x,home.x+1,"v1 path x changed");Near(zPos,home.z+.5,"v1 path progress changed");w.RecallArmy();Near(w.order.duration,12,"v1 recall duration changed");
            copy=Copy(w);Valid(copy);copy.Tick(1000);Check(copy.order==null,"v1 return never completes");
            Console.WriteLine("MAP_INTERACTION_CHECKS_OK: "+checks);return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
