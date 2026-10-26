# B01-VIS-LEGENDARY-W12-R1 — actual reworked asset packet

B01-30 owns this isolated package. Receiver: Codex, after B01-40, B01-60 and B01-00 gates. Unity 6000.3.14f1 URP. W12 contract PR #240 SHA 93f290acca26839792a5bec8ea42415ac83932f3. NEVER merge automatically.

**M01-018 / LEG-WPN-001** — reused actual original PR #195 SHA a8f93e9e1fb9cd31ff9e913f472f989b5d6d4f94. Real edited animated GLB committed at binaries/M01-018_LEG-WPN-001_R1.glb, SHA256 795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd; 53 original meshes, 7 materials, 1 original animation, 3 new preview marker nodes. Original visual rest bounding box 1.172×0.9125×3.672 metres: nominal proposed 3×2×2 grid at 0.55m has depth 1.10m, so **real mounting BLOCKED** until owner approves physical footprint. No authoritatively changed mass or collider.

**M04-024 / LEG-MOV-001** — reused actual original PR #196 SHA eef02864b106546b200011a2f0286fe7d9a83a1f. Actual M04 original GLB from Actions artifact 12586794828, SHA256 280bb77e21995b7440fdda5c7551fc135b2b3b5ce6999322708fb670ff979414. R1.1 reworked GLB SHA256 c40ce2c090b232833340e3135d75184d01eb7f81c072ab07c80d46508f2af0d8 produced in Actions and verified; prior R1 hash ab26d649... is obsolete; mesh 105 / 8 materials / 4 original gimbal animation channels retained, BIN unchanged. Existing Blender source GLB used, **no duplicate new model produced**. Rest world AABB 1.5×0.869×1.5 metres, original triangles 29,472. Source tool and GitHub Actions workflow reproduce output from frozen artifact, then pack full two-asset ZIP.

Tool adds named VIS_LEG_*_SOCKET_*, VIS_LEG_*_WEAK_* and VIS_LEG_*_NOMINAL_AIM_AXIS **visual-only empties** and URP material asset-reference policy. They cannot authorize build and are not rigidbody/collider. MobilityHub socket is not currently authoritative; never substitute Structural fallback.

Mass LEG-WPN R0 650kg vs R1 220kg, LEG-MOV R0 780kg vs R1 150kg: all unresolved; no approved mass inferred. Source geometry/animation binary integrity PASS independently, Blender/Unity actual new runtime tests NOT_RUN, G2 HOLD.

The offline B01-30 package ZIP contains actual edited M04 and test tools; M01 is committed in this exact Git branch (offline ZIP deliberately references it rather than mirroring). The GitHub Actions workflow generates a **complete** two-binary Codex ZIP once its actual run succeeds; do not claim workflow PASS before inspecting the run.

## R1.1 — combined gimbal animation and real sampled bounds

Preserved all four old single-channel clips and added `LEG-MOV-001_ALL_FOUR_GIMBALS` with four synchronous rotation channels referencing **unchanged GLB vertex and animation buffers**. M04 R1.1 GLB is 857292 bytes SHA256 `c40ce2c090b232833340e3135d75184d01eb7f81c072ab07c80d46508f2af0d8`. Source-only M04 export hash R1 `ab26d649...` is superseded, NEVER mix R1 archive with R1.1 manifest.

New Python stdlib `tools/swept_geometry_qa.py` samples actual vertex positions at 121 times per animation clip, evaluating glTF quaternion interpolation and node hierarchy; CI ships machine-readable `qa/M01-018_SWEEP.json` and `qa/M04-024_SWEEP.json` with the inner ZIP. M04 combined clip sampled swept size **1.57756 × 0.884306 × 1.57756 m**; nominal 3×3×2 footprint 1.65×1.10×1.65m provides only 0.07244m total lateral dimension difference (0.03622m per side IF perfectly centered). This is **not** a certified gameplay clearance or a continuous triangle collision pass. Mass/MobilityHub authority remain blocked.

## W13 / R1.2 — RC1 geometric safety gate (supersedes previous nominal fit claim)

Executable `tools/footprint_fit_qa.py` validates ACTUAL pinned GLB SHA256, all 121 sampled poses/clip, and canonical **X=Width / Y=Height / Z=Depth**. In contrast to earlier R1.1 wording, M04 does **NOT** fit approved proposed `3×3×2` (1.65×1.65×1.10m): animated mesh depth is **1.57756m**, exceeds by **0.47756m**. M01 depth is **3.672m**, exceeds 1.10m by **2.572m**. Both **BLOCKED_FOR_PHYSICAL_MOUNT**. This is a verified negative check, NOT game production PASS.

Original R1.1 wording erroneously treated M04's 2-cell dimension as the Y axis. The typed W/H/D contract has Height as Y, Depth as Z; arbitrary axis permutation is NOT authorized. Rotation mapping that changes exhaust direction is not approved. Review-only alternative M04 `3×2×3` is NOT a replacement decision; even this has only 0.03622m per face on XZ, below the standalone diagnostic 0.05m clearance policy. Renderer AABB is not `Physics.ComputePenetration`; this gate **never** creates a BuildSocket or authorizes mounting.

CI packages machine-readable `qa/FOOTPRINT_FIT_W13.json` with exact original GLB hash, dimensions and explicit negative verdict, and executes 15 added actual-binary tests. B01-10/B01-22/B01-00 must decide a legal physical footprint or revised existing asset geometry *before* Codex mounts it. Preview-only import can remain optional.

## W15 receipt-forgery bug fixed (RC2 visual QA)

An actual W14 model-evidence attack was reproduced: leaving M04 real GLB SHA intact but changing `qa/M04-024_SWEEP.json` to report Z bounds [-0.5,+0.5]m rather than the real [-0.78878,+0.78878]m was accepted by old code. A correct GLB hash alone **does not** authenticate a separately stored animation report. W15 `tools/verify_sweep_provenance.py` recomputes all original mesh vertices at 121 time samples per animation clip and compares all saved clip/snapshot/vertex-count/extent/time fields to the actual GLB. Rejects mismatches as `SWEEP_RECEIPT_FORGED` and prevents package production before footprint scoring. CI includes 19 real binary mutation/negative tests and ships `qa/REAL_GLB_SWEEP_PROVENANCE_W15.json` plus scripts.
This proves **E2 source-recomputed evidence integrity**, not Unity importer, native animation correctness, PhysX or continuous triangle safety. Original LEG-WPN and LEG-MOV mounting both remain BLOCKED as W13/W14.
