using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    public sealed class StructuralScanController : MonoBehaviour
    {
        [SerializeField] private StructuralScanSettings settings = new StructuralScanSettings();
        [SerializeField] private Transform sensor;
        [SerializeField] private Transform ownerRoot;
        [SerializeField] private StructuralScanRegistry registry;
        [SerializeField] private MonoBehaviour energySource;
        [SerializeField] private MonoBehaviour selfFilter;
        [SerializeField] private int ownerKey = 1;
        [SerializeField] private int teamKey = 1;
        [SerializeField] private bool operational = true;

        private readonly StructuralScanClock clock = new StructuralScanClock();
        private readonly List<StructuralScanContact> contacts = new List<StructuralScanContact>();
        private readonly List<StructuralScanContact> candidateContacts = new List<StructuralScanContact>();
        private readonly RaycastHit[] hitBuffer = new RaycastHit[128];
        private readonly Collider[] originBuffer = new Collider[128];
        private int selectedTargetOwner;
        private IStructuralScanEnergy energy;
        private IStructuralScanSelfFilter ownFilter;

        public bool Operational { get { return operational && isActiveAndEnabled; } }
        public bool IsReady { get { return Operational && clock.Ready; } }
        public bool HasVisibleContacts { get { return clock.Visible && contacts.Count > 0; } }
        public float CooldownLeft { get { return clock.CooldownLeft; } }
        public float DisplayLeft { get { return clock.VisibleLeft; } }
        public int SelectedTargetOwner { get { return HasVisibleContacts ? selectedTargetOwner : 0; } }
        public IReadOnlyList<StructuralScanContact> Contacts { get { if (HasVisibleContacts) return contacts; return EmptyContacts; } }
        private static readonly StructuralScanContact[] EmptyContacts = new StructuralScanContact[0];
        public event Action ResultsChanged;

        public void Bind(Transform newSensor, Transform newOwnerRoot, int newOwnerKey, int newTeamKey,
            StructuralScanRegistry newRegistry, MonoBehaviour newEnergy = null, MonoBehaviour newSelfFilter = null)
        {
            sensor = newSensor;
            ownerRoot = newOwnerRoot;
            ownerKey = newOwnerKey;
            teamKey = newTeamKey;
            registry = newRegistry;
            energySource = newEnergy;
            selfFilter = newSelfFilter;
            RefreshAdapters();
            ClearResults();
        }

        public void Configure(StructuralScanSettings newSettings)
        {
            if (newSettings == null) return;
            settings = newSettings;
            settings.Validate();
        }
        // Restore physical part readiness after a garage rebuild; never replay visibility.
        public void RestoreCooldown(float remaining)
        {
            if(float.IsNaN(remaining)||float.IsInfinity(remaining))remaining=0;
            clock.Start(Mathf.Clamp(remaining,0,settings.cooldownSeconds),0);ClearResults();
        }
        public void SetOperational(bool value)
        {
            operational = value;
            if (!value) ClearResults();
        }
        private void Awake() { RefreshAdapters(); settings.Validate(); }
        private void OnValidate() { if (settings != null) settings.Validate(); }
        private void OnDisable() { ClearResults(); }
        private void Update() { Advance(Time.deltaTime); }
        public void Advance(float dt)
        {
            bool wasVisible = HasVisibleContacts;
            clock.Advance(dt);
            if (wasVisible && !clock.Visible) ClearResults();
        }

        // Returns false without consuming energy/cooldown unless a visible foreign target exists.
        public bool TryScan()
        {
            if (!IsReady || registry == null || sensor == null || settings == null) return false;
            settings.Validate();
            if (registry.Count > settings.maxRegisteredParts) return false; // fail closed
            RefreshAdapters();
            if (settings.activationEnergy > 0f && energy == null) return false;

            IReadOnlyList<IStructuralScanPart> parts = registry.Parts;
            int targetOwner = 0;
            float targetDistance2 = float.PositiveInfinity;
            // Choose closest *visible* part in the cone. No knowledge of occluded parts leaks.
            for (int i = 0; i < parts.Count; ++i)
            {
                IStructuralScanPart part = parts[i];
                if (!Eligible(part) || !Visible(part)) continue;
                float d2 = (part.ScanPoint.position - sensor.position).sqrMagnitude;
                if (d2 < targetDistance2 - 0.0001f ||
                    (Mathf.Abs(d2 - targetDistance2) <= 0.0001f && (targetOwner == 0 || part.OwnerKey < targetOwner)))
                {
                    targetOwner = part.OwnerKey;
                    targetDistance2 = d2;
                }
            }
            if (targetOwner == 0) return false; // OwnerKey=0 reserved/unusable in this adapter.

            candidateContacts.Clear();
            for (int i = 0; i < parts.Count; ++i)
            {
                IStructuralScanPart part = parts[i];
                if (part != null && part.OwnerKey == targetOwner && Eligible(part) && Visible(part))
                    candidateContacts.Add(new StructuralScanContact(part));
            }
            if (candidateContacts.Count == 0) return false;
            candidateContacts.Sort(CompareContacts);
            if (candidateContacts.Count > settings.maxRevealedParts)
                candidateContacts.RemoveRange(settings.maxRevealedParts, candidateContacts.Count - settings.maxRevealedParts);

            if (settings.activationEnergy > 0f && !energy.TrySpend(settings.activationEnergy)) return false;

            contacts.Clear();
            contacts.AddRange(candidateContacts);
            selectedTargetOwner = targetOwner;
            clock.Start(settings.cooldownSeconds, settings.displaySeconds);
            ResultsChanged?.Invoke();
            return true;
        }

        private static int CompareContacts(StructuralScanContact a, StructuralScanContact b)
        {
            int score = b.Priority.CompareTo(a.Priority);
            return score != 0 ? score : a.PartKey.CompareTo(b.PartKey);
        }
        private bool Eligible(IStructuralScanPart part)
        {
            return part != null && part.ScanPoint != null && part.OwnerKey != 0 &&
                   part.OwnerKey != ownerKey && part.TeamKey != teamKey && part.IsAttached &&
                   StructuralScanMath.InCone(sensor.position, sensor.forward, part.ScanPoint.position,
                       settings.rangeMeters, settings.fullConeDegrees);
        }

        // The visible part must be the FIRST non-own, non-trigger solid collider encountered.
        private bool Visible(IStructuralScanPart part)
        {
            Vector3 origin = sensor.position;
            Vector3 delta = part.ScanPoint.position - origin;
            float distance = delta.magnitude;
            if (distance <= 0.0001f) return false;
            Vector3 direction = delta / distance;

            int overlapping = Physics.OverlapSphereNonAlloc(origin, 0.025f, originBuffer,
                settings.obstructionMask, QueryTriggerInteraction.Ignore);
            if (overlapping == originBuffer.Length) return false;
            for (int i = 0; i < overlapping; i++)
            {
                Collider c = originBuffer[i];
                if (c != null && !IsOwn(c) && !part.OwnsCollider(c)) return false;
            }

            int count = Physics.RaycastNonAlloc(origin, direction, hitBuffer, distance + 0.02f,
                settings.obstructionMask, QueryTriggerInteraction.Ignore);
            if (count == hitBuffer.Length) return false;
            float first = float.PositiveInfinity;
            bool firstIsTarget = false;
            for (int i = 0; i < count; ++i)
            {
                Collider c = hitBuffer[i].collider;
                if (c == null || IsOwn(c)) continue;
                float t = hitBuffer[i].distance;
                if (t < first - 0.0001f)
                {
                    first = t;
                    firstIsTarget = part.OwnsCollider(c);
                }
                else if (Mathf.Abs(t - first) <= 0.0001f && !part.OwnsCollider(c))
                {
                    firstIsTarget = false; // ambiguity must not reveal hidden target
                }
            }
            return firstIsTarget;
        }
        private bool IsOwn(Collider c)
        {
            return (ownerRoot != null && c.transform.IsChildOf(ownerRoot)) || (ownFilter != null && ownFilter.IsOwnCollider(c));
        }
        private void RefreshAdapters()
        {
            energy = energySource as IStructuralScanEnergy;
            ownFilter = selfFilter as IStructuralScanSelfFilter;
        }
        public void ClearResults()
        {
            bool changed = contacts.Count > 0;
            contacts.Clear();
            candidateContacts.Clear();
            selectedTargetOwner = 0;
            clock.ClearVisibility();
            if (changed) ResultsChanged?.Invoke();
        }
    }
}
