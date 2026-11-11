using System.Collections;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R6;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        public bool SandboxWorkshop {get;private set;}
        public bool CreativeWorkshop {get;private set;}
        public bool RangeDisabled {get;private set;}
        R3VehicleModel workshopModel;
        R1RunStats testStats;
        Vector3 workshopPosition;
        Quaternion workshopRotation;
        R6TestRange testRange;
        GameObject creativeLegacyFloor;
        bool creativeLegacyFloorWasActive;

        public bool OpenCreativeGarage()
        {
            if(Error!=null||Transitioning||Progress==null||!Progress.OpenWorkshop())return false;
            SandboxWorkshop=true;CreativeWorkshop=true;
            var design=new R3VehicleDesign{Name="Kreatywny",Description="Własna konstrukcja",Speed=7.5f,TurnSpeed=82,CoreHp=100};
            design.Parts.Add(new R3PartState(Vector3Int.zero,R3PartKind.Core));
            VehicleModel=new R3VehicleModel(design,Definition.GaragePrices){CreativeMode=true};
            Player=R3VehicleAssembly.Build(VehicleModel,Progress.Stats,palette,new Vector3(0,.7f,-25),Quaternion.identity);
            followCamera.Configure(Player.transform,Player.CameraPivot);SetSimulation(false);Garage.Open();return true;
        }
        public bool AddCreativeBonus(string id)
        {
            if(!CreativeWorkshop||Transitioning||Progress?.State!=R1RunState.Garage||VehicleModel==null||string.IsNullOrEmpty(id))return false;
            var bonus=System.Array.Find(Definition.Upgrades,item=>item!=null&&item.Id==id);
            return bonus!=null&&VehicleModel.ReceiveBonuses(new[]{bonus});
        }

        public bool OpenSandboxGarage()
        {
            if(Error!=null||Transitioning||Progress==null||!Progress.OpenWorkshop())return false;
            SandboxWorkshop=true;VehicleModel=new R3VehicleModel(R3VehicleCatalog.Create(SelectedVehicle,SelectedVehicleVariant),Definition.GaragePrices);
            VehicleModel.Award(R6Tuning.Current.SandboxScrap);
            Player=R3VehicleAssembly.Build(VehicleModel,Progress.Stats,palette,new Vector3(0,.7f,-25),Quaternion.identity);
            followCamera.Configure(Player.transform,Player.CameraPivot);SetSimulation(false);Garage.Open();return true;
        }
        public bool EnterTestRange()
        {
            if(Transitioning||Progress?.State!=R1RunState.Garage||VehicleModel==null||!VehicleModel.CanDeploy)return false;
            Player.GetComponent<R3VehicleAssembly>().Capture();
            if(!VehicleModel.CanDeploy)return false;
            workshopModel=VehicleModel;testStats=Progress.Stats.Copy();workshopPosition=Player.transform.position;workshopRotation=Player.transform.rotation;
            Garage.Close();Progress.BeginTest();StartCoroutine(BuildTestRange());return true;
        }
        public bool ResetTestRange()
        {
            if(Transitioning||Progress?.State!=R1RunState.TestDrive||workshopModel==null)return false;
            StartCoroutine(BuildTestRange());return true;
        }
        IEnumerator BuildTestRange()
        {
            Transitioning=true;Paused=false;RangeDisabled=false;SetSimulation(false);Encounter.Clear();
            if(Player!=null){Player.gameObject.SetActive(false);Destroy(Player.gameObject);}
            if(testRange!=null)testRange.Clear();
            R1EncounterManager.ClearTransientObjects();
            if(Arena.Layout!=null)Arena.Layout.gameObject.SetActive(false);
            VehicleModel=workshopModel.DeepCopy();
            // The range is a disposable copy. Initial charging here must not
            // consume the real vehicle's first-deployment charge before the
            // player finishes building and enters the first encounter.
            VehicleModel.PrepareInitialEnergy();
            if(testRange==null)testRange=gameObject.AddComponent<R6TestRange>();
            if(CreativeWorkshop&&creativeLegacyFloor==null)
            {
                creativeLegacyFloor=GameObject.Find("P12_Ground");
                if(creativeLegacyFloor!=null){creativeLegacyFloorWasActive=creativeLegacyFloor.activeSelf;creativeLegacyFloor.SetActive(false);}
            }
            testRange.Build(palette,CreativeWorkshop);
            Player=R3VehicleAssembly.Build(VehicleModel,testStats,palette,new Vector3(0,.7f,-26),Quaternion.identity);
            if(!CreativeWorkshop&&enemyTemplate!=null)
            {
                var target=Instantiate(enemyTemplate,new Vector3(-5,0,-20),Quaternion.identity,testRange.Layout);
                target.name="Cel elektroniczny • trening rakietowy";
                target.ConfigureArchetype(R3EnemyArchetype.Gunner);
                target.ConfigureVariant(RoboRougelike.R5.R5EnemyVariant.Normal);
                target.ConfigureSalvage(null,"practice-target");target.gameObject.SetActive(true);
                target.ConfigureEncounter(Player.transform,1,.35f,1.8f,1);
                target.ConfigureMovement(0,12,0);target.SetCombatActive(false);target.EquipRocket();
                RoboRougelike.WeaponIntegration.RRLEmpVehicleAdapter.Configure(target.transform);
                testRange.SetTrainingTarget(target);
            }
            followCamera.Configure(Player.transform,Player.CameraPivot);
            yield return null;yield return null;
            Transitioning=false;SetSimulation(true);
        }
        public bool SpawnCreativeEnemy(R3EnemyArchetype archetype)
        {
            if(!CreativeWorkshop||Transitioning||Progress?.State!=R1RunState.TestDrive||testRange==null||Player==null||enemyTemplate==null||
                !System.Enum.IsDefined(typeof(R3EnemyArchetype),archetype))return false;
            return testRange.SpawnEnemy(enemyTemplate,Player.transform,archetype,!Paused);
        }
        public void ClearCreativeEnemies()
        {if(CreativeWorkshop&&testRange!=null)testRange.ClearEnemies();}
        public bool ReturnFromTestRange()
        {
            if(Transitioning||Progress?.State!=R1RunState.TestDrive||workshopModel==null)return false;
            StartCoroutine(RestoreWorkshop());return true;
        }
        IEnumerator RestoreWorkshop()
        {
            Transitioning=true;Paused=false;RangeDisabled=false;SetSimulation(false);
            if(Player!=null){Player.gameObject.SetActive(false);Destroy(Player.gameObject);}
            testRange.Clear();R1EncounterManager.ClearTransientObjects();
            if(creativeLegacyFloor!=null){creativeLegacyFloor.SetActive(creativeLegacyFloorWasActive);creativeLegacyFloor=null;}
            VehicleModel=workshopModel;workshopModel=null;
            if(Arena.Layout!=null)Arena.Layout.gameObject.SetActive(true);
            Player=R3VehicleAssembly.Build(VehicleModel,Progress.Stats,palette,workshopPosition,workshopRotation);
            followCamera.Configure(Player.transform,Player.CameraPivot);Progress.EndTest();
            yield return null;yield return null;
            Garage.Open();Transitioning=false;
        }
        void UpdateRange()
        {
            if(Paused)return;
            bool defeated=Player==null||(CreativeWorkshop?Player.Structure==null||!Player.Structure.CoreAlive:Player.DefeatReason!=null);
            if(defeated){RangeDisabled=true;Paused=true;SetSimulation(false);}
        }
    }
}
