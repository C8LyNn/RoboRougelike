using System;
using System.Collections.Generic;
using UnityEngine;
using B01.DebrisPlayground001;
using B01.ReattachClearance001;

namespace B01.DockHardening001
{
    /// <summary>
    /// Opt-in W09 Unity 6 demo hardening: hides the W07/W08 unguarded buttons,
    /// audits *all* PhysX layers at the target mounting pose before W08 Inspect,
    /// then invokes unchanged W07 Reattach only if BOTH checks allow.
    /// NOT a canonical robot reattachment integration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HardenedDockDemo : MonoBehaviour
    {
        private const int QueryCapacity = 64;
        private const float MaxDistance = 3f;
        private const float MaxPenetration = 0.005f;
        private readonly Collider[] candidates = new Collider[QueryCapacity];
        private readonly List<ContactObservation> observations = new List<ContactObservation>(QueryCapacity);
        private DebrisRigPlayground demo;
        private DebrisSafeReattachGate previousGate;
        private bool armed;

        public bool Armed => armed;
        public HardenedDockVerdict LastVerdict { get; private set; } = HardenedDockVerdict.NotArmed;

        private void Start() => Arm();

        public bool Arm()
        {
            if (armed) return true;
            demo = GetComponent<DebrisRigPlayground>();
            previousGate = GetComponent<DebrisSafeReattachGate>();
            if (demo == null || previousGate == null)
            {
                Debug.LogError("W09 safe dock requires original W07 and W08 components on the same isolated demo object");
                return false;
            }
            // Demo must be built before disabling its MonoBehaviour.OnGUI/Start.
            // Avoid resetting an existing detached demo if already initialized.
            try { demo.PartBody(0); }
            catch (InvalidOperationException) { demo.BuildDemo(); }
            demo.enabled = false;
            previousGate.enabled = false;
            armed = true;
            return true;
        }

        private void Update()
        {
            if (armed)
            {
                // Prevent a manually re-enabled old GUI from bypassing the guarded path.
                if (demo != null && demo.enabled) demo.enabled = false;
                if (previousGate != null && previousGate.enabled) previousGate.enabled = false;
            }
        }

        public HardenedDockVerdict Inspect(int index)
        {
            if (demo == null || previousGate == null || !armed)
                return LastVerdict = HardenedDockVerdict.NotArmed;
            if (demo.enabled || previousGate.enabled)
                return LastVerdict = HardenedDockVerdict.UnsafeLegacyUi;
            if (index < 0 || index > 1)
                return LastVerdict = HardenedDockVerdict.NewAuditRejected;

            DockVerdict fullLayerResult = AuditEveryLayer(index);
            if (fullLayerResult != DockVerdict.Allowed)
                return LastVerdict = HardenedDockPolicy.Evaluate(
                    armed, demo.enabled, previousGate.enabled, HardenedDockPolicy.AllPhysicsLayers,
                    fullLayerResult, DockVerdict.InvalidInput);
            DockVerdict oldResult = previousGate.Inspect(demo, index);
            return LastVerdict = HardenedDockPolicy.Evaluate(
                armed, demo.enabled, previousGate.enabled, HardenedDockPolicy.AllPhysicsLayers,
                fullLayerResult, oldResult);
        }

        public bool TryDock(int index)
        {
            if (Inspect(index) != HardenedDockVerdict.Allowed)
                return false;
            // No persistence, teleport when blocked, or ledger mutation until both gates pass.
            return demo.Reattach(index);
        }

        private DockVerdict AuditEveryLayer(int index)
        {
            if (!demo.IsDetached(index)) return DockVerdict.NotDetached;
            Rigidbody body = demo.PartBody(index);
            if (body == null || body.isKinematic)
                return DockVerdict.InvalidInput;
            BoxCollider ownBox = body.GetComponent<BoxCollider>();
            if (ownBox == null || !ownBox.enabled)
                return DockVerdict.InvalidInput;
            RigDemoDefinition spec = DebrisRigDemoCatalog.ByIndex(index);
            Vector3 target = new Vector3((float)(spec.RootX + spec.PartOffsetX), 1.2f, 0f);
            Vector3 scale = body.transform.lossyScale;
            if (!Finite(scale.x) || !Finite(scale.y) || !Finite(scale.z) ||
                scale.x == 0 || scale.y == 0 || scale.z == 0)
                return DockVerdict.InvalidInput;
            Vector3 absolute = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Vector3 half = Vector3.Scale(ownBox.size, absolute) * 0.5f;
            if (!Finite(half.x) || !Finite(half.y) || !Finite(half.z) ||
                half.x <= 0 || half.y <= 0 || half.z <= 0)
                return DockVerdict.InvalidInput;
            Vector3 center = target + Vector3.Scale(ownBox.center, scale);
            Physics.SyncTransforms();
            int count = Physics.OverlapBoxNonAlloc(center, half, candidates,
                Quaternion.identity, HardenedDockPolicy.AllPhysicsLayers,
                QueryTriggerInteraction.Ignore);
            observations.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider other = candidates[i];
                if (other == null)
                {
                    observations.Add(new ContactObservation(false,false,false,double.NaN));
                    continue;
                }
                bool own = other == ownBox || other.attachedRigidbody == body;
                bool supported = other is BoxCollider || other is SphereCollider ||
                                 other is CapsuleCollider ||
                                 (other is MeshCollider mesh && mesh.convex);
                double depth = 0;
                if (!own && !other.isTrigger && supported)
                {
                    if (Physics.ComputePenetration(ownBox, target, Quaternion.identity,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 _, out float overlap))
                        depth = overlap;
                }
                observations.Add(new ContactObservation(own,other.isTrigger,supported,depth));
            }
            return DockClearancePolicy.Evaluate(true,!body.isKinematic,
                Vector3.Distance(body.transform.position,target),
                MaxDistance,count,QueryCapacity,MaxPenetration,observations);
        }

        private static bool Finite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        // Original two GUIs remain disabled: this panel is the sole demo UI when armed.
        private void OnGUI()
        {
            if (!armed) return;
            GUILayout.BeginArea(new Rect(15,15,430,230),GUI.skin.box);
            GUILayout.Label("B01-23 / W09 PROTECTED DOCK DEMO");
            for (int i = 0; i < 2; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(i == 0 ? "Light 1.5kg" : "Heavy 12kg");
                if (GUILayout.Button("Detach")) demo.Detach(i);
                if (GUILayout.Button("SAFE reattach")) TryDock(i);
                GUILayout.EndHorizontal();
                GUILayout.Label("Detached=" + demo.IsDetached(i));
            }
            if (GUILayout.Button("Reset isolated demo"))
            {
                demo.ResetDemo();
                demo.BuildDemo();
                LastVerdict = HardenedDockVerdict.NotArmed;
            }
            GUILayout.Label("Dock verdict: " + LastVerdict);
            GUILayout.EndArea();
        }
    }
}
