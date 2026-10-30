# B01-23 / P17 W07: Original dependency bridge

This is a REAL deterministic 17-member Unity asset source ZIP made from 16 exact original Git blobs at frozen B01-23 #375 SHA 5666a39eabd4cea91b2b605a9eb99ea2f1e226e8. It addresses the four source prerequisites missing in the exact Codex #383 source SHA 203a0805251ed28293888e43f2d49f0df63767c6, found by independent B01-61 #419. It does not replace the original bracing ZIP #391, whose new armor parts depend upon these sources.

Contents: P17 root folder metadata; one C# assembly definition; original real PartSizeContract and PartSizeBaselineCatalog C# with Unity .meta; three authored base vehicle JSON blueprints and their .meta, including intermediate folder .meta. The ZIP is physical and immutable, with per-file original Git blob SHA-1, SHA-256 and byte counts. No newly invented base rig, no new canonical game source, no fake physical clearance claims.

The executable Python package tool has build, verify, preflight, stage and rollback operations. Preflight uses the actual Codex Git Assets tree (all original .meta GUIDs) and fails closed on differing source, path casing or GUID conflict. Staging is only to a *new disposable directory outside any Git worktree*, refuses existing destinations, and rollback refuses modified or foreign files. No direct writes to Codex. This is source-data integrity, NOT Unity Editor import. For a NEW Codex target SHA, rerun the complete original GUID/path preflight; past result is not transferable.

Reproduce in a shell with Git, Python 3.11 and .NET 8:
1. git fetch origin refs/pull/375/head refs/pull/383/head
2. python Tools/B01/B01-23/P17W07DependencyBridge/pack_deps.py build --zip ORIGINAL-P17-DEPS.zip
3. python Tools/B01/B01-23/P17W07DependencyBridge/pack_deps.py verify --zip ORIGINAL-P17-DEPS.zip
4. python Tools/B01/B01-23/P17W07DependencyBridge/pack_deps.py preflight --zip ORIGINAL-P17-DEPS.zip --receipt target-preflight.json
5. python -m unittest discover -s Tools/B01/B01-23/P17W07DependencyBridge -p test_pack_deps.py -v

Dedicated GitHub Actions (Windows/Ubuntu) compiles and executes the EXACT original packed C# PartSizeContract + PartSizeBaselineCatalog, verifying three original P17 blueprints and source-logical rotation. Results are logical only. Raw logs and physical ZIP provided in CI artifacts. Actual licensed Unity 6000.3.14f1 Editor and PlayMode tests, real Collider/PhysX/typed sockets, wheel/track/hover movement and native import remain NOT_RUN, not PASS.

Installation / rollback for authorized operator: (1) verify SHA256 of INNER original ZIP and embedded 16 Git blobs; (2) B01-40 + B01-61 preflight on ACTUAL Codex receiver SHA, (3) stage to a brand-new *non-repo* directory using --dest and verify exact original bytes, (4) only Codex with explicit scope ACK may selectively copy missing source + meta onto a fresh disposable Unity branch, with no overwrite; (5) test real Unity native import/typed sockets/colliders/wheels; (6) rollback only the own new files on disposable receiver or use stage rollback command, which refuses foreign/modified data. Do not merge main/develop or modify #286/#348/#375/#391/#405 or Codex R3.

Copy-ready task to Codex: Independently verify SOURCE_SHA and physical ZIP hash plus sixteen original Git blobs; compare all paths/GUIDs to your ACTUAL current receiver SHA. If no collision, stage ONLY missing immutable PARTSIZE runtime source and three original base P17 rigs before the frozen bracing ZIP. Run native Unity 6000.3.14f1 compile, EditMode, PlayMode, real Collider/typed sockets and mobility. Report source/receiver SHA, true output, XML/logs, explicit ACK and rollback. STOP on target drift, duplicate path/GUID or missing actual prefabs.

Status: producer source CI pending until a real successful run; independent B01-40 G0 and B01-61 acceptance PENDING; B01-60 native G1 NOT_RUN, G2 HOLD, Codex ACK/INTEGRATED NO. Receiver B01-40/61 then B01-60, B01-00 controls #79.
