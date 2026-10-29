using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using EasternDynasty;

internal static class CityQuestRewardChecks
{
    private static int checks;
    private static void Check(bool condition,string name)
    { if(!condition)throw new Exception("FAIL "+name);checks++;Console.WriteLine("PASS "+name); }
    private static string Json(World world)
    { using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(World)).WriteObject(stream,world);return System.Text.Encoding.UTF8.GetString(stream.ToArray());} }
    private static World Copy(World world)
    { using(var stream=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Json(world))))return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream); }
    private static double[] Values(Supplies value)
    { return new[]{value.grain,value.grass,value.stone,value.iron,value.gold,value.jade}; }
    private static void Same(Supplies left,Supplies right,string label)
    { var a=Values(left);var b=Values(right);for(int i=0;i<a.Length;i++)Check(Math.Abs(a[i]-b[i])<.000001,label+" resource "+i); }
    private static void CompleteBuilding(World world,CityBuilding building)
    {
        int plot=building==CityBuilding.TownHall?4:world.cityPlots.First(p=>p.building==CityBuilding.None&&world.IsCityDistrict(p.landId)).index;
        world.StartCityConstruction(plot,building);Check(world.CityJob!=null,"started "+building);
        world.Tick(world.CityJob.remaining+.01);
    }
    public static int Main()
    {
        try
        {
            string message;
            Check(CityQuestClaimReceipt.Claim(null,1,out message)==null,"no campaign cannot issue receipt");
            var legacy=World.Create(Homeland.Han,4,"Legacy");string legacyBefore=Json(legacy);
            Check(CityQuestClaimReceipt.Claim(legacy,1,out message)==null&&Json(legacy)==legacyBefore,"legacy campaign cannot receive city rewards");
            var world=World.CreateYouzhou();string before=Json(world);
            foreach(int invalid in new[]{0,1,2,10,11})
                Check(CityQuestClaimReceipt.Claim(world,invalid,out message)==null&&Json(world)==before,"unready or out-of-order claim is side-effect free "+invalid);
            world.RecordCityAction("visit");world.resources.grain+=.125;world.resources.grass+=.25;
            var receipt=CityQuestClaimReceipt.Claim(world,1,out message);
            Check(receipt!=null&&receipt.questId==1&&receipt.nextQuestId==2&&!receipt.chapterComplete&&!receipt.allComplete,"first receipt identifies completed and next task");
            Same(receipt.credited,CityCatalog.Quests[0].reward,"actual credited reward");
            Same(receipt.balance,world.resources,"balance frozen at credit time");
            Check(!ReferenceEquals(receipt.balance,world.resources)&&!ReferenceEquals(receipt.credited,CityCatalog.Quests[0].reward),"receipt owns copies instead of live supplies");
            var frozen=Values(receipt.balance);world.Tick(30);
            Check(Values(receipt.balance).SequenceEqual(frozen),"income cannot alter historical receipt balance");
            before=Json(world);
            for(int i=0;i<5;i++)Check(CityQuestClaimReceipt.Claim(world,1,out message)==null&&Json(world)==before,"repeated claim neither creates receipt nor pays "+i);
            world=Copy(world);before=Json(world);
            Check(CityQuestClaimReceipt.Claim(world,1,out message)==null&&Json(world)==before,"saved claim stays paid after reload");
            receipt.balance.gold=0;receipt.credited.grain=999999;
            Check(Json(world)==before&&CityCatalog.Quests[0].reward.grain==300,"mutating transient presentation cannot alter campaign or reward catalog");

            world.resources=new Supplies{grain=1000000,grass=1000000,stone=1000000,iron=1000000,gold=1000000};
            CompleteBuilding(world,CityBuilding.Mill);
            receipt=CityQuestClaimReceipt.Claim(world,2,out message);
            Check(receipt!=null&&!receipt.chapterComplete&&receipt.nextQuestId==3,"middle task keeps chapter open");
            CompleteBuilding(world,CityBuilding.TownHall);
            receipt=CityQuestClaimReceipt.Claim(world,3,out message);
            Check(receipt!=null&&receipt.chapterIndex==0&&receipt.chapterComplete&&!receipt.allComplete&&receipt.nextQuestId==4,"first chapter closes only after its final successful claim");
            int farmTarget=-1;
            for(int id=4;id<=10;id++)
            {
                if(id==5){world.Recruit(200);Check(world.cityRecruitActions>0,"actual recruitment completes its objective");}
                if(id==6)
                {
                    for(int capture=0;capture<3;capture++)
                    {
                        world.Recruit(world.TroopCap);
                        var target=world.lands.Where(t=>!t.capital&&!t.bridge&&!world.IsCityDistrict(t.id)&&world.AttackProblem(t.id)=="").OrderBy(t=>t.level).First();
                        world.March(target.id);world.Tick(1000);
                        Check(target.owned&&world.order==null,"actual resource capture and return "+capture);
                        if(farmTarget<0)farmTarget=target.id;
                    }
                }
                if(id==7)CompleteBuilding(world,CityBuilding.Barracks);
                if(id==8){world.Farm(farmTarget);world.Tick(1000);Check(world.cityFarmCompletions==1&&world.order==null,"actual farming completes and returns before its claim");}
                if(id==9)CompleteBuilding(world,CityBuilding.TownHall);
                receipt=CityQuestClaimReceipt.Claim(world,id,out message);
                Check(receipt!=null&&receipt.questId==id,"sequential successful reward "+id);
                Same(receipt.credited,CityCatalog.Quests[id-1].reward,"exact reward "+id);
                Check(receipt.chapterComplete==(id==7||id==10),"chapter end marker "+id);
                Check(receipt.allComplete==(id==10)&&receipt.nextQuestId==(id==10?0:id+1),"next target and all-complete marker "+id);
            }
            Check(world.cityClaimedQuests.SequenceEqual(Enumerable.Range(1,10)),"stable IDs remain sequential and unique");
            world=Copy(world);before=Json(world);
            Check(CityQuestClaimReceipt.Claim(world,10,out message)==null&&Json(world)==before,"finished save cannot reopen reward claim");
            string problem;Check(world.IsValid(out problem),"completed campaign remains valid "+problem);
            Console.WriteLine("PASS: "+checks+" city quest reward checks.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
