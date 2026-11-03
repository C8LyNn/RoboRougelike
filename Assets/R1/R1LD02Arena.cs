using UnityEngine;
using RoboRougelike.MapEditor;
using B01.MapJumpPad001;
using System.Linq;

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
            ld02World.gameObject.SetActive(true);Physics.SyncTransforms();
            CreateJumpPadOnActiveLD02Ground();
            return ld02PlayerSpawn;
        }

        // Original B01-21 physical pad, mounted in the actual active LD02 combat arena.
        // Optional area: no mutation when a genuine wall/crate occupies the footprint.
        void CreateJumpPadOnActiveLD02Ground()
        {
            if(ld02World==null||ld02World.GetComponentInChildren<MapJumpPad>()!=null)return;
            var desired=ld02PlayerSpawn+Vector3.right*(MapWorld.Grid*2);
            var hits=Physics.RaycastAll(desired+Vector3.up*5,Vector3.down,12f,
                ~0,QueryTriggerInteraction.Ignore);
            var ground=hits.Where(hit=>hit.collider!=null&&
                hit.collider.transform.IsChildOf(ld02World.transform)&&
                hit.collider.GetComponentInParent<MapItemReference>()?.Item.kind==MapItemKind.Ground)
                .OrderBy(hit=>hit.distance).FirstOrDefault();
            if(ground.collider==null||ground.normal.y<.85f)
            {
                Debug.LogWarning("[W29 JumpPad] No certified LD02 ground. Optional module skipped.");
                return;
            }
            var position=ground.point+Vector3.up*.04f;
            foreach(var obstacle in Physics.OverlapBox(position+Vector3.up*.44f,
                new Vector3(.49f,.24f,.49f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                if(obstacle!=null&&obstacle.enabled&&obstacle.gameObject.activeInHierarchy&&
                   obstacle.GetComponentInParent<MapItemReference>()?.Item.kind!=MapItemKind.Ground)
                {
                    Debug.LogWarning("[W29 JumpPad] Real obstacle in footprint. Pad skipped.");
                    return;
                }
            var root=new GameObject("LD02 MAP-JUMPPAD-001 Physical");
            root.transform.SetParent(ld02World.transform,false);
            root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,90,0));
            var trigger=root.AddComponent<BoxCollider>();
            trigger.isTrigger=true;trigger.center=new Vector3(0,.28f,0);
            trigger.size=new Vector3(1f,.55f,1f);
            root.AddComponent<MapJumpPad>().Configure(8.5f,2f,1.25f);
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name="LD02 JumpPad visual (no physics authority)";
            visual.transform.SetParent(root.transform,false);
            visual.transform.localPosition=new Vector3(0,.08f,0);
            visual.transform.localScale=new Vector3(1f,.16f,1f);
            var extra=visual.GetComponent<Collider>();
            extra.enabled=false;Destroy(extra);
            visual.GetComponent<Renderer>().sharedMaterial=palette.Selection;
            Physics.SyncTransforms();
        }
        void ClearLD02Arena()
        {
            if(ld02World!=null){ld02World.gameObject.SetActive(false);Destroy(ld02World.gameObject);ld02World=null;}
            ld02Requested=false;
        }
    }
}
