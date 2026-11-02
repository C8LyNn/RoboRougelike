# B01-41 — W28 / exact Unity-API source compiler gate

**Gameplay delta:** none. This package supplies a **real, repeatable compiler negative witness** against original #797, not a competing Save implementation or an independent claim about the current playable game.

## Provenance and observed risk

Original full-game V7 selected producer #797 `2b7983f0f9e09dd1d56906b379a3f7af7b02f21a` imports genuine original #780 binary V7 section, original #729 single owner gateway, frozen #556 codec and #510/W06 types. B01-62 owns its replacement. The exact immutable source contains `SHA256.HashData`, `Convert.ToHexString`, and `File.Move(staging,authoritative,true)`. Those APIs are not available on the target Unity-compatible .NET Standard 2.1 surface. This is **P0 PRODUCT RED**, not corrected by green .NET8 tests. No destructive delete-before-move quick fix is permitted.

The dedicated GitHub Actions on **Windows2025 and Ubuntu24.04**:
1. Fetches exact SHA and Git blob OIDs for **9 original C# sources** from #797/#780/#729/#556/#510/W06. It stages **read-only exact bytes** in disposable CI scratch.
2. Generates a C#9 `netstandard2.1` compilation of the **actual original types**, not a made-up placeholder calling the three APIs. Preserves original compiler stdout and exit code. This is **NOT the licensed Unity Editor** and does not validate its full runtime.
3. Confirms expected `CS0117` and `CS1501` when compiler encounters the old incompatible APIs; unexpected build failures or a changed source pin cause explicit HOLD rather than fake PASS. A green Actions workflow means **the negative witness was detected**, not Save compatibility.
4. Verifies exact #817 parent `e7a85944465d908f175a6f5e2ae07f4aedcc593f` and current pinned #824 W27 source `e03d36f2b86f8c73a067e574b67237a8921f622a` for five actual source/test Git blobs, verifies authored original W27 beam/pellet and optional batch Player fixture. **No native test is executed by CI41.**
5. Executes **10 true negative Python unit tests per OS** for tampered Git blob, unsafe paths, changed branch/commit, missing source tests, missing W27 harness define and compatibility scanner.

**Source inventory at pinned W27 SHA**, not proof of full functionality: shotgun/railgun/rocket plus beam source tests present, Basic/Automatic/Heavy original lineage; saw/EMP/dynamic own-contact/multiple weapons/garage/Player remain unconfirmed. The Player QA harness is guarded by `B01_UNIFIED_RC2_ACCEPTANCE` and not auto-run just by building it. W28 requires an actual final Windows ZIP + Editor.log + NUnit XML + Player log, and repeat at the **actual final gameplay SHA**.

## Operator and gate

Use the produced GitHub Actions logs/JSON **only to establish incompatibility and provenance**. For corrected B01-62 version create a new exact HEAD test run with the genuine new replacement source, and require an **actual green target Unity compilation** + independent B01-40/B01-61 formal G0 before Codex intake. Preserve one real #729 .owner and one whole game envelope, atomic replace/ACK logic. This CI41 code never writes to user Save, author Unity Assets, producer PR, Codex worktree or main/develop.

- `SOURCE_ORIGINAL_PIN=PASS` when executed.
- `SAVE_API_COMPATIBILITY=RED` for known original #797.
- `COMPATIBLE_NEW_PRODUCER=NOT_DELIVERED` until separately selected.
- `COMPOSITE_G0=HOLD`.
- `UNITY_G1=NOT_RUN`, `WINDOWS_PLAYER=NOT_RUN`, `G2=HOLD`.
- `CODEX_ACK=NONE`, `MERGE=NO`, `RELEASE=NO`.

**Next owners:** B01-62 fixes source; B01-40/B01-61 independent exact-source repeat and rollback/security; B01-60 actual licensed Unity and Windows Player regressions; B01-00 decides future handoff. **Rollback:** close the isolated CI41 Draft and delete its own CI artifacts only.
