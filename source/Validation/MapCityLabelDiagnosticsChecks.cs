using System;
using System.Collections;
using System.Collections.Generic;

// Synthetic adapters for executing the production diagnostic coroutines without
// Unity. These tests establish observation lifetime and restoration, not pixels.
namespace UnityEngine
{
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
    public struct Rect
    {
        public float x,y,width,height;
        public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public Vector2 center { get {return new Vector2(x+width/2,y+height/2);} }
        public bool Contains(Vector2 p){return p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;}
        public bool Overlaps(Rect q){return x<q.x+q.width&&x+width>q.x&&y<q.y+q.height&&y+height>q.y;}
    }
    public static class Mathf { public static float Abs(float value){return Math.Abs(value);} }
    public static class Screen { public const int height=900; }
    public static class Debug { public static void Log(string message){} }
    public static class JsonUtility { public static string ToJson(object value){return value.ToString();} }
}

namespace EasternDynasty
{
    using UnityEngine;
    public sealed partial class EasternDynastyGame
    {
        private sealed class Land { public int id;public string name; }
        private sealed class World
        {
            public bool IsYouzhou=true;public int homeId=0;
            public List<Land> lands=new List<Land>{new Land{id=0,name="原主城"}};
            public override string ToString(){return lands[0].name;}
        }
        private sealed class Camera
        {
            public Vector3 WorldToScreenPoint(Vector3 point){return new Vector3(450,450,1);}
        }
        private sealed class Map
        {
            public int selected=9;public Vector3 focus=new Vector3(3,0,4);public float zoom=6;
            public Camera viewCamera=new Camera();
            public bool IsBlocked(Vector2 point){return true;}
            public Vector3 SurfacePoint(Land land){return new Vector3();}
            public void CenterHome(){focus=new Vector3(8,0,15);selected=-1;}
            public void AssertYouzhouTerrainGeometry(){}
            public void AssertYouzhouRiverGeometry(bool selected){}
        }
        private sealed class ReviewTarget { public Rect rect; }
        private readonly World world=new World();
        private readonly Map map=new Map();
        private readonly Dictionary<string,ReviewTarget> reviewTargets=new Dictionary<string,ReviewTarget>();
        private bool reviewMode,mapCityLabelTextFits=true;
        private float speed=2,scale=1;
        private Rect uiPixels=new Rect(0,0,1440,900);
        private bool MapCityLabelDesignReady { get {return true;} }
        private int captures;
        private bool omitHome,shortLongPanel,failCapture;
        private Rect MapHudReviewRect(ReviewTarget target){return target.rect;}
        private Rect RequiredMapCommandRect(string id)
        {
            ReviewTarget target;if(!reviewTargets.TryGetValue(id,out target))throw new Exception("Missing visible target: "+id);
            return target.rect;
        }
        private bool MapCommandContains(Rect outer,Rect inner)
        {return inner.x>=outer.x&&inner.y>=outer.y&&inner.x+inner.width<=outer.x+outer.width&&inner.y+inner.height<=outer.y+outer.height;}
        private void AssertMapHudTargets(){}
        private void AssertMinimapAtlas(){}
        private IEnumerator CaptureState(string filename)
        {
            captures++;
            // Yield before and after drawing as the production capture does.
            yield return null;
            if(failCapture&&captures==2)throw new Exception("Synthetic capture failed.");
            reviewTargets.Clear();
            if(reviewMode&&!omitHome)
            {
                float height=world.lands[0].name.Length>10&&!shortLongPanel?72:52;
                Add("city-label-0",300,300,304,height);
                Add("city-label-art-0",306,300+(height-28)/2,40,28);
                Add("city-label-name-0",360,302,234,height-28);
                Add("city-label-detail-0",360,300+height-24,234,22);
            }
            yield return null;
        }
        private void Add(string id,float x,float y,float w,float h){reviewTargets.Add(id,new ReviewTarget{rect=new Rect(x,y,w,h)});}
        private static void Drain(IEnumerator routine)
        {
            try
            {
                while(routine.MoveNext())
                {
                    var nested=routine.Current as IEnumerator;
                    if(nested!=null)Drain(nested);
                }
            }
            finally {var disposable=routine as IDisposable;if(disposable!=null)disposable.Dispose();}
        }
        private static void RunCase(bool initialReview,string fault)
        {
            var game=new EasternDynastyGame{reviewMode=initialReview,omitHome=fault=="missing",shortLongPanel=fault=="long",failCapture=fault=="capture"};
            string failed=null;
            try{Drain(game.MapCityLabelSmoke());}catch(Exception error){failed=error.Message;}
            string expected=fault=="missing"?"actual home marker":fault=="long"?"full two-line design":fault=="capture"?"Synthetic capture failed":null;
            if(expected==null&&failed!=null||expected!=null&&(failed==null||!failed.Contains(expected)))
                throw new Exception("Wrong diagnostic result initialReview="+initialReview+" fault="+fault+": "+failed);
            if(game.reviewMode!=initialReview||game.speed!=2||game.world.lands[0].name!="原主城"
                ||game.map.selected!=9||game.map.focus.x!=3||game.map.focus.z!=4||game.map.zoom!=6)
                throw new Exception("Diagnostic leaked review, campaign, speed or camera state: "+fault);
            if(!initialReview&&game.reviewTargets.Count!=0)throw new Exception("Inactive review retained synthetic targets.");
            if(fault==""&&game.captures!=2)throw new Exception("Did not execute both real production fixture assertions.");
            Console.WriteLine("PASS: initialReview="+initialReview+" fault="+(fault==""?"none":fault)+"; production coroutine restoration and assertion semantics.");
        }
        public static int Main()
        {
            try
            {
                foreach(bool initialReview in new[]{false,true})foreach(string fault in new[]{"","missing","long","capture"})RunCase(initialReview,fault);
                Console.WriteLine("MAP_CITY_LABEL_DIAGNOSTICS_OK: 8 pure coroutine lifecycle cases; synthetic drawing only, no Unity execution or visual claim.");return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
        }
    }
}
