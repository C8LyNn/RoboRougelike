# B01-23 / W26 PART.P3 — SOURCE-PINNED NATIVE RECEIPT / READ-ONLY

**Not a gameplay package, Codex adapter, Unity implementation, source rework or G2 handoff.** Owner B01-23. [HQ dependency queue #297](https://github.com/C8LyNn/RoboRougelike/issues/297) requires no speculative PARTSIZE changes while B01-40 original physical package/source and rollback G0 already pass at the EXACT SHA.

## Immutable input and dependency proof

- Frozen **observed** producer [PARTSIZE Draft PR #286](https://github.com/C8LyNn/RoboRougelike/pull/286): `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`. Producer still **IN_PROGRESS / NOT OFFERED** — this receipt does not freeze or claim ownership.
- Original complete GitHub Actions [run #40484486467](https://github.com/C8LyNn/RoboRougelike/actions/runs/40484486467), [physical source artifact #12725235814](https://github.com/C8LyNn/RoboRougelike/actions/runs/40484486467/artifacts/12725235814). Original **inner** source ZIP 41 entries/112,237 bytes SHA256 `05b86bf90ba2b1a71ce32b081b91e2f24793ea81cb00f2025757c5c017d95f54`. Do not hash the outer GitHub artifact wrapper as though it were the inner source archive.
- Independent B01-40 [#141 original source/Git/rollback report](https://github.com/C8LyNn/RoboRougelike/issues/141#issuecomment-6438945311): original **40/40** Git blob/source checks, **17 GUID**, temporary disposable 41/41 stage+rollback PASS. **This does not imply target-live G0.**
- B01-60 [#286 read-only verification](https://github.com/C8LyNn/RoboRougelike/pull/286#issuecomment-6438839917) actual source contract 8/8 PASS; **native Unity NOT_RUN**. Current 17 EditMode and 11 PlayMode are authored, not executed. No licensed Unity activation available in producer GitHub CI.

## Actual executable operator procedure (Windows; manual disposable checkout only)

The external script `Run-UnityNative.ps1` DOES NOT stage or edit PARTSIZE sources, never touches Codex's active checkout, does not invoke G2 and refuses a wrong SHA, missing explicit disposable marker, tracked dirty source, wrong Unity project version or missing Editor. It writes immutable input identifiers plus **actual process exit codes**, raw original NUnit `EditMode.xml`/`PlayMode.xml`, corresponding full Editor logs and SHA256 into `receipt.json` OUTSIDE the project.

1. From an **independent authorized QA checkout**, fetch original producer commit `be2be0e4802bc3f450179901f9ae91c6d84bcb5b`, then create a genuine **linked, detached Git worktree** for this source (an ordinary detached clone, attached worktree, or Git folder with forged marker is now rejected). Do **not** create the worktree from Codex's active worktree. Example from a separate QA repository:

```powershell
git fetch --no-tags origin be2be0e4802bc3f450179901f9ae91c6d84bcb5b
git worktree add --detach "C:\QA\PartsizeDisposable" be2be0e4802bc3f450179901f9ae91c6d84bcb5b
New-Item -ItemType File -Path "C:\QA\PartsizeDisposable\.b01-60-disposable-qa"
```

2. Verify this is a fresh worktree with a `.git` **file**, registered in `git worktree list --porcelain` as `detached` at the exact SHA, Unity project version `6000.3.14f1`, and **no tracked edits or untracked/ignored files within Unity source roots**. Generated `Library/`, `Temp/`, `Logs/`, `obj/` and `.vs/` cache paths may exist. A used worktree with `.b01-60-disposable-qa.used` cannot run a second native session. Choose an evidence path **outside** the project (neither equal to nor within it; symlinked path components are rejected).
3. From a separate checkout of THIS read-only QA-only branch, run the operator's **real** PowerShell preflight before any Editor launch. The `-PreflightOnly` switch does not invoke Unity or consume the worktree:

```powershell
pwsh -File "Tools/B01/PARTSIZE001/W26Receipt/Run-UnityNative.ps1" -UnityEditor "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" -ProjectPath "C:\QA\PartsizeDisposable" -EvidenceDir "C:\QA\Evidence\Partsize_be2be0e" -PreflightOnly
```

After **B01-40 independent OPERATOR_PREFLIGHT_PASS and HQ licensed-operator authorization**, omit `-PreflightOnly` to execute real Unity.

```powershell
pwsh -File "Tools/B01/PARTSIZE001/W26Receipt/Run-UnityNative.ps1" -UnityEditor "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe" -ProjectPath "C:\QA\PartsizeDisposable" -EvidenceDir "C:\QA\Evidence\Partsize_be2be0e"
```

4. Parse original generated files, not a rewritten XML summary:

```powershell
python Tools/B01/PARTSIZE001/W26Receipt/check_receipt.py receipt --dir "C:\QA\Evidence\Partsize_be2be0e"
```

5. The parser rejects missing/wrong exact source, missing/duplicated/foreign NUnit cases, changed XML or Editor log hashes, nonzero native exit, anything other than 17/17 Edit+11/11 Play `Passed`, or absent Unity version in Editor logs. Even structurally complete evidence returns only `STRUCTURAL_EVIDENCE_REVIEW_REQUIRED`; **B01-60 must independently inspect original Unity output and physically reproduce**. No machine-generated G1/G2 PASS.
6. Separately open `Assets/PARTSIZE001/Demo/PARTSIZE001_Demo.unity` in Unity on disposable clone, use Play, capture 720p and 1080p screenshots of actual green/green/intentional-red box fixtures, real `Physics.OverlapBox` overlapping/external omissions, all 0/90/180/270 rotations, child-collider omissions, socket authorization denial and detailed Editor log; keep video and provenance. Use original signed collider geometry from B01-30 #281 to test moving parts only when actual R3 source/art assets are imported into a **separately approved** disposable target.
7. B01-40/60 retain exact original SHA/ZIP, logs, screenshots, receiver target SHA, clean source proof, real Unity execution exit codes, raw XML. Codex #79 ACK must be separate and explicit; **NO OFFERED, CODEX_READY, G2 or merge** from this script.

## Eleven PARTSIZE physically UNMEASURED kinds — exact audit at source SHA

The following is re-derived from **actual** `PartSizeBaselineCatalog.cs` and `observed_r3_size_matrix.csv` at pinned producer Git blobs, not speculative sizes. All have one **logical** 1×1×1 cell, but actual complete physical Collider/visual/swept bounds are `NOT_RUN` (including socket and clearance). Audit tool `python check_receipt.py source --repo <Git repo containing fetched producer commit>` enforces original blobs, 19 names, 11 unknown, and all 17+11 test declarations.

| Canonical kind | Existing source note; what operator must actually measure |
|---|---|
| Wheel | Convex wheel MeshCollider, tire radius/axle direction, rotation sweep, terrain clearance; source-only conservative signed physical AABB from B01-30 is **not** native narrowphase |
| Gun | Barrel and different gun variants, pitch+yaw muzzle swept clearance, physical socket face/contact |
| Suspension | True physical root versus **decorative** piston overhang; do not turn visual geometry into a new collider/socket |
| Booster | Exhaust cylinder/projection and functional back clearance |
| JumpDrive | Lower offset device, landing and upward/downward clearance |
| Blink | Sphere payload beyond 0.52m core root and effect nonphysical bounds |
| Gyroscope | Ring orientation, render swept bounds versus true physical collider |
| OmniDrive | Lower wheel physical envelope versus movement/suspension sweep |
| MagneticGrapple | Forward harpoon/tether dynamic range, authored mesh versus true physical collider |
| HoverEngine | Lower lifting device, rotor/hover gap and functional exclusion zones |
| CrawlerTrack | Real source BoxCollider may project beyond one cell; B01-30 calculated analytical adjacent +Z BoxCollider overlap **0.008182304 m³**; **Unity PhysX NOT_RUN** |

Known independent historical QA reference: [B01-30 W17 signed pivot audit #281](https://github.com/C8LyNn/RoboRougelike/pull/281), four yaw rotations. Source Wheel/Track/HeavyGun/Suspension analytic values are *diagnostics*, not measured Unity full imported prefab/broadphase/rotating dynamic fixtures. Keep these 11 kinds UNMEASURED, do not approve global rescale or derive socket permission from an exposed grid face.

**TASK_ID:** `PART.P3.B01-23.W26.READONLY_NATIVE_RECEIPT`; `REQUIRES PART.P1/P2 PASS@be2be0e`; `BLOCKED_BY LICENSED_UNITY_6000.3.14f1 + B01_60_INDEPENDENT_G1`; `QA_KIND SOURCE/PARSER_ACTUAL; NATIVE_NOT_RUN`; `UNLOCKS B01-60 original NUnit+PhysX measurement operator execution and B01-40 later exact-head G0_TARGET verification`. Preserve original producer Draft #286 HEAD exactly.
