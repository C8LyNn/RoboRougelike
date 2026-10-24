using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R5;
using RoboRougelike.OBJ001Integration;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        OBJ001ReactorActor reactorActor;
        bool reactorRequested;
        float nextDefenseWave=-1,nextDefenseTarget,lastReactorHp;
        public bool ReactorPrepared {get;private set;}
        public bool ReactorActive=>reactorActor!=null&&reactorActor.gameObject.activeInHierarchy;
        public bool LastActivityWasReactor {get;private set;}
        public float ReactorHp=>ReactorActive?reactorActor.CurrentIntegrity:lastReactorHp;
        public float ReactorMaxHp=>ReactorActive?reactorActor.MaximumIntegrity:600;
        public int DefenseWave {get;private set;}
        public int DefenseWaveCount=>3;
        public float DefenseIntermission=>nextDefenseWave<0?0:Mathf.Max(0,nextDefenseWave-Time.time);
        public OBJ001ReactorActor ReactorActor=>reactorActor;
        public bool CanChooseReactorDefense=>!Transitioning&&!SandboxWorkshop&&VehicleModel!=null&&VehicleModel.CanDeploy&&
            Progress!=null&&Progress.EncounterIndex<Progress.TotalEncounters-1&&
            (Progress.State==R1RunState.Garage||Progress.State==R1RunState.Route);

        public bool PrepareReactorRun()
        {
            if(Progress?.State!=R1RunState.Ready||!StartRun()||!Progress.PrepareGarage())return false;
            ReactorPrepared=true;
            Player=R3VehicleAssembly.Build(VehicleModel,Progress.Stats,palette,new Vector3(0,.7f,-22),Quaternion.identity);
            followCamera.Configure(Player.transform,Player.CameraPivot);SetSimulation(false);Garage.Open();return true;
        }
        public bool ChooseReactorDefense()
        {
            if(!CanChooseReactorDefense)return false;
            if(Progress.State==R1RunState.Garage&&!StartNextEncounter())return false;
            if(!Progress.ChooseRoute())return false;
            CurrentEncounter=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(RouteOffers[0]));
            CurrentEncounter.Id="OBJ001/"+CurrentEncounter.Id;CurrentEncounter.Title="Obrona reaktora";
            CurrentEncounter.Rule="Chroń reaktor przez trzy fale";CurrentEncounter.Arena=R5ArenaKind.SalvageYard;
            CurrentEncounter.Variants=null;CurrentEncounter.AlternativeRoster=null;
            CurrentEncounter.Roster=new[]{R3EnemyArchetype.Scout,R3EnemyArchetype.Gunner};CurrentEncounter.EnemyCount=1;
            Definition.Encounters[Progress.EncounterIndex]=CurrentEncounter;
            ReactorPrepared=false;reactorRequested=true;LastActivityWasReactor=false;
            StartCoroutine(BeginEncounter());return true;
        }
        void BeginReactorActivity()
        {
            reactorActor=OBJ001ReactorActor.Build(transform,palette.Parts[0],new Vector3(-8,1.2f,-17),600);
            reactorActor.Objective.Begin();DefenseWave=1;nextDefenseWave=-1;lastReactorHp=600;
            SpawnDefenseWave();
        }
        void SpawnDefenseWave()
        {
            var wave=JsonUtility.FromJson<R1EncounterDefinition>(JsonUtility.ToJson(CurrentEncounter));
            // The existing spawner adds one ordinary escort: authored1/1/2 becomes actual2/2/3.
            wave.EnemyCount=DefenseWave==3?2:1;
            Encounter.Spawn(enemyTemplate,Player.transform,wave,Salvage,runId+"/encounter/"+Progress.EncounterIndex+"/wave/"+DefenseWave);
            nextDefenseTarget=0;UpdateDefenseTargets();
        }
        void UpdateDefenseTargets()
        {
            if(!ReactorActive||Time.time<nextDefenseTarget)return;nextDefenseTarget=Time.time+.5f;
            for(int i=0;i<Encounter.Enemies.Count;i++)
            {
                var enemy=Encounter.Enemies[i];if(enemy==null||enemy.IsDefeated)continue;
                float distance=Vector3.ProjectOnPlane(Player.transform.position-enemy.transform.position,Vector3.up).magnitude;
                bool engagePlayer=distance<8||(i%2==1&&distance<32);
                enemy.RetargetEncounter(engagePlayer?Player.transform:reactorActor.transform);
            }
        }
        string ReactorDefeat()
        {
            if(!ReactorActive)return null;reactorActor.Objective.SynchronizeIntegrity();
            return reactorActor.CurrentIntegrity<=0?"Reaktor został zniszczony. Obrona zakończona porażką.":null;
        }
        // Called only from the existing active/unpaused combat update, before ordinary settlement.
        bool UpdateReactorDefense()
        {
            UpdateDefenseTargets();
            if(nextDefenseWave>=0)
            {
                if(Time.time<nextDefenseWave)return false;
                DefenseWave++;nextDefenseWave=-1;SpawnDefenseWave();return false;
            }
            if(!Encounter.Cleared)return false;
            if(DefenseWave<DefenseWaveCount)
            {
                DefeatedEnemies+=Encounter.Enemies.Count;Encounter.Clear();nextDefenseWave=Time.time+3;return false;
            }
            return reactorActor.Objective.CompleteDefense();
        }
        void ClearReactorActivity()
        {
            if(reactorActor!=null)
            {
                lastReactorHp=reactorActor.CurrentIntegrity;LastActivityWasReactor=true;
                reactorActor.gameObject.SetActive(false);Destroy(reactorActor.gameObject);reactorActor=null;
            }
            reactorRequested=false;nextDefenseWave=-1;
        }
    }
}
