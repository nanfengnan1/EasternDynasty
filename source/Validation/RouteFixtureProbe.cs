using System;
using System.Linq;
using EasternDynasty;
public static class RouteFixtureProbe {
 public static void Main() {
  var w=World.CreateYouzhou(0,"Route fixture probe");
  foreach(var target in w.lands.Where(t=>t.capital&&t.x>23).OrderBy(t=>t.x)) {
   var path=w.FindMarchPath(w.homeId,target.id);if(path==null)continue;
   int bridge=path.FindIndex(id=>w.lands[id].bridge),turn=-1;
   for(int i=1;i<path.Count-1;i++){var a=w.lands[path[i-1]];var b=w.lands[path[i]];var c=w.lands[path[i+1]];if(b.x-a.x!=c.x-b.x||b.z-a.z!=c.z-b.z){turn=i;break;}}
   if(bridge<2||bridge>=path.Count-2||turn<1||turn>=bridge)continue;
   target.owned=true;string problem=w.TerritoryOrderProblem(target.id,false);if(problem.Length>0)throw new Exception(problem);
   w.Garrison(target.id);if(!w.IsValid(out problem))throw new Exception(problem);
   double distance=0;for(int i=1;i<=bridge;i++){var a=w.lands[path[i-1]];var b=w.lands[path[i]];distance+=Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.z-b.z)*(a.z-b.z));}
   w.Tick(w.order.duration*distance/w.MarchPathLength(path));if(!w.IsValid(out problem))throw new Exception(problem);
   Console.WriteLine("CORE_FIXTURE_VALID home="+w.homeId+" target="+target.id+" bridgeIndex="+bridge+" bridgeTile="+path[bridge]+" turnIndex="+turn+" count="+path.Count+" path="+string.Join(",",path));return;
  }
  throw new Exception("No natural turning bridge route found.");
 }
}