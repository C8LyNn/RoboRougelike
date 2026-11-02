# B01-23 BUILD-IMPACT-TELEMETRY-001 R1 — PASS17 W27

## Product behavior
Add the `BuildImpactTelemetry` component to a physical vehicle part that already has a Collider. Actual Unity PhysX `OnCollisionEnter/Stay` records collision impulse (N·s), relative velocity (m/s), first world contact and counter. It raises a read-only event for later garage diagnostic or damaged-part visualization integration. Existing vehicle dynamics, damage, structural graph, Save, weapon clearance and Codex source remain unchanged.

## Owner / integration
Source owner B01-23. Receiver B01-20 / Codex after B01-40 and B01-60 independent review. Target Unity 6000.3.14f1. Independent draft source branch; install ONLY this new Assets/B0123BuildImpactTelemetry001 namespace and assembly. No modification of existing game files, garage, canonical AI, #792 physical Save bridge, #787/#797 writer, #780 economy or #750 fixtures. No automatic receiver staging.

## Acceptance
1. Drop a real Rigidbody cube onto a non-trigger floor; at least one physical impact event with positive impulse and speed.
2. Event listener does not alter mass, kinematic state, velocities, health, Save or parts.
3. Reject negative/NaN/Infinity configuration.
4. Verify idle/zero-contact state has zero events; collision callback requires an actual Collider contact.
5. Test high-speed impact and many loose bodies manually for diagnostics/performance before production use.

## Gate and rollback
SOURCE: authored; C# compilation NOT_RUN; Unity EditMode NOT_RUN; Unity PlayMode NOT_RUN; Windows build NOT_RUN; human playtest NOT_RUN. No G0/G1/G2 PASS claimed. Independent reviewers inspect .asmdef paths, Unity .meta import GUIDs, assembly compilation and actual native test XML/log. Rollback: remove this isolated folder after checking no downstream consumer has adopted its public event; never alter existing game-owned data. This component is diagnostic only; it must not be wired to authoritative damage without a separately approved contract.

## Known limitations
`Collision.impulse` may be zero for resting or trigger contacts, and OnCollisionStay can emit multiple samples per physics step. Counter measures received callbacks, not distinct impacts. Collider identity is per-running-process only (never persist). No claim about contact force, damage, ownership or deterministic replay. `capacity` is reserved for future bounded sample history and presently unused; do not interpret it as an active cap.
