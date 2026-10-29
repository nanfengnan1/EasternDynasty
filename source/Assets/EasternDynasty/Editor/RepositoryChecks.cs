using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EasternDynasty.Editor
{
    // Integration checks run only in fresh project-local directories, never in the player's save folder.
    public static class RepositoryChecks
    {
        private static int checks;
        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception("Repository check: " + message); checks++; }
        private static void Throws<T>(Action action, string message) where T : Exception
        { try { action(); } catch (T) { checks++; return; } throw new Exception("Repository check: " + message); }
        private static World Fresh(bool youzhou)
        { return youzhou ? World.CreateYouzhou(0, "Repository checks") : World.Create(Homeland.Han, 4, "Repository checks"); }
        private static void RoundTrip(JsonCampaignRepository repository, World original, string name)
        {
            repository.Save(original); bool backup;
            var loaded = repository.Load(original.IsYouzhou, out backup);
            Check(!backup && !ReferenceEquals(original, loaded), name + " instance/slot");
            string difference=Difference(original,loaded,typeof(World),"World");
            Check(difference==null, name + " serialized data changed: " + difference);
        }

        private static string Difference(object expected, object actual, Type type, string path)
        {
            // Unity normalizes null strings and JSON floating-point text can round at the last binary digit.
            if(type==typeof(string))return (expected as string ?? "")== (actual as string ?? "")?null:path;
            if(expected==null||actual==null)return expected==actual?null:path;
            if(type==typeof(double)||type==typeof(float))
            {double a=Convert.ToDouble(expected),b=Convert.ToDouble(actual);return Math.Abs(a-b)<=Math.Max(1,Math.Abs(a))*1e-12?null:path+" "+a+" != "+b;}
            if(type.IsPrimitive||type.IsEnum)return expected.Equals(actual)?null:path;
            var left=expected as IList;var right=actual as IList;
            if(left!=null)
            {
                if(right==null||left.Count!=right.Count)return path+" count";
                Type element=type.IsArray?type.GetElementType():type.GetGenericArguments()[0];
                for(int i=0;i<left.Count;i++){string error=Difference(left[i],right[i],element,path+"["+i+"]");if(error!=null)return error;}
                return null;
            }
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public))
            {if(field.IsNotSerialized)continue;string error=Difference(field.GetValue(expected),field.GetValue(actual),field.FieldType,path+"."+field.Name);if(error!=null)return error;}
            return null;
        }

        private static int LifetimeExperience(World world)
        { return world.cards.Sum(c=>c.level*(c.level-1)*40+c.experience); }

        private static void ConstructionRefundJsonChecks(string root)
        {
            bool backup;string problem;
            var repository=new JsonCampaignRepository(Path.Combine(root,"city-refund"));
            var legacy=Fresh(false);repository.Save(legacy);
            string legacyPath=Path.Combine(root,"city-refund",JsonCampaignRepository.LegacyFileName);
            string legacyBefore=File.ReadAllText(legacyPath);
            var city=Fresh(true);
            int plot=city.cityPlots.First(p=>p.building==CityBuilding.None&&city.IsCityDistrict(p.landId)).index;
            city.StartCityConstruction(plot,CityBuilding.Mill);
            Check(city.CityJob!=null,"refund JSON fixture did not start");
            // This is the pre-refund-policy construction shape: no migration field is necessary.
            var oldPending=JsonUtility.FromJson<CityConstruction>("{\"plotIndex\":"+plot+
                ",\"targetLevel\":1,\"building\":2,\"remaining\":12,\"duration\":20,"+
                "\"paid\":{\"grain\":100,\"grass\":90,\"stone\":150,\"iron\":35,\"gold\":40,\"jade\":0}}");
            city.cityConstruction[0]=oldPending;
            city.resources=new Supplies{grain=1000.125,grass=2000.25,stone=3000.375,iron=4000.5,gold=5000.625,jade=37};
            Check(city.IsValid(out problem),"old pending paid receipt rejected: "+problem);
            RoundTrip(repository,city,"pre-policy pending construction receipt and fractional balance");
            var loaded=repository.Load(true,out backup);
            Check(loaded.cityVersion==1&&loaded.CityJob.remaining==12&&loaded.CityJob.targetLevel==1,
                "refund JSON load reset construction progress or version");
            string paidBefore=JsonUtility.ToJson(loaded.CityJob.paid);
            var preview=loaded.CityConstructionRefund();
            Check(JsonUtility.ToJson(preview)==paidBefore&&!ReferenceEquals(preview,loaded.CityJob.paid),
                "refund JSON preview differs from original receipt or exposes it");
            preview.gold=0;
            Check(JsonUtility.ToJson(loaded.CityJob.paid)==paidBefore,"refund preview changed persisted receipt");
            var invalid=JsonUtility.FromJson<World>(JsonUtility.ToJson(loaded));invalid.CityJob.paid.stone+=.125;
            Check(!invalid.IsValid(out problem),"non-price fractional paid receipt accepted");
            Throws<InvalidDataException>(()=>repository.Save(invalid),"fractional paid receipt saved");
            invalid=JsonUtility.FromJson<World>(JsonUtility.ToJson(loaded));invalid.CityJob.paid.jade=1;
            Check(!invalid.IsValid(out problem),"paid jade inflation accepted");
            Throws<InvalidDataException>(()=>repository.Save(invalid),"inflated paid jade saved");
            var expected=new Supplies{grain=1100.125,grass=2090.25,stone=3150.375,iron=4035.5,gold=5040.625,jade=37};
            loaded.CancelCityConstruction();
            Check(Difference(expected,loaded.resources,typeof(Supplies),"refund")==null,
                "old pending JSON refund lost a resource fraction, refunded below full paid, or created jade");
            Check(loaded.CityJob==null&&loaded.CityLevel(CityBuilding.Mill)==0&&!loaded.CityQuestReady(2),
                "JSON cancellation completed the facility or retained construction");
            RoundTrip(repository,loaded,"cancelled construction without duplicate refund");
            loaded=repository.Load(true,out backup);loaded.CancelCityConstruction();
            Check(Difference(expected,loaded.resources,typeof(Supplies),"refund")==null,
                "reloaded cancellation paid twice");
            loaded.Tick(30);
            Check(loaded.CityJob==null&&loaded.CityLevel(CityBuilding.Mill)==0,"cancelled saved job completed later");
            Check(File.ReadAllText(legacyPath)==legacyBefore,"S1 refund changed legacy campaign slot");
        }

        private static void BattleReplayJsonChecks(JsonCampaignRepository repository, string root)
        {
            bool backup; string problem;
            var battle = Fresh(true); battle.troops = 2000;
            var target = battle.lands.First(t => battle.AttackProblem(t.id) == ""); target.level = 1;
            battle.ActiveGenerals[0].learnedSkill = "抚军";
            battle.March(target.id, true); string issuedId = battle.order.battleId;
            battle.Tick(battle.order.remaining / 2);
            Check(battle.reports.Count == 0, "battle JSON fixture settled before arrival");
            RoundTrip(repository, battle, "battle outbound without early replay");
            battle = repository.Load(true, out backup);
            Check(battle.order.battleId == issuedId && battle.order.watchOnArrival, "outbound JSON lost battle identity or watch intent");
            battle.Tick(battle.order.remaining);
            var report = battle.reports[0];
            Check(report.battleId == issuedId && battle.order.battleId == issuedId && battle.order.returning,
                "JSON battle settlement was not associated with its departure");
            Check(report.HasReplay && report.won && report.events.Any(e => e.kind == BattleReplayEventKind.Recovery), "battle trace did not record settlement and healing");
            Check(report.attacker.generals[0].level == 1 && report.attacker.generals[0].awardedExperience == 100, "battle JSON snapshot is not pre-reward formation");
            RoundTrip(repository, battle, "structured battle frozen formation events rewards");
            var loaded = repository.Load(true, out backup);
            Check(loaded.reports[0].battleId == report.battleId && BattleReplay.IsValid(loaded.reports[0]), "battle identity or trace changed after JSON reload");
            string before = JsonUtility.ToJson(loaded);
            string main = Path.Combine(root, JsonCampaignRepository.YouzhouFileName);
            string diskBefore = File.ReadAllText(main);
            var player = new BattleReplayPlayer(loaded.reports[0]);
            var outcome = BattleOutcomeSummary.Create(loaded.reports[0]);
            Check(outcome != null && outcome.battleId == report.battleId && outcome.goldReward == report.goldReward
                && outcome.attackerInitial - outcome.lossTotal + outcome.recoveryTotal == outcome.attackerFinal,
                "JSON battle outcome lost its identity, rewards or troop ledger");
            Check(outcome.participants.Count == report.attacker.generals.Count && outcome.participants[0].level == 1
                && outcome.participants[0].awardedExperience == 100
                && outcome.experienceReward == report.attacker.generals.Sum(g => g.awardedExperience),
                "JSON outcome used current levels or nominal XP instead of actual recorded credits");
            var formation = BattleFormationSummary.Create(loaded.reports[0]);
            Check(formation != null && formation.battleId == report.battleId && formation.targetName == report.targetName
                && formation.targetX == report.targetX && formation.targetZ == report.targetZ
                && formation.attackerName == report.attacker.name && formation.defenderName == report.defender.name
                && formation.attackerInitial == report.attacker.troops && formation.defenderInitial == report.defender.troops
                && formation.participants.Count == report.attacker.generals.Count,
                "JSON formation lost frozen battle identity, armies or member count");
            for (int i = 0; i < formation.participants.Count; i++)
            {
                var member = formation.participants[i]; var frozen = report.attacker.generals[i];
                Check(member.uid == frozen.uid && member.definitionId == frozen.definitionId && member.name == frozen.name
                    && member.level == frozen.level && member.rank == frozen.rank && member.attack == frozen.attack
                    && member.skill == frozen.skill && member.learnedSkill == frozen.learnedSkill,
                    "JSON formation changed recorded order, pre-reward attributes or carried skills");
            }
            var optionalSkills = JsonUtility.FromJson<BattleRecord>(JsonUtility.ToJson(report));
            optionalSkills.attacker.generals[0].skill = null; optionalSkills.attacker.generals[0].learnedSkill = "";
            var optionalFormation = BattleFormationSummary.Create(optionalSkills);
            Check(optionalFormation != null && optionalFormation.participants[0].skill == null
                && optionalFormation.participants[0].learnedSkill == "", "formation collapsed missing and empty skill fields");
            optionalSkills = JsonUtility.FromJson<BattleRecord>(JsonUtility.ToJson(optionalSkills));
            optionalFormation = BattleFormationSummary.Create(optionalSkills);
            Check(optionalFormation != null && optionalFormation.participants[0].skill == optionalSkills.attacker.generals[0].skill
                && optionalFormation.participants[0].learnedSkill == optionalSkills.attacker.generals[0].learnedSkill,
                "JSON optional skill fields were replaced with current catalog or invented skills");
            for (int repeat = 0; repeat < 10; repeat++)
            {
                player.Seek(0);
                for (int i = 1; i < player.Count; i++) player.Step(1);
                Check(player.Current.kind == BattleReplayEventKind.Result && player.Current.attackerTroops == loaded.troops, "JSON replay did not finish at recorded army state");
                player.Step(-1); player.Seek(int.MaxValue); player.Seek(-1);
            }
            Check(JsonUtility.ToJson(loaded) == before && File.ReadAllText(main) == diskBefore, "JSON replay mutated campaign or wrote a save");
            loaded.Tick(500);
            Check(loaded.reports.Count == 1 && loaded.troops == report.attackerFinal, "returning loaded battle settled twice");
            RoundTrip(repository, loaded, "structured battle after return");

            // Actual pre-replay field shape: optional fields absent, not merely set to their defaults.
            var historical = JsonUtility.FromJson<BattleRecord>("{\"title\":\"旧战报\",\"text\":\"只读历史文本\",\"won\":true,\"time\":0}");
            Check(historical.replayVersion == 0 && !historical.HasReplay && BattleReplay.IsValid(historical), "old battle JSON created an invented replay");
            loaded.reports.Add(historical); repository.Save(loaded);
            loaded = repository.Load(true, out backup);
            Check(loaded.IsValid(out problem) && loaded.reports.Last().title == "旧战报" && loaded.reports.Last().text == "只读历史文本"
                && !loaded.reports.Last().HasReplay, "text-only report did not survive repository migration: " + problem);
            var historicalPlayer = new BattleReplayPlayer(loaded.reports.Last()); historicalPlayer.Seek(99);
            Check(historicalPlayer.Current == null && historicalPlayer.Count == 0, "text-only report exposed replay events");
            Check(BattleOutcomeSummary.Create(loaded.reports.Last()) == null, "old text-only JSON invented a structured outcome");
            Check(BattleFormationSummary.Create(loaded.reports.Last()) == null, "old text-only JSON invented a structured formation");
            Debug.Log("BATTLE_OUTCOME_JSON_REPOSITORY_OK: restored frozen identity, troop ledger and actual participant awards; legacy text remains text-only.");
            Debug.Log("BATTLE_FORMATION_JSON_REPOSITORY_OK: restored frozen order, initial attributes and carried skills; optional skills and legacy text remain compatible.");
        }

        private static void BattleArrivalJsonChecks(string root)
        {
            bool backup; string problem;
            var old = Fresh(true); int target = old.lands.First(t => old.AttackProblem(t.id) == "").id;
            old.March(target);
            string oldJson = System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(old),
                "\"(?:battleId|watchOnArrival)\":(?:\"[^\"]*\"|true|false|null),?", "").Replace(",}", "}");
            Check(!oldJson.Contains("battleId") && !oldJson.Contains("watchOnArrival"), "pre-watch Unity fixture still includes new fields");
            old = JsonUtility.FromJson<World>(oldJson);
            Check(old.IsValid(out problem) && string.IsNullOrEmpty(old.order.battleId) && !old.order.watchOnArrival,
                "pre-watch Unity JSON defaults rejected: " + problem);
            old.EnsureSeasonState(); string migratedId = old.order.battleId;
            var migratedRepository = new JsonCampaignRepository(Path.Combine(root, "battle-watch-migration"));
            RoundTrip(migratedRepository, old, "pre-watch attack normalized identity");
            old = migratedRepository.Load(true, out backup); old.EnsureSeasonState();
            Check(!string.IsNullOrEmpty(migratedId) && old.order.battleId == migratedId && !old.order.watchOnArrival,
                "Unity old-order normalization was unstable or opted into playback");
            old.Tick(old.order.remaining);
            Check(old.reports.Count == 1 && old.reports[0].battleId == migratedId, "Unity migrated attack lost identity on arrival");

            DateTime now = new DateTime(2026, 10, 25, 14, 0, 0, DateTimeKind.Utc);
            var repository = new JsonCampaignRepository(Path.Combine(root, "battle-watch-offline"));
            var session = new CampaignSession(repository, () => now);
            var world = session.StartYouzhou(0, "Offline battle watch"); world.troops = 2000;
            var land = world.lands.First(t => world.AttackProblem(t.id) == ""); land.level = 1;
            world.March(land.id, true); string issuedId = world.order.battleId;
            double duration = world.order.duration;
            session.Save(); now = now.AddSeconds(duration / 2); session.Load(true, out backup);
            Check(session.World.reports.Count == 0 && session.World.order.battleId == issuedId && session.World.order.watchOnArrival,
                "offline partial march showed an early result or lost watch intent");
            session.Save(); now = now.AddMinutes(15); session.Load(true, out backup);
            world = session.World;
            Check(world.order == null && world.reports.Count == 1 && world.reports[0].battleId == issuedId
                && world.lands[land.id].owned, "offline arrival did not settle exactly one associated battle");
            string report = JsonUtility.ToJson(world.reports[0]); int troops = world.troops;
            int experience = LifetimeExperience(world); double gold = world.resources.gold;
            session.Save(); session.Load(true, out backup);
            Check(session.World.order == null && session.World.reports.Count == 1 && JsonUtility.ToJson(session.World.reports[0]) == report
                && session.World.troops == troops && LifetimeExperience(session.World) == experience && session.World.resources.gold == gold,
                "saved offline completion repeated losses/rewards or retained a pending watch order");
        }

        private static void TrainingJsonChecks(JsonCampaignRepository repository,string root)
        {
            bool backup;string problem;
            var training=Fresh(true);
            int target=training.lands.First(t=>training.AttackProblem(t.id)=="").id;
            training.lands[target].owned=true;
            int before=LifetimeExperience(training),commands=training.AvailableCommands;
            training.StartTraining(target);
            Check(training.order!=null&&training.order.action==(int)ArmyAction.Training,"training order absent");
            int[] participants=training.order.trainingParticipants.ToArray();
            int reward=training.order.trainingExperience;
            training.Tick(training.order.remaining*.4);
            RoundTrip(repository,training,"training partial outbound frozen participants");
            training=repository.Load(true,out backup);
            Check(training.order.trainingParticipants.SequenceEqual(participants)&&training.order.trainingExperience==reward,"training JSON lost frozen participants/reward");
            Check(training.AvailableCommands==commands-World.TrainingCommandCost&&LifetimeExperience(training)==before,"outbound reload charged twice or awarded early");
            training.Tick(training.order.remaining);
            Check(training.order.phase==(int)ArmyPhase.Working,"loaded training did not enter working phase");
            RoundTrip(repository,training,"training working");
            training=repository.Load(true,out backup);
            training.Tick(training.order.remaining+.001);
            Check(training.LastTrainingReceipt!=null&&!training.LastTrainingReceipt.cancelled&&training.cityTrainingCompletions==1,"training completion receipt absent");
            Check(training.LastTrainingReceipt.participantIds.SequenceEqual(participants)&&training.LastTrainingReceipt.experiencePerGeneral==reward,"receipt changed frozen participants/reward");
            Check(LifetimeExperience(training)-before==training.LastTrainingReceipt.awardedExperience.Sum(),"receipt award does not equal real card progression");
            RoundTrip(repository,training,"training completed returning receipt");
            training=repository.Load(true,out backup);int earned=LifetimeExperience(training);
            training.Tick(200);training.Tick(200);
            Check(training.order==null&&training.cityTrainingCompletions==1&&training.trainingHistory.Count==1&&LifetimeExperience(training)==earned,"loaded completion settled twice");
            RoundTrip(repository,training,"training returned history");

            training.StartTraining(target);training.Tick(training.order.remaining);
            training.RecallArmy();
            Check(training.LastTrainingReceipt.cancelled&&training.LastTrainingReceipt.awardedExperience.Sum()==0,"cancelled training gave experience");
            RoundTrip(repository,training,"training cancelled returning");
            training=repository.Load(true,out backup);training.Tick(500);
            Check(training.order==null&&training.cityTrainingCompletions==1&&training.trainingHistory.Count==2&&LifetimeExperience(training)==earned,"cancelled training reload settled reward");

            var preTraining=Fresh(true);int oldTarget=preTraining.lands.First(t=>preTraining.AttackProblem(t.id)=="").id;
            preTraining.March(oldTarget);
            string oldJson=JsonUtility.ToJson(preTraining);
            oldJson=System.Text.RegularExpressions.Regex.Replace(oldJson,"\"(?:trainingVersion|trainingExperience|cityTrainingCompletions)\":-?[0-9.Ee+]+,?","");
            oldJson=System.Text.RegularExpressions.Regex.Replace(oldJson,"\"(?:trainingParticipants|trainingHistory)\":(?:null|\\[\\]),?","");
            oldJson=System.Text.RegularExpressions.Regex.Replace(oldJson,"\"(?:trainingSettled|trainingCancelled)\":false,?","").Replace(",}","}");
            Check(!oldJson.Contains("trainingVersion")&&!oldJson.Contains("trainingHistory")&&!oldJson.Contains("cityTrainingCompletions"),"pre-training fixture still contains new fields");
            var migrated=JsonUtility.FromJson<World>(oldJson);
            Check(migrated.IsValid(out problem),"pre-training JSON invalid: "+problem);
            Check(migrated.LastTrainingReceipt==null&&migrated.cityTrainingCompletions==0,"old JSON manufactured a training receipt");
            migrated.EnsureSeasonState();
            Check(migrated.trainingHistory!=null&&migrated.order.trainingParticipants!=null&&migrated.order.trainingParticipants.Count==0,"pre-training JSON did not normalize empty optional collections");
            migrated.Tick(500);
            Check(migrated.order==null&&migrated.reports.Count==1&&migrated.cityTrainingCompletions==0,"old attack JSON changed semantics");
            RoundTrip(repository,migrated,"migrated pre-training attack");

            DateTime now=new DateTime(2026,10,25,12,0,0,DateTimeKind.Utc);
            var offlineRepository=new JsonCampaignRepository(Path.Combine(root,"training-session"));
            var session=new CampaignSession(offlineRepository,()=>now);
            session.StartYouzhou(0,"Training offline");
            int offlineTarget=session.World.lands.First(t=>session.World.AttackProblem(t.id)=="").id;
            session.World.lands[offlineTarget].owned=true;session.World.StartTraining(offlineTarget);session.Save();
            now=now.AddMinutes(15);session.Load(true,out backup);
            Check(session.World.order==null&&session.World.cityTrainingCompletions==1&&session.World.trainingHistory.Count==1,"offline training did not finish once");
            int offlineEarned=LifetimeExperience(session.World);session.Save();session.Load(true,out backup);
            Check(session.World.cityTrainingCompletions==1&&session.World.trainingHistory.Count==1&&LifetimeExperience(session.World)==offlineEarned,"offline training completed again on reload");
            string offlineMain=Path.Combine(root,"training-session",JsonCampaignRepository.YouzhouFileName);
            string expectedBackup=File.ReadAllText(offlineMain+".bak");File.WriteAllText(offlineMain,"{bad training primary");
            var recovered=offlineRepository.Load(true,out backup);
            Check(backup&&recovered.order!=null&&recovered.order.action==(int)ArmyAction.Training,"active training order was lost during backup fallback");
            recovered.Tick(1000);
            Check(recovered.cityTrainingCompletions==1&&recovered.trainingHistory.Count==1&&LifetimeExperience(recovered)==offlineEarned,"backup training did not settle exactly once");
            Check(File.ReadAllText(offlineMain+".bak")==expectedBackup,"training backup changed during fallback");
        }

        private static void ReinforcementJsonChecks(string root)
        {
            // Reproduce the failed smoke setup: construction income creates repeating
            // fractional balances before a non-round recruitment quantity is paid.
            string directory = Path.Combine(root, "reinforcement");
            var repository = new JsonCampaignRepository(directory);
            var legacy = Fresh(false); repository.Save(legacy);
            string legacyPath = Path.Combine(directory, JsonCampaignRepository.LegacyFileName);
            string legacyJson = File.ReadAllText(legacyPath);
            var world = Fresh(true);
            world.resources = new Supplies { grain = 50000, grass = 50000, stone = 50000, iron = 50000, gold = 50000 };
            world.RecordCityAction("visit"); world.ClaimCityQuest(1);
            world.StartCityConstruction(0, CityBuilding.Mill);
            Check(world.CityJob != null, "recruitment fixture starts mill");
            world.Tick(world.CityJob.remaining + .01); world.ClaimCityQuest(2);
            world.StartCityConstruction(4, CityBuilding.TownHall);
            Check(world.CityJob != null, "recruitment fixture upgrades town hall");
            world.Tick(world.CityJob.remaining + .01); world.ClaimCityQuest(3); world.ClaimCityQuest(4);
            Check(world.CurrentCityQuest.id == 5 && !world.CityQuestReady(5), "recruitment JSON fixture reaches unfinished real task 5");
            var plan = ReinforcementPlan.Create(world, 137);
            ReinforcementReceipt receipt; string message;
            Check(plan.TryExecute(world, out receipt, out message) && receipt.amount == 137, "137-person confirmation executes after fractional income");
            Check(world.cityRecruitActions == 1 && world.CityQuestReady(5), "recruitment records one completed objective");
            RoundTrip(repository, world, "137-person recruitment after fractional income");
            bool backup; var loaded = repository.Load(true, out backup);
            Check(!backup && loaded.troops == world.troops && loaded.cityRecruitActions == 1
                && loaded.CurrentCityQuest.id == 5 && loaded.CityQuestReady(5), "loaded recruitment preserves exact troops and unclaimed task");
            string completed = JsonUtility.ToJson(world);
            Check(!plan.TryExecute(world, out receipt, out message) && JsonUtility.ToJson(world) == completed, "replayed approval after save remains inert");
            double iron = loaded.resources.iron; loaded.resources.iron += .01;
            Check(Difference(world, loaded, typeof(World), "World") != null, "JSON comparison rejects a real 0.01 resource discrepancy");
            loaded.resources.iron = iron; loaded.troops++;
            Check(Difference(world, loaded, typeof(World), "World") != null, "JSON comparison rejects one extra soldier");
            loaded.troops--; loaded.cityRecruitActions++;
            Check(Difference(world, loaded, typeof(World), "World") != null, "JSON comparison rejects changed task progress");
            loaded = repository.Load(true, out backup);
            var reward = CityQuestClaimReceipt.Claim(loaded, 5, out message);
            Check(reward != null && reward.questId == 5 && reward.nextQuestId == 6, "loaded recruit task claims through existing reward command");
            RoundTrip(repository, loaded, "recruit task reward claimed after reload");
            loaded = repository.Load(true, out backup); string claimed = JsonUtility.ToJson(loaded);
            Check(CityQuestClaimReceipt.Claim(loaded, 5, out message) == null && JsonUtility.ToJson(loaded) == claimed,
                "reloaded recruit task cannot pay twice");
            Check(File.ReadAllText(legacyPath) == legacyJson, "recruitment and reward leave legacy slot untouched");
            Debug.Log("REINFORCEMENT_JSON_REPOSITORY_OK: fractional construction income, 137 troops, full serialized World comparison, negative controls, single task reward and legacy slot isolation.");
        }

        private static void ConstructionCompletionJsonChecks(string root)
        {
            var repository = new JsonCampaignRepository(Path.Combine(root, "city-completion"));
            repository.Save(Fresh(false));
            string legacyPath = Path.Combine(root, "city-completion", JsonCampaignRepository.LegacyFileName);
            string legacyBefore = File.ReadAllText(legacyPath);
            var now = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc);
            var session = new CampaignSession(repository, () => now);
            var world = session.StartYouzhou(0, "Construction completion JSON");
            int plot = world.cityPlots.First(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId)).index;
            world.StartCityConstruction(plot, CityBuilding.Mill);
            Check(world.CityJob != null, "completion JSON starts real construction");
            session.Tick(world.CityJob.duration * .4); session.Save();
            Check(session.LastCityCompletion == null, "partial construction has no completion receipt");
            now = now.AddSeconds(60); bool backup;
            var loadedSession = new CampaignSession(repository, () => now);
            loadedSession.Load(true, out backup);
            var receipt = loadedSession.LastCityCompletion;
            Check(!backup && receipt != null && receipt.completedWhileAway, "old pending construction JSON yields offline completion");
            Check(receipt.plotIndex == plot && receipt.building == CityBuilding.Mill && receipt.previousLevel == 0 && receipt.targetLevel == 1
                && receipt.previousEffect == 0 && receipt.completedEffect == 160, "completion JSON freezes actual prior and final effects");
            Check(loadedSession.World.CityHourlyIncome().grain == 160 && loadedSession.World.CityJob == null, "completion JSON applies the facility once");
            string before = JsonUtility.ToJson(loadedSession.World);
            loadedSession.ClearCityCompletion(receipt); loadedSession.Tick(0);
            Check(JsonUtility.ToJson(loadedSession.World) == before && loadedSession.LastCityCompletion == null,
                "receipt dismiss and zero tick preserve entire JSON world");
            loadedSession.Save(); loadedSession.Load(true, out backup);
            Check(loadedSession.LastCityCompletion == null && loadedSession.World.CityLevel(CityBuilding.Mill) == 1,
                "completed JSON reload does not reconstruct transient history");
            Check(loadedSession.World.cityVersion == 1 && loadedSession.World.IsYouzhou, "completion receipt changes no save version or slot");
            Check(File.ReadAllText(legacyPath) == legacyBefore, "construction completion preserves independent legacy slot");
            Debug.Log("CITY_CONSTRUCTION_JSON_REPOSITORY_OK: pending construction JSON, offline receipt, dismiss, completed reload and legacy slot isolation.");
        }

        public static void Run()
        {
            checks = 0;
            string root = Path.GetFullPath(Path.Combine("Validation", "RepositoryChecks", Guid.NewGuid().ToString("N")));
            var repository = new JsonCampaignRepository(root);
            Check(!Directory.Exists(root) && !repository.Exists(true) && !repository.Exists(false), "empty repository created files");
            bool ignored;
            Throws<FileNotFoundException>(() => repository.Load(true, out ignored), "missing save not rejected");
            ReinforcementJsonChecks(root);
            foreach (bool youzhou in new[] { false, true })
            {
                var world = Fresh(youzhou);
                RoundTrip(repository, world, "idle");
                world.March(world.lands.First(t => world.AttackProblem(t.id).Length == 0).id);
                world.Tick(world.order.duration * .5);
                RoundTrip(repository, world, "outbound");
                world.Tick(world.order.remaining + .01);
                RoundTrip(repository, world, "returning");
                world.Tick(world.order.remaining + .01);
                RoundTrip(repository, world, "returned");
            }
            Check(repository.Exists(true) && repository.Exists(false), "independent slots missing");
            var season=Fresh(true);
            int seasonTarget=season.lands.First(t=>season.AttackProblem(t.id)=="").id;season.lands[seasonTarget].owned=true;
            season.Garrison(seasonTarget);RoundTrip(repository,season,"S1 garrison outbound");
            season.Tick(season.order.remaining);RoundTrip(repository,season,"S1 stationed");
            season.RecallArmy();season.Tick(season.order.remaining/2);RoundTrip(repository,season,"S1 partial return");
            season.Tick(1000);season.Farm(seasonTarget);RoundTrip(repository,season,"S1 farm outbound");
            season.Tick(season.order.remaining+20);RoundTrip(repository,season,"S1 farm working");
            season.Tick(season.order.remaining+.01);RoundTrip(repository,season,"S1 farm returning");
            season.Tick(1000);RoundTrip(repository,season,"S1 farm completed");
            var city=Fresh(true);city.RecordCityAction("visit");
            string rewardMessage;var rewardReceipt=CityQuestClaimReceipt.Claim(city,1,out rewardMessage);
            Check(rewardReceipt!=null&&rewardReceipt.questId==1&&rewardReceipt.nextQuestId==2,"successful first claim did not create a transient receipt");
            Check(rewardReceipt.credited.grain==300&&rewardReceipt.credited.grass==250&&rewardReceipt.credited.stone==250&&rewardReceipt.credited.iron==100&&rewardReceipt.credited.gold==100,
                "claim receipt does not match five actual resource deltas");
            RoundTrip(repository,city,"claim through receipt command retains old city save format");
            var receiptReload=repository.Load(true,out ignored);string receiptReloadJson=JsonUtility.ToJson(receiptReload);
            Check(CityQuestClaimReceipt.Claim(receiptReload,1,out rewardMessage)==null&&JsonUtility.ToJson(receiptReload)==receiptReloadJson,
                "receipt claim duplicated resources or changed the loaded campaign");
            Check(!receiptReloadJson.Contains("chapterComplete")&&!receiptReloadJson.Contains("credited"),"transient receipt leaked into serialized World");
            int cityPlot=city.cityPlots.First(p=>p.building==CityBuilding.None&&city.IsCityDistrict(p.landId)).index;
            city.StartCityConstruction(cityPlot,CityBuilding.Mill);
            Check(city.CityJob!=null,"city construction did not start");
            city.Tick(city.CityJob.duration*.4);RoundTrip(repository,city,"city partial construction");
            city.Tick(city.CityJob.remaining+.01);city.ClaimCityQuest(2);
            RoundTrip(repository,city,"city completion and claimed reward");
            var cityLoaded=repository.Load(true,out ignored);double cityGold=cityLoaded.resources.gold;
            cityLoaded.ClaimCityQuest(2);
            Check(cityLoaded.cityClaimedQuests.Count==2&&cityLoaded.resources.gold==cityGold,"loaded quest reward was issued twice");
            var oldCity=Fresh(true);oldCity.cityVersion=0;oldCity.cityPlots=null;oldCity.cityConstruction=null;oldCity.cityClaimedQuests=null;
            string preCityJson=JsonUtility.ToJson(oldCity);
            preCityJson=System.Text.RegularExpressions.Regex.Replace(preCityJson,"\"city(?:Version|RecruitActions|FarmCompletions|GarrisonActions|OccupyActions)\":0,?","");
            preCityJson=System.Text.RegularExpressions.Regex.Replace(preCityJson,"\"city(?:Plots|Construction|ClaimedQuests)\":(?:null|\\[\\]),?","");
            preCityJson=preCityJson.Replace("\"cityVisited\":false,","").Replace(",}","}");
            var migratedCity=JsonUtility.FromJson<World>(preCityJson);migratedCity.Tick(.001);
            Check(migratedCity.cityVersion==1&&migratedCity.cityPlots.Count==9,"pre-city JSON did not migrate");
            RoundTrip(repository,migratedCity,"migrated pre-city save");
            ConstructionRefundJsonChecks(root);
            ConstructionCompletionJsonChecks(root);
            var preSeason=Fresh(true);int oldTarget=preSeason.lands.First(t=>preSeason.AttackProblem(t.id)=="").id;
            preSeason.order=new ArmyOrder{target=oldTarget,duration=12,remaining=6};
            string oldJson=JsonUtility.ToJson(preSeason);
            oldJson=System.Text.RegularExpressions.Regex.Replace(oldJson,"\"(?:seasonMechanicsVersion|commands|commandRecovery|action|phase|routeVersion|routeStart|routeEnd)\":-?[0-9.Ee+]+,?","");
            oldJson=System.Text.RegularExpressions.Regex.Replace(oldJson,"\"path\":\\[\\],?","").Replace(",}","}");
            Check(!oldJson.Contains("seasonMechanicsVersion")&&!oldJson.Contains("routeVersion"),"pre-S1 fixture still contains new fields");
            var migrated=JsonUtility.FromJson<World>(oldJson);string migrationProblem;
            Check(migrated.IsValid(out migrationProblem),"pre-S1 JSON rejected: "+migrationProblem);
            migrated.Tick(100);Check(migrated.order==null&&migrated.reports.Count==1&&migrated.commands==20,"pre-S1 JSON migration lost march or command initialization");
            TrainingJsonChecks(repository,root);
            BattleReplayJsonChecks(repository,root);
            BattleArrivalJsonChecks(root);

            string main = Path.Combine(root, JsonCampaignRepository.YouzhouFileName), backupPath = main + ".bak";
            var a = Fresh(true); a.resources.grain = 4123; repository.Save(a);
            var b = Fresh(true); b.resources.grain = 4567; repository.Save(b);
            bool backup;
            Check(repository.Load(true, out backup).resources.grain == 4567 && !backup, "primary rotation");
            string savedBackup = File.ReadAllText(backupPath);
            Check(JsonUtility.FromJson<World>(savedBackup).resources.grain == 4123, "backup rotation");
            File.WriteAllText(main, "{invalid json");
            var recovered = repository.Load(true, out backup);
            Check(backup && recovered.resources.grain == 4123, "corrupt primary recovery");
            Check(File.ReadAllText(main) == "{invalid json" && File.ReadAllText(backupPath) == savedBackup, "load modified candidates");
            repository.Save(recovered);
            Check(File.ReadAllText(backupPath) == savedBackup && repository.Load(true, out backup).resources.grain == 4123 && !backup, "recovery save destroyed backup");
            File.Move(main, main + ".retained");
            Check(repository.Exists(true) && repository.Load(true, out backup).resources.grain == 4123 && backup, "backup-only load");
            repository.Save(recovered);
            Check(File.Exists(main), "backup-only save");

            string beforeMain = File.ReadAllText(main), beforeBackup = File.ReadAllText(backupPath);
            var invalid = Fresh(true); invalid.mapWidth = 128;
            Throws<InvalidDataException>(() => repository.Save(invalid), "invalid write accepted");
            Check(File.ReadAllText(main) == beforeMain && File.ReadAllText(backupPath) == beforeBackup, "invalid save changed data");
            using (var locked = new FileStream(main, FileMode.Open, FileAccess.Read, FileShare.None))
                Throws<IOException>(() => repository.Save(Fresh(true)), "locked primary write accepted");
            Check(File.ReadAllText(main) == beforeMain && File.ReadAllText(backupPath) == beforeBackup, "failed write changed saves");
            Check(Directory.GetFiles(root, "*.tmp").Length == 0, "failed save left temporary file");

            string wrongRoot = Path.Combine(root, "wrong-slot"); Directory.CreateDirectory(wrongRoot);
            File.WriteAllText(Path.Combine(wrongRoot, JsonCampaignRepository.LegacyFileName), JsonUtility.ToJson(Fresh(true)));
            var wrong = new JsonCampaignRepository(wrongRoot);
            Throws<InvalidDataException>(() => wrong.Load(false, out ignored), "wrong map accepted in legacy slot");
            File.WriteAllText(Path.Combine(wrongRoot, JsonCampaignRepository.YouzhouFileName), JsonUtility.ToJson(invalid));
            File.WriteAllText(Path.Combine(wrongRoot, JsonCampaignRepository.YouzhouFileName) + ".bak", "bad backup");
            Throws<InvalidDataException>(() => wrong.Load(true, out ignored), "two invalid candidates accepted");

            string orphanRoot = Path.Combine(root, "orphan"); Directory.CreateDirectory(orphanRoot);
            File.WriteAllText(Path.Combine(orphanRoot, JsonCampaignRepository.YouzhouFileName) + ".orphan.tmp", JsonUtility.ToJson(Fresh(true)));
            var orphan = new JsonCampaignRepository(orphanRoot);
            Check(!orphan.Exists(true), "orphan temporary file considered a save");
            Throws<FileNotFoundException>(() => orphan.Load(true, out ignored), "orphan temporary file loaded");

            DateTime now = new DateTime(2026,10,25,12,0,0,DateTimeKind.Utc);
            var sessionRepo = new JsonCampaignRepository(Path.Combine(root, "session"));
            var session = new CampaignSession(sessionRepo, () => now);
            session.StartYouzhou(0,"Session"); session.Save();
            Check(session.World.savedUtcTicks == now.Ticks, "session timestamp");
            now = now.AddHours(2); session.Load(true, out backup);
            Check(Math.Abs(session.World.elapsed - 7200) < .001, "offline settlement");
            session.Save(); session.Load(true, out backup);
            Check(Math.Abs(session.World.elapsed - 7200) < .001, "offline settlement applied twice after save");
            string sessionPath = Path.Combine(root,"session",JsonCampaignRepository.YouzhouFileName);
            string untouched = File.ReadAllText(sessionPath);
            var readOnly = new CampaignSession(sessionRepo, () => now.AddHours(1), true);
            readOnly.Load(true, out backup); long stamp = readOnly.World.savedUtcTicks; readOnly.Save();
            Check(File.ReadAllText(sessionPath) == untouched && readOnly.World.savedUtcTicks == stamp, "read-only session wrote a save");
            File.WriteAllText(Path.Combine(root,"result.txt"), "PASS: " + checks + " Unity JSON filesystem repository checks");
            Debug.Log("EASTERN_DYNASTY_REPOSITORY_OK: " + checks + " checks; " + root);
        }
    }
}
