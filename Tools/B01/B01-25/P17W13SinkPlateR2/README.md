# B01-25 SINKPLATE001 R2 / PASS17 W13 — swept Rigidbody footprint entry

## Genuine R1 defect and complete replacement
Frozen original [R1 #529 at 4cb1ef07fc903cbfb0e5ad5caed6dd22145fd290](https://github.com/C8LyNn/RoboRougelike/pull/529) selects plate occupancy using ONLY current center (\`abs(x)<2.85 && abs(z)<1.35\`). If an actual vehicle traverses X -7 → 7 between two host samples, both endpoint centers are outside; trigger is incorrectly skipped even though motion crossed the physical platform. A Heavy (wide) vehicle can also touch the plate without its CENTER entering the narrow threshold. This is a code review / source test verified R1 RED **witness**, NOT actual native Unity playback.

R2 is a **NEW fully isolated Unity6 source version**, never an in-place R1 SHA mutation. It copies its own deterministic C#9 plate timer, scene generator, actual Rigidbody/BoxCollider graybox, 12+ stable R2 folder + file GUIDs, three distinct R2 assemblies, and four native R1 tests + one new native test. The ONLY new decision rule is pure C#9 \`PlateSweptOccupancy.Intersects\`: exact 2D segment-vs-axis-aligned 6x3m platform rectangle expanded by actually sampled \`Collider.bounds.extents.x/z\`; Liang-Barsky slab intersection catches fast frames and partial wide-body edge contact. It rejects invalid or unreasonably large footprint and nonfinite positions; both directions and touching the boundary count. R2 calls this from real Unity6 MonoBehaviour \`FixedUpdate\` using previous and current \`Rigidbody.position\`. Teleport/restart resets history to avoid phantom swept trigger. Occupancy requires previous AND current robot center height above -2m. The permanent safe northern bypass at Z4 remains excluded for real Light/Heavy extents, independent collision floor available during collapse. Legacy R1 still available on its frozen branch, not silently replaced.

The original R1 timer/physics rules (0.8s Light / 0.35s Heavy warning, 1.2s down, 0.4s recovery, AwaitClear, fail-closed dt <=0.1s, fall respawn, mass-dependent ForceMode.Force) and scene-generate/nonoverwrite guarantees remain. New copy has unique asmdefs + GUIDs to avoid Unity type collisions with R1; no Codex gameplay or #510/#511/#494 edits.

## Real source CI, evidence and truthful limits
From the repository checkout, run:
    P=Research/B01-25/P17/SINKPLATE001R2
    mkdir -p "$P/.output"
    dotnet build "$P/SinkPlateRuntime.csproj" -c Release -p:LangVersion=9.0 -warnaserror
    dotnet run --project "$P/SinkPlateTests.csproj" -c Release -- "$P/.output/gameplay.json"
    python Tools/B01/B01-25/P17W13SinkPlateR2/check.py --out Tools/B01/B01-25/P17W13SinkPlateR2/.output/receipt.json

Unique GitHub Actions W13 runs real executable original-source C#9 + positive and negative tests against the actual R2 rule, and Git raw object/scene/asmdef/meta/rollback/physical ZIP verifier Windows and Ubuntu. A third job compares their original inner 22-member ZIP byte by byte. Never claim GREEN until checking an exact SHA. Real Unity 6000.3.14f1 EditMode/PlayMode/PhysX native and test XML/log+exit **NOT_RUN** unless a licensed Unity host actually executes the authored 5 Test Runner cases. Source G0 cannot certify exact PhysX hull traversal or full compound collider geometry.

## Receivers and rollback
Ownership only new \`Assets/B0125SinkPlateR2/**\` plus root .meta, \`Research/B01-25/P17/SINKPLATE001R2/**\`, \`Tools/B01/B01-25/P17W13SinkPlateR2/**\`, unique W13 workflow. Source code, data, schema, Unity asmdef, original Git blobs and native-test source must be independently reviewed at exact SHA by B01-40. B01-60 independently retests real original positive/negative, then on licensed Unity runs physical fast-crossing and Heavy contact PlayMode tests and captures actual XML/Editor.log/exit. Full gameplay integration requires B01-00 decision and explicit Codex ACK. No automatic OFFERED, ACK, G1, G2 or merge. Rollback: delete ONLY new R2 paths on disposable branch; never remove original R1, Codex files or any foreign file/folder.
