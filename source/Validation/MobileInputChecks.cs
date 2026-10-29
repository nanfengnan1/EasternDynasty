using System;
using EasternDynasty;

internal static class MobileInputChecks
{
    private static int checks;
    private static void Check(bool pass,string name) {if(!pass)throw new Exception(name);checks++;}
    private static void Near(double actual,double expected,string name){Check(Math.Abs(actual-expected)<.001,name);}
    private static MobileTouchPoint T(int id,float x,float y,MobileTouchPhase phase,bool allowed=true)
    {return new MobileTouchPoint{Id=id,X=x,Y=y,Phase=phase,Allowed=allowed};}
    private static MobileMapMotion One(MobileMapGesture g,MobileTouchPoint t){return g.Step(1,t,default(MobileTouchPoint),12);}
    private static void Lift(MobileMapGesture g){g.Step(0,default(MobileTouchPoint),default(MobileTouchPoint),12);}
    private static void Main()
    {
        var g=new MobileMapGesture();
        var m=One(g,T(4,100,100,MobileTouchPhase.Began));Check(!m.Tap&&!m.Pan,"begin is not a tap");
        m=One(g,T(4,104,103,MobileTouchPhase.Moved));Check(!m.Pan,"finger jitter stays tap");
        m=One(g,T(4,104,103,MobileTouchPhase.Ended));Check(m.Tap&&!m.Pan,"release commits one tap");
        m=One(g,T(4,104,103,MobileTouchPhase.Ended));Check(!m.Tap,"duplicate end cannot tap again");
        Lift(g);One(g,T(4,100,100,MobileTouchPhase.Began));
        m=One(g,T(4,113,100,MobileTouchPhase.Moved));Check(m.Pan&&!m.Tap&&g.Dragging,"drag passes threshold");
        Near(m.PreviousX,100,"drag previous x");Near(m.X,113,"drag current x");
        m=One(g,T(4,120,105,MobileTouchPhase.Ended));Check(!m.Tap,"drag release cannot select land");
        Lift(g);One(g,T(4,100,100,MobileTouchPhase.Began,false));
        m=One(g,T(4,200,200,MobileTouchPhase.Moved));Check(!m.Pan,"UI-start cannot become map drag");
        m=One(g,T(4,200,200,MobileTouchPhase.Ended));Check(!m.Tap,"UI-start cannot release into land");
        Lift(g);One(g,T(1,20,20,MobileTouchPhase.Began));
        One(g,T(1,30,30,MobileTouchPhase.Moved,false));
        m=One(g,T(1,22,22,MobileTouchPhase.Ended));Check(!m.Tap&&!m.Pan,"crossing a blocked HUD cancels gesture");
        Lift(g);One(g,T(1,20,20,MobileTouchPhase.Began));
        m=One(g,T(1,20,20,MobileTouchPhase.Cancelled));Check(!m.Tap,"OS cancelled contact cannot select");
        Lift(g);m=One(g,T(1,20,20,MobileTouchPhase.Moved));Check(!m.Tap&&!m.Pan,"ongoing contact cannot start after focus return");
        Lift(g);One(g,T(1,20,20,MobileTouchPhase.Began));g.Cancel();
        m=One(g,T(1,20,20,MobileTouchPhase.Ended));Check(!m.Tap,"explicit focus cancellation discards pending tap");
        Lift(g);One(g,T(1,20,20,MobileTouchPhase.Began));
        m=One(g,T(2,20,20,MobileTouchPhase.Ended));Check(!m.Tap,"replacement finger cannot complete a tap");

        Lift(g);One(g,T(1,100,200,MobileTouchPhase.Began));
        m=g.Step(2,T(1,100,200,MobileTouchPhase.Stationary),T(2,200,200,MobileTouchPhase.Began),12);
        Check(g.Pinching&&!m.Pinch&&!m.Tap,"second finger establishes pinch baseline");
        m=g.Step(2,T(1,50,200,MobileTouchPhase.Moved),T(2,250,200,MobileTouchPhase.Moved),12);
        Check(m.Pinch&&m.Pan&&!m.Tap,"two fingers produce pinch motion");Near(m.ZoomRatio,.5,"spread zooms toward map");
        Near(m.X,150,"pinch midpoint x");Near(m.PreviousX,150,"pinch prior midpoint x");
        m=g.Step(2,T(2,200,210,MobileTouchPhase.Moved),T(1,100,210,MobileTouchPhase.Moved),12);
        Near(m.ZoomRatio,2,"pinch closes fingers to zoom out");Near(m.Y,210,"pinch midpoint pan");
        Check(m.Pinch,"finger-index reorder preserves identities");
        m=One(g,T(1,100,210,MobileTouchPhase.Stationary));Check(!m.Tap&&!m.Pan,"pinch to one finger stays suppressed");
        m=One(g,T(1,100,210,MobileTouchPhase.Ended));Check(!m.Tap,"remaining pinch finger cannot select");
        Lift(g);One(g,T(7,100,100,MobileTouchPhase.Began));m=One(g,T(7,100,100,MobileTouchPhase.Ended));Check(m.Tap,"tap works after all pinch fingers lift");
        Lift(g);g.Step(2,T(1,0,0,MobileTouchPhase.Began),T(2,10,0,MobileTouchPhase.Began),12);
        m=g.Step(2,T(1,0,0,MobileTouchPhase.Moved),T(2,0,0,MobileTouchPhase.Moved),12);Near(m.ZoomRatio,1,"zero pinch distance stays finite");
        Lift(g);g.Step(2,T(1,0,0,MobileTouchPhase.Began),T(2,20,0,MobileTouchPhase.Began,false),12);
        m=g.Step(2,T(1,0,0,MobileTouchPhase.Moved),T(2,40,0,MobileTouchPhase.Moved),12);Check(!m.Pinch,"UI second finger blocks map pinch");
        Lift(g);g.Step(2,T(1,0,0,MobileTouchPhase.Began),T(2,20,0,MobileTouchPhase.Began),12);
        m=g.Step(2,T(1,0,0,MobileTouchPhase.Moved),T(3,40,0,MobileTouchPhase.Began),12);Check(!m.Pinch,"changed pinch pair cancels");
        Lift(g);g.Step(3,T(1,0,0,MobileTouchPhase.Began),T(2,20,0,MobileTouchPhase.Began),12);
        m=One(g,T(1,0,0,MobileTouchPhase.Ended));Check(!m.Tap,"third finger cannot leave a tap behind");

        var desktop=MobileScreenLayout.Calculate(1440,900,84,24,1272,864,false);
        Near(desktop.X,0,"desktop x unchanged");Near(desktop.Y,0,"desktop y unchanged");Near(desktop.Scale,1,"desktop scale unchanged");
        var phone=MobileScreenLayout.Calculate(2400,1080,84,20,2246,1048,true);
        Near(phone.X,84,"phone safe left");Near(phone.Y,12,"safe bottom-left converted to top-left");Near(phone.LogicalHeight,820,"phone readable logical height");
        Near(phone.X+phone.LogicalWidth*phone.Scale,2330,"right HUD stays within safe region");
        Near(phone.Y+phone.LogicalHeight*phone.Scale,1060,"bottom HUD stays above navigation inset");
        var tablet=MobileScreenLayout.Calculate(1600,1200,0,24,1600,1176,true);
        Check(tablet.LogicalWidth>=1439.9&&tablet.LogicalHeight>=820,"tablet fits fixed minimum layouts");
        var small=MobileScreenLayout.Calculate(480,320,0,0,480,320,true);Check(small.LogicalWidth>=1439.9,"small phone fits rather than cropping navigation");
        var invalid=MobileScreenLayout.Calculate(1920,1080,0,0,0,0,true);Near(invalid.Width,1920,"invalid platform safe area falls back to screen");
        var mirrored=MobileScreenLayout.Calculate(2400,1080,70,20,2246,1048,true);Near(mirrored.X,70,"rotated notch changes safe left");

        Check(MobileNavigation.Back(true,true,true,"图鉴",3)==MobileBackAction.CancelConfirmation,"back cancels top confirmation");
        Check(MobileNavigation.Back(false,false,false,"练兵",3,trainingHistory:true)==MobileBackAction.TrainingPlan,"training history back returns to the plan before its origin");
        Check(MobileNavigation.Back(false,true,false,"练兵",3,trainingHistory:true)==MobileBackAction.CloseCard,"history card closes before leaving the recorded result");
        Check(MobileNavigation.Back(true,true,false,"练兵",3,trainingHistory:true)==MobileBackAction.CancelConfirmation,"history confirmation remains the top back layer");
        Check(MobileNavigation.Back(false,false,false,"领地",3,trainingHistory:true)==MobileBackAction.ClearLand,"stale training history flag cannot intercept another tab");
        Check(MobileNavigation.Back(false,true,true,"图鉴",3)==MobileBackAction.CloseCard,"back closes full card first");
        Check(MobileNavigation.Back(false,false,true,"招募",-1)==MobileBackAction.CloseDraw,"back dismisses draw without another draw");
        Check(MobileNavigation.Back(false,false,false,"图鉴",3)==MobileBackAction.Recruit,"back restores recruitment from archive");
        Check(MobileNavigation.Back(false,false,false,"战斗回放",3)==MobileBackAction.BattleReports,"replay returns to its battle report");
        Check(MobileNavigation.Back(true,false,false,"战斗回放",3)==MobileBackAction.CancelConfirmation,"replay confirmation closes first");
        Check(MobileNavigation.Back(false,false,false,"战斗等待",3)==MobileBackAction.BattleMap,"arrival waiting returns to map without recalling army");
        Check(MobileNavigation.Back(true,false,false,"战斗等待",3)==MobileBackAction.CancelConfirmation,"waiting recall confirmation closes before leaving battle");
        Check(MobileNavigation.Back(false,true,false,"战斗等待",3)==MobileBackAction.CloseCard,"waiting card overlay has return priority");
        Check(MobileNavigation.Back(false,false,false,"练兵",3)==MobileBackAction.TrainingOrigin,"training back restores its city or land origin");
        Check(MobileNavigation.Back(false,false,false,"征兵",3)==MobileBackAction.ReinforcementOrigin,"reinforcement back restores its task or city origin");
        Check(MobileNavigation.Back(true,false,false,"征兵",3)==MobileBackAction.CancelConfirmation,"reinforcement approval is cancelled before leaving page");
        Check(MobileNavigation.Back(false,true,false,"征兵",3)==MobileBackAction.CloseCard,"reinforcement card closes before leaving plan");
        Check(MobileNavigation.Back(false,false,false,"武将",3,false,"quests",false,false,true)==MobileBackAction.ReinforcementPlan,"formation returns to the original reinforcement plan");
        Check(MobileNavigation.Back(false,true,false,"武将",3,false,"quests",false,false,true)==MobileBackAction.CloseCard,"formation card retains priority over reinforcement return");
        Check(MobileNavigation.Back(true,false,false,"练兵",3)==MobileBackAction.CancelConfirmation,"training confirmation closes before leaving page");
        Check(MobileNavigation.Back(false,true,false,"练兵",3)==MobileBackAction.CloseCard,"training card inspection closes before leaving page");
        Check(MobileNavigation.Back(false,false,false,"武将",3,true)==MobileBackAction.TrainingPlan,"formation back restores the pending training plan");
        Check(MobileNavigation.Back(true,false,false,"武将",3,true)==MobileBackAction.CancelConfirmation,"formation confirmation closes before restoring training");
        Check(MobileNavigation.Back(false,true,false,"武将",3,true)==MobileBackAction.CloseCard,"formation card closes before restoring training");
        Check(MobileNavigation.Back(false,false,false,"主城",3,false,"facilities")==MobileBackAction.CityOverview,"facilities back returns to city overview");
        Check(MobileNavigation.Back(false,false,false,"主城",3,false,"quests")==MobileBackAction.CityOverview,"quests back returns to city overview");
        Check(MobileNavigation.Back(false,false,false,"主城",3,false,"quest-detail")==MobileBackAction.CityOverview,"quest objective delegates to city layered return");
        Check(MobileNavigation.Back(false,false,false,"主城",3,false,"quest-detail",false,true)==MobileBackAction.CloseQuestReward,"objective receipt closes before returning to list");
        Check(MobileNavigation.Back(false,false,false,"武将",3,false,"quest-detail",true)==MobileBackAction.CityFormationOrigin,"formation preserves the objective detail origin");
        Check(MobileNavigation.Back(false,false,false,"领地",3,questLandOrigin:true)==MobileBackAction.CityQuestObjective,"task land returns to its objective before clearing selection");
        Check(MobileNavigation.Back(true,false,false,"领地",3,questLandOrigin:true)==MobileBackAction.CancelConfirmation,"land attack approval closes before returning to task");
        Check(MobileNavigation.Back(false,true,false,"领地",3,questLandOrigin:true)==MobileBackAction.CloseCard,"land card closes before returning to task");
        Check(MobileNavigation.Back(false,false,false,"领地",3,questReward:true,questLandOrigin:true)==MobileBackAction.CloseQuestReward,"land reward receipt retains priority over task return");
        Check(MobileNavigation.Back(false,false,false,"招募",3,questLandOrigin:true)==MobileBackAction.Map,"stale land task source does not override other pages");
        Check(MobileNavigation.Back(false,false,false,"主城",3,false,"catalog")==MobileBackAction.CityOverview,"facility catalog back uses city navigation hierarchy");
        Check(MobileNavigation.Back(false,false,false,"主城",3,false,"construction")==MobileBackAction.CityOverview,"construction progress uses city parent navigation");
        Check(MobileNavigation.Back(true,false,false,"主城",3,false,"construction")==MobileBackAction.CancelConfirmation,"construction cancellation dialog closes before progress page");
        Check(MobileNavigation.Back(false,false,false,"主城",3,cityPlanning:true)==MobileBackAction.CityOverview,"city site selection closes before leaving the panorama");
        Check(MobileNavigation.Back(false,true,false,"主城",3,cityPlanning:true)==MobileBackAction.CloseCard,"city planning card remains the top back layer");
        Check(MobileNavigation.Back(true,false,false,"主城",3,cityPlanning:true)==MobileBackAction.CancelConfirmation,"city planning confirmation retains return priority");
        Check(MobileNavigation.Back(false,false,false,"领地",3,cityPlanning:true)==MobileBackAction.ClearLand,"stale city planning state cannot intercept map back");
        Check(MobileNavigation.Back(false,false,false,"主城",3,cityExpanded:true)==MobileBackAction.CityOverview,"expanded city closes before leaving city");
        Check(MobileNavigation.Back(false,false,false,"主城",3,cityPlanning:true,cityExpanded:true)==MobileBackAction.CityOverview,"planning in expanded city keeps layered return");
        Check(MobileNavigation.Back(false,true,false,"主城",3,cityExpanded:true)==MobileBackAction.CloseCard,"card closes before expanded city");
        Check(MobileNavigation.Back(false,false,false,"领地",3,cityExpanded:true)==MobileBackAction.ClearLand,"stale expanded city cannot intercept map back");
        Check(MobileNavigation.Back(false,false,false,"主城",3,false,"quests",false,true)==MobileBackAction.CloseQuestReward,"quest receipt closes before leaving its page");
        Check(MobileNavigation.Back(false,false,false,"领地",3,false,"overview",false,true)==MobileBackAction.CloseQuestReward,"tracker receipt closes before clearing selected land");
        Check(MobileNavigation.Back(true,false,false,"主城",3,false,"quests",false,true)==MobileBackAction.CancelConfirmation,"confirmation above receipt closes first");
        Check(MobileNavigation.Back(false,true,false,"主城",3,false,"quests",false,true)==MobileBackAction.CloseCard,"card above receipt closes first");
        Check(MobileNavigation.Back(false,false,false,"武将",3,false,"quests",true)==MobileBackAction.CityFormationOrigin,"city formation returns to source city page");
        Check(MobileNavigation.Back(false,true,false,"武将",3,false,"quests",true)==MobileBackAction.CloseCard,"city formation card closes first");
        Check(MobileNavigation.Back(true,false,false,"主城",3,false,"facilities")==MobileBackAction.CancelConfirmation,"city construction confirmation closes first");
        Check(MobileNavigation.Back(false,false,false,"练兵",3,false,"quests")==MobileBackAction.TrainingOrigin,"training preserves city quest origin before city back");
        foreach(string page in new[]{"主城","武将","招募","战报","规则"})Check(MobileNavigation.Back(false,false,false,page,3)==MobileBackAction.Map,"back returns map from "+page);
        Check(MobileNavigation.Back(false,false,false,"领地",3)==MobileBackAction.ClearLand,"back clears selected tile before quit");
        Check(MobileNavigation.Back(false,false,false,"领地",-1)==MobileBackAction.ConfirmExit,"root back requires explicit exit confirmation");

        var clock=new MobileSuspendClock();var start=new DateTime(2026,10,25,0,0,0,DateTimeKind.Utc);
        Near(clock.Resume(start,604800),0,"initial resume has no elapsed time");
        Check(clock.Suspend(start),"first pause records suspension");Check(!clock.Suspend(start.AddSeconds(10)),"duplicate pause does not move start");
        Near(clock.Resume(start.AddSeconds(60),604800),60,"resume uses real elapsed seconds not accelerated speed");
        Check(!clock.IsPaused,"resume clears paused state");Near(clock.Resume(start.AddSeconds(90),604800),0,"duplicate resume cannot settle twice");
        clock.Suspend(start);Near(clock.Resume(start.AddSeconds(-2),604800),0,"clock rollback cannot reverse simulation");
        clock.Suspend(start);Near(clock.Resume(start.AddDays(10),604800),604800,"resume uses same seven-day offline cap");
        Console.WriteLine("MOBILE_INPUT_CHECKS_OK: "+checks+" touch, safe-area, back-navigation and pause/resume checks.");
    }
}
