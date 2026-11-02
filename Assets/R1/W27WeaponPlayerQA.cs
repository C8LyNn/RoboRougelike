#if B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;
namespace RoboRougelike.R1
{
    // Explicit batch-only Player test. It never opens or writes the save slot.
    public sealed class W27WeaponPlayerQA:MonoBehaviour
    {
        [Serializable] sealed class Result { public string status="RUNNING",error="";public int blocked,unblocked;public string method="actual R3 assembled guns and PhysX; API-driven, not human gameplay"; }
        readonly Result report=new Result();string directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-w27-weapon-player-qa");
            if(i<0||i+1>=args.Length||!Application.isBatchMode)return;
            var go=new GameObject("W27 hidden Player QA");DontDestroyOnLoad(go);go.AddComponent<W27WeaponPlayerQA>().directory=args[i+1];
        }
        void Start(){Directory.CreateDirectory(directory);Application.runInBackground=true;StartCoroutine(Run());}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        IEnumerator Run()
        {
            var action=Exercise();bool ok=true;
            while(true){bool next=false;object value=null;try{next=action.MoveNext();if(next)value=action.Current;}catch(Exception e){ok=false;report.error=e.ToString();}if(!next||!ok)break;yield return value;}
            report.status=ok?"PASS":"FAIL";File.WriteAllText(Path.Combine(directory,"w27-weapons.json"),JsonUtility.ToJson(report,true));Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            var paletteObject=new GameObject("W27 palette");var palette=paletteObject.AddComponent<R3VisualPalette>();
            var shader=Shader.Find("Universal Render Pipeline/Lit");Require(shader!=null,"URP shader");var material=new Material(shader);
            for(int i=0;i<palette.Parts.Length;i++)palette.Parts[i]=material;
            for(int i=0;i<palette.Weapons.Length;i++)palette.Weapons[i]=material;
            var kinds=new[]{R4WeaponKind.Basic,R4WeaponKind.Shotgun,R4WeaponKind.Railgun,R4WeaponKind.RocketLauncher};
            for(int i=0;i<kinds.Length;i++)
            {
                var model=new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Light));var state=model.Parts.First(p=>p.Kind==R3PartKind.Gun);state.Weapon=kinds[i];
                var rig=R3VehicleAssembly.Build(model,new R1RunStats(),palette,new Vector3(i*40,20,0),Quaternion.identity);yield return null;
                var assembly=rig.GetComponent<R3VehicleAssembly>();var gun=assembly.Parts.Single(p=>p.Id==state.Id);
                var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="W27 owned armor";blocker.transform.SetParent(rig.transform,true);
                blocker.transform.position=gun.Muzzle.position+gun.Muzzle.forward*.6f;blocker.transform.rotation=gun.Muzzle.rotation;blocker.transform.localScale=Vector3.one*.3f;
                Physics.SyncTransforms();int before=state.Ammo;float cooldown=gun.NextFireTime,deviceReady=state.DeviceReadyAt;
                assembly.FireGroup(false);
                Require(state.Ammo==before&&gun.NextFireTime==cooldown&&state.DeviceReadyAt==deviceReady,kinds[i]+" blocked spent resources");report.blocked++;
                blocker.SetActive(false);Physics.SyncTransforms();assembly.FireGroup(false);
                Require(gun.NextFireTime>cooldown,kinds[i]+" clear did not fire");
                if(kinds[i]!=R4WeaponKind.Basic)Require(state.Ammo==before-1,kinds[i]+" clear ammo");report.unblocked++;
                Destroy(rig.gameObject);R1EncounterManager.ClearTransientObjects();yield return null;
            }
            Destroy(paletteObject);Destroy(material);
        }
    }
}
#endif
