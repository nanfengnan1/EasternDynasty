using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillCityCatalogReview(ReviewState review)
        {
            review.cityCatalogCategory=cityCatalogCategory;
            review.cityCatalogScrollY=cityCatalogScroll.y;
            review.cityFacilitiesOrigin=cityFacilitiesOrigin;
            review.visibleFacilityIds=reviewTargets.Keys.Where(id=>id.StartsWith("city-catalog-card-",StringComparison.Ordinal))
                .Select(id=>int.Parse(id.Substring("city-catalog-card-".Length))).OrderBy(id=>id).ToArray();
            bool visible=tab=="主城"&&cityPage=="catalog"&&reviewTargets.ContainsKey("city-catalog-scroll");
            review.cityCatalogStyle=visible?"inkscape-facility-atlas-v2":"";
            review.cityCatalogDesignReady=visible&&cityCatalogDesignReady;
            review.cityCatalogDesignTextFits=visible&&cityCatalogDesignTextFits;
            review.cityCatalogCanvas=visible?CityOverviewReviewBounds(cityCatalogCanvas):new float[0];
            review.cityCatalogHallArt=visible?CityOverviewReviewBounds(cityCatalogHallArt):new float[0];
            review.cityCatalogNodes=visible?cityCatalogNodes.Select(n=>new ReviewCityCatalogNode{
                id=(int)n.kind,level=n.level,next=n.next,requiredHall=n.requiredHall,
                card=CityOverviewReviewBounds(n.card),art=CityOverviewReviewBounds(n.art),
                name=CityOverviewReviewBounds(n.name),state=CityOverviewReviewBounds(n.state),
                effect=CityOverviewReviewBounds(n.effect),condition=CityOverviewReviewBounds(n.condition),
                action=CityOverviewReviewBounds(n.action)}).ToArray():new ReviewCityCatalogNode[0];
        }

        private sealed partial class ReviewState
        {
            public int cityCatalogCategory;
            public float cityCatalogScrollY;
            public string cityFacilitiesOrigin;
            public int[] visibleFacilityIds;
            public string cityCatalogStyle;
            public bool cityCatalogDesignReady,cityCatalogDesignTextFits;
            public float[] cityCatalogCanvas,cityCatalogHallArt;
            public ReviewCityCatalogNode[] cityCatalogNodes;
        }

        [Serializable] private sealed class ReviewCityCatalogNode
        {
            public int id,level,next,requiredHall;
            public float[] card,art,name,state,effect,condition,action;
        }

        private static bool CityCatalogDesignRectMatches(Rect actual,Rect expected)
        {
            return Mathf.Abs(actual.x-expected.x)+Mathf.Abs(actual.y-expected.y)
                +Mathf.Abs(actual.width-expected.width)+Mathf.Abs(actual.height-expected.height)<.1f;
        }

        private void AssertCityCatalogAtlasGeometry()
        {
            if(tab!="主城"||cityPage!="catalog"||!BlocksMapInput||cityCatalogNodes.Count==0)
                throw new Exception("Facility atlas must expose real, fully visible building nodes.");
            if(!cityCatalogDesignReady||!cityCatalogDesignTextFits)
                throw new Exception("Facility atlas must load its inspected Inkscape assets and fit its actual Chinese GUI fonts.");
            var content=new Rect(24,142,width-48,height-240);
            if(!CityOverviewContains(content,cityCatalogCanvas)||!CityOverviewContains(content,cityCatalogHallArt)
                ||cityCatalogCanvas.Overlaps(cityCatalogHallArt))
                throw new Exception("Facility atlas core building overlaps or escapes the drawing sheet.");
            if(!CityCatalogDesignRectMatches(cityCatalogHallArt,new Rect(content.x+12,content.y+56,184,138)))
                throw new Exception("Facility atlas hall illustration differs from the inspected rectangular design.");
            int columns=cityCatalogCanvas.width>=1460?3:2;
            float cardWidth=(cityCatalogCanvas.width-24-16*(columns-1))/columns;
            var definitions=CityCatalog.Buildings.Where(d=>cityCatalogCategory==0||CityFacilityCategory(d.kind)==cityCatalogCategory).ToArray();
            foreach(var node in cityCatalogNodes)
            {
                if(!CityOverviewContains(cityCatalogCanvas,node.card)||!CityOverviewContains(node.card,node.art)
                    ||!CityOverviewContains(node.card,node.name)||!CityOverviewContains(node.card,node.action)
                    ||!CityOverviewContains(node.card,node.state)||!CityOverviewContains(node.card,node.effect)
                    ||!CityOverviewContains(node.card,node.condition)
                    ||!CityOverviewContains(uiPixels,GuiRectToPixels(node.card)))
                    throw new Exception("Facility artwork, label or action is clipped: "+node.kind);
                if(node.art.width<160||node.art.height<160||node.action.height<44||node.art.Overlaps(node.action))
                    throw new Exception("Facility atlas lost its large image or separate touch command.");
                int index=Array.FindIndex(definitions,d=>d.kind==node.kind);
                var expectedCard=new Rect(cityCatalogCanvas.x+(index%columns)*(cardWidth+16),
                    cityCatalogCanvas.y+(index/columns)*222-cityCatalogScroll.y,cardWidth,206);
                if(index<0||!CityCatalogDesignRectMatches(node.card,expectedCard)
                    ||!CityCatalogDesignRectMatches(node.art,new Rect(node.card.x+10,node.card.y+14,216,162))
                    ||!CityCatalogDesignRectMatches(node.name,new Rect(node.card.x+238,node.card.y+8,node.card.width-250,32))
                    ||!CityCatalogDesignRectMatches(node.state,new Rect(node.card.x+238,node.card.y+42,node.card.width-250,22))
                    ||!CityCatalogDesignRectMatches(node.effect,new Rect(node.card.x+238,node.card.y+72,node.card.width-250,24))
                    ||!CityCatalogDesignRectMatches(node.condition,new Rect(node.card.x+238,node.card.y+102,node.card.width-250,28))
                    ||!CityCatalogDesignRectMatches(node.action,new Rect(node.card.x+238,node.card.y+150,node.card.width-250,44))
                    ||node.art.xMax>=node.name.xMin||node.name.yMax>node.state.yMin
                    ||node.state.yMax>node.effect.yMin||node.effect.yMax>node.condition.yMin
                    ||node.condition.yMax>node.action.yMin)
                    throw new Exception("Facility atlas image, title, factual rows and separate command differ from the inspected Inkscape design: "+node.kind);
                var definition=CityCatalog.Get(node.kind);
                if(node.level!=world.CityLevel(node.kind)||node.next!=Math.Min(definition.maximumLevel,node.level+1)
                    ||node.requiredHall!=CityCatalog.RequiredHallLevel(node.kind,node.next))
                    throw new Exception("Facility atlas presents a fabricated level or prerequisite.");
                if(cityCatalogCategory!=0&&CityFacilityCategory(node.kind)!=cityCatalogCategory)
                    throw new Exception("Facility atlas image does not belong to its selected category.");
                var route=CityFacilityNavigation.Resolve(world,node.kind);
                foreach(string prefix in new[]{"city-catalog-image-","city-catalog-name-","city-catalog-go-"})
                    if(reviewTargets.ContainsKey(prefix+(int)node.kind)!=route.canNavigate)
                        throw new Exception("Facility image/name/action target does not match its actual navigation route.");
                if(route.canNavigate)
                {
                    AssertCityCompositionTarget("city-catalog-image-"+(int)node.kind,node.art,node.card);
                    AssertCityCompositionTarget("city-catalog-name-"+(int)node.kind,node.name,node.card);
                    AssertCityCompositionTarget("city-catalog-go-"+(int)node.kind,node.action,node.card);
                }
            }
            if(reviewTargets.Keys.Count(id=>id.StartsWith("city-catalog-card-",StringComparison.Ordinal))!=cityCatalogNodes.Count)
                throw new Exception("Clipped facility atlas nodes were incorrectly registered as visible.");
            foreach(var target in reviewTargets.Values.Where(t=>t.id.StartsWith("city-catalog-category-",StringComparison.Ordinal)||t.id=="city-catalog-queue"))
                if(target.h<44*scale-.1f||!CityOverviewContains(uiPixels,new Rect(target.x-target.w*.5f,target.y-target.h*.5f,target.w,target.h)))
                    throw new Exception("Facility atlas navigation is clipped or too small for touch.");
        }

        private IEnumerator CityFacilityCatalogSmoke()
        {
            var oldSession=session;string oldTab=tab,oldPage=cityPage,oldStatus=status,oldOrigin=cityFacilitiesOrigin;
            bool oldReview=reviewMode;
            int oldSelected=map.selected,oldPlot=citySelectedPlot,oldCategory=cityCatalogCategory;
            var oldBuilding=citySelectedBuilding;var oldScroll=cityCatalogScroll;var oldDetailScroll=cityDetailScroll;
            var oldFocus=map.focus;float oldZoom=map.zoom,oldSpeed=speed;
            string original=JsonUtility.ToJson(world);
            session=new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,"smoke-city-catalog-unused")),()=>DateTime.UtcNow,true);
            try
            {
                session.StartYouzhou(0,"设施分类验收");map.Setup(world);speed=0;reviewMode=true;
                OpenCityFacilityCatalog();cityCatalogCategory=0;cityCatalogScroll=Vector2.zero;
                string unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-catalog.png");
                AssertCityCatalogAtlasGeometry();
                yield return CaptureState("smoke-facility-catalog-design.png");
                AssertCityCatalogAtlasGeometry();
                if(cityPage!="catalog"||!BlocksMapInput||world.CityQuestReady(1)||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Facility catalog did not open read-only or block the map.");
                // The gallery alone does not complete the district-visit objective.
                // Entering its real facility panel preserves the established visit rule.
                NavigateCityFacility(CityBuilding.TownHall);
                if(cityPage!="facilities"||!world.CityQuestReady(1))throw new Exception("Catalog actual facility visit did not advance the district objective.");
                ReturnFromCityPage();unchanged=JsonUtility.ToJson(world);
                cityCatalogCategory=3;cityCatalogScroll=Vector2.zero;
                yield return CaptureState("smoke-facility-catalog-military.png");
                AssertCityCatalogAtlasGeometry();
                NavigateCityFacility(CityBuilding.Barracks);
                if(cityPage!="facilities"||citySelectedPlot!=4||citySelectedBuilding!=CityBuilding.TownHall||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Locked catalog facility did not reach its real prerequisite without spending.");
                HandleMobileBack();
                if(cityPage!="catalog"||cityCatalogCategory!=3||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("System back lost the catalog category or changed world state.");
                cityCatalogCategory=2;cityCatalogScroll=Vector2.zero;
                yield return CaptureState("smoke-facility-catalog-economy.png");
                AssertCityCatalogAtlasGeometry();
                NavigateCityFacility(CityBuilding.Mill);
                int millPlot=citySelectedPlot;
                if(cityPage!="facilities"||citySelectedBuilding!=CityBuilding.Mill||world.cityPlots[millPlot].building!=CityBuilding.None||world.CityJob!=null||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Catalog new facility did not reach a real empty plot without construction.");
                world.resources=new Supplies{grain=50000,grass=50000,stone=50000,iron=50000,gold=50000};
                world.StartCityConstruction(millPlot,CityBuilding.Mill);
                if(world.CityJob==null||world.CityJob.building!=CityBuilding.Mill)throw new Exception("Catalog fixture failed normal mill construction.");
                ReturnFromCityPage();unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-catalog-working.png");
                AssertCityCatalogAtlasGeometry();
                NavigateCityFacility(CityBuilding.Quarry);
                if(citySelectedPlot!=millPlot||citySelectedBuilding!=CityBuilding.Mill||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Catalog busy action did not locate the actual construction queue.");
                ReturnFromCityPage();world.Tick(world.CityJob.remaining+.01);
                if(world.CityLevel(CityBuilding.Mill)!=1||world.CityHourlyIncome().grain!=160)throw new Exception("Catalog facility completion disagrees with actual income.");
                yield return CaptureState("smoke-facility-catalog-built.png");
                AssertCityCatalogAtlasGeometry();
                unchanged=JsonUtility.ToJson(world);NavigateCityFacility(CityBuilding.Mill);
                if(citySelectedPlot!=millPlot||citySelectedBuilding!=CityBuilding.Mill||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Catalog existing facility lost its actual site or started another job.");
                ReturnFromCityPage();NavigateCityFacility(CityBuilding.Mill);
                tab="领地";OpenCityFacilities(4);ReturnFromCityPage();
                if(cityPage!="overview"||cityFacilitiesOrigin!="overview"||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Facility entry from another tab inherited a stale catalog return origin.");
                OpenCityFacilityCatalog();HandleMobileBack();
                if(cityPage!="overview"||JsonUtility.ToJson(world)!=unchanged)throw new Exception("Catalog back did not return to city overview read-only.");
                Debug.Log("CITY_CATALOG_RUNTIME_OK: isolated read-only catalog; real prerequisite, empty plot, queue, completed facility, actual income and system-back hierarchy; no navigation charge or duplicate construction.");
                // Build through actual Core commands; no edited facility levels in this visual fixture.
                CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);
                CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);
                foreach(var definition in CityCatalog.Buildings.Where(d=>world.CityLevel(d.kind)==0))
                    CompleteCitySmokeConstruction(world,world.cityPlots.First(p=>p.building==CityBuilding.None&&world.IsCityDistrict(p.landId)).index,definition.kind);
                OpenCityFacilityCatalog();cityCatalogCategory=0;cityCatalogScroll=new Vector2(0,10000);
                unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-catalog-developed.png");
                AssertCityCatalogAtlasGeometry();
                if(!cityCatalogNodes.Any(n=>n.kind==CityBuilding.Workshop)||cityCatalogScroll.y<=0||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Developed facility atlas did not expose the built workshop at its real scroll position.");
                // Exercise the longest real effect label with normal construction commands in this isolated session.
                world.resources=new Supplies{grain=1000000,grass=1000000,stone=1000000,iron=1000000,gold=1000000};
                for(int level=world.CityLevel(CityBuilding.TownHall);level<8;level++)
                    CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);
                int barracksPlot=world.cityPlots.First(p=>p.building==CityBuilding.Barracks).index;
                for(int level=world.CityLevel(CityBuilding.Barracks);level<10;level++)
                    CompleteCitySmokeConstruction(world,barracksPlot,CityBuilding.Barracks);
                cityCatalogCategory=3;cityCatalogScroll=Vector2.zero;unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-catalog-maximum.png");
                AssertCityCatalogAtlasGeometry();
                var maximumNode=cityCatalogNodes.FirstOrDefault(n=>n.kind==CityBuilding.Barracks);
                var maximumRoute=CityFacilityNavigation.Resolve(world,CityBuilding.Barracks);
                if(maximumNode==null||maximumNode.level!=10||maximumNode.next!=10||world.CityLevel(CityBuilding.TownHall)!=8
                    ||maximumRoute.state!=CityFacilityRouteState.MaximumLevel||!maximumRoute.canNavigate
                    ||maximumRoute.plotIndex!=barracksPlot||maximumRoute.building!=CityBuilding.Barracks
                    ||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Maximum barracks atlas must fit its actual capped effect and retain read-only navigation to its real site.");
                NavigateCityFacility(CityBuilding.Barracks);
                if(cityPage!="facilities"||citySelectedPlot!=barracksPlot||citySelectedBuilding!=CityBuilding.Barracks
                    ||cityFacilitiesOrigin!="catalog"||world.CityJob!=null||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Maximum barracks inspection changed the campaign or left its actual built site.");
                ReturnFromCityPage();
                if(cityPage!="catalog"||cityCatalogCategory!=3||cityCatalogScroll.y!=0||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Maximum barracks return lost the military category or changed the campaign.");
                Ask("设施图谱遮挡验收",()=>{});
                for(int i=0;i<3;i++)yield return null;
                yield return new WaitForEndOfFrame();
                if(reviewTargets.Keys.Any(id=>id.StartsWith("city-catalog-",StringComparison.Ordinal))||cityCatalogNodes.Count!=0||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Covered facility atlas exposed actionable background targets or changed the campaign.");
                HandleMobileBack();
                Debug.Log("CITY_CATALOG_ATLAS_RUNTIME_OK: large actual building images, visible level/prerequisite nodes, filtered categories, 44-high actions, built lower row and modal target isolation.");
                Debug.Log("CITY_CATALOG_DESIGN_RUNTIME_OK: inspected Inkscape horizontal building illustrations, exact title/state/effect/condition/action geometry, real Chinese font fit including level-10 barracks, exported assets, image/name/action routes, maximum-level read-only inspection, visible scroll rows and modal isolation.");
            }
            finally
            {
                session=oldSession;map.Setup(world);map.selected=oldSelected;map.focus=oldFocus;map.zoom=oldZoom;
                tab=oldTab;cityPage=oldPage;status=oldStatus;cityFacilitiesOrigin=oldOrigin;
                citySelectedPlot=oldPlot;citySelectedBuilding=oldBuilding;cityCatalogCategory=oldCategory;
                cityCatalogScroll=oldScroll;cityDetailScroll=oldDetailScroll;speed=oldSpeed;
                confirmation=null;reviewMode=oldReview;if(!reviewMode)reviewTargets.Clear();
                if(JsonUtility.ToJson(world)!=original)throw new Exception("Catalog fixture modified the original campaign.");
            }
        }
    }
}
