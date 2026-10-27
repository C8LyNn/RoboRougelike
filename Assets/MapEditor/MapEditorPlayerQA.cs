using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R6;
using RoboRougelike.R3;

namespace RoboRougelike.MapEditor
{
    // Explicit opt-in QA. The normal game has no test runner and never starts this fixture.
    public sealed class MapEditorPlayerQA : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string status="Running",error="";
            public bool batchMode,menu,garage,mapPersistence,testDrive,actualWheelContact,driveAcrossRamp,finish,returnToEditor;
            public bool globalFloor,scaledObject,ghost,additionalObjects;
            public float maxZ;
        }
        string directory;readonly Report report=new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rrl-map-editor-qa");
            if(i<0||i+1>=args.Length||!Application.isBatchMode)return;
            var obj=new GameObject("Explicit map editor batch QA");DontDestroyOnLoad(obj);
            obj.AddComponent<MapEditorPlayerQA>().directory=args[i+1];
        }
        void Start(){Directory.CreateDirectory(directory);Application.runInBackground=true;Application.targetFrameRate=60;report.batchMode=Application.isBatchMode;StartCoroutine(Run());}
        void Save()=>File.WriteAllText(Path.Combine(directory,"map-editor-player-qa.json"),JsonUtility.ToJson(report,true));
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        IEnumerator Run()
        {
            var flow=Exercise();bool ok=true;
            while(true)
            {
                bool next=false;object current=null;
                try{next=flow.MoveNext();if(next)current=flow.Current;}
                catch(Exception e){report.error=e.ToString();ok=false;}
                if(!next||!ok)break;
                yield return current;
            }
            report.status=ok?"PASS":"FAIL";Save();Application.Quit(ok?0:1);
        }
        IEnumerator Exercise()
        {
            float deadline=Time.realtimeSinceStartup+40;
            while(R1RunController.Instance?.Progress==null&&Time.realtimeSinceStartup<deadline)yield return null;
            var run=R1RunController.Instance;Require(run!=null&&run.Progress!=null,"Main menu did not initialize");
            Require(run.SelectVehicle(R3VehicleKind.Light),"Select light vehicle");
            Require(run.OpenMapEditor(),"Menu to map editor");report.menu=run.MapEditing;var original=run.VehicleModel;
            run.ToggleMapFloor();report.globalFloor=run.EditorDraft.GlobalFloor;
            run.EditorDraft.Place(MapItemKind.Spawn,0,-3,0);run.EditorDraft.Place(MapItemKind.Ramp,0,0,0);run.EditorDraft.Place(MapItemKind.Finish,0,3,0);
            run.EditorDraft.Place(MapItemKind.Platform,4,0,0);run.EditorDraft.Place(MapItemKind.Pillar,7,0,0);
            run.EditorDraft.Place(MapItemKind.LowBarrier,10,0,0);run.EditorDraft.Place(MapItemKind.Steps,13,0,0);
            report.additionalObjects=run.EditorDraft.Items.Count==7;
            run.SetMapTool(true,false);run.SelectMapAt(0,0);run.ChangeMapScale(2,1);
            report.scaledObject=run.EditorSelected!=null&&run.EditorSelected.scaleZ==2;
            run.SetMapTool(false,false);
            var preview=new MapItem{kind=MapItemKind.Steps,x=20,z=0,scaleX=2,scaleY=3,scaleZ=1};
            var palette=run.GetComponent<R3VisualPalette>();run.EditorWorld.ShowGhost(preview,true,palette);
            var ghost=run.EditorWorld.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Widmo edytora map"&&t.gameObject.activeInHierarchy);
            report.ghost=ghost!=null&&ghost.localScale==new Vector3(2,3,1)&&ghost.GetComponentInChildren<Renderer>().sharedMaterial==palette.Valid;
            run.EditorWorld.HideGhost();Require(report.globalFloor&&report.scaledObject&&report.ghost&&report.additionalObjects,"Expanded editor tools");
            var expected=run.EditorDraft.ToJson();string path=Path.Combine(directory,"test-map.json");
            MapStorage.Save(path,run.EditorDraft);var loaded=new MapDraft();Require(MapStorage.Load(path,loaded,out var why),"Map load: "+why);
            Require(loaded.ToJson()==expected,"Map round trip");report.mapPersistence=true;
            run.MapEditorGarage();Require(run.ReturnToMapEditorFromGarage(),"Garage to editor");report.garage=ReferenceEquals(original,run.VehicleModel);
            Require(run.TestEditedMap(),"Start custom test drive");deadline=Time.realtimeSinceStartup+40;
            while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(!run.Transitioning&&run.MapEditorTesting,"Test drive transition");
            var vehicle=run.Player;var drive=vehicle.GetComponent<R6VehicleDrive>();var body=vehicle.GetComponent<Rigidbody>();
            Require(drive!=null&&body!=null,"Real R6 vehicle");report.testDrive=true;vehicle.Controller.enabled=false;
            report.maxZ=body.position.z;
            for(int step=0;step<600&&!run.EditorWorld.Finished;step++)
            {
                float previous=Time.fixedTime;float limit=Time.realtimeSinceStartup+3;
                while(Time.fixedTime<=previous&&Time.realtimeSinceStartup<limit)yield return null;
                Require(Time.fixedTime>previous,"Physics did not advance");drive.Step(1,0,10,4,90);
                report.actualWheelContact|=drive.GroundedWheels>0;report.maxZ=Mathf.Max(report.maxZ,body.position.z);
            }
            report.driveAcrossRamp=report.maxZ>2;report.finish=run.EditorWorld.Finished;
            Require(report.actualWheelContact&&report.driveAcrossRamp&&report.finish,"R6 drive / ramp / finish");
            run.TogglePause();Require(run.Paused,"Pause");Require(run.ReturnFromEditedMapTest(),"Return request");
            deadline=Time.realtimeSinceStartup+40;while(run.Transitioning&&Time.realtimeSinceStartup<deadline)yield return null;
            report.returnToEditor=!run.Transitioning&&run.MapEditing&&ReferenceEquals(original,run.VehicleModel)&&run.EditorDraft.ToJson()==expected;
            Require(report.returnToEditor,"Return preserves map and original vehicle");
        }
    }
}
