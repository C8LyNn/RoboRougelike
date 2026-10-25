using UnityEngine;

namespace B01.RUNSAFETY001
{
    /// <summary>Isolated manual demonstration. NOT R1RunController, not a game vehicle.</summary>
    public sealed class RunSafetyDemo : MonoBehaviour
    {
        private RunSafetyState state;
        private GameObject loaner;
        private string message;
        private int nextId;
        private bool simulateFailure;

        private void Start()
        {
            var light = new GameObject("RUNSAFETY_DEMO_LIGHT").AddComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.1f;
            light.transform.rotation=Quaternion.Euler(45f,40f,0);
            var cameraObject=new GameObject("RUNSAFETY_DEMO_CAMERA");
            var camera=cameraObject.AddComponent<Camera>();
            camera.tag="MainCamera";camera.transform.position=new Vector3(5f,5f,-8f);
            camera.transform.LookAt(Vector3.up);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name="RUNSAFETY_DEMO_FLOOR";
            ground.transform.position=new Vector3(0f,-.4f,0f);
            ground.transform.localScale=new Vector3(14f,.5f,14f);
            ResetModel();
        }

        private void ResetModel()
        {
            if(loaner!=null)Destroy(loaner);
            state=new RunSafetyState(new[]{
                new SafetyOwnedPart("original-gun",SafetyPartRole.Weapon,0,100,70,false),
                new SafetyOwnedPart("original-left-wheel",SafetyPartRole.Drive,0,40,28,false),
                new SafetyOwnedPart("original-bridge",SafetyPartRole.Structure,0,20,14,false)
            },0);
            nextId=0;simulateFailure=false;
            message="Original 0HP gun, wheel, bridge | Scrap 0. Queue one free rebuild; request loaner.";
        }

        private SafetyVehicleSnapshot BrokenVehicle() => new SafetyVehicleSnapshot {
            RunActive=true,InGarage=true,NextChallengeAvailable=true,CoreAlive=true,
            StructureConnected=false,FunctionalWeapon=false,FunctionalDrive=false,
            CanonicalCanDeploy=false,OtherStartBlocker=false
        };

        private void Begin()
        {
            if(!state.TryBeginChallenge("DEMO-CHALLENGE-"+(++nextId),
                BrokenVehicle(),out var lease,out var decision))
            {message="CANNOT START: "+decision;return;}
            if(lease.UsesLoaner)
            {
                loaner=GameObject.CreatePrimitive(PrimitiveType.Cube);
                loaner.name=lease.LeaseId+"_TEST_ONLY_NOT_OWNED";
                loaner.transform.position=new Vector3(0f,1.4f,0f);
                loaner.transform.localScale=new Vector3(2f,.5f,3f);
                var body=loaner.AddComponent<Rigidbody>();
                body.mass=150f;
                body.useGravity=true;
                loaner.GetComponent<Renderer>().material.color=new Color(.2f,.6f,1f);
            }
            message="Temporary lease: "+lease.LeaseId+"; original parts still 0HP.";
        }

        private void Complete(bool victory)
        {
            if(state.ActiveEncounterId==null){message="No active prototype challenge.";return;}
            var result=state.RecordOutcome(state.ActiveEncounterId,victory);
            if(loaner!=null){Destroy(loaner);loaner=null;}
            // This is a standalone simulation of a result, never a real R1 win
            // or defeat and never a canonical reward/HP mutation.
            message=(victory?"TEST_WIN: ":"TEST_DEFEAT: ")+result+
                " | VictorySerial="+state.VictorySerial;
        }

        private void OnGUI()
        {
            if(state==null)return;
            GUILayout.BeginArea(new Rect(12f,12f,650f,290f),GUI.skin.box);
            GUILayout.Label("RUN-SAFETY-001 R1 / opt-in prototype. NOT RUNNING IN REAL ROGUELITE.");
            GUILayout.Label("Loaner decision="+state.Evaluate(BrokenVehicle())+
                " | Scrap="+state.Scrap+" | accepted wins="+state.AcceptedVictories);
            foreach(var part in state.Parts)
                GUILayout.Label(part.Id+": HP="+part.HealthPercent+" pending="+
                    part.IsRebuilding+" mounted="+part.IsMounted);
            GUILayout.Label(message);
            if(GUILayout.Button("Queue FREE: original gun"))
            { var x=state.TryQueueRebuild("original-gun",out var price);message=x+" charged="+price; }
            if(GUILayout.Button("Queue wheel / requires paid charge if free used"))
            { var x=state.TryQueueRebuild("original-left-wheel",out var price);message=x+" charged="+price; }
            if(GUILayout.Button("Begin trial challenge (loaner)"))Begin();
            if(GUILayout.Button("SIMULATE victory (test-only)"))Complete(true);
            if(GUILayout.Button("SIMULATE defeat (test-only)"))Complete(false);
            if(GUILayout.Button("Reset all prototype state"))ResetModel();
            GUILayout.EndArea();
        }
    }
}
