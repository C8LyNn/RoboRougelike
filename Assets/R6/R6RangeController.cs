using System.Collections;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R6;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        public bool SandboxWorkshop {get;private set;}
        public bool RangeDisabled {get;private set;}
        R3VehicleModel workshopModel;
        R1RunStats testStats;
        Vector3 workshopPosition;
        Quaternion workshopRotation;
        R6TestRange testRange;

        public bool OpenSandboxGarage()
        {
            if(Error!=null||Transitioning||Progress==null||!Progress.OpenWorkshop())return false;
            SandboxWorkshop=true;VehicleModel=new R3VehicleModel(R3VehicleCatalog.Create(SelectedVehicle),Definition.GaragePrices);
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
            if(testRange==null)testRange=gameObject.AddComponent<R6TestRange>();
            testRange.Build(palette);
            Player=R3VehicleAssembly.Build(VehicleModel,testStats,palette,new Vector3(0,.7f,-26),Quaternion.identity);
            if(enemyTemplate!=null)
            {
                var target=Instantiate(enemyTemplate,new Vector3(-27,0,24),Quaternion.identity,testRange.Layout);
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
            if(Player==null||Player.DefeatReason!=null){RangeDisabled=true;Paused=true;SetSimulation(false);}
        }
    }
}
