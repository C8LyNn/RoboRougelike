#if B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections;
using System.IO;
using RoboRougelike.R3;
using RoboRougelike.R4;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.EMP;
using UnityEngine;

namespace RoboRougelike.R1
{
    // Isolated QA only; active solely for an explicit batch-mode command line.
    public sealed class W27EmpPlayerQA:MonoBehaviour
    {
        [Serializable] sealed class Result{public string status="RUNNING",error="",method="real R3 EMP device in Windows Player; direct API, not human gameplay";public bool disconnectedDenied,energyPreserved,cooldownPreserved,reattachedPulse,receiverDisrupted;}
        readonly Result result=new Result();string directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-w27-emp-player-qa");
            if(i<0||i+1>=args.Length||!Application.isBatchMode)return;
            var go=new GameObject("W27 isolated EMP Player QA");DontDestroyOnLoad(go);go.AddComponent<W27EmpPlayerQA>().directory=args[i+1];
        }
        void Start(){Directory.CreateDirectory(directory);Application.runInBackground=true;StartCoroutine(Run());}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        IEnumerator Run()
        {
            var action=Exercise();bool ok=true;
            while(true){bool next=false;object value=null;try{next=action.MoveNext();if(next)value=action.Current;}catch(Exception e){ok=false;result.error=e.ToString();}if(!next||!ok)break;yield return value;}
            result.status=ok?"PASS":"FAIL";File.WriteAllText(Path.Combine(directory,"w27-emp.json"),JsonUtility.ToJson(result,true));Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            float deadline=Time.realtimeSinceStartup+45;
            while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(R1RunController.Instance?.Progress!=null,"game bootstrap");
            var colors=new GameObject("W27 EMP palette");var palette=colors.AddComponent<R3VisualPalette>();
            var shader=Shader.Find("Universal Render Pipeline/Lit");Require(shader!=null,"URP shader");var material=new Material(shader);
            for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
            for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
            var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));R3PartState state=null;
            foreach(var p in model.Parts)if(p.Kind==R3PartKind.Gun){state=p;break;}
            Require(state!=null,"gun state");state.Weapon=R4WeaponKind.EmpEmitter;
            var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(0,20,0),Quaternion.identity);
            rig.GetComponent<Rigidbody>().isKinematic=true;yield return null;
            R3MountedPart gun=null;foreach(var p in rig.GetComponent<R3VehicleAssembly>().Parts)if(p.Id==state.Id){gun=p;break;}
            Require(gun!=null,"mounted gun");var binding=gun.GetComponent<RRLEmpBinding>();Require(binding!=null&&binding.Device.Available,"available EMP binding");
            var emitter=binding.Module;var bank=gun.gameObject.AddComponent<EmpEnergyReservoir>();bank.Configure(100,40,0);
            emitter.Configure(9,3,12,7);emitter.SetEnergySource(bank);
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="W27 EMP target";target.transform.position=gun.transform.position+Vector3.up*3;
            var receiver=target.AddComponent<EmpReceiver>();receiver.ConfigureOwnership(target.transform,target.GetInstanceID(),2);receiver.ConfigureSusceptibility(0);Physics.SyncTransforms();
            gun.transform.SetParent(null,true);Physics.SyncTransforms();
            result.disconnectedDenied=!emitter.TryPulse(out _);result.energyPreserved=Mathf.Approximately(bank.Current,40);result.cooldownPreserved=Mathf.Approximately(emitter.CooldownRemaining,0);
            Require(result.disconnectedDenied&&result.energyPreserved&&result.cooldownPreserved,"detached emitter spent resources or pulsed");
            gun.transform.SetParent(rig.transform,true);Physics.SyncTransforms();
            result.reattachedPulse=emitter.TryPulse(out var report);result.receiverDisrupted=receiver.IsDisrupted&&report.ReceiversAffected>0;
            Require(result.reattachedPulse&&result.receiverDisrupted&&Mathf.Approximately(bank.Current,33),"reattached EMP pulse did not affect a real receiver");
            Destroy(rig.gameObject);Destroy(target);Destroy(colors);Destroy(material);
        }
    }
}
#endif
