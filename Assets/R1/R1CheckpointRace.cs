using System;
using System.Linq;
using B01.MAPGEN001;
using B01.MAPGENWORLD001;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.RAC001;
using RoboRougelike.MapSystemR4;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        bool raceRequested;
        GameObject raceHost;
        public CheckpointRaceChallenge CheckpointRace {get;private set;}
        public bool CanChooseCheckpointRace => CanChooseReactorDefense&&!ReactorPrepared&&
            VehicleModel.SelectedDrive==R3DriveKind.Wheels;
        public bool ChooseCheckpointRace()
        {
            if(!CanChooseCheckpointRace)return false;
            Physics.SyncTransforms();
            if(!TryMeasureEnvelope(Player.GetComponent<Rigidbody>(),out var envelope))return false;
            var profile=SelectedVehicle==R3VehicleKind.Light?MapVehicleProfile.Light:MapVehicleProfile.Heavy;
            profile.Width=Mathf.Max(profile.Width,envelope.x);profile.Length=Mathf.Max(profile.Length,envelope.z);
            var config=MapGenConfig.Default;
            config.LaneWidth=Mathf.Max(config.LaneWidth,Mathf.Ceil((profile.Width+3.5f)/.6f));
            if(generatedArena==null){var host=new GameObject("MAPGEN activities");host.transform.SetParent(transform,false);generatedArena=host.AddComponent<MapGenWorldAdapter>();}
            if(!generatedArena.TryGenerate(new MapWorldRequest{Recipe=MapRecipe.ObstacleCourse,Seed=RequestedSeed,
                Config=config,Vehicle=profile,EnvelopeMeasured=true,ClearancePerSideMeters=.25f},out var world,out var failure))
            {
                string detail="";try{MapGenPlanner.Plan(MapRecipe.ObstacleCourse,RequestedSeed,config,profile);}catch(ArgumentException e){detail=e.Message;}
                return RejectGeneratedArena("Wyścig: "+failure+" "+detail+" obrys="+envelope);
            }
            if(world.Checkpoints.Length==0){generatedArena.ClearWorld();return false;}
            if(Progress.State==R1RunState.Garage&&!StartNextEncounter()){generatedArena.ClearWorld();return false;}
            if(!Progress.ChooseRoute()){generatedArena.ClearWorld();return false;}
            CurrentEncounter=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(RouteOffers[0]));
            CurrentEncounter.Id="RAC001/"+CurrentEncounter.Id;CurrentEncounter.Title="Wyścig przez checkpointy";
            CurrentEncounter.Rule="Przejedź kolejno przez bramki i metę • 180 s";
            Definition.Encounters[Progress.EncounterIndex]=CurrentEncounter;
            reactorRequested=false;ReactorPrepared=false;LastActivityWasReactor=false;
            raceRequested=true;mapgenRequested=true;MapgenMessage=null;
            StartCoroutine(BeginEncounter());return true;
        }
        void BeginCheckpointRace()
        {
            raceHost=new GameObject("RAC001 live R6 course");raceHost.transform.SetParent(transform,false);
            var points=GeneratedWorld.Checkpoints.Concat(new[]{GeneratedWorld.Finish}).Distinct().Select((p,i)=>
            {
                var point=new GameObject("Checkpoint "+(i+1));point.transform.SetParent(raceHost.transform,false);point.transform.position=p;
                ActivityGate(point.transform,palette.Parts[(int)R3PartKind.Core],2.8f);
                return point.transform;
            }).ToArray();
            CheckpointRace=raceHost.AddComponent<CheckpointRaceChallenge>();
            CheckpointRace.Configure(Player.transform,points,2.8f,180);
            if(!CheckpointRace.Begin())throw new InvalidOperationException("RAC001 failed to begin");
        }
        static void ActivityGate(Transform parent,Material material,float radius)
        {
            // Non-colliding primitive marker; never adds a second vehicle/body.
            foreach(float x in new[]{-radius,radius}){
                var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.transform.SetParent(parent,false);
                marker.transform.localPosition=new Vector3(x,1.5f,0);marker.transform.localScale=new Vector3(.18f,3,.18f);
                var collider=marker.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                marker.GetComponent<Renderer>().sharedMaterial=material;
            }
        }
        string FullActivityDefeat()
        {
            if(CheckpointRace!=null&&(CheckpointRace.Phase==RacePhase.TimedOut||CheckpointRace.Phase==RacePhase.Invalidated))return "Wyścig zakończony: "+CheckpointRace.Phase;
            if(RoadChallenge?.Status==MapR4Status.Failed)return "Wyzwanie drogowe: "+RoadChallenge.Resolution;
            return null;
        }
        bool UpdateFullActivity()
        {
            if(CheckpointRace!=null)return CheckpointRace.Won;
            if(RoadChallenge!=null)return UpdateRoadChallenge();
            return true;
        }
        void ClearFullActivities()
        {
            CheckpointRace=null;raceRequested=false;
            if(raceHost!=null){raceHost.SetActive(false);Destroy(raceHost);}raceHost=null;
            ClearRoadActivity();
        }
    }
}
