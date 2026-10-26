using System.Collections.Generic;
using UnityEngine;

namespace B01.BUILD001
{
    /// <summary>Standalone test scene: original primitives, no R3/P12 code hooks.</summary>
    public sealed class Build001Demo : MonoBehaviour
    {
        private readonly List<BuildInstalledPart> placed = new List<BuildInstalledPart>();
        private GameObject ghost;
        private BuildPlacementResult verdict;
        private string supporter, message;
        private int choice = -1;
        private Material good, bad, neutral;
        private Shader previewShader;

        private void Start()
        {
            previewShader = Resources.Load<Shader>("BUILD001_PreviewURP");
            if (previewShader == null || !previewShader.isSupported)
            {
                Debug.LogError("[BUILD001] URP preview shader missing/unsupported; demo fail closed.");
                enabled = false;
                return;
            }
            good = MakeMaterial(new Color(.12f,.85f,.28f,1f));
            bad = MakeMaterial(new Color(.95f,.12f,.12f,1f));
            neutral = MakeMaterial(new Color(.30f,.42f,.62f,1f));
            var light = new GameObject("BUILD001_LIGHT").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation=Quaternion.Euler(50,30,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name="DemoFloor";
            floor.transform.position=new Vector3(0,-.4f,0);
            floor.transform.localScale=new Vector3(12,.5f,12);
            floor.GetComponent<Renderer>().sharedMaterial = neutral;
            var cameraObject=new GameObject("BUILD001_DEMO_CAMERA");
            var cam=cameraObject.AddComponent<Camera>();
            cam.tag="MainCamera";
            cameraObject.transform.position=new Vector3(2.5f,3.5f,-5f);
            cameraObject.transform.LookAt(new Vector3(0,1,0));
            placed.Add(new BuildInstalledPart("core",BuildFixtureCatalog.Core(),
                new BuildPose(Vector3Int.zero,Vector3Int.zero)));
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name="LogicalCore_OneOwner";
            cube.transform.position=Vector3.up;
            cube.transform.localScale=Vector3.one*.52f;
            cube.GetComponent<Renderer>().sharedMaterial = neutral;
            Cycle();
        }

        private void Cycle()
        {
            choice=(choice+1)%5;
            if(ghost!=null)Destroy(ghost);
            ghost=GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name="BUILD001_GHOST_PREVIEW";
            BuildDefinition definition;
            BuildPose pose;
            switch(choice)
            {
                case 0: definition=BuildFixtureCatalog.Wheel();pose=new BuildPose(Vector3Int.right,Vector3Int.zero);break;
                case 1: definition=BuildFixtureCatalog.Wheel();pose=new BuildPose(Vector3Int.up,Vector3Int.zero);break;
                case 2: definition=BuildFixtureCatalog.Gun();pose=new BuildPose(Vector3Int.up,Vector3Int.zero);break;
                case 3: definition=BuildFixtureCatalog.Gun();pose=new BuildPose(Vector3Int.right,Vector3Int.zero);break;
                default:definition=BuildFixtureCatalog.ArmorCube();pose=new BuildPose(Vector3Int.right,Vector3Int.zero);break;
            }
            verdict=BuildPlacementRules.Check(placed,new BuildInstalledPart("ghost",definition,pose),
                out supporter);
            ghost.transform.position=Vector3.up+
                (Vector3)pose.Anchor*BuildSocketGeometry.GridMetres;
            ghost.transform.localScale=Vector3.one*.52f;
            var renderer=ghost.GetComponent<Renderer>();
            renderer.sharedMaterial = verdict==BuildPlacementResult.Valid ? good : bad;
            message=definition.Id+" at "+pose.Anchor+": "+verdict+
                (supporter==null?"":" / support="+supporter);
            Debug.Log("[BUILD001] "+message);
        }

        private Material MakeMaterial(Color color)
        {
            var material = new Material(previewShader);
            material.name = "BUILD001_PREVIEW_RUNTIME_URP";
            material.SetColor("_BaseColor", color);
            return material;
        }

        private void OnDestroy()
        {
            if (good != null) Destroy(good);
            if (bad != null) Destroy(bad);
            if (neutral != null) Destroy(neutral);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,650,150),GUI.skin.box);
            GUILayout.Label("BUILD001 / structural mounting surface preview / Unity fixture");
            GUILayout.Label("Green=valid socket; red=invalid / wheel tread is NEVER mountable.");
            GUILayout.Label(message ?? "Loading...");
            if(GUILayout.Button("Next candidate: hub / top tire / gun base / barrel / armor"))
                Cycle();
            GUILayout.Label("Real R3 garage, inventory, HP and wheel physics NOT integrated.");
            GUILayout.EndArea();
        }
    }
}
