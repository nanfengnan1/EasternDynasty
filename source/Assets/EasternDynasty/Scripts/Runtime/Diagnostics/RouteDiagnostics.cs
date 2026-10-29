using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        [Serializable] private sealed class RouteRuntimeReceipt
        {
            public string status, error, savedFile, verifiedUtc, captureMethod;
            public int width, height, home, target, bridge, turn, checks;
            public bool isolatedSave, restored, continuedAfterLoad, unreachableRejected;
            public int[] path;
            public List<RouteRuntimeSample> samples = new List<RouteRuntimeSample>();
        }
        [Serializable] private sealed class RouteRuntimeSample
        {
            public string stage, screenshot;
            public double expectedX, expectedZ, renderedX, renderedZ, progress;
            public int renderedRouteVertices;
        }
        private IEnumerator RouteRuntimeCheck()
        {
            string output = null;
            var args = Environment.GetCommandLineArgs();
            for (int i=0;i+1<args.Length;i++) if(args[i]=="-edRouteOutput")output=Path.GetFullPath(args[i+1]);
            if(string.IsNullOrWhiteSpace(output)){Debug.LogError("Route diagnostic output argument is required.");Application.Quit(1);yield break;}
            Directory.CreateDirectory(output);
            var receipt=new RouteRuntimeReceipt{status="running",isolatedSave=true,captureMethod="map-camera-rendertexture"};
            // Advance the iterator ourselves so assertions after a frame yield are
            // caught and produce a failed receipt instead of an orphaned Player.
            var body=RouteRuntimeBody(output,receipt);
            Exception failure=null;
            while(true)
            {
                bool more=false;object wait=null;
                try {more=body.MoveNext();if(more)wait=body.Current;}
                catch(Exception e){failure=e;}
                if(failure!=null||!more)break;
                yield return wait;
            }
            receipt.width=Screen.width;receipt.height=Screen.height;receipt.verifiedUtc=DateTime.UtcNow.ToString("o");
            if(failure!=null||smokeFailed)
            {
                receipt.status="failed";receipt.error=failure==null?"Player reported an error.":failure.ToString();
                File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(receipt,true));
                Debug.LogError(receipt.error);Application.Quit(1);yield break;
            }
            receipt.status="passed";File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(receipt,true));
            Debug.Log("ROUTE_RUNTIME_OK: rendered turn, bridge crossing, isolated JSON restore, continuation and unreachable rejection; "+receipt.checks+" checks.");
            Application.Quit(0);
        }
        private static void RouteCheck(bool condition,string message,RouteRuntimeReceipt receipt)
        {if(!condition)throw new InvalidOperationException("Route runtime check: "+message);receipt.checks++;}
        private IEnumerator RouteRuntimeBody(string output,RouteRuntimeReceipt receipt)
        {
            while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
            for(int i=0;i<25;i++)yield return null;
            var clock=new DateTime(2026,10,25,0,0,0,DateTimeKind.Utc);
            var storage=Path.Combine(output,"isolated-save");
            var repository=new JsonCampaignRepository(storage);
            session=new CampaignSession(repository,()=>clock,true);session.StartYouzhou(0,"路线验证");
            useYouzhou=true;ActivateCampaign();speed=0;tab="领地";map.edgeScrollEnabled=false;
            // Disabled behaviours keep their coroutines alive. This isolated check
            // renders the map without an interactive HUD or desktop input; updates
            // below still execute the real MapView path/army rendering code.
            enabled=false;map.enabled=false;map.inputEnabled=false;
            receipt.home=world.homeId;
            List<int> path=null;
            foreach(var candidate in world.lands.Where(t=>t.capital&&t.x>23).OrderBy(t=>t.x))
            {
                var proposed=world.FindMarchPath(world.homeId,candidate.id);
                if(proposed==null)continue;
                int bridgeIndex=proposed.FindIndex(id=>world.lands[id].bridge);
                int turnIndex=RouteFirstTurn(world,proposed);
                if(bridgeIndex<2||bridgeIndex>=proposed.Count-2||turnIndex<1||turnIndex>=bridgeIndex)continue;
                path=proposed;receipt.target=candidate.id;receipt.bridge=bridgeIndex;receipt.turn=turnIndex;break;
            }
            RouteCheck(path!=null,"natural Youzhou map lacks a turning bridge route",receipt);
            // Own only the destination in this transient fixture; terrain/pathfinding
            // remain the real map. This is movement verification, not siege balancing.
            world.lands[receipt.target].owned=true;
            string problem=world.TerritoryOrderProblem(receipt.target,false);
            RouteCheck(problem.Length==0,problem,receipt);
            world.Garrison(receipt.target);
            RouteCheck(world.order!=null&&world.order.routeVersion==2,"new routed order missing",receipt);
            RouteCheck(world.order.path.SequenceEqual(path),"issued route differs from selected natural route",receipt);
            receipt.path=path.ToArray();map.selected=-1;
            var firstTurn=world.lands[path[receipt.turn]];
            var afterTurn=world.lands[path[receipt.turn+1]];
            double cornerX=firstTurn.x+(afterTurn.x-firstTurn.x)*.35;
            double cornerZ=firstTurn.z+(afterTurn.z-firstTurn.z)*.35;
            double total=world.MarchPathLength(path);
            double turnDistance=RouteDistanceTo(world,path,receipt.turn)+Math.Sqrt(Math.Pow(afterTurn.x-firstTurn.x,2)+Math.Pow(afterTurn.z-firstTurn.z,2))*.35;
            RouteAdvance(world,turnDistance/total);
            map.focus=new Vector3((float)cornerX,0,(float)cornerZ);map.zoom=5.3f;
            for(int i=0;i<8;i++)yield return null;
            var turnSample=RouteRenderedSample("turn",output,cornerX,cornerZ,receipt);
            receipt.samples.Add(turnSample);RouteCaptureCamera(turnSample.screenshot);
            for(int i=0;i<3;i++)yield return null;

            var bridge=world.lands[path[receipt.bridge]];
            RouteAdvance(world,RouteDistanceTo(world,path,receipt.bridge)/total);
            map.focus=new Vector3(bridge.x,0,bridge.z);map.zoom=4.4f;
            for(int i=0;i<8;i++)yield return null;
            var bridgeSample=RouteRenderedSample("bridge",output,bridge.x,bridge.z,receipt);
            receipt.samples.Add(bridgeSample);
            RouteCheck(bridge.bridge,"sample is not on an actual bridge tile",receipt);
            RouteCaptureCamera(bridgeSample.screenshot);
            for(int i=0;i<3;i++)yield return null;

            world.savedUtcTicks=clock.Ticks;
            repository.Save(world);receipt.savedFile=Path.Combine(storage,JsonCampaignRepository.YouzhouFileName);
            RouteCheck(File.Exists(receipt.savedFile),"isolated Unity save was not written",receipt);
            double remaining=world.order.remaining;
            session=new CampaignSession(repository,()=>clock,true);bool restoredBackup;session.Load(true,out restoredBackup);
            RouteCheck(!restoredBackup,"isolated restore unexpectedly used a backup",receipt);
            RouteCheck(world.order!=null&&world.order.path.SequenceEqual(path)&&Math.Abs(world.order.remaining-remaining)<.000001,"JSON load changed marching progress",receipt);
            ActivateCampaign();speed=0;map.edgeScrollEnabled=false;map.focus=new Vector3(bridge.x,0,bridge.z);map.zoom=4.4f;
            for(int i=0;i<8;i++)yield return null;
            RouteRenderedSample("restored-bridge",output,bridge.x,bridge.z,receipt);receipt.restored=true;
            int beyond=receipt.bridge+2;
            RouteAdvance(world,RouteDistanceTo(world,path,beyond)/total);
            var afterBridge=world.lands[path[beyond]];map.focus=new Vector3(afterBridge.x,0,afterBridge.z);
            for(int i=0;i<8;i++)yield return null;
            var restoredSample=RouteRenderedSample("after-load",output,afterBridge.x,afterBridge.z,receipt);
            receipt.samples.Add(restoredSample);RouteCaptureCamera(restoredSample.screenshot);
            for(int i=0;i<3;i++)yield return null;

            world.Tick(world.order.remaining+.001);
            RouteCheck(world.order!=null&&world.order.phase==(int)ArmyPhase.Stationed,"restored army did not reach destination",receipt);
            var destination=world.lands[receipt.target];map.focus=new Vector3(destination.x,0,destination.z);
            for(int i=0;i<8;i++)yield return null;
            var arrivalSample=RouteRenderedSample("arrived",output,destination.x,destination.z,receipt);
            receipt.samples.Add(arrivalSample);receipt.continuedAfterLoad=true;RouteCaptureCamera(arrivalSample.screenshot);
            for(int i=0;i<3;i++)yield return null;

            // Separate runtime fixture: a legal land target enclosed by mountains.
            // It is never installed in the rendered session or written to player saves.
            var unreachable=JsonUtility.FromJson<World>(JsonUtility.ToJson(world));unreachable.order=null;
            var enclosure=unreachable.lands[receipt.target];enclosure.capital=false;enclosure.bridge=false;enclosure.kind=LandKind.Grain;enclosure.owned=true;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                if(dx!=0||dz!=0){var side=unreachable.TryGetLand(enclosure.x+dx,enclosure.z+dz);if(side!=null){side.kind=LandKind.Mountain;side.bridge=false;}}
            RouteCheck(unreachable.FindMarchPath(unreachable.homeId,enclosure.id)==null,"enclosed legal land was reachable",receipt);
            RouteCheck(unreachable.TerritoryOrderProblem(enclosure.id,false).Contains("没有可通行"),"unreachable order lacks a route error",receipt);
            unreachable.Garrison(enclosure.id);RouteCheck(unreachable.order==null,"unreachable order mutated the army",receipt);receipt.unreachableRejected=true;
        }
        private static int RouteFirstTurn(World state,IList<int> path)
        {
            for(int i=1;i<path.Count-1;i++)
            {
                var a=state.lands[path[i-1]];var b=state.lands[path[i]];var c=state.lands[path[i+1]];
                if(b.x-a.x!=c.x-b.x||b.z-a.z!=c.z-b.z)return i;
            }
            return -1;
        }
        private static double RouteDistanceTo(World state,IList<int> path,int index)
        {
            double distance=0;
            for(int i=1;i<=index;i++){var a=state.lands[path[i-1]];var b=state.lands[path[i]];distance+=Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.z-b.z)*(a.z-b.z));}
            return distance;
        }
        private static void RouteAdvance(World state,double progress)
        {
            double desiredRemaining=state.order.duration*(1-progress);
            if(desiredRemaining>state.order.remaining+.000001)throw new InvalidOperationException("Route diagnostic attempted to rewind time.");
            state.Tick(Math.Max(0,state.order.remaining-desiredRemaining));
        }
        private RouteRuntimeSample RouteRenderedSample(string stage,string output,double expectedX,double expectedZ,RouteRuntimeReceipt receipt)
        {
            map.SetViewport(new Rect(0,0,Screen.width,Screen.height));
            map.inputEnabled=false;map.SendMessage("Update",SendMessageOptions.RequireReceiver);
            var army=map.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Marching army banner"&&t.gameObject.activeInHierarchy);
            RouteCheck(army!=null,"actual rendered army is absent at "+stage,receipt);
            RouteCheck(Math.Abs(army.position.x-expectedX)<.002&&Math.Abs(army.position.z-expectedZ)<.002,"rendered army is not on the expected path segment at "+stage,receipt);
            var rendered=map.GetComponentsInChildren<LineRenderer>().FirstOrDefault(line=>line.name=="March route");
            RouteCheck(rendered!=null,"actual route LineRenderer missing",receipt);
            if(world.order.phase==(int)ArmyPhase.Marching)
            {
                RouteCheck(rendered.positionCount==receipt.path.Length,"rendered route vertex count differs",receipt);
                for(int i=0;i<rendered.positionCount;i++)
                {var p=rendered.GetPosition(i);var t=world.lands[receipt.path[i]];RouteCheck(Math.Abs(p.x-t.x)<.002&&Math.Abs(p.z-t.z)<.002,"rendered line cuts across route at vertex "+i,receipt);}
            }
            else RouteCheck(rendered.positionCount==0,"arrived army retains an active marching line",receipt);
            var sample=new RouteRuntimeSample{stage=stage,screenshot=stage=="restored-bridge"?"":Path.Combine(output,"route-"+stage+".png"),expectedX=expectedX,expectedZ=expectedZ,renderedX=army.position.x,renderedZ=army.position.z,progress=world.ArmyRouteProgress,renderedRouteVertices=rendered.positionCount};
            Debug.Log("ROUTE_RENDERED_SAMPLE "+JsonUtility.ToJson(sample));return sample;
        }
        private void RouteCaptureCamera(string file)
        {
            // A hidden Player's backbuffer can remain black even when the actual
            // scene is valid. Render the unchanged MapView camera explicitly into
            // an offscreen texture, independent of the desktop/swapchain or HUD.
            const int captureWidth=1440,captureHeight=900;
            Camera camera=map.viewCamera;
            var previousTarget=camera.targetTexture;
            var previousActive=RenderTexture.active;
            Rect previousRect=camera.rect;float previousAspect=camera.aspect;
            var target=RenderTexture.GetTemporary(captureWidth,captureHeight,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(captureWidth,captureHeight,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.aspect=(float)captureWidth/captureHeight;
                camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,captureWidth,captureHeight),0,0);pixels.Apply(false,false);
                File.WriteAllBytes(file,pixels.EncodeToPNG());
                Debug.Log("ROUTE_CAMERA_CAPTURE "+file);
            }
            finally
            {
                camera.targetTexture=previousTarget;camera.rect=previousRect;camera.aspect=previousAspect;
                RenderTexture.active=previousActive;RenderTexture.ReleaseTemporary(target);Destroy(pixels);
            }
        }
    }
}
