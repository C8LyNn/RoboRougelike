# Native acceptance evidence (in progress)

Game source under test: `df294b8e85ef61b8a79390a1b0dd9a5c735c1c54`; game Draft PR #937. This evidence-only branch is not another game integration or PR.

- Unity 6000.3.14f1 full PlayMode: 478/478 passed, exit 0. Full EditMode: FAIL, 1308 passed / 82 failed / 1 skipped (1391 total), exit 2. See EditMode.xml and EditMode-Editor.log; diagnosis and repairs are in progress.
- Windows x64 build completed with 0 errors and 2 obsolete API warnings. ZIP SHA-256 `91739fe00c53297b5d6e51e7cd664d6dd19e5af2943d3bb2d8c5cee6b4eb0077`.
- Four API-driven weapons, two live encounters with API-driven victory/rewards, and cold Save/Load in two independent Player processes all exited 0; results and Player logs are included. This does not claim human gameplay.
- Light/Standard/Heavy drive probes use actual game cameras, R3 vehicles, R6 physics and virtual in-process input; see individual JSON files. The separate GPU RenderTexture screenshots show the game camera scene but exclude IMGUI, so they are not evidence of catalogue/UI appearance. Batch-mode ordinary screenshots were black and are intentionally not presented as visual proof.
- Text logs and XML copies are sanitized for the Windows user path and Unity session identifiers; hashes of their unsanitized originals are recorded in `original-log-hashes.txt`. Original files remain in the local QA worktree. Zip lives in the owner's Drive; no public sharing policy is assumed.
- #923 is not integrated; #927/#931 and #834 remain separate open policy/integrity matters. Full acceptance and human playtest remain HOLD.