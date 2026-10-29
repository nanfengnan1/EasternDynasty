using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EasternDynasty;

public static class TrainingExecutionChecks
{
    private static int checks;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static void Near(double a, double b, string message) { Check(Math.Abs(a - b) < .000001, message + ": " + a + " / " + b); }
    private static string Json(World w)
    { using (var s = new MemoryStream()) { new DataContractJsonSerializer(typeof(World)).WriteObject(s, w); return Encoding.UTF8.GetString(s.ToArray()); } }
    private static World Copy(World w)
    { using (var s = new MemoryStream(Encoding.UTF8.GetBytes(Json(w)))) return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(s); }
    private static World Start()
    {
        var w = World.CreateYouzhou();
        int target = w.lands.Where(t => w.AttackProblem(t.id) == "").OrderBy(t => t.level).First().id;
        w.March(target); w.Tick(1000);
        Check(w.lands[target].owned && w.order == null, "fixture did not capture land through real battle");
        w.StartTraining(target);
        Check(w.order != null && w.order.action == (int)ArmyAction.Training, "training rejected");
        return w;
    }
    private static TrainingExecutionStatus Observe(World w)
    {
        string before = Json(w); var s = TrainingExecutionStatus.Create(w);
        Check(s != null && Json(w) == before, "status changed World");
        Check(s.Participants.SequenceEqual(w.order.trainingParticipants), "participants not frozen order IDs");
        Check(s.Target == w.order.target && s.ConfiguredExperience == w.order.trainingExperience, "status used preparation target or recalculated reward");
        Check(s.PhaseProgress >= 0 && s.PhaseProgress <= 1, "progress outside bar");
        var copy = Copy(w); var read = TrainingExecutionStatus.Create(copy);
        Near(read.UntilHome, s.UntilHome, "JSON resume changed ETA");
        Near(read.UntilAward, s.UntilAward, "JSON resume changed credit ETA");
        Near(read.PhaseProgress, s.PhaseProgress, "JSON resume changed progress");
        Check(read.Stage == s.Stage && read.AwardedExperience == s.AwardedExperience, "JSON resume changed phase or reward");
        return s;
    }
    private static void FinishAtPredictedTime(World w, TrainingExecutionStatus s)
    {
        var actual = Copy(w); int count = actual.trainingHistory.Count;
        if (!s.Settled)
        {
            actual.Tick(s.UntilAward - .001);
            Check(actual.trainingHistory.Count == count, "credit predicted after real settlement");
            actual.Tick(.001);
            Check(actual.trainingHistory.Count == count + 1 && actual.order.returning, "credit ETA does not hit settlement");
            actual.Tick(s.UntilHome - s.UntilAward);
        }
        else actual.Tick(s.UntilHome);
        Check(actual.order == null, "home ETA does not hit actual return");
        int settledCount = actual.trainingHistory.Count; actual.Tick(100);
        Check(actual.trainingHistory.Count == settledCount, "return or status viewing duplicates settlement");
    }
    public static int Main()
    {
        try
        {
            Check(TrainingExecutionStatus.Create(null) == null, "null generated execution");
            Check(TrainingExecutionStatus.Create(World.CreateYouzhou()) == null, "idle generated execution");
            var w = Start(); var s = Observe(w);
            Check(s.Stage == 0 && !s.Settled && !s.ReceiptAvailable && s.AwardedExperience == 0, "outbound falsely awarded");
            Near(s.PhaseProgress, 0, "start progress"); FinishAtPredictedTime(w, s);
            w.Tick(w.order.remaining / 2); s = Observe(w); Near(s.PhaseProgress, .5, "outbound half progress"); FinishAtPredictedTime(w, s);
            var snapshot = s; w.order.trainingExperience += 1; w.order.trainingParticipants.Reverse();
            Check(snapshot.ConfiguredExperience != w.order.trainingExperience && !snapshot.Participants.SequenceEqual(w.order.trainingParticipants), "status retained mutable order data");
            w = Start(); w.Tick(w.order.remaining); s = Observe(w);
            Check(s.Stage == 1 && !s.Settled, "arrival not working"); Near(s.UntilAward, World.TrainingSeconds, "arrival countdown"); FinishAtPredictedTime(w, s);
            w.Tick(30); s = Observe(w); Near(s.PhaseProgress, .5, "work half progress"); FinishAtPredictedTime(w, s);
            w.Tick(w.order.remaining); s = Observe(w);
            Check(s.Stage == 2 && s.Settled && !s.Cancelled && s.ReceiptAvailable && s.AwardedExperience == w.LastTrainingReceipt.awardedExperience.Sum(), "return not displaying actual receipt");
            Near(s.UntilAward, 0, "already credited timer"); FinishAtPredictedTime(w, s);
            w.Tick(w.order.remaining / 2); s = Observe(w); Near(s.PhaseProgress, .5, "return half progress"); FinishAtPredictedTime(w, s);
            foreach (bool working in new[] { false, true })
            {
                w = Start(); if (working) { w.Tick(w.order.remaining); w.Tick(20); } else w.Tick(w.order.remaining / 3);
                int commands = w.commands; w.RecallArmy(); s = Observe(w);
                Check(s.Stage == 2 && s.Settled && s.Cancelled && s.ReceiptAvailable && s.AwardedExperience == 0, "cancel created reward");
                Check(w.commands == commands && w.cityTrainingCompletions == 0, "observation refunded or completed cancellation");
                FinishAtPredictedTime(w, s);
            }
            w = Start(); var land = w.lands[w.order.target]; land.owned = false; w.Tick(w.order.remaining); s = Observe(w);
            Check(s.Cancelled && s.AwardedExperience == 0, "lost target not cancelled"); FinishAtPredictedTime(w, s);
            w = Start(); w.Tick(w.order.remaining); w.Tick(w.order.remaining); w.LastTrainingReceipt.target = w.homeId;
            Check(!TrainingExecutionStatus.Create(w).ReceiptAvailable, "mismatched receipt masqueraded as current reward");
            Console.WriteLine("PASS: " + checks + " training execution checks; real capture, exact phase/credit/return timers, cancellation, JSON resume and immutable World."); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine("FAIL: " + e); return 1; }
    }
}
