# B01-40 W16 / exclusive-owner receiver evidence

**Order of precedence:** [HQ issue #276](https://github.com/C8LyNn/RoboRougelike/issues/276) over earlier #71/#79 wave advice.

**Verified before implementation (2026-10-27):** Only substantive new authoring source streams are `PARTSIZE-001 R1` / B01-23 / #141 and `CHL-003 R1` / B01-20 / #91. Current GitHub open-PR list did not yet include a frozen producer Draft for either W16 package; therefore **there is no producer ZIP/SHA to independently hash**. `CODEX INBOX #79` comment `6436974097` reserves older ECONREBUILD #252, GUARD #263, GARAGE-WARNINGS #266 under `RESERVED_FOR_CODEX / ACK_PENDING` during Codex #275, **not** proven CODEX_CLAIMED. Codex is responsible for #275 gameplay fixes and #273 Windows exit, not these two new isolated producer trees unless future direct claim evidence arrives.

## Actual independent work in this PR

`gate.py`: read-only actual-ZIP+external receipt+optional actual local Git-object/source-blob and collision/Unity `.meta`/asmdef/owner-scope validation with enforced fail-closed ownership state. Enforces two current package IDs only, pinned producers and safe path roots. Defends against byte tampering, forged file-list SHA, incomplete ZIP, unsafe Windows names, traversal/symlink, duplicate GUID or case-insensitive path, mismatched Git blobs/commit SHA, overwritten occupied target path or foreign GUID and false CODEX_ACK. Valid SOURCE pass does **not** promote native test or Codex acceptance.

Runnable Python unittest suite constructs real physical **synthetic QA fixtures** for the two specific package shapes and one real temporary Git commit, and triggers positive and many malicious mutation cases. Source tests do not imply functional PARTSIZE/CHL readiness. Source authoring remains exclusively B01-23/B01-20.

## Finite next handoff (only after real producer outputs)

**PARTSIZE (1):** B01-23 publishes isolated frozen source ZIP with measured colliders / orientations / physical envelope demo and raw native Unity Edit/Play plus negative contacts, submits exact SHA+manifest; B01-40 runs this gate on the *actual* archive and reversible worktree preflight; B01-60 independently reproduces overlap/rotation case. **Native not yet verified.**

**CHL (2):** B01-20 publishes full isolated scene with 3–5 ordered checkpoints, fall/retry/pause, two physically distinct rigidbody rigs and real trace; B01-40 original ZIP/ASM/GUID/rollback; B01-60 independently drives both rigs with seed/time/actual collision telemetry. **Native not yet verified.**

**Never send G2/Codex integration automatically.** The gate is safe to re-run on any immutable revision; a subsequent change must receive a **new version and full SHA**, not silently mutate a frozen artifact.

**Historical public Codex game tree read-only evidence:** original integrated gameplay commit `88b8eed60345c49481648833e1fde6dc1a400258` (source referenced by Codex #272) has a nontruncated GitHub REST tree with 2,658 blobs and **zero files in either newly reserved PARTSIZE/CHL root**. This does **NOT** provide present private Codex checkout SHA, dirty state or CODEX_ACK; target gate remains PENDING.
