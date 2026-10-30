using System.Collections.Generic;
using UnityEngine;
using B01.DebrisPlayground001;

namespace B01.ReattachClearance001
{
    /// <summary>
    /// Opt-in safe docking path for the isolated W07 two-rig demonstration.
    /// Does not modify, patch or automatically intercept W07's original Reattach UI.
    /// A Codex host may adopt this contract after independently authorized physics QA.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebrisSafeReattachGate : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 50f)] private float maximumReachMetres = 3f;
        [SerializeField, Range(0f, 0.05f)] private float penetrationToleranceMetres = 0.005f;
        [SerializeField] private LayerMask collisionLayers = ~0;
        private const int Capacity = 64;
        private readonly Collider[] overlapBuffer = new Collider[Capacity];
        private readonly ContactObservation[] contacts = new ContactObservation[Capacity];
        private readonly List<ContactObservation> collected = new List<ContactObservation>(Capacity);

        public DockVerdict LastVerdict { get; private set; } = DockVerdict.InvalidInput;

        public DockVerdict Inspect(DebrisRigPlayground demo, int index)
        {
            if (demo == null || index < 0 || index > 1)
                return LastVerdict = DockVerdict.InvalidInput;
            if (!demo.IsDetached(index))
                return LastVerdict = DockVerdict.NotDetached;

            Rigidbody body = demo.PartBody(index);
            if (body == null || body.isKinematic)
                return LastVerdict = DockVerdict.InvalidInput;

            BoxCollider box = body.GetComponent<BoxCollider>();
            if (box == null || !box.enabled)
                return LastVerdict = DockVerdict.InvalidInput;

            RigDemoDefinition rig = DebrisRigDemoCatalog.ByIndex(index);
            Vector3 targetPosition = new Vector3(
                (float)(rig.RootX + rig.PartOffsetX), 1.2f, 0f);
            Quaternion targetRotation = Quaternion.identity;

            Vector3 scale = body.transform.lossyScale;
            if (!Valid(scale.x) || !Valid(scale.y) || !Valid(scale.z) ||
                Mathf.Abs(scale.x) <= 0f || Mathf.Abs(scale.y) <= 0f ||
                Mathf.Abs(scale.z) <= 0f)
                return LastVerdict = DockVerdict.InvalidInput;

            Vector3 targetColliderCenter = targetPosition +
                                           targetRotation * Vector3.Scale(box.center, scale);
            Vector3 boxSize = Vector3.Scale(box.size,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            Vector3 halfSize = boxSize * 0.5f;
            if (!Valid(halfSize.x) || !Valid(halfSize.y) || !Valid(halfSize.z) ||
                halfSize.x <= 0f || halfSize.y <= 0f || halfSize.z <= 0f)
                return LastVerdict = DockVerdict.InvalidInput;

            float reach = Vector3.Distance(body.transform.position, targetPosition);
            Physics.SyncTransforms();
            int count = Physics.OverlapBoxNonAlloc(targetColliderCenter, halfSize,
                overlapBuffer, targetRotation, collisionLayers, QueryTriggerInteraction.Ignore);

            collected.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider other = overlapBuffer[i];
                bool own = other == box ||
                    (other != null && other.attachedRigidbody == body);
                if (other == null)
                {
                    collected.Add(new ContactObservation(false, false, false, double.NaN));
                    continue;
                }

                bool supported = other is BoxCollider ||
                    other is SphereCollider ||
                    other is CapsuleCollider ||
                    (other is MeshCollider mesh && mesh.convex);
                double depth = 0.0;
                if (!own && !other.isTrigger && supported)
                {
                    bool penetrating = Physics.ComputePenetration(box,
                        targetPosition, targetRotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 _, out float penetration);
                    if (penetrating)
                        depth = penetration;
                }

                collected.Add(new ContactObservation(own, other.isTrigger, supported, depth));
            }

            return LastVerdict = DockClearancePolicy.Evaluate(
                true, !body.isKinematic, reach, maximumReachMetres,
                count, Capacity, penetrationToleranceMetres, collected);
        }

        public bool TrySafeReattach(DebrisRigPlayground demo, int index)
        {
            if (Inspect(demo, index) != DockVerdict.Allowed)
                return false;
            // Only W07's original method mutates parent/velocity/identity.
            return demo.Reattach(index);
        }

        private static bool Valid(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private void OnGUI()
        {
            DebrisRigPlayground demo = GetComponent<DebrisRigPlayground>();
            if (demo == null)
                return;
            GUILayout.BeginArea(new Rect(15, 205, 410, 110), GUI.skin.box);
            GUILayout.Label("W08 CONTACT-SAFE REATTACH (isolated, opt-in)");
            for (int i = 0; i < 2; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(i == 0 ? "Light" : "Heavy");
                if (GUILayout.Button("Check + safe reattach"))
                    TrySafeReattach(demo, i);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("Gate verdict: " + LastVerdict);
            GUILayout.EndArea();
        }
    }
}
