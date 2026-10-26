using System;
using System.Linq;
using RoboRougelike.MapSystemR4;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        bool roadRequested,roadFloorActive,roadLayoutActive;
        GameObject roadFloor,roadLayout;
        MapR4MapData roadMap;
        MapR4Mode roadMode;
        Quaternion previousRoadRotation;
        float nextRoadWave;
        int roadWave;
        public MapR4BuildResult RoadWorld {get;private set;}
        public MapR4ChallengeRuntime RoadChallenge {get;private set;}
        public MapR4StateController RoadState {get;private set;}
        public MapR4VehicleProfile RoadProfile {get;private set;}
        public bool CanChooseRoadChallenge=>CanChooseCheckpointRace;
        public static string RoadModeName(MapR4Mode mode)=>mode==MapR4Mode.TimeAttack?"Próba czasowa":mode==MapR4Mode.Survival?"Przetrwanie":mode==MapR4Mode.Escape?"Ewakuacja":"Arena bossa";
        public bool ChooseRoadChallenge(MapR4Mode mode)
        {
            if(!CanChooseRoadChallenge||!Enum.IsDefined(typeof(MapR4Mode),mode))return false;
            Physics.SyncTransforms();
            if(!TryMeasureEnvelope(Player.GetComponent<Rigidbody>(),out var envelope))return false;
            var source=Resources.Load<TextAsset>("MAP_R4_quarry_03");if(source==null)return false;
            var map=MapR4MapData.Parse(source.text);
            // Fit original source geometry to the measured assembled robot, not a demo cube.
            var profile=new MapR4VehicleProfile{id="R3/"+SelectedVehicle,width=envelope.x,height=envelope.y,length=envelope.z,
                minTurnRadius=4.5f,maxSlopeDeg=16,capabilities=new[]{"ground"}};
            foreach(var edge in map.edges){edge.width=Mathf.Max(edge.width,envelope.x+2*profile.sideMargin);edge.clearHeight=Mathf.Max(edge.clearHeight,envelope.y+profile.verticalMargin);}
            foreach(var node in map.nodes)node.padWidth=Mathf.Max(node.padWidth,envelope.x+envelope.z+2*profile.minTurnRadius+2*profile.sideMargin);
            foreach(var node in map.nodes)if(node.id=="central_arena")node.padWidth=Mathf.Max(40,node.padWidth);
            string state=mode==MapR4Mode.Escape?"bridge_collapsed":"intact";
            string target=mode==MapR4Mode.TimeAttack?"upper_ramp":mode==MapR4Mode.Escape?"exit":"central_arena";
            if(!MapR6RouteValidator.CanReachWithTurns(map,state,"start",target,profile,out string reason))
            {Garage.Message="Droga odrzucona: "+reason;return false;}
            if(Progress.State==R1RunState.Garage&&!StartNextEncounter())return false;
            if(!Progress.ChooseRoute())return false;
            var template=mode==MapR4Mode.Boss?Definition.RouteStages.Last().Options[0]:RouteOffers[0];
            CurrentEncounter=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(template));
            CurrentEncounter.Id="MAP-R6/"+mode+"/"+CurrentEncounter.Id;CurrentEncounter.Title=RoadModeName(mode);
            CurrentEncounter.Rule=mode==MapR4Mode.TimeAttack?"Górny checkpoint → meta • 180 s":mode==MapR4Mode.Escape?"Dotrzyj do wschodniego wyjścia • most zamknięty":mode==MapR4Mode.Survival?"Przetrwaj 30 sekund kolejnych fal":"Pokonaj Oblężnika";
            Definition.Encounters[Progress.EncounterIndex]=CurrentEncounter;
            reactorRequested=false;ReactorPrepared=false;LastActivityWasReactor=false;raceRequested=false;mapgenRequested=false;
            roadRequested=true;roadMap=map;roadMode=mode;RoadProfile=profile;
            StartCoroutine(BeginEncounter());return true;
        }
        Vector3 ActivateRoadWorld()
        {
            previousRoadRotation=playerRotation;
            roadFloor=GameObject.Find("P12_Ground");roadFloorActive=roadFloor!=null&&roadFloor.activeSelf;
            roadLayout=Arena.Layout!=null?Arena.Layout.gameObject:null;roadLayoutActive=roadLayout!=null&&roadLayout.activeSelf;
            if(roadFloor!=null)roadFloor.SetActive(false);if(roadLayout!=null)roadLayout.SetActive(false);
            var material=new Material(palette.Parts[(int)R3PartKind.Armor]){name="MAP-R6 canonical URP road"};material.color=new Color(.32f,.4f,.45f);
            RoadWorld=MapR4GeometryBuilder.Build(roadMap,transform,material,material);RoadWorld.OwnedMaterials.Add(material);
            RoadState=new MapR4StateController(roadMap,RoadWorld);RoadState.TrySetState(roadMode==MapR4Mode.Escape?"bridge_collapsed":"intact");
            playerRotation=Quaternion.Euler(0,90,0);Physics.SyncTransforms();
            return RoadWorld.Anchors[roadMode==MapR4Mode.Survival||roadMode==MapR4Mode.Boss?"arena_middle":"spawn_west"].position+Vector3.up*1.05f;
        }
        void BeginRoadChallenge()
        {
            RoadChallenge=new MapR4ChallengeRuntime(roadMode,180,30);RoadChallenge.Begin();roadWave=0;nextRoadWave=0;
            foreach(var item in RoadWorld.Anchors){
                if(item.Key!="checkpoint_upper"&&item.Key!="finish_east"&&item.Key!="exit_east")continue;
                var trigger=item.Value.gameObject.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=2.8f;
                item.Value.gameObject.AddComponent<R1RoadAnchor>().Bind(this,item.Key);
                ActivityGate(item.Value,palette.Parts[(int)R3PartKind.Core],2.8f);
            }
            if(roadMode==MapR4Mode.Survival||roadMode==MapR4Mode.Boss)SpawnRoadOpponents();
            if(Player.GetComponent<RoboRougelike.R6.MapR6ProductionEvidenceBridge>()==null)Player.gameObject.AddComponent<RoboRougelike.R6.MapR6ProductionEvidenceBridge>();
        }
        void SpawnRoadOpponents()
        {
            roadWave++;var center=RoadWorld.Anchors["arena_middle"].position;
            var spawns=Enumerable.Range(0,CurrentEncounter.SpawnCount).Select(i=>{
                float angle=(i+.5f)*Mathf.PI*2/CurrentEncounter.SpawnCount;
                return center+new Vector3(Mathf.Cos(angle)*12,.7f,Mathf.Sin(angle)*12);
            }).ToArray();
            Encounter.Spawn(enemyTemplate,Player.transform,CurrentEncounter,Salvage,runId+"/roads/"+Progress.EncounterIndex+"/wave/"+roadWave,spawns);
        }
        bool UpdateRoadChallenge()
        {
            RoadChallenge.Tick(Time.deltaTime);
            if(roadMode==MapR4Mode.Boss&&Encounter.Cleared)RoadChallenge.NotifyBossDefeated();
            if(RoadChallenge.Status==MapR4Status.Completed)return true;
            if(roadMode==MapR4Mode.Survival&&Encounter.Cleared){
                if(nextRoadWave==0)nextRoadWave=RoadChallenge.ElapsedSeconds+3;
                if(RoadChallenge.ElapsedSeconds>=nextRoadWave){DefeatedEnemies+=Encounter.Enemies.Count(e=>e!=null&&e.IsDefeated);Encounter.Clear();SpawnRoadOpponents();nextRoadWave=0;}
            }
            return false;
        }
        internal void NotifyRoadAnchor(Collider collider,string id)
        {
            if(Transitioning||Paused||Progress?.State!=R1RunState.Combat||Player==null||collider==null)return;
            if(collider.attachedRigidbody==Player.GetComponent<Rigidbody>()&&collider.transform.IsChildOf(Player.transform))RoadChallenge?.NotifyAnchor(id);
        }
        void ClearRoadActivity()
        {
            RoadChallenge=null;RoadState=null;roadRequested=false;nextRoadWave=0;
            if(RoadWorld!=null){if(RoadWorld.Root!=null){RoadWorld.Root.SetActive(false);Destroy(RoadWorld.Root);}MapR4GeometryBuilder.DisposeMeshes(RoadWorld);RoadWorld=null;playerRotation=previousRoadRotation;}
            if(roadFloor!=null)roadFloor.SetActive(roadFloorActive);if(roadLayout!=null)roadLayout.SetActive(roadLayoutActive);
            roadFloor=roadLayout=null;roadMap=null;RoadProfile=null;
        }
    }
}
