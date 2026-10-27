using System.Collections.Generic;
using UnityEngine;

namespace B01.PARTSIZE001
{
    /// <summary>Drop prefab into an EMPTY disposable Unity scene, press Play.
    /// Runtime-spawned independent tests; NEVER attaches to R3 live garage.</summary>
    public sealed class PartSizeDemoEntry : MonoBehaviour
    {
        private readonly List<GameObject> spawned=new List<GameObject>();
        private readonly List<string> receipts=new List<string>();
        public IReadOnlyList<string> Receipts => receipts;

        public void Start() { BuildPreview(); }
        public void BuildPreview()
        {
            Cleanup();
            Camera cam=Camera.main;
            if(cam==null)
            {
                var go=new GameObject("PARTSIZE demo camera");
                spawned.Add(go);
                cam=go.AddComponent<Camera>();
                cam.tag="MainCamera";
            }
            cam.transform.position=new Vector3(2.0f,5.4f,-10.5f);
            cam.transform.LookAt(new Vector3(2.0f,0f,0f));
            cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.065f,.09f,.12f);

            Add(PartSizeReferenceFixtures.LegacyCube(),new Cell3(0,0,0),
                new Cell3(0,0,0),new Vector3(.52f,.52f,.52f),"CUBE");
            Add(PartSizeReferenceFixtures.LegacyWide(),new Cell3(5,0,0),
                new Cell3(0,1,0),new Vector3(1.07f,.52f,1.07f),"WIDE ROT Y90");
            Add(PartSizeReferenceFixtures.LegacyCube("OversizeCase"),
                new Cell3(10,0,0),new Cell3(0,0,0),
                new Vector3(1.2f,.52f,.52f),"OVERSIZE NEGATIVE");
        }

        void Add(PartSizeDefinition spec,Cell3 anchor,Cell3 turns,
            Vector3 geometry,string name)
        {
            var go=new GameObject("PARTSIZE "+name);
            spawned.Add(go);
            go.transform.position=new Vector3(anchor.X*.55f,anchor.Y*.55f,anchor.Z*.55f);
            go.transform.rotation=Quaternion.Euler(turns.X*90f,turns.Y*90f,turns.Z*90f);
            var child=GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawned.Add(child);
            child.name="Actual BoxCollider "+name;
            child.transform.SetParent(go.transform,false);
            var center=spec.LocalBoxCenter.Value;
            child.transform.localPosition=new Vector3((float)center.X,(float)center.Y,(float)center.Z);
            child.transform.localScale=geometry;
            var collider=child.GetComponent<BoxCollider>();
            var reason=PartSizeNativePhysics.Validate(spec,anchor,turns,go.transform,
                new Collider[]{collider},new Collider[0],true);
            var expected=name.Contains("NEGATIVE")?"PHYSICAL_ENVELOPE_OUTSIDE_OWNED_CELLS":null;
            bool pass=reason==expected;
            receipts.Add(name+": "+(reason??"PASS")+" / assertion "+(pass?"PASS":"FAIL"));
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            if(shader==null)shader=Shader.Find("Unlit/Color");
            if(shader!=null)
            {
                var material=new Material(shader);
                material.color=pass?(reason==null?new Color(.13f,.76f,.35f):new Color(.93f,.29f,.25f)):
                    new Color(.9f,.05f,.8f);
                child.GetComponent<Renderer>().sharedMaterial=material;
            }
        }

        public void Cleanup()
        {
            receipts.Clear();
            foreach(var go in spawned)
                if(go!=null)Destroy(go);
            spawned.Clear();
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20,20,620,205),"PARTSIZE-001 R1 (isolated demo)","box");
            GUILayout.Label("TRUE Collider envelopes / 0.55m cells / no canonical R3 edits");
            foreach(var r in receipts) GUILayout.Label(r);
            GUILayout.Label("Native visual/Physics acceptance requires separate Unity QA.");
            if(GUILayout.Button("Rebuild three physical fixtures"))BuildPreview();
            GUILayout.EndArea();
        }
        private void OnDestroy() { Cleanup(); }
    }
}
