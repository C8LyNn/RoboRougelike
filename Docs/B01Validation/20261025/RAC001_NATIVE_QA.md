# RAC-001 R3 — full stacked source, native compilation failure

Dispatch HQ-B01-CODEX-W9-20261025. Target OBJ001 `4c9236a4cdc27814ea2e7679872ec4a524150cc8`; independent checkout `RoboRougelike-RAC001-Validation`, branch `qa/rrl-rac001-native`, capsule `52222a7e3c9f1c25c00df3cb768757773c979eb4`. Full immutable source PR135 `6c1aa7c2351a29695e4288dddad2565cdec3682a` includes parent PR13 `7884f2737197e26769842c087dcd2a1ead75ac95` and R3; not the child diff alone. Existing parent folder GUID was preserved.

**FAIL: Unity6000.3.14f1 C# compilation, actual native exit1.** Run RAC001-original-20261025T092220Z,09:22:20Z–09:23:30Z. No NUnit XML. Error:

```text
Assets/RoboRougelikeMechanics/RAC001CheckpointRace/Editor/RAC001DemoSceneBuilder.cs(67,1): error CS1027: #endif directive expected
```

The exact source file opens `#if UNITY_EDITOR` at its beginning and ends after namespace/class braces without the matching `#endif`. A clean full package import reproduces it. No CAMERA or BUILD package is present in this checkout, so their assembly errors cannot explain this failure.

**NOT RUN:** all parent EditMode/PlayMode cases, new force-driven physical demonstration test, real R3/R6 grounded wheel run, ordered checkpoints, timeout/wrong actor/multiple colliders, lost-wheel mobility, interruption/restart, reward-once and OBJ001 compatibility, visuals and Windows build. The source's150kg AddForce cube demo is test-only and is not a substitute for real game wheel drive. No race/reward adapter was installed in the game.

Local integrity: all34 manifest blob/byte identities verified against full R3 and checkout, computed SHA256, no target GUID collisions; see RAC001-source-audit.json. No independent gate is implied. B01-20 should publish the missing preprocessor closure in a new immutable R3.1 source and request native compile/Edit/Play plus grounded-vehicle testing. Suggested correction is not a passing retest. B01-60 G1 unconfirmed/G2 HOLD; user approval gates remain binding. Original source was not modified and existing OBJ001/main/develop were untouched.
