#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoboRougelike.Mechanics.IndependentSteering.Demo;

namespace RoboRougelike.Mechanics.IndependentSteering.Demo.Editor
{
    public static class SteeringDemoSceneCreator
    {
        [MenuItem("Tools/RoboRougelike/MOV-011/Create Temporary Steering Demo Scene")]
        private static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name="DEMO FLOOR (nonproduction)";
            floor.transform.position=new Vector3(0f,-0.45f,0f);
            floor.transform.localScale=new Vector3(80f,0.5f,80f);
            var car=new GameObject("MOV011 Demo Vehicle - NOT for retail");
            car.transform.position=new Vector3(0f,1f,0f);
            var body=car.AddComponent<Rigidbody>();
            body.mass=250f;
            body.useGravity=false;
            body.constraints=RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.linearDamping=0.1f;
            body.angularDamping=1f;
            var hull=GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name="Hull (visual only)";
            hull.transform.SetParent(car.transform,false);
            hull.transform.localScale=new Vector3(1.5f,0.4f,2.2f);
            var wheelPositions=new [] {
                new Vector3(-1f,-0.05f,1.4f),new Vector3(1f,-0.05f,1.4f),
                new Vector3(-1f,-0.05f,-1.4f),new Vector3(1f,-0.05f,-1.4f)
            };
            var steering=car.AddComponent<IndependentSteeringController>();
            steering.Bind(body);
            for(int i=0;i<4;i++)
            {
                var anchor=new GameObject(((SteeringWheelPosition)i).ToString()+" Mount");
                anchor.transform.SetParent(car.transform,false);
                anchor.transform.localPosition=wheelPositions[i];
                var wheel=anchor.AddComponent<SteeringWheelModule>();
                var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name="Steer Visual";
                visual.transform.SetParent(anchor.transform,false);
                visual.transform.localScale=new Vector3(.3f,.3f,.55f);
                Collider col=visual.GetComponent<Collider>();
                if(col!=null) Object.DestroyImmediate(col);
                wheel.Configure((SteeringWheelPosition)i,visual.transform);
                steering.RegisterWheel(wheel);
            }
            var demo=car.AddComponent<SteeringDemoDrive>();
            demo.Bind(steering,body);
            car.AddComponent<SteeringDemoInput>();
            var camGo=new GameObject("Demo camera");
            var cam=camGo.AddComponent<Camera>();
            camGo.transform.position=new Vector3(0,25,-14);
            camGo.transform.rotation=Quaternion.Euler(63,0,0);
            cam.orthographic=true; cam.orthographicSize=16f;
            var lightGo=new GameObject("Demo light");
            var light=lightGo.AddComponent<Light>();
            light.type=LightType.Directional; lightGo.transform.rotation=Quaternion.Euler(45,-30,0);
            Selection.activeGameObject=car;
            Debug.Log("MOV011 temporary scene created. WASD drive; 1 front, 2 rear, 3 counterphase, 4 crab. SAVE SCENE MANUALLY if needed. Legacy Input Manager required for demo keys.");
        }
    }
}
#endif
