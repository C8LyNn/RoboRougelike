#if MOVSERIES_VISUAL_QA
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R4
{
    // Native graphical fixture, separate QA executable only. Victory and module loss
    // use damage APIs; the first reward selection and mount are real operator UI input.
    public sealed class RRLW34NativeQA : MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public string label,state,moduleId;public int inventory,bonusItems,scrap;
            public Vector3Int rotation;public float effectiveDamage,baselineDamage,moduleHealth,liveHp,liveMaxHp;
            public bool moduleOperational;public int structuralOwners,moduleColliders;
        }
        [Serializable] public sealed class Result
        {
            public string runId,startedUtc,finishedUtc,status="Running",error;
            public string method="Native graphical fixture: seeded victory/loss via damage APIs; operator selects one bonus + three parts and rotates/mounts through UI; later repair/range use public APIs";
            public int width,height,assertions;public string graphics;
            public List<Sample> samples=new List<Sample>();public List<string> screenshots=new List<string>();
        }
        readonly Result result=new Result();string output,moduleId;int stage;float next;
        bool capturePending,fullInventory;string captureName;Vector3Int lastRotation;int initialInventory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rrl-w34-qa-output");
            if(at<0||at+1>=args.Length)return;
            var root=new GameObject("W34 native graphical fixture");DontDestroyOnLoad(root);
            root.AddComponent<RRLW34NativeQA>().Initialize(args[at+1]);
        }
        void Initialize(string directory)
        {
            output=directory;Directory.CreateDirectory(output);result.runId=Path.GetFileName(output);
            fullInventory=Environment.GetCommandLineArgs().Contains("-rrl-w34-full-inventory");
            if(fullInventory)result.method="Native graphical capacity fixture: 200 owned wheels and seeded victory; operator chooses one bonus + three parts, explicitly confirms dismantling four owned items, returns and claims through UI";
            result.startedUtc=DateTime.UtcNow.ToString("o");result.graphics=SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName;
            Application.runInBackground=true;Application.targetFrameRate=60;next=Time.unscaledTime+1;
        }
        void Save()=>File.WriteAllText(Path.Combine(output,"w34-native-result.json"),JsonUtility.ToJson(result,true));
        void Check(bool condition,string message)
        {result.assertions++;if(!condition)throw new InvalidOperationException(message);}
        void Capture(R1RunController run,string label)
        {
            var bonus=run.VehicleModel?.Parts.Concat(run.VehicleModel.Inventory).FirstOrDefault(p=>p.Id==moduleId);
            var live=run.Player?.GetComponent<R3VehicleAssembly>().Parts.FirstOrDefault(p=>p!=null&&p.Id==moduleId);
            result.samples.Add(new Sample{label=label,state=run.Progress.State.ToString(),moduleId=moduleId,
                inventory=run.VehicleModel?.Inventory.Count??0,bonusItems=run.VehicleModel?.Inventory.Count(p=>p.Kind==R3PartKind.BonusModule)??0,
                scrap=run.VehicleModel?.Scrap??0,effectiveDamage=run.ActiveStats?.Damage??1,baselineDamage=run.Progress.Stats.Damage,
                rotation=bonus?.RotationQuarterTurns??default,moduleHealth=bonus?.Health??0,
                moduleOperational=live!=null&&live.Operational&&live.transform.IsChildOf(run.Player.transform),
                liveHp=live?.Damageable?.CurrentHp??0,liveMaxHp=live?.Damageable?.MaxHp??0,
                structuralOwners=live!=null?live.GetComponentsInChildren<P12_PipelineSafePlayerBlock>().Length:0,
                moduleColliders=live!=null?live.GetComponentsInChildren<Collider>().Length:0});
            captureName=label+".png";capturePending=true;next=Time.unscaledTime+.6f;Save();
        }
        void Finish(bool passed,string error=null)
        {
            result.status=passed?"Passed":"Failed";result.error=error;result.finishedUtc=DateTime.UtcNow.ToString("o");Save();enabled=false;
            Debug.Log("[W34 NATIVE QA] "+result.status+" assertions="+result.assertions+" "+error);
        }
        void VictoryFixture(R1RunController run)
        {
            run.Encounter.SetCombatActive(false);
            foreach(var enemy in run.Encounter.Enemies)
                if(enemy!=null&&!enemy.IsDefeated)enemy.Core.ApplyDamage(100000,enemy.Core.transform.position,Vector3.up);
        }
        void Update()
        {
            var run=R1RunController.Instance;
            if(run?.Progress==null||run.Transitioning||Time.unscaledTime<next)return;
            try
            {
                if(capturePending)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,captureName));result.screenshots.Add(captureName);
                    capturePending=false;next=Time.unscaledTime+.4f;Save();return;
                }
                switch(stage)
                {
                    case 0:
                        result.width=Screen.width;result.height=Screen.height;Capture(run,"01-menu");stage++;break;
                    case 1:
                        run.Definition.Upgrades=new[]{R1UpgradeEffect.Damage,R1UpgradeEffect.Hull,R1UpgradeEffect.Mobility}
                            .Select(effect=>run.Definition.Upgrades.First(u=>u.Effect==effect)).ToArray();
                        Check(run.SelectVehicle(R3VehicleKind.Standard)&&run.StartRun()&&run.ChooseRoute(0),"Start vehicle/fight");stage++;break;
                    case 2:
                        run.Encounter.SetCombatActive(false);
                        if(fullInventory)Check(run.VehicleModel.ReceiveSalvage(Enumerable.Range(0,R3VehicleModel.InventoryCapacity).Select(i=>new R3PartState(default,R3PartKind.Wheel){Source="Capacity QA fixture"})),"Seed full owned inventory");
                        Capture(run,"02-real-combat-scene-fixture");stage++;break;
                    case 3:
                        var foe=run.Encounter.Enemies.First();
                        foe.WeaponBlock.ApplyDamage(100000,foe.WeaponBlock.transform.position,Vector3.up);
                        VictoryFixture(run);stage++;break;
                    case 4:
                        if(run.Progress.State!=R1RunState.Salvage)return;
                        Check(run.Progress.Offers.Count==3,"Three physical bonus proposals");
                        Capture(run,"03-rewards-await-operator");stage++;break;
                    case 5:
                        if(run.Progress.State!=R1RunState.Garage)return;
                        Check(run.VehicleModel.Inventory.Count==(fullInventory?R3VehicleModel.InventoryCapacity:4),"Operator chose required bonus and three parts");
                        Check(run.RecoveredModules==3,"Exactly three real salvage parts claimed");
                        Check(run.VehicleModel.Inventory.Count(p=>p.Kind==R3PartKind.BonusModule)==1,"Exactly one physical bonus");
                        var reward=run.VehicleModel.Inventory.Single(p=>p.Kind==R3PartKind.BonusModule);
                        Check(reward.UpgradeEffect==R1UpgradeEffect.Damage,"Operator selected damage proposal");moduleId=reward.Id;
                        Check(Mathf.Approximately(run.ActiveStats.Damage,1),"Inventory bonus remains inactive");initialInventory=run.VehicleModel.Inventory.Count;
                        Capture(run,"04-garage-inactive-inventory");stage=fullInventory?19:6;break;
                    case 6:
                        if(run.VehicleModel.Inventory.Any(p=>p.Id==moduleId))
                        {
                            if(lastRotation!=run.Garage.SelectedRotation)
                            {lastRotation=run.Garage.SelectedRotation;Capture(run,"05-rotated-ghost-"+result.samples.Count);}
                            return;
                        }
                        var mounted=run.VehicleModel.Parts.Single(p=>p.Id==moduleId);
                        Check(mounted.RotationQuarterTurns!=Vector3Int.zero,"Operator used XYZ rotation controls");
                        Check(run.VehicleModel.Inventory.Count==initialInventory-1,"Only one inventory item consumed");
                        Check(run.ActiveStats.Damage>1,"Mounted module activates real weapon modifier");
                        Check(Mathf.Approximately(run.Progress.Stats.Damage,1),"Baseline unchanged");
                        Capture(run,"06-mounted-active");stage++;break;
                    case 7:
                        Check(run.StartNextEncounter()&&run.ChooseRoute(0),"Next fight and map");stage++;break;
                    case 8:
                        run.Encounter.SetCombatActive(false);Check(run.ActiveStats.Damage>1,"Bonus persists on next map");
                        Capture(run,"07-next-map-active");stage++;break;
                    case 9:
                        var part=run.Player.GetComponent<R3VehicleAssembly>().Parts.Single(p=>p.Id==moduleId);
                        part.Damageable.ApplyDamage(100000,part.transform.position,Vector3.right);
                        stage++;next=Time.unscaledTime+.3f;break;
                    case 10:
                        Check(Mathf.Approximately(run.ActiveStats.Damage,1),"Physical module loss disables bonus");
                        Capture(run,"08-module-loss-effect-off");stage++;break;
                    case 11:VictoryFixture(run);stage++;break;
                    case 12:
                        if(run.Progress.State!=R1RunState.Salvage)return;
                        Check(run.ConfirmRewards(Array.Empty<string>(),new[]{0}),"Second fixture settlement");stage++;break;
                    case 13:
                        var state=run.VehicleModel.Parts.Single(p=>p.Id==moduleId);Check(state.Health==0,"Lost module captured as one wreck plan");
                        int cost=run.VehicleModel.RepairCost(state),funds=run.VehicleModel.Scrap;
                        Check(funds>=cost,"Real encounter rewards fund repair");
                        Check(run.GarageRepair(state.Cell),"Paid module repair");Check(run.VehicleModel.Scrap==funds-cost,"Repair charged exactly once");
                        var cell=state.Cell;var rotation=state.RotationQuarterTurns;
                        Check(run.GarageRemove(cell)&&run.GarageMount(moduleId,cell),"Remove and remount same physical item");
                        Check(run.VehicleModel.Parts.Single(p=>p.Id==moduleId).RotationQuarterTurns==rotation,"Remount preserves rotation");
                        Check(run.ActiveStats.Damage>1,"Effect returns after repair/remount");
                        Capture(run,"09-paid-repair-remount");stage++;break;
                    case 14:Check(run.EnterTestRange(),"Range entry");stage++;break;
                    case 15:Check(run.ActiveStats.Damage>1,"Range active modifier");Capture(run,"10-range-active");stage++;break;
                    case 16:Check(run.ResetTestRange(),"Range reset");stage++;break;
                    case 17:Check(run.ActiveStats.Damage>1,"Range reset no duplicate/lost modifier");Check(run.ReturnFromTestRange(),"Return workshop");stage++;break;
                    case 18:
                        Check(run.VehicleModel.Parts.Single(p=>p.Id==moduleId).Health==1,"Workshop identity and HP restored");
                        Check(run.ActiveStats.Damage>1&&Mathf.Approximately(run.Progress.Stats.Damage,1),"Workshop no permanent stacking");
                        Capture(run,"11-workshop-restored");stage++;break;
                    case 19:Finish(true);break;
                }
            }
            catch(Exception ex){Finish(false,ex.ToString());}
        }
    }
}
#endif
