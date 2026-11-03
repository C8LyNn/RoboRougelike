# B01-21 MAP-JUMPPAD-001 | PASS17 W27 R1

Independent Unity 6000.3.14f1 grid footprint 1m x 1m module. OnTriggerEnter + real Rigidbody.AddForce(ForceMode.VelocityChange); configurable up/forward velocity change and per-Rigidbody cooldown. Explicit SetActive and acceptedLayers. Multi-collider body cannot receive duplicate launch before exit; no map generator/SAFEROUTE, robot AI or Save writes.

Unity menu **B01 > Map Jump Pad 001 > Generate Prefab And Demo Scene** generates real 1x1m prefab and test scene in this package folder; they are not yet physical source artifacts in this Draft until Unity Editor runs. The grid pivot is at floor height; a child is visual only, physics root uses single trigger collider. Generated scene contains a 10kg real Rigidbody test vehicle.

Three true Unity PlayMode [UnityTest] scripts authored: heavy fall launches, disabled pad denies, duplicate multi-collider denial. NATIVE UNITY G1 NOT_RUN; prefab/scene generation NOT_RUN. Independent G0/real Unity import required before Codex handoff. Physics tuning for heavy vehicles, stacked pads, exits after Destroy and high-velocity tunnels requires native manual and stress QA.

Ownership B01-21 review, B01-40/61 independent G0, B01-60 native G1; HQ #800. Only own exclusive Assets/B01Packages/MapJumpPad001/**. Rollback strictly this folder in scratch, never touch MAPGEN or other empty user directories.
