# R4 — safe receiver instructions / no automatic Codex handoff

**NOT AN OFFER / NOT A CODEX ACK.** Codex W26 target `feature/pass17-reload-fix` @ `f92c873bb1e397a598b133e107ae704e15f86768`, but local unpushed files/target state not known. B01-00 must approve after B01-40 independent exact source-transport G0 and B01-60 native QA. DO NOT edit its active worktree. Never coinstall old R1/R2/R3 with R4 (same original GUIDs and asmdefs).

1. Pin source exact SHA from current R4 Draft PR and compare against `manifest.json` `parentR3Sha` and `gitBlobs` before handling ANY files.
2. Independently fetch author GitHub Actions original **24-member Unity SOURCE ZIP** and verify sha256 from actual original job receipt; never mistake this for a Unity Player.
3. Import ONLY the 23 owner-original Unity code+meta+asmdef files `Assets/B0125SafeRoute001/**` + root meta, in a **fresh disposable unoccupied Unity checkout**; reject all symlink ancestors or existing paths. The `PACKAGE_MANIFEST.json` inside ZIP is a source ledger, not to be deployed to Assets.
4. Before any target installation, B01-40 validates all 51 Git blobs, 14 stable unique GUIDs, 3 independent asmdefs, immutable R1/R2/R3 manifest lineage, physical bytes/source ZIP, staging+rollback and possible physical/serialized dependencies. Explicit review of namespace/code conflicts and Codex scope ACK.
5. Only after explicit authority, use Unity 6000.3.14f1 Editor (with valid activation) and menu `B01/PASS17 W23/Create SAFEROUTE001 Standalone Scenario`; on a clean isolated project this creates an additive scene. Execute the **17 actual PlayMode** source tests, export original NUnit3 XML, Editor.log, process exit, SHA of source/build. Re-run R3 and prior Light/Heavy cases.
6. Stop on FAIL/NOT_RUN. Do not modify Save/Rig canonical Unity scenes or unknown active Codex worktree. B01-00 decides if a later PR or cherry-pick is authorized.

**Rollback:** on dirty or occupied destination = ABORT, never overlay. For pristine scratch import, unlink only the precisely staged new 23 original owner files; preserve foreign files AND empty directories. Original protected sources, user data, persistentDataPath, Codex worktree and other agents untouched. No recursive delete.
