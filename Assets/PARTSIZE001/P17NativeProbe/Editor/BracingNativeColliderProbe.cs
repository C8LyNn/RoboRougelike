// B01-23: ISOLATED Editor-only native PhysX measurement on actual imported prefabs.
// No dummy cube fallback. No Codex garage/runtime modification. Manual opt-in only.
// A collider PASS is NOT an authorised typed socket, articulation sweep or G1.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using B01.PARTSIZE001;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace B01.PARTSIZE001.P17Probe
{
    [CreateAssetMenu(fileName="P17NativePhysicalSettings",menuName="B01/P17 native physical probe settings")]
    public sealed class P17NativePhysicalSettings : ScriptableObject
    {
        public GameObject armorPrefab;
        public GameObject corePrefab;
        public GameObject crawlerTrackPrefab;
        public GameObject gunPrefab;
        public GameObject hoverEnginePrefab;
        public GameObject wheelPrefab;

        public GameObject ForKind(string kind)
        {
            switch(kind)
            {
                case "Armor":return armorPrefab;
                case "Core":return corePrefab;
                case "CrawlerTrack":return crawlerTrackPrefab;
                case "Gun":return gunPrefab;
                case "HoverEngine":return hoverEnginePrefab;
                case "Wheel":return wheelPrefab;
                default:return null;
            }
        }
    }

    [Serializable] public sealed class NativeOutcome
    {
        public string blueprintId;
        public int partOwners;
        public int realColliders;
        public int measuredColliderPairs;
        public int invalidOwnerCount;
        public int penetrationsOver5mm;
        public string[] errors;
    }
    [Serializable] public sealed class NativeReceipt
    {
        public string schema="B01-P17-W03-REAL-UNITY-COLLIDER-RECEIPT-v1";
        public string producerSourceSha=BracingPhysicalContract.OriginalProducerSha;
        public string result="NOT_RUN";
        public string actualUnityEditorVersion="NOT_RUN";
        public string typedSocketStatus="NOT_RUN_NO_CODEX_AUTHORITY";
        public string movementSweepsStatus="NOT_RUN";
        public string damageDetachStatus="NOT_RUN";
        public string playerBuildStatus="NOT_RUN";
        public NativeOutcome[] outcomes=new NativeOutcome[0];
        public string[] errors=new string[0];
    }

    public static class BracingNativeColliderProbe
    {
        private const float Overshoot=0.0055f;
        private static readonly string[] RigFiles={
            "Assets/PARTSIZE001/Bracing/P17/P17_Flanker_Reinforced.json",
            "Assets/PARTSIZE001/Bracing/P17/P17_Bulwark_Reinforced.json",
            "Assets/PARTSIZE001/Bracing/P17/P17_Skirmisher_Reinforced.json"
        };
        private sealed class Placed
        {
            public RigPart part;
            public GameObject instance;
            public Collider[] colliders;
        }

        [MenuItem("B01/P17 Physical Probe/Measure with real assigned prefabs (manual)")]
        private static void MenuRun()
        {
            string[] ids=AssetDatabase.FindAssets("t:P17NativePhysicalSettings");
            if(ids.Length!=1)
            {
                Debug.LogError("P17_NATIVE_NOT_RUN: create exactly one P17NativePhysicalSettings asset, assign 6 real project prefabs; no primitives are substituted.");
                return;
            }
            var path=AssetDatabase.GUIDToAssetPath(ids[0]);
            Run(AssetDatabase.LoadAssetAtPath<P17NativePhysicalSettings>(path));
        }

        public static NativeReceipt Run(P17NativePhysicalSettings settings)
        {
            var receipt=new NativeReceipt();
            var failures=new List<string>();
            Scene isolated=new Scene();
            bool newScene=false;
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("REFUSE_ACTIVE_EDITOR_PLAY_MODE");
                if(settings==null)
                    throw new InvalidOperationException("MISSING_REAL_PREFAB_SETTINGS");
                var rigs=new PhysicalRig[3];
                for(int i=0;i<3;i++)
                {
                    var asset=AssetDatabase.LoadAssetAtPath<TextAsset>(RigFiles[i]);
                    if(asset==null)throw new InvalidOperationException("MISSING_EXACT_ORIGINAL_P17_JSON:"+RigFiles[i]);
                    rigs[i]=JsonUtility.FromJson<PhysicalRig>(asset.text);
                }
                var bindings=new List<Binding>();
                var mapped=new Dictionary<string,GameObject>(StringComparer.Ordinal);
                foreach(string kind in BracingPhysicalContract.Kinds)
                {
                    GameObject prefab=settings.ForKind(kind);
                    if(prefab==null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
                        throw new InvalidOperationException("REAL_UNITY_PREFAB_NOT_ASSIGNED:"+kind);
                    string path=AssetDatabase.GetAssetPath(prefab);
                    string guid=AssetDatabase.AssetPathToGUID(path);
                    if(string.IsNullOrWhiteSpace(guid))
                        throw new InvalidOperationException("NO_REAL_ASSET_DATABASE_GUID:"+kind);
                    mapped.Add(kind,prefab);
                    bindings.Add(new Binding{kind=kind,assetPath=path,assetGuid=guid});
                }
                var input=new Intake{
                    schema=BracingPhysicalContract.IntakeSchema,
                    originalBracingSha=BracingPhysicalContract.OriginalProducerSha,
                    nativeEvidence=BracingPhysicalContract.ReportState,
                    rigs=rigs,bindings=bindings.ToArray()
                };
                string contract=BracingPhysicalContract.InputError(input);
                if(contract!=null)
                    throw new InvalidOperationException("SOURCE_PREFAB_INTAKE_REJECTED:"+contract);
                // Create additive temporary scene and explicitly target it for each prefab.
                // Never load/modify Codex R3 or existing garage scenes.
                isolated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                newScene=true;
                var outcomes=new List<NativeOutcome>();
                for(int i=0;i<rigs.Length;i++)
                    outcomes.Add(MeasureOne(isolated,rigs[i],mapped,failures));
                receipt.outcomes=outcomes.ToArray();
                receipt.actualUnityEditorVersion=Application.unityVersion;
                receipt.result=failures.Count==0
                    ?"PHYSICS_COLLIDER_SCREEN_PASS_SOCKET_AND_G1_HOLD"
                    :"PHYSICS_COLLIDER_SCREEN_FAIL";
            }
            catch(Exception e)
            {
                failures.Add(e.GetType().Name+":"+e.Message);
                receipt.result="NOT_RUN_OR_PRECHECK_REJECTED";
            }
            finally
            {
                if(newScene && isolated.IsValid())
                    EditorSceneManager.CloseScene(isolated,true);
                receipt.errors=failures.ToArray();
                WriteReceipt(receipt);
            }
            if(failures.Count>0)
                Debug.LogError("P17_NATIVE_GATE_HOLD: "+string.Join(" | ",failures.ToArray()));
            else
                Debug.LogWarning("P17_COLLIDERS_SCREENED: typed sockets, wheel/track/hover sweeps, damage and full G1 remain NOT_RUN.");
            return receipt;
        }

        private static NativeOutcome MeasureOne(Scene scene,PhysicalRig rig,
            Dictionary<string,GameObject> mapped,List<string> allErrors)
        {
            var errors=new List<string>();
            var placed=new List<Placed>();
            foreach(RigPart p in rig.parts)
            {
                GameObject instance=PrefabUtility.InstantiatePrefab(mapped[p.kind],scene) as GameObject;
                if(instance==null)
                {
                    errors.Add(p.localId+":PREFAB_INSTANTIATE_FAILED");
                    continue;
                }
                instance.name="B01 P17 PROBE "+p.localId;
                instance.transform.position=new Vector3(
                    (float)(p.cell.x*PartSizeGrid.GridMeters),
                    (float)(p.cell.y*PartSizeGrid.GridMeters),
                    (float)(p.cell.z*PartSizeGrid.GridMeters));
                instance.transform.rotation=Quaternion.Euler(
                    p.rotationQuarterTurns.x*90f,p.rotationQuarterTurns.y*90f,
                    p.rotationQuarterTurns.z*90f);
                if((instance.transform.lossyScale-Vector3.one).sqrMagnitude>1e-7f)
                    errors.Add(p.localId+":UNEXPECTED_PREFAB_SCALE");
                Collider[] all=instance.GetComponentsInChildren<Collider>(true);
                if(all.Length==0)
                    errors.Add(p.localId+":NO_REAL_COLLIDERS");
                foreach(Collider c in all)
                {
                    if(c==null||!c.enabled||c.isTrigger||!c.gameObject.activeInHierarchy||
                       !(c is BoxCollider||c is SphereCollider||c is CapsuleCollider||
                         (c is MeshCollider mc&&mc.convex)))
                        errors.Add(p.localId+":UNSUPPORTED_DISABLED_TRIGGER_OR_INACTIVE_COLLIDER");
                }
                placed.Add(new Placed{part=p,instance=instance,colliders=all});
            }
            Physics.SyncTransforms();
            int colliders=0,overhangs=0,penetrations=0,pairs=0;
            foreach(Placed p in placed)
            {
                var partDef=PartSizeBaselineCatalog.ForSourceKind(p.part.kind);
                Bounds limit=PartSizeNativePhysics.GridBounds(
                    partDef,p.part.cell.AsCell(),p.part.rotationQuarterTurns.AsCell());
                foreach(Collider c in p.colliders)
                {
                    if(c==null)continue;
                    colliders++;
                    Bounds actual=c.bounds;
                    if(actual.min.x<limit.min.x-Overshoot||
                       actual.min.y<limit.min.y-Overshoot||
                       actual.min.z<limit.min.z-Overshoot||
                       actual.max.x>limit.max.x+Overshoot||
                       actual.max.y>limit.max.y+Overshoot||
                       actual.max.z>limit.max.z+Overshoot)
                    {
                        overhangs++;
                        errors.Add(p.part.localId+":COLLIDER_EXCEEDS_550MM_CELL_ENVELOPE");
                    }
                }
            }
            for(int i=0;i<placed.Count;i++)
            for(int j=i+1;j<placed.Count;j++)
            foreach(Collider a in placed[i].colliders)
            foreach(Collider b in placed[j].colliders)
            {
                if(a==null||b==null||!a.enabled||!b.enabled||
                   a.isTrigger||b.isTrigger)continue;
                pairs++;
                bool overlapping=Physics.ComputePenetration(
                    a,a.transform.position,a.transform.rotation,
                    b,b.transform.position,b.transform.rotation,
                    out Vector3 _,out float depth);
                if(overlapping && depth>PartSizeNativePhysics.MaxPenetrationMeters)
                {
                    penetrations++;
                    errors.Add(placed[i].part.localId+" ↔ "+placed[j].part.localId+
                        ":PHYSICS_PENETRATION_GT_5MM depth="+depth);
                }
            }
            if(pairs==0)
                errors.Add(rig.blueprintId+":NO_REAL_PAIRWISE_COLLIDER_COMPARISONS");
            foreach(string error in errors)
                allErrors.Add(rig.blueprintId+"/"+error);
            return new NativeOutcome{
                blueprintId=rig.blueprintId,partOwners=placed.Count,
                realColliders=colliders,measuredColliderPairs=pairs,
                invalidOwnerCount=errors.Count,penetrationsOver5mm=penetrations,
                errors=errors.ToArray()
            };
        }

        private static void WriteReceipt(NativeReceipt report)
        {
            const string folder="Library/B01P17PhysicalProbe";
            Directory.CreateDirectory(folder);
            string file=Path.Combine(folder,"actual-unity-p17-collider-receipt.json");
            File.WriteAllText(file,JsonUtility.ToJson(report,true));
            Debug.Log("P17 physical receipt: "+Path.GetFullPath(file)+
                " state="+report.result+"; no Unity QA certification granted.");
        }
    }
}
