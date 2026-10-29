using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private bool mobileMode, mobileLayoutReview, skipMobileTick, previousKeyboardVisible;
        private Rect uiPixels;
        private readonly MobileSuspendClock mobileClock = new MobileSuspendClock();
        private bool mobileMultiTouch;
        private int mobileSuppressUiThrough = -1;
        private int mobileScrollFinger = -1, mobileScrollFrame = -1;
        private Rect mobileScrollArea;
        private Vector2 mobileScrollStart, mobileScrollPrevious;
        private bool mobileScrollDragging;

        private void ConfigureMobilePlatform(string[] args)
        {
            mobileLayoutReview = !Application.isMobilePlatform && Array.IndexOf(args,"-edMobileReview") >= 0;
            mobileMode = Application.isMobilePlatform || mobileLayoutReview;
            if(!mobileMode)return;
            // IMGUI still needs its primary simulated pointer. MapView uses only
            // native touches so a second finger cannot become a right-mouse drag.
            Input.simulateMouseWithTouches = true;
            if(Application.isMobilePlatform)
            {
                Application.runInBackground=false;
                Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
                Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;
                Screen.orientation=ScreenOrientation.AutoRotation;
            }
        }

        private Font CreateInterfaceFont()
        {
            if(mobileMode)
            {
                var bundled=Resources.Load<Font>("Fonts/CJK");
                if(bundled!=null)return bundled;
                Debug.LogWarning("Bundled Chinese font is missing; using device font fallback.");
                return Font.CreateDynamicFontFromOSFont(new[]{"Noto Sans CJK SC","Noto Sans SC","Droid Sans Fallback","sans-serif","Arial"},18);
            }
            return Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},18);
        }

        private void PrepareInterfaceLayout()
        {
            Rect safe=mobileMode?Screen.safeArea:new Rect(0,0,Screen.width,Screen.height);
            if(mobileLayoutReview)
                safe=new Rect(Screen.width*.035f,24,Screen.width*.94f,Mathf.Max(1,Screen.height-36));
            var layout=MobileScreenLayout.Calculate(Screen.width,Screen.height,safe.x,safe.y,safe.width,safe.height,mobileMode);
            Rect nextPixels=new Rect(layout.X,layout.Y,layout.Width,layout.Height);
            if(mobileMode&&nextPixels!=uiPixels)
            {
                map.CancelPointerInput();mobileScrollFinger=-1;mobileScrollDragging=false;CancelCityPanoramaPointer();
                mobileSuppressUiThrough=Time.frameCount+1;
            }
            uiPixels=nextPixels;
            scale=layout.Scale;width=layout.LogicalWidth;height=layout.LogicalHeight;
            GUI.matrix=Matrix4x4.identity;
            if(mobileMode)
            {
                RectFill(new Rect(0,0,Screen.width,uiPixels.y),Ink);
                RectFill(new Rect(0,uiPixels.yMax,Screen.width,Screen.height-uiPixels.yMax),Ink);
                RectFill(new Rect(0,uiPixels.y,uiPixels.x,uiPixels.height),Ink);
                RectFill(new Rect(uiPixels.xMax,uiPixels.y,Screen.width-uiPixels.xMax,uiPixels.height),Ink);
            }
            GUI.matrix=Matrix4x4.TRS(new Vector3(uiPixels.x,uiPixels.y,0),Quaternion.identity,new Vector3(scale,scale,1));
            SuppressMobileUiEvent();
        }

        private Rect GuiRectToPixels(Rect rect)
        {return new Rect(uiPixels.x+rect.x*scale,uiPixels.y+rect.y*scale,rect.width*scale,rect.height*scale);}
        private Vector2 GuiPointToPixels(Vector2 point)
        {return new Vector2(uiPixels.x+point.x*scale,uiPixels.y+point.y*scale);}
        private Vector2 ScreenPointToGui(Vector3 screenBottomLeft)
        {return new Vector2((screenBottomLeft.x-uiPixels.x)/scale,(Screen.height-screenBottomLeft.y-uiPixels.y)/scale);}

        private void UpdateMobileInterface()
        {
            if(!mobileMode)return;
            if(Input.touchCount>1)mobileMultiTouch=true;
            if(mobileMultiTouch)
            {
                mobileSuppressUiThrough=Time.frameCount+1;
                mobileScrollFinger=-1;mobileScrollDragging=false;
                if(Input.touchCount==0)mobileMultiTouch=false;
            }
            if(Input.touchCount==0){mobileScrollFinger=-1;mobileScrollDragging=false;}
            if(Input.touchCount==1&&Input.GetTouch(0).phase==TouchPhase.Canceled)mobileSuppressUiThrough=Time.frameCount+1;
            bool keyboard=TouchScreenKeyboard.visible;
            if(Input.GetKeyDown(KeyCode.Escape)&&!keyboard&&!previousKeyboardVisible)HandleMobileBack();
            previousKeyboardVisible=keyboard;
        }

        private void SuppressMobileUiEvent()
        {
            if(!mobileMode||Time.frameCount>mobileSuppressUiThrough)return;
            GUIUtility.hotControl=0;
            if(Event.current.isMouse)Event.current.Use();
        }

        // Finger scrolling for existing IMGUI lists. Desktop mouse-wheel and
        // scrollbars keep their original behaviour; a scroll cannot open a card.
        private Vector2 BeginContentScroll(Rect area,Vector2 scroll,Rect content)
        {
            if(mobileMode&&GUI.enabled&&Input.touchCount==1&&!mobileMultiTouch)
            {
                Touch touch=Input.GetTouch(0);
                Vector2 point=ScreenPointToGui(touch.position);
                if(touch.phase==TouchPhase.Began&&area.Contains(point)&&mobileScrollFinger<0)
                {
                    mobileScrollFinger=touch.fingerId;mobileScrollArea=area;
                    mobileScrollStart=mobileScrollPrevious=point;mobileScrollDragging=false;mobileScrollFrame=Time.frameCount;
                }
                if(mobileScrollFinger==touch.fingerId&&mobileScrollArea==area&&mobileScrollFrame!=Time.frameCount)
                {
                    mobileScrollFrame=Time.frameCount;
                    float threshold=Mathf.Clamp(Screen.dpi>0?Screen.dpi/160f*8f:12f,10f,30f)/scale;
                    if((point-mobileScrollStart).sqrMagnitude>=threshold*threshold)mobileScrollDragging=true;
                    if(mobileScrollDragging)
                    {
                        scroll.y=Mathf.Clamp(scroll.y-(point.y-mobileScrollPrevious.y),0,Mathf.Max(0,content.height-area.height));
                        mobileSuppressUiThrough=Time.frameCount+1;
                        SuppressMobileUiEvent();
                    }
                    mobileScrollPrevious=point;
                    if(touch.phase==TouchPhase.Ended||touch.phase==TouchPhase.Canceled)mobileScrollFinger=-1;
                }
            }
            SuppressMobileUiEvent();
            return GUI.BeginScrollView(area,scroll,content);
        }

        private void HandleMobileBack()
        {
            map.CancelPointerInput();
            switch(MobileNavigation.Back(confirmation!=null,cardDetailId!=null,drawResult!=null,tab,map.selected,trainingEditingFormation,cityPage,cityEditingFormation,cityQuestReceipt!=null,reinforcementEditingFormation,trainingHistoryOpen,cityOverviewPlanning,cityOverviewExpanded,CityQuestLandReturnAvailable))
            {
                case MobileBackAction.CancelConfirmation: confirmation=null;break;
                case MobileBackAction.CloseCard: CloseCardDetails();break;
                case MobileBackAction.CloseDraw: drawResult=null;break;
                case MobileBackAction.CloseQuestReward: CloseCityQuestReward();break;
                case MobileBackAction.Recruit: tab="招募";break;
                case MobileBackAction.BattleReports: CloseBattleReplay();break;
                case MobileBackAction.BattleMap: CloseCurrentBattle();break;
                case MobileBackAction.TrainingOrigin: ReturnFromTraining();break;
                case MobileBackAction.TrainingPlan: if(trainingHistoryOpen)CloseTrainingHistory();else ResumeTraining();break;
                case MobileBackAction.ReinforcementOrigin: ReturnFromReinforcement();break;
                case MobileBackAction.ReinforcementPlan: ResumeReinforcement();break;
                case MobileBackAction.CityOverview: ReturnFromCityPage();break;
                case MobileBackAction.CityFormationOrigin: ReturnToCityFormationOrigin();break;
                case MobileBackAction.CityQuestObjective: ReturnFromCityQuestLand();break;
                case MobileBackAction.Map: tab="领地";break;
                case MobileBackAction.ClearLand: map.selected=-1;break;
                default: Ask(world==null?"退出游戏？":"保存当前征程并退出游戏？",()=>{if(TrySave(false))Application.Quit();});break;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if(!mobileMode)return;
            if(paused)
            {
                CancelCityPanoramaPointer();
                if(mobileClock.Suspend(DateTime.UtcNow))Save(false);
                if(map!=null)map.CancelPointerInput();
                return;
            }
            if(!mobileClock.IsPaused)return;
            double elapsed=mobileClock.Resume(DateTime.UtcNow,CampaignSession.MaximumOfflineSeconds);
            if(world!=null){ObserveBattleMarch();session.Tick(elapsed);Save(false);}
            skipMobileTick=true;autosave=0;
        }
    }
}
