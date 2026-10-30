# B01-62 — PASS17 WAVE09 — REATTACH-ROUTE-INTAKE-001/R1

Independent original-source **cross-package integration acceptance gate** for B01-23 W06 #421 (DEBRIS-REPULSE), W07 #439 (two real Rigidbody rigs), and W08 #455 (opt-in REATTACH-CLEARANCE). No code, meta GUID, gameplay or Codex source is modified.

## Concrete integration blocker (expected original source RED)

The existing W07 physical demo exposes a GUI "Reattach" button that directly calls its own public Reattach(i), without a collision or reach check. Original Reattach sets rigidbody velocity to zero, makes the part kinematic, parents it back to the chassis, and **rearms the original consumed stable-part ID**. W08 adds a *separate* safe GUI and DebrisSafeReattachGate.TrySafeReattach, which runs real Physics.OverlapBoxNonAlloc and Physics.ComputePenetration and calls original Reattach only after DockVerdict.Allowed. The W08 editor installer merely Undo.AddComponent of the gate. **It neither replaces nor disables W07's original GUI call.**

Thus, even if the optional W08 gate is present, a user operating the original W07 GUI can still run an unguarded path. Original W08 owner itself discloses this limitation. This independently executable source-intake gate converts the known integration limitation into a reproducible **SOURCE_INTEGRATION_HOLD**, not a fabricated native crash, not a core math defect and not a claim that #455's guarded method is broken. It also explains why a future save/restore contract for consumed IDs (#457) must be coordinated with successful validated reattachment and run reset.

## Five immutable original sources and checks

The independent Python 3.12 tool reads *actual Git source object bytes* from:
- W06 DEBRIS #421 exact cb2b3a76e84266e2730fffc4ab048c1039f0652f: Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs, Git blob fa1aa5e531927cbe73cf5fdfe161b4a981316db5.
- W07 physical demo #439 exact cdcaea750fda1a9d7db6018e34970f4b9f206427: Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs, Git blob e4e31c32cb5abda2fef987efa4b45fd61e828312.
- W08 safe-gate #455 exact 99dfbd25d471f9983f9472b12d3725f4c002f7e9: DebrisSafeReattachGate.cs blob 7c7e96a10d69149119309863a0c2a9bdab829072, DockClearancePolicy.cs blob de0a847dbecacb92ad63a09ca12f5c9c372d597a, SafeReattachDemoInstaller.cs blob 5892f7e31d1e9fa07c124d63324c796b5abc282b.

The tool verifies Git source-blob IDs against raw bytes with git show + git rev-parse. It inspects complete original C# method bodies using comment/string-aware masking and balanced braces rather than a repository-wide grep. It follows W07 original direct GUI path and state rearm, confirms the presence of W08 safe control/installer/policy path, and refuses an inconclusive proof if owner method shape/safety checks have drifted.

**16 executable, independently authored positive/negative tests per OS**: original source Git bytes, direct W07 UI bypass, rearming ID, W08 real Physics API calls and Inspect guard, W08 GUI control and installer opt-in, in-memory disabled-GUI control, commented fake direct calls, string fake direct calls, hidden direct call with changed index, removal/reversal of guarded conditional, missing penetration check, missing ID rearm, unsafe change to new GUI, missing AddComponent, deterministic source-red receipt. Tests are source-level and use no fake Unity passes.

Windows2025 and Ubuntu24.04 physically execute Python 3.12 tests and generate actual deterministic receipt.json. A separate CI job compares the exact bytes across both OS and captures SHA256. No source original C# assembly build is claimed for this package; producer #455 already compiles its pure DockClearancePolicy C#9 at author SHA.

## Reproduction

In a read-only checkout with Python 3.12 and Git:

    git fetch --no-tags origin cb2b3a76e84266e2730fffc4ab048c1039f0652f
    git fetch --no-tags origin cdcaea750fda1a9d7db6018e34970f4b9f206427
    git fetch --no-tags origin 99dfbd25d471f9983f9472b12d3725f4c002f7e9
    python -m unittest discover -s Tools/B01/B01-62/P17W09ReattachRouteIntake -p test_intake.py -v
    mkdir -p /tmp/b01-62-w09-reattach-evidence
    python Tools/B01/B01-62/P17W09ReattachRouteIntake/intake.py --repo . --output /tmp/b01-62-w09-reattach-evidence/receipt.json --expect-hold

Use a NEW output path. The tool never modifies either original producer source tree or actual gameplay/checkpoints.

## Acceptance and action required

**Source verdict:** expected B01-62 test PASS means a reproducible upstream **SOURCE_INTEGRATION_HOLD**, not a GREEN end-to-end reattach mechanic. B01-23 should own a separately versioned actual wiring change to eliminate W07 unguarded Reattach UI, or route all user-controlled reattach entrypoints through the W08 gate; do NOT edit frozen #421, #439 or #455 SHA. B01-40: confirm physical exact-source Git identities, ZIP/GUID/asmdef and fail-closed integration gate. B01-60/61: independently check call graph and native Unity PhysX/PlayMode on licensed isolated host before approving. B01-00 controls later Codex #79 routing; Codex ACK and integration are not inferred.

**Critical limits:** This checks *only source method shape and calls* and intentionally does NOT compile original W07+W08 C# together, launch Unity, measure overlapping colliders, or assert Player functionality. The static parser is not a general Roslyn semantic analyzer and does not prove that any future alternate entrypoint is safe. Owner must independently execute native Unity tests and verify there are no other exposed bypasses. Unity G1/G2 NOT_RUN; no merge. Rollback = close this Draft and delete only isolated B01-62 QA folder/workflow and self-created scratch receipt, never touch Codex or genuine saves.
