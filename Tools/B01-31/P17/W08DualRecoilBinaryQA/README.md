# B01-31 — PASS17/WAVE08 actual B01-32 Dual Recoil binary visual QA

**Purpose:** physical verification of immutable [producer #446](https://github.com/C8LyNn/RoboRougelike/pull/446) at exact `d020db8bf43626dfecf979ffc47b57dd526a7ca0`, distinct from B01-31 #436 art generator. This is a **receiver**, not a second producer implementation or another A1 prototype.

## Actual inputs and properties tested

The CI retrieves the **original immutable physical GitHub Actions artifact**, not a regenerated mock or original author's claimed test counters: [run #41038102796](https://github.com/C8LyNn/RoboRougelike/actions/runs/41038102796), [artifact #12968263808](https://github.com/C8LyNn/RoboRougelike/actions/runs/41038102796/artifacts/12968263808). It verifies run SHA and artifact name/ID; then directly decodes original 8 PNGs (512×512 RGBA), `frames.json` sampled from original compiled C#9, the 2048×1024 contact PNG, original PNG hash receipt, plus `B0132-DualRecoil-UnityImport.zip` with original 17 Unity members and all .meta GUIDs. Unity source files inside ZIP are compared bytewise to immutable Git original #446 C# sources. Source produces only diagnostic CPU screenshots; this receiver does not prove native 3D renderer quality or game integration.

Independent pixel criteria: alpha/silhouette, non-overlap center, changed pixels for left-only / right / dual shots, no right-lane change after left-only, exact OFF reset against rest, original sheet tile pixel-for-pixel reproduction, real binary PNG SHA256. Original Unity ZIP SHA256, CRC, 17 safe case-distinct paths, 11 unique deterministic GUIDs, prefab MonoBehaviour binding and runtime/test assembly references checked. Synthetic negative controls reject opaque image, path traversal, Windows path case collision, changed source ZIP and tampered PNG hashes.

## Repeat with read-only GitHub scope

Run the dedicated Windows+Ubuntu GitHub Actions workflow on this isolated branch. Each OS has `actions: read` and `contents: read`, checks original producer run head SHA and immutable Git HEAD, downloads original artifact through `gh run download`, installs Pillow 11.3.0 and executes:

```sh
python Tools/B01-31/P17/W08DualRecoilBinaryQA/receiver.py --original original-recoil --output qa-evidence
```

Output `qa-evidence/receiver.json` records every PASS/FAIL from actual physical bytes. Producer #446 source and previous #436 source are never modified. Failures are reported as failures, not converted into synthetic PASS.

## Gating, recipients and rollback

B01-31 presents **visual/binary receiver evidence**, not formal B01-60 signoff. B01-40 independently checks source scope, complete import/rollback and collision risk against Codex; B01-60 performs licensed native Unity 6000.3.14f1 URP PlayMode/Editor import, 3D mesh and GPU screenshot review. These Unity tests are **NOT_RUN** here. B01-00 alone decides CODEX INBOX #79 and Codex remains sole game integrator. No merge to main/develop, no CHL/PARTSIZE/MODEL_JOB changes. Rollback simply removes this new B01-31 QA folder plus uniquely owned workflow on isolated branch.
