# B01-30 / P17 W32 — independent v5 visual QA source+native fixture

Base original B01-31 [PR #904](https://github.com/C8LyNn/RoboRougelike/pull/904) exact `ba92a52a9bb73fb63b17e94a3af4a199868ef92e`, parent #893. **10 ADD-only owned QA files**; no owner production source edited.

## P0 defect
Original W31 `B0131W30CreateR41V5AdapterCandidate.cs` still uses `copy.transform.SetParent(pitch, true)` on entire imported FBX (carried from #893). This imparts barrel pitch to the whole fixed mount/base. W31 path and preclone safety fixes do not address rig segmentation.

The two original Unity EditMode test sources invoke the actual SHA-pinned importer candidate with the ORIGINAL external v5 FBX. They check that full source visual root is outside pitch and remains static over four elevation angles. **Native tests NOT_RUN; expected to fail on current producer.** A missing FBX is NUnit INCONCLUSIVE, not PASS. Real FBX SHA256 expected by original producer: `fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533`.

A read-only Python verifier checks upstream exact Git source, 10 own ADD-only files, GUID uniqueness across repository, and expected source P0 negative witness. Green CI = defect correctly detected, **NOT** product/Unity PASS.

**Fix owner:** B01-31 must segment authentic fixed mount vs yaw visual vs pitch-only barrels, then B01-60 executes real imported FBX/12 poses and Windows Player/real physical dual-fire tests. Keep original working turret fallback. No merge, release, Codex #873/#877 modification or fake model.
