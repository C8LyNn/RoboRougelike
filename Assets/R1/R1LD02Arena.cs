using UnityEngine;
using RoboRougelike.MapEditor;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        MapWorld ld02World;
        bool ld02Requested;
        Vector3 ld02PlayerSpawn;
        static readonly Vector3[] LD02EnemySpawns={new Vector3(36,0,12),new Vector3(36,0,20),new Vector3(36,0,28)};
        public bool CanChooseLD02Arena=>Player!=null&&CanChooseReactorDefense&&!ReactorPrepared&&
            RouteOffers.Length>0&&RouteOffers[0].SpawnCount<=LD02EnemySpawns.Length;

        public bool ChooseLD02Arena()
        {
            if(!CanChooseLD02Arena)return false;
            if(!TryMeasureEnvelope(Player.GetComponent<Rigidbody>(),out var size)||size.x>12||size.z>12)
            {Garage.Message="Pojazd jest zbyt szeroki lub długi na bezpieczny start LD02. Wybierz inną trasę.";return false;}
            var asset=Resources.Load<TextAsset>("R1/LD02_ARENA");var draft=new MapDraft();
            if(asset==null||!draft.TryLoad(asset.text,out _)||draft.TestError()!=null)
            {Garage.Message="Nie można wczytać poprawnej areny LD02.";return false;}
            ClearGeneratedArena();ClearFullActivities();
            var host=new GameObject("LD02 arena");host.transform.SetParent(transform,false);
            ld02World=host.AddComponent<MapWorld>();ld02World.Build(draft,palette);host.SetActive(false);
            ld02PlayerSpawn=MapWorld.Position(draft.Spawn)+Vector3.up*.7f;
            if(Progress.State==R1RunState.Garage&&!StartNextEncounter()){ClearLD02Arena();return false;}
            if(!Progress.ChooseRoute()){ClearLD02Arena();return false;}
            CurrentEncounter=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(RouteOffers[0]));
            CurrentEncounter.Id="LD02/"+CurrentEncounter.Id;CurrentEncounter.Title="Arena LD02";
            CurrentEncounter.Rule="Pokonaj przeciwników";Definition.Encounters[Progress.EncounterIndex]=CurrentEncounter;
            reactorRequested=false;ReactorPrepared=false;LastActivityWasReactor=false;ld02Requested=true;
            StartCoroutine(BeginEncounter());return true;
        }
        Vector3 ActivateLD02Arena()
        {
            if(ld02World==null)throw new System.InvalidOperationException("Arena LD02 nie została przygotowana.");
            generatedLegacyFloor=GameObject.Find("P12_Ground");
            generatedFloorWasActive=generatedLegacyFloor!=null&&generatedLegacyFloor.activeSelf;
            generatedLegacyLayout=Arena.Layout!=null?Arena.Layout.gameObject:null;
            generatedLayoutWasActive=generatedLegacyLayout!=null&&generatedLegacyLayout.activeSelf;
            if(generatedLegacyFloor!=null)generatedLegacyFloor.SetActive(false);
            if(generatedLegacyLayout!=null)generatedLegacyLayout.SetActive(false);
            ld02World.gameObject.SetActive(true);Physics.SyncTransforms();return ld02PlayerSpawn;
        }
        void ClearLD02Arena()
        {
            if(ld02World!=null){ld02World.gameObject.SetActive(false);Destroy(ld02World.gameObject);ld02World=null;}
            ld02Requested=false;
        }
    }
}
