using System;
using System.Linq;
using B01.MAPGEN001;
using B01.MAPGENWORLD001;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R1
{
    // Experimental opt-in host wiring. MAPGEN owns geometry, R3/R6/Pack own all vehicles and combat.
    public sealed partial class R1RunController
    {
        MapGenWorldAdapter generatedArena;
        GameObject generatedLegacyFloor,generatedLegacyLayout;
        bool generatedFloorWasActive,generatedLayoutWasActive,mapgenRequested;
        public MapWorldSnapshot GeneratedWorld => generatedArena!=null?generatedArena.Current:null;
        public Vector3 MapgenMeasuredEnvelope {get;private set;}
        public string MapgenMessage {get;private set;}
        public bool CanChooseGeneratedArena => Player!=null&&CanChooseReactorDefense&&!ReactorPrepared&&
            VehicleModel.SelectedDrive==R3DriveKind.Wheels&&RouteOffers.Length>0&&RouteOffers[0].SpawnCount<=3;

        public bool PrepareMapgenRun()=>PrepareUnifiedRun();
        public bool PrepareUnifiedRun()
        {
            if(Progress?.State!=R1RunState.Ready||!StartRun()||!Progress.PrepareGarage())return false;
            Player=R3VehicleAssembly.Build(VehicleModel,Progress.Stats,palette,new Vector3(0,.7f,-22),Quaternion.identity);
            followCamera.Configure(Player.transform,Player.CameraPivot);SetSimulation(false);Garage.Open();
            Garage.Message="Przygotuj pojazd. Wybierz trasę, wygenerowaną arenę albo obronę reaktora. Seed pochodzi z menu.";
            return true;
        }
        public bool ChooseGeneratedArena()
        {
            if(!CanChooseGeneratedArena)return RejectGeneratedArena("Ten etap lub napęd nie obsługuje eksperymentalnej areny. Wybierz zwykłą trasę.");
            Physics.SyncTransforms();
            if(!TryMeasureEnvelope(Player.GetComponent<Rigidbody>(),out var envelope))
                return RejectGeneratedArena("Nie można zmierzyć fizycznych colliderów pojazdu.");
            MapgenMeasuredEnvelope=envelope;
            var profile=SelectedVehicle==R3VehicleKind.Light?MapVehicleProfile.Light:MapVehicleProfile.Heavy;
            profile.Width=Mathf.Max(profile.Width,envelope.x);profile.Length=Mathf.Max(profile.Length,envelope.z);
            if(generatedArena==null)
            {
                var host=new GameObject("B01 RC1 MAPGEN world");host.transform.SetParent(transform,false);
                generatedArena=host.AddComponent<MapGenWorldAdapter>();
            }
            var request=new MapWorldRequest {Recipe=MapRecipe.CombatArena,Seed=RequestedSeed,
                Config=MapGenConfig.Default,Vehicle=profile,EnvelopeMeasured=true,ClearancePerSideMeters=.25f};
            if(!generatedArena.TryGenerate(request,out var world,out var failure))
                return RejectGeneratedArena("Mapa odrzucona: "+failure+". Zmniejsz pojazd lub wybierz zwykłą trasę.");
            if(world.EnemySpawns.Length<RouteOffers[0].SpawnCount)
            {generatedArena.ClearWorld();return RejectGeneratedArena("Mapa nie ma dostatecznej liczby bezpiecznych spawnów.");}
            if(Progress.State==R1RunState.Garage&&!StartNextEncounter())
            {generatedArena.ClearWorld();return false;}
            if(!Progress.ChooseRoute()){generatedArena.ClearWorld();return false;}
            CurrentEncounter=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(RouteOffers[0]));
            CurrentEncounter.Id="MAPGEN/"+CurrentEncounter.Id;
            CurrentEncounter.Title="Wygenerowana arena";CurrentEncounter.Rule="Pokonaj przeciwników • seed "+RequestedSeed;
            Definition.Encounters[Progress.EncounterIndex]=CurrentEncounter;
            reactorRequested=false;ReactorPrepared=false;LastActivityWasReactor=false;mapgenRequested=true;MapgenMessage=null;
            StartCoroutine(BeginEncounter());return true;
        }
        bool RejectGeneratedArena(string message)
        {MapgenMessage=message;if(Garage!=null)Garage.Message=message;return false;}

        Vector3 ActivateGeneratedArena()
        {
            var world=GeneratedWorld;
            if(world==null)throw new InvalidOperationException("Generated arena was not prepared");
            generatedLegacyFloor=GameObject.Find("P12_Ground");
            generatedFloorWasActive=generatedLegacyFloor!=null&&generatedLegacyFloor.activeSelf;
            generatedLegacyLayout=Arena.Layout!=null?Arena.Layout.gameObject:null;
            generatedLayoutWasActive=generatedLegacyLayout!=null&&generatedLegacyLayout.activeSelf;
            if(generatedLegacyFloor!=null)generatedLegacyFloor.SetActive(false);
            if(generatedLegacyLayout!=null)generatedLegacyLayout.SetActive(false);
            Physics.SyncTransforms();return world.Start;
        }
        void ClearGeneratedArena()
        {
            ClearLD02Arena();
            if(generatedArena!=null)generatedArena.ClearWorld();
            if(generatedLegacyFloor!=null)generatedLegacyFloor.SetActive(generatedFloorWasActive);
            if(generatedLegacyLayout!=null)generatedLegacyLayout.SetActive(generatedLayoutWasActive);
            generatedLegacyFloor=generatedLegacyLayout=null;mapgenRequested=false;
        }
        static bool TryMeasureEnvelope(Rigidbody body,out Vector3 size)
        {
            size=default;if(body==null)return false;
            var colliders=body.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&c.attachedRigidbody==body).ToArray();
            bool found=false;Bounds bounds=default;
            foreach(var collider in colliders)
            {
                // Measure shape-local data. A just-restored paused/interpolated
                // PhysX collider can still return stale world bounds after MoveTo;
                // that inflated a road→garage robot to94m at x=-90.
                Bounds local;
                if(collider is BoxCollider box)local=new Bounds(box.center,box.size);
                else if(collider is SphereCollider sphere)local=new Bounds(sphere.center,Vector3.one*sphere.radius*2);
                else if(collider is CapsuleCollider capsule){var diameter=Vector3.one*capsule.radius*2;diameter[capsule.direction]=Mathf.Max(capsule.height,capsule.radius*2);local=new Bounds(capsule.center,diameter);}
                else if(collider is MeshCollider mesh&&mesh.sharedMesh!=null)local=mesh.sharedMesh.bounds;
                else return false; // An unknown shape is not a measured envelope.
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                {
                    var sign=new Vector3(x,y,z);
                    Vector3 point=collider.transform.TransformPoint(local.center+Vector3.Scale(local.extents,sign));
                    point=body.transform.InverseTransformPoint(point);
                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                }
            }
            size=bounds.size;return found&&size.x>0&&size.z>0;
        }
    }
}
