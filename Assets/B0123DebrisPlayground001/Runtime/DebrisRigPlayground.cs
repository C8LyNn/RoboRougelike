using UnityEngine;
using B01.DebrisRepulse001;

namespace B01.DebrisPlayground001
{
    /// <summary>
    /// Play-only physical playground. Does not discover or change canonical vehicles,
    /// damage, inventory or shared scenes. Two differently dimensioned physical rigs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebrisRigPlayground : MonoBehaviour
    {
        private sealed class Rig
        {
            internal RigDemoDefinition Definition;
            internal GameObject Root;
            internal GameObject Part;
            internal Rigidbody Body;
            internal DebrisRepulsionEmitter Emitter;
            internal Vector3 LocalMountPosition;
            internal bool Detached;
        }

        private readonly Rig[] rigs = new Rig[2];
        private bool built;

        private void Start()
        {
            if (!built) BuildDemo();
        }

        public void BuildDemo()
        {
            if (built) ResetDemo();
            for (int i = 0; i < rigs.Length; i++)
            {
                RigDemoDefinition spec = DebrisRigDemoCatalog.ByIndex(i);
                var root = new GameObject(spec.Name + " / owned demonstration root");
                root.transform.SetParent(transform, false);
                root.transform.position = new Vector3((float)spec.RootX, 0f, 0f);

                var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chassis.name = spec.Name + " chassis (kinematic)";
                chassis.transform.SetParent(root.transform, false);
                chassis.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                chassis.transform.localScale = new Vector3((float)spec.ChassisWidth, 0.8f, 1.0f);
                var chassisBody = chassis.AddComponent<Rigidbody>();
                chassisBody.isKinematic = true;
                chassisBody.useGravity = false;

                var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.name = spec.Name + " detachable real body";
                part.transform.SetParent(root.transform, false);
                Vector3 mount = new Vector3((float)spec.PartOffsetX, 1.2f, 0f);
                part.transform.localPosition = mount;
                part.transform.localScale = new Vector3((float)spec.PartWidth, 0.65f, 0.65f);
                var body = part.AddComponent<Rigidbody>();
                body.mass = (float)spec.PartMassKg;
                body.isKinematic = true;
                body.useGravity = false;
                body.linearDamping = 0f;
                body.angularDamping = 0f;
                body.constraints = RigidbodyConstraints.FreezeRotation;

                var emitter = root.AddComponent<DebrisRepulsionEmitter>();
                rigs[i] = new Rig {
                    Definition = spec, Root = root, Part = part,
                    Body = body, Emitter = emitter, LocalMountPosition = mount
                };
            }
            built = true;
        }

        public Rigidbody PartBody(int index) => RequireRig(index).Body;
        public bool IsDetached(int index) => RequireRig(index).Detached;
        public long PartId(int index) => RequireRig(index).Definition.PartId;
        public int AppliedCount(int index) => RequireRig(index).Emitter.AppliedCount;

        public bool Detach(int index)
        {
            Rig rig = RequireRig(index);
            if (rig.Detached) return false;
            rig.Part.transform.SetParent(null, true);
            rig.Body.isKinematic = false;
            rig.Body.linearVelocity = new Vector3((float)rig.Definition.InitialRadialSpeed, 0f, 0f);
            Vector3 ownerCenter = rig.Root.transform.position + new Vector3(0f, 1.2f, 0f);
            if (!rig.Emitter.TryApplyDetachImpulse(rig.Definition.PartId, rig.Body, ownerCenter))
            {
                rig.Body.linearVelocity = Vector3.zero;
                rig.Body.isKinematic = true;
                rig.Part.transform.SetParent(rig.Root.transform, false);
                rig.Part.transform.localPosition = rig.LocalMountPosition;
                return false;
            }
            rig.Detached = true;
            return true;
        }

        public bool Reattach(int index)
        {
            Rig rig = RequireRig(index);
            if (!rig.Detached) return false;
            // W10 v2: THIS public API is the mandatory physical dock boundary.
            // It cannot be bypassed by W07 GUI, W08 safe adapter, W09 UI, or external scripts.
            // No body, parent, emitter or ID mutation on denial.
            if (!ValidatePhysicalReattach(rig)) return false;
            rig.Body.linearVelocity = Vector3.zero;
            rig.Body.angularVelocity = Vector3.zero;
            rig.Body.isKinematic = true;
            rig.Part.transform.SetParent(rig.Root.transform, false);
            rig.Part.transform.localPosition = rig.LocalMountPosition;
            rig.Detached = false;
            if (!rig.Emitter.RearmAfterReattach(rig.Definition.PartId))
                throw new System.InvalidOperationException("Missing detach ID at reattach");
            return true;
        }

        // W10 non-optional guard, in W07-owned runtime assembly: NO circular
        // asmdef dependency on newer W08/W09 callers and NO public guard delegate.
        private const int DockQueryCapacity = 64;
        private const float DockMaxReachMeters = 3f;
        private const float DockMaxPenetrationMeters = 0.005f;
        private readonly Collider[] dockQuery = new Collider[DockQueryCapacity];

        private bool ValidatePhysicalReattach(Rig rig)
        {
            if (rig == null || rig.Root == null || rig.Part == null ||
                rig.Body == null || rig.Emitter == null || !rig.Detached ||
                !rig.Part.activeInHierarchy || rig.Body.isKinematic)
                return false;
            var box = rig.Part.GetComponent<BoxCollider>();
            if (box == null || !box.enabled || box.isTrigger)
                return false;
            Transform owner = rig.Root.transform;
            Transform part = rig.Part.transform;
            Vector3 target = owner.TransformPoint(rig.LocalMountPosition);
            Quaternion rotation = owner.rotation;
            Vector3 scale = part.lossyScale;
            if (!Finite(target) || !Finite(scale) ||
                Mathf.Abs(scale.x) <= 0f || Mathf.Abs(scale.y) <= 0f ||
                Mathf.Abs(scale.z) <= 0f ||
                !Finite(box.size) || !Finite(box.center))
                return false;
            Vector3 half = Vector3.Scale(box.size,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;
            if (!Finite(half) || half.x <= 0f || half.y <= 0f || half.z <= 0f)
                return false;
            Vector3 center = target + rotation * Vector3.Scale(box.center, scale);
            if (!Finite(center) || !Finite(part.position))
                return false;
            float reach = Vector3.Distance(part.position, target);
            if (float.IsNaN(reach) || float.IsInfinity(reach) ||
                reach > DockMaxReachMeters)
                return false;

            // ALL physics layers and triggers included in the finite buffer.
            // We ignore known triggers in the narrowphase, but saturation is RED.
            // Never trust W08's user-editable layer mask for the public entrypoint.
            Physics.SyncTransforms();
            int count = Physics.OverlapBoxNonAlloc(center, half, dockQuery,
                rotation, ~0, QueryTriggerInteraction.Collide);
            if (count < 0 || count >= DockQueryCapacity)
                return false;
            for (int i = 0; i < count; i++)
            {
                Collider other = dockQuery[i];
                if (other == null) return false;
                if (other == box || other.attachedRigidbody == rig.Body ||
                    other.isTrigger)
                    continue;
                bool supported = other is BoxCollider || other is SphereCollider ||
                                 other is CapsuleCollider ||
                                 (other is MeshCollider mesh && mesh.convex);
                if (!other.enabled || !supported)
                    return false;
                Vector3 direction;
                float depth;
                bool overlapping = Physics.ComputePenetration(box, target, rotation,
                    other, other.transform.position, other.transform.rotation,
                    out direction, out depth);
                if (!Finite(direction) || float.IsNaN(depth) ||
                    float.IsInfinity(depth) || depth < 0f)
                    return false;
                if (overlapping && depth > DockMaxPenetrationMeters)
                    return false;
            }
            return true;
        }

        private static bool Finite(Vector3 p) =>
            !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) &&
            !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z);

        public void ResetDemo()
        {
            for (int i = 0; i < rigs.Length; i++)
            {
                Rig rig = rigs[i];
                if (rig == null) continue;
                if (rig.Part != null && rig.Part.transform.parent == null) DestroyOwned(rig.Part);
                if (rig.Root != null) DestroyOwned(rig.Root);
                rigs[i] = null;
            }
            built = false;
        }

        private void OnDestroy() => ResetDemo();

        private Rig RequireRig(int index)
        {
            if (!built || index < 0 || index >= rigs.Length || rigs[index] == null)
                throw new System.InvalidOperationException("BuildDemo first and use rig index 0 or 1");
            return rigs[index];
        }

        private static void DestroyOwned(GameObject obj)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void OnGUI()
        {
            if (!built) return;
            GUILayout.BeginArea(new Rect(15, 15, 400, 180), GUI.skin.box);
            GUILayout.Label("B01-23 / DEBRIS PHYSICS PLAYGROUND 001");
            for (int i = 0; i < 2; i++)
            {
                Rig rig = rigs[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(rig.Definition.Name + " / " + rig.Definition.PartMassKg + "kg");
                if (GUILayout.Button("Detach")) Detach(i);
                if (GUILayout.Button("Reattach")) Reattach(i);
                GUILayout.EndHorizontal();
                GUILayout.Label("Part velocity X: " + rig.Body.linearVelocity.x.ToString("F2") +
                                " m/s; detached=" + rig.Detached);
            }
            if (GUILayout.Button("Reset both rigs")) { ResetDemo(); BuildDemo(); }
            GUILayout.EndArea();
        }
    }
}
