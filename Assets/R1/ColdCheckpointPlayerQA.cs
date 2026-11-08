#if B01_UNIFIED_RC2_ACCEPTANCE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R1
{
    // Two separate batch processes, explicit temporary slot; never touches the player's save.
    public sealed class ColdCheckpointPlayerQA : MonoBehaviour
    {
        [Serializable] sealed class Result { public string status="RUNNING", phase, error, method="Two separate Windows Player processes, API-driven; not human gameplay"; public bool isolated, technologyIsolated, battery, inventory, ids, hp, energy, scrap; }
        readonly Result result=new Result(); string folder,phase;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-cold-checkpoint-qa");
            if(i<0||i+2>=args.Length||!Application.isBatchMode)return;
            var host=new GameObject("isolated cold checkpoint QA");DontDestroyOnLoad(host);
            var qa=host.AddComponent<ColdCheckpointPlayerQA>();qa.folder=args[i+1];qa.phase=args[i+2];
        }
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        void Start(){Directory.CreateDirectory(folder);result.phase=phase;Application.runInBackground=true;StartCoroutine(Run());}
        IEnumerator Run()
        {
            var flow=Exercise();bool ok=true;
            while(true){bool next=false;object item=null;try{next=flow.MoveNext();if(next)item=flow.Current;}catch(Exception e){result.error=e.ToString();ok=false;}if(!next||!ok)break;yield return item;}
            result.status=ok?"PASS":"FAIL";File.WriteAllText(Path.Combine(folder,"cold-"+phase+".json"),JsonUtility.ToJson(result,true));Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            string slot=Path.GetFullPath(Path.Combine(folder,"isolated-checkpoint.json"));
            result.isolated=string.Equals(Path.GetFullPath(R1RunCheckpointStorage.PathName),slot,StringComparison.OrdinalIgnoreCase);
            Require(result.isolated,"refuse to touch player save");
            string temporaryRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
            result.technologyIsolated=Path.GetFullPath(R1TechnologyProgress.DefaultPath).StartsWith(temporaryRoot,StringComparison.OrdinalIgnoreCase);
            Require(result.technologyIsolated,"refuse to use the player's persistent technology profile");
            float deadline=Time.realtimeSinceStartup+45;
            while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var run=R1RunController.Instance;Require(run?.Progress!=null,"game bootstrap");
            if(phase=="save")
            {
                Require(!File.Exists(slot),"isolated slot must start empty");
                Require(run.SelectVehicle(R3VehicleKind.Light)&&run.PrepareUnifiedRun(),"prepare actual garage");yield return null;
                var model=run.VehicleModel;model.Award(200);
                Require(run.GaragePurchase(R3PartKind.Wheel),"purchase wheel to inventory");
                var wheel=model.Inventory.Last(p=>p.Kind==R3PartKind.Wheel);wheel.Health=.55f;
                Require(run.GaragePurchase(R3PartKind.Battery),"purchase battery");
                var battery=model.Inventory.Last(p=>p.Kind==R3PartKind.Battery);
                var cells=Enumerable.Range(0,5).SelectMany(y=>Enumerable.Range(-6,13).SelectMany(x=>Enumerable.Range(-7,15).Select(z=>new Vector3Int(x,y,z))));
                var cell=cells.First(c=>model.OwnedPlacementError(battery.Id,c,Vector3Int.zero)==null);
                Require(run.GarageMount(battery.Id,cell,Vector3Int.zero),"mount actual battery");yield return null;
                Require(model.Energy.TrySpend(123),"spend battery energy");
                Require(run.SaveRunCheckpoint(),"save: "+run.RunSaveMessage);
                var expected=model.MakeCheckpoint(Time.time);
                File.WriteAllText(Path.Combine(folder,"expected-model.json"),JsonUtility.ToJson(expected));
                result.battery=model.Parts.Any(p=>p.Id==battery.Id);result.inventory=model.Inventory.Any(p=>p.Id==wheel.Id&&Mathf.Abs(p.Health-.55f)<.001f);
                Require(result.battery&&result.inventory&&File.Exists(slot),"saved owned battery and damaged wheel");
            }
            else if(phase=="load")
            {
                var expected=JsonUtility.FromJson<R3ModelCheckpoint>(File.ReadAllText(Path.Combine(folder,"expected-model.json")));
                Require(run.LoadRunCheckpoint(),"cold load: "+run.RunSaveMessage);
                var actual=run.VehicleModel;var parts=actual.Parts.Concat(actual.Inventory).ToArray();
                result.ids=parts.Select(p=>p.Id).OrderBy(s=>s).SequenceEqual(expected.mounted.Concat(expected.inventory).Select(p=>p.Id).OrderBy(s=>s));
                result.hp=parts.All(p=>Mathf.Abs(p.Health-expected.mounted.Concat(expected.inventory).Single(q=>q.Id==p.Id).Health)<.001f);
                result.energy=Mathf.Abs(actual.Energy.Current-expected.energy)<.01f;
                result.scrap=actual.Scrap==expected.scrap;
                result.battery=actual.Parts.Any(p=>p.Kind==R3PartKind.Battery);
                result.inventory=actual.Inventory.Any(p=>p.Kind==R3PartKind.Wheel&&Mathf.Abs(p.Health-.55f)<.001f);
                Require(result.ids&&result.hp&&result.energy&&result.scrap&&result.battery&&result.inventory,"cold state mismatch");
            }
            else throw new InvalidOperationException("unknown phase");
        }
    }
}
#endif
