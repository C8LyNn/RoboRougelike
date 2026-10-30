using System;
using System.IO;
using System.Threading;
using UnityEngine;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;

namespace B01.SecureSaveHost001
{
    // ISOLATED physical Unity 6 demo host, NOT a mutation of #510 private Rig[].
    // Only controlled by its own AtomicColdUnityHostBridge (one Unity main thread).
    [DisallowMultipleComponent]
    public sealed class IsolatedSecureV2SaveHost : MonoBehaviour
    {
        private AtomicColdUnityHostBridge bridge;
        private int mainThread;
        private bool liveCommand;
        private const string DemoFilename = "b0123_secure_v2_atomic_host_demo.save";
        public long Generation => bridge == null ? 0 : bridge.Generation;
        public bool IsQuarantined => bridge != null && bridge.IsQuarantined;

        private void Awake()
        {
            mainThread = Thread.CurrentThread.ManagedThreadId;
            bridge = new AtomicColdUnityHostBridge(77, new UnityRigFactory(transform),
                () => !liveCommand);
        }

        private void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThread)
                throw new InvalidOperationException("Unity Save/Load only on owning main thread");
        }

        // A separate isolated Light/Heavy fixture. Other scenes and the private #510
        // controller are NOT mutated or implicitly migrated by initialization.
        public RestoreVerdict InitializeDemo(bool lightDetached, bool heavyDetached)
        {
            CheckThread();
            if (bridge.HasWorld) return RestoreVerdict.TransactionBusy;
            var parts = new PersistedPart[2];
            for (int i = 0; i < 2; i++)
            {
                bool detached = i == 0 ? lightDetached : heavyDetached;
                double x = i == 0 ? -2.3 : 5.3; // original frozen #510 rig catalog mounts
                var velocity = detached ? new Vector3d(.25, 0, 0) : new Vector3d(0, 0, 0);
                parts[i] = new PersistedPart(
                    FrozenPartCatalog.Id(i), FrozenPartCatalog.Owner(i),
                    detached ? 0 : FrozenPartCatalog.Owner(i),
                    FrozenPartCatalog.Asset(i), FrozenPartCatalog.Socket(i),
                    detached ? PartDockState.Detached : PartDockState.Attached,
                    detached, !detached,
                    new Vector3d(x, 1.2, 0), 0, 0, 0, 1,
                    velocity, new Vector3d(0,0,0), FrozenPartCatalog.Mass(i));
            }
            return bridge.TryLoad(SnapshotCodec.Encode(new RunSnapshot(77, 1, parts)), 1);
        }

        public RestoreVerdict TryCapture(out byte[] bytes)
        {
            CheckThread();
            return bridge.TrySave(out bytes);
        }

        public RestoreVerdict TryRestore(byte[] bytes, long minimumGeneration)
        {
            CheckThread();
            return bridge.TryLoad(bytes, minimumGeneration);
        }

        // Fixed dedicated demo slot only, never an arbitrary Codex or shared path.
        // Same-volume temp + Flush(true) + rename/replace is not a claim of a fully
        // crash-proof save on all filesystems: directory fsync is OS-dependent.
        public RestoreVerdict TryWriteDemoSlot()
        {
            CheckThread();
            RestoreVerdict verdict = bridge.TrySave(out byte[] bytes);
            if (verdict != RestoreVerdict.Accepted) return verdict;
            string target = Path.Combine(Application.persistentDataPath, DemoFilename);
            string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                using (var stream = new FileStream(temp, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                // No unrelated files or folders are deleted; never touch a user path.
                if (File.Exists(target)) File.Replace(temp, target, null);
                else File.Move(temp, target);
                return RestoreVerdict.Accepted;
            }
            catch (Exception e)
            {
                Debug.LogError("B01-23 atomic demo save slot refused: " + e.Message);
                return RestoreVerdict.Quarantine;
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp); // only our unique .tmp file
            }
        }

        public RestoreVerdict TryReadDemoSlot()
        {
            CheckThread();
            string target = Path.Combine(Application.persistentDataPath, DemoFilename);
            try
            {
                if (!File.Exists(target)) return RestoreVerdict.InvalidSnapshot;
                return TryRestore(File.ReadAllBytes(target), 1);
            }
            catch (Exception e)
            {
                Debug.LogError("B01-23 demo load refused: " + e.Message);
                return RestoreVerdict.Quarantine;
            }
        }

        private void OnDestroy()
        {
            if (bridge != null)
            {
                try { bridge.Close(); }
                catch (Exception e) { Debug.LogError("B01-23 demo host cleanup: " + e); }
            }
        }

        // Every world is assembled INACTIVE with fresh original W06 ledgers. Detached
        // body is rooted under the scenario container, NOT under the robot owner.
        // This fixture doesn't reattach through original #510 public guard; canonical
        // owner MUST provide a future transactional private AtomicDockLedger hydration.
        private sealed class UnityRigFactory : IIsolatedRigFactory
        {
            private readonly Transform host;
            public UnityRigFactory(Transform host) { this.host = host; }
            public IIsolatedRigWorld Build(ColdRestore plan)
            {
                GameObject root = new GameObject("B01-23 isolated atomic SaveV2 cold world");
                root.SetActive(false);
                try
                {
                    root.transform.SetParent(host, false);
                    return new UnityRigWorld(root, plan.Snapshot);
                }
                catch
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(root);
                    else UnityEngine.Object.DestroyImmediate(root);
                    throw;
                }
            }
        }

        private sealed class UnityRigWorld : IIsolatedRigWorld
        {
            private readonly GameObject root;
            private readonly GameObject[] chassis = new GameObject[2];
            private readonly Rigidbody[] body = new Rigidbody[2];
            private readonly DebrisImpulseLedger[] ledgers = new DebrisImpulseLedger[2];
            private bool busy;
            private bool disposed;
            public bool IsBusy => busy;
            public bool IsVisible => root != null && root.activeSelf;

            public UnityRigWorld(GameObject stageRoot, RunSnapshot state)
            {
                root = stageRoot;
                var parts = state.Parts;
                for (int i = 0; i < 2; i++)
                {
                    PersistedPart part = parts[i];
                    bool detached = part.State == PartDockState.Detached;
                    chassis[i] = new GameObject("owner " + part.OwnerId);
                    chassis[i].SetActive(false);
                    chassis[i].transform.SetParent(root.transform, false);
                    chassis[i].transform.localPosition = new Vector3(i == 0 ? -3.5f : 3.5f, 0, 0);

                    GameObject box = new GameObject("stable part " + part.Id);
                    box.SetActive(false);
                    box.transform.SetParent(detached ? root.transform : chassis[i].transform, false);
                    box.transform.position = Vec(part.Position);
                    box.transform.rotation = new Quaternion(
                        (float)part.Qx,(float)part.Qy,(float)part.Qz,(float)part.Qw);
                    var collider = box.AddComponent<BoxCollider>();
                    collider.size = new Vector3(i == 0 ? .5f : .7f,.65f,.65f);
                    var rb = box.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.mass = (float)part.MassKg;
                    rb.isKinematic = part.Kinematic;
                    rb.linearVelocity = Vec(part.LinearVelocity);
                    rb.angularVelocity = Vec(part.AngularVelocity);
                    body[i] = rb;

                    var ledger = new DebrisImpulseLedger();
                    if (detached)
                    {
                        var zero=new Vector3d(0,0,0);
                        if (!ledger.TryDetach(part.Id,zero,zero,zero,part.MassKg,0,0,
                                out Vector3d impulse) || impulse.MagnitudeSquared!=0)
                            throw new InvalidDataException("W06 consumed ID staging failure");
                    }
                    ledgers[i] = ledger;
                    chassis[i].SetActive(true);
                    box.SetActive(true); // hierarchy still disabled by root
                }
            }

            public RunSnapshot Capture(long runId,long generation)
            {
                if (disposed) throw new ObjectDisposedException("UnityRigWorld");
                var parts=new PersistedPart[2];
                for (int i=0;i<2;i++)
                {
                    Rigidbody rb=body[i];
                    long id=FrozenPartCatalog.Id(i);
                    bool consumed=ledgers[i].Contains(id);
                    bool detached = rb.transform.parent == root.transform;
                    if (!detached && rb.transform.parent != chassis[i].transform)
                        throw new InvalidDataException("part has foreign physical parent");
                    if (consumed!=detached) throw new InvalidDataException("physical/ledger ID divergence");
                    Vector3 pos=rb.position,linear=rb.linearVelocity,angular=rb.angularVelocity;
                    Quaternion rot=rb.rotation;
                    parts[i]=new PersistedPart(id,FrozenPartCatalog.Owner(i),
                        detached?0:FrozenPartCatalog.Owner(i),
                        FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                        detached?PartDockState.Detached:PartDockState.Attached,
                        consumed,rb.isKinematic,V3(pos),rot.x,rot.y,rot.z,rot.w,
                        V3(linear),V3(angular),rb.mass);
                }
                return new RunSnapshot(runId,generation,parts);
            }

            public void SetVisible(bool visible)
            {
                if(disposed) throw new ObjectDisposedException("UnityRigWorld");
                root.SetActive(visible);
            }
            public void Dispose()
            {
                if(disposed)return;
                disposed=true;
                root.SetActive(false);
                if(Application.isPlaying) UnityEngine.Object.Destroy(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
            private static Vector3 Vec(Vector3d p)=>new Vector3((float)p.X,(float)p.Y,(float)p.Z);
            private static Vector3d V3(Vector3 p)=>new Vector3d(p.x,p.y,p.z);
        }
    }
}
