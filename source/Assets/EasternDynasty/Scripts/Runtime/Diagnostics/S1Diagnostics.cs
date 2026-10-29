using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private IEnumerator S1Smoke(int target)
        {
            if(Catalog.Generals.Count(g=>g.s1)!=245)throw new Exception("S1 snapshot roster is incomplete.");
            foreach(var g in Catalog.Generals.Where(g=>g.s1))if(portraits[g.id]==null)throw new Exception("S1 portrait missing: "+g.id);
            tab="领地";map.CenterHome();map.zoom=3.3f;map.selected=target;
            world.Garrison(target);if(world.order==null)throw new Exception("S1 garrison command failed.");
            world.Tick(world.order.duration*.5);
            yield return CaptureState("smoke-s1-garrison-march.png");
            world.Tick(world.order.remaining);
            if(world.order.phase!=1||!world.IsGuarded(target))throw new Exception("S1 garrison arrival failed.");
            yield return CaptureState("smoke-s1-garrison.png");
            world.RecallArmy();world.Tick(world.order.duration*.4);
            yield return CaptureState("smoke-s1-return.png");world.Tick(200);
            int before=world.AvailableCommands;world.Farm(target);
            if(world.order==null||world.AvailableCommands!=before-2)throw new Exception("S1 farming cost failed.");
            yield return CaptureState("smoke-s1-farm-march.png");world.Tick(world.order.remaining);
            if(world.order.phase!=2)throw new Exception("S1 farming did not begin.");
            yield return CaptureState("smoke-s1-farming.png");world.Tick(300);
            if(world.order!=null)throw new Exception("S1 farm return failed.");
            tab="图鉴";s1Faction="全部";s1Archives=true;s1Scroll=Vector2.zero;
            yield return ArchiveVisualSmoke();
            s1Faction="魏";s1Selected=world.S1Pool("魏",true).First().id;
            int cards=world.cards.Count;world.DrawS1(true,"魏",new System.Random(731));
            if(world.cards.Count!=cards+1||world.cards.Last().Definition.faction!="魏")throw new Exception("S1 faction recruitment failed.");
            drawResult=world.cards.Last();yield return CaptureState("smoke-s1-draw.png");drawResult=null;
            Debug.Log("S1_RUNTIME_OK: 245 portraits, faction archive/recruitment, marching, garrison coverage, recall, farming and return.");
        }
    }
}
