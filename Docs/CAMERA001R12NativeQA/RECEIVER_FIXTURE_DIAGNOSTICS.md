# Bounded follow-up of receiver QA observations

The first complete Windows run at receiver `2236e08fcadaf55cecb6e094b94a1691effc125a` exited **2**. All eight rows completed; original data retained. Light at both resolutions exceeded the receiver muzzle-angle check. Core CAMERA final-ray/continuity/overlap checks stayed zero violations. Do not count this run as a total PASS.

Read-only peer review identified receiver instrumentation limitations:

- End-of-frame `ModuleAimPoint()` can differ from the target consumed before firing in controller LateUpdate. Record both aim points, first-hit identities, angles and per-gun data in JSONL. Retain the old post-fire angle. Add a synchronized pre-LateUpdate aim sample and enforce **0.05 degrees** (stricter than the original receiver's 0.1) against that point; do not modify producer tests.
- Count only actual active `P17PhysicalShell` bodies owned by the tested actor with a matching `R4ProjectileOrigin.PartId`; do not treat arbitrary fast debris as a shot.
- Require observed LMB, RMB and W exposure plus >1m physical displacement for the drive/input fixture. Record per-shot positions, directions, velocities, owner and part.
- Explicitly ignore triggers for visible-hull rays. Independent origin-inside/collider and conservative near-plane checks remain unchanged.
- Propagate failing native/test outcomes through the outer scripts, alongside original native exit receipts.

One bounded additional Windows run is warranted by these evidence issues. The producer's source, thresholds and original 5 EditMode/13 PlayMode cases remain byte-identical. No generator, game, weapon, physics, camera implementation or RC1 edits. The strict muzzle check may still fail; preserve and diagnose, never silently remove it.
