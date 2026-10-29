using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private sealed partial class ReviewState
        {
            public string cityFacilityStyle;
            public int cityFacilityPortraitId;
            public float[] cityFacilityPortrait,cityFacilityLedger,cityFacilityCosts,cityFacilityAction,cityFacilityOptions;
            public bool cityFacilityDesignAssetsReady,cityFacilityDesignTextFits;
            public float[] cityFacilityDescription,cityFacilityEffectRow;
            public string cityFacilityGalleryStyle;
            public bool cityFacilityGalleryReady,cityFacilityGalleryTextFits;
            public string cityFacilityGalleryTextFailure;
            public float[] cityFacilityGalleryImage,cityFacilityGalleryFooter;
        }

        private void FillCityFacilityDossierReview(ReviewState review)
        {
            bool visible=tab=="主城"&&cityPage=="facilities"&&reviewTargets.ContainsKey("city-detail-scroll");
            review.cityFacilityStyle=visible?CityFacilitySheetLayout.Style:"";
            review.cityFacilityDesignAssetsReady=visible&&citySheetEffect!=null&&citySheetCost!=null&&citySheetArrow!=null&&citySheetCommand!=null&&citySheetCommandHover!=null;
            review.cityFacilityDesignTextFits=visible&&citySheetTextFits;
            review.cityFacilityGalleryStyle=visible?CityFacilityGalleryLayout.Style:"";
            review.cityFacilityGalleryReady=visible&&cityGalleryStage!=null&&cityGalleryFooter!=null&&cityGallerySelected!=null;
            review.cityFacilityGalleryTextFits=visible&&cityGalleryTextFits;
            review.cityFacilityGalleryTextFailure=visible?cityGalleryTextFailure:"";
            review.cityFacilityGalleryImage=visible?CityOverviewReviewBounds(cityGalleryImageRect):new float[0];
            review.cityFacilityGalleryFooter=visible?CityOverviewReviewBounds(cityGalleryFooterRect):new float[0];
            review.cityFacilityDescription=visible?CityOverviewReviewBounds(citySheetDescriptionRect):new float[0];
            review.cityFacilityEffectRow=visible?CityOverviewReviewBounds(citySheetEffectRect):new float[0];
            review.cityFacilityPortraitId=visible?(int)cityFacilityPortraitKind:0;
            review.cityFacilityPortrait=visible?CityOverviewReviewBounds(cityFacilityPortraitRect):new float[0];
            review.cityFacilityLedger=visible?CityOverviewReviewBounds(cityFacilityLedgerRect):new float[0];
            review.cityFacilityCosts=visible&&cityFacilityCostRect.width>0?CityOverviewReviewBounds(cityFacilityCostRect):new float[0];
            review.cityFacilityAction=visible?CityOverviewReviewBounds(cityFacilityActionRect):new float[0];
            review.cityFacilityOptions=visible&&cityFacilityOptionsRect.width>0?CityOverviewReviewBounds(cityFacilityOptionsRect):new float[0];
        }

        private void AssertCityFacilityDossierGeometry(bool costsAtTop)
        {
            if(!cityGalleryTextFits)
                throw new Exception("Facility gallery actual GUI font does not fit: "+cityGalleryTextFailure);
            if(cityGalleryStage==null||cityGalleryFooter==null||cityGallerySelected==null
                ||cityGalleryStage.width!=960||cityGalleryStage.height!=640||cityGalleryFooter.width!=960||cityGalleryFooter.height!=68
                ||cityGallerySelected.width!=192||cityGallerySelected.height!=192)
                throw new Exception("Facility gallery requires three actual exported skins with their original dimensions.");
            if(!CityOverviewContains(cityFacilityPortraitRect,cityGalleryImageRect)||!CityOverviewContains(cityFacilityPortraitRect,cityGalleryFooterRect)
                ||cityGalleryImageRect.Overlaps(cityGalleryFooterRect)||cityGalleryImageRect.height<190||cityGalleryFooterRect.height!=34)
                throw new Exception("Complete facility illustration must clear its independent 34-high name/level footer.");
            if(!citySheetTextFits||citySheetEffect==null||citySheetCost==null||citySheetArrow==null||citySheetCommand==null||citySheetCommandHover==null)
                throw new Exception("Facility sheet requires the five real Inkscape assets and fitting actual GUI fonts.");
            if(tab!="主城"||cityPage!="facilities"||!BlocksMapInput||cityFacilityPortraitKind!=SelectedCityFacilityKind())
                throw new Exception("Facility portrait does not identify the actual selected facility.");
            var content=new Rect(24,142,width-48,height-240);
            var regions=new[]{cityFacilityPortraitRect,cityFacilityLedgerRect,cityFacilityActionRect};
            foreach(var region in regions)
                if(!CityOverviewContains(content,region)||!CityOverviewContains(uiPixels,GuiRectToPixels(region)))
                    throw new Exception("Facility portrait, ledger or fixed action escapes its visible safe area.");
            if(cityFacilityPortraitRect.height<190||cityFacilityActionRect.height<44
                ||cityFacilityLedgerRect.Overlaps(cityFacilityActionRect)||cityFacilityLedgerRect.Overlaps(cityFacilityPortraitRect))
                throw new Exception("Facility artwork is too small or overlaps its independent ledger/action.");
            if(costsAtTop&&!CityOverviewContains(cityFacilityLedgerRect,cityFacilityCostRect))
                throw new Exception("All five actual resource costs must be visible at the top of the facility sheet.");
            if(costsAtTop&&(!CityOverviewContains(cityFacilityLedgerRect,citySheetDescriptionRect)||!CityOverviewContains(cityFacilityLedgerRect,citySheetEffectRect)
                ||citySheetDescriptionRect.yMax>citySheetEffectRect.y||citySheetEffectRect.yMax>cityFacilityCostRect.y))
                throw new Exception("Facility description, horizontal effects and five costs must follow the inspected first-screen order.");
            if(System.Math.Abs(cityFacilityActionRect.width-360)>.1f||System.Math.Abs(cityFacilityActionRect.height-44)>.1f)
                throw new Exception("Facility action differs from the inspected independent 360x44 command.");
            var controls=reviewTargets.Values.Where(t=>t.id.StartsWith("city-slot-",StringComparison.Ordinal)||t.id.StartsWith("city-option-",StringComparison.Ordinal)).ToArray();
            if(controls.Count(t=>t.id.StartsWith("city-slot-",StringComparison.Ordinal))!=9)
                throw new Exception("Compact district locator lost one of the nine actual plots.");
            for(int i=0;i<controls.Length;i++)
            {
                var t=controls[i];var rect=new Rect(t.x-t.w*.5f,t.y-t.h*.5f,t.w,t.h);
                if(t.w<44*scale-.1f||t.h<44*scale-.1f||!CityOverviewContains(uiPixels,rect))
                    throw new Exception("Facility selector is clipped or smaller than a touch target: "+t.id);
                for(int j=0;j<i;j++)
                {var other=controls[j];if(rect.Overlaps(new Rect(other.x-other.w*.5f,other.y-other.h*.5f,other.w,other.h)))throw new Exception("Facility selectors overlap.");}
            }
            bool empty=world.cityPlots[citySelectedPlot].building==CityBuilding.None
                &&(world.CityJob==null||world.CityJob.plotIndex!=citySelectedPlot);
            if(controls.Count(t=>t.id.StartsWith("city-option-",StringComparison.Ordinal))!=(empty?8:0))
                throw new Exception("Facility image selectors do not match the current build/upgrade state.");
            foreach(var control in controls.Where(t=>t.id.StartsWith("city-option-",StringComparison.Ordinal)))
            {
                var suffix=control.id.Substring("city-option-".Length);
                var parent=new Rect(control.x-control.w*.5f,control.y-control.h*.5f,control.w,control.h);
                foreach(string prefix in new[]{"city-facility-image-","city-facility-title-"})
                {
                    if(!reviewTargets.ContainsKey(prefix+suffix))throw new Exception("Facility visible image/name target missing.");
                    var child=reviewTargets[prefix+suffix];var rect=new Rect(child.x-child.w*.5f,child.y-child.h*.5f,child.w,child.h);
                    if(!CityOverviewContains(parent,rect))throw new Exception("Facility image/name target escapes its real parent command.");
                }
            }
        }
    }
}
