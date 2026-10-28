# CV-02 — Original Unity / Windows map-save validation handoff
**Owner of integrated game, engine execution and fixes:** Codex. **Independent QA receiver:** B01-60 with B01-62 data-integrity review. **Existing queue item:** [CV-02 in #79](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223); do not create duplicate CV issue.

## Authority and provenance
- Current public integrated candidate: [Codex PR #310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`. Before a native test the Codex operator MUST declare **actual local integrated SHA** and the three exact original source blob SHAs (not assume this PR is installed).
- The original source at the pinned public SHA has `MapStorage.TryPath` accepting space-trim and case-variant aliases; `MapStorage.Save` uses temporary `.GUID.tmp` plus `File.Move/Replace`; `MapStorage.Load` only opens canonical JSON. Original code paths: `Assets/MapEditor/{MapStorage.cs,MapDraft.cs,R1MapEditor.cs,R1MapEditorHUD.cs}`. Pinned MapStorage blob `f31a94c16e8cb244e66e39b0ac344b085095b630`, MapDraft blob `b5f8f395f93972c0ede000701dbbada8fcfcdfdb`, controller blob `c92e120d9bc3ce26b3eac68cafdfeea4bacefad7`, UI blob `64105bc77bc35f2a897efbf465919a620dacc30b`.
- Run **only** in a disposable, owner-authorized **legitimately activated Unity 6000.3.14f1** Editor / Windows Player environment. No source edits by B01-62, no touching an active Codex worktree, no assumptions of activated Unity.
- Set aside a unique disposable `Application.persistentDataPath/Maps` test namespace, record the exact resolved directory and baseline file inventory, and **restore/remove only test-owned files** after recording evidence. No personal saves.

## Exact native GUI paths and observations
In the current in-game editor `R1MapEditorHUD.cs`, the UI has a **Nazwa** TextField (max 64 characters), **Zapisz** and **Wczytaj** buttons. `R1MapEditor.SaveEditedMap` calls `TryPath` → `Save` without a native confirmation dialog. Operators must test both the **public GUI** and original **NUnit/original C#** methods. For each input: record before/after SHA-256 and bytes for every test-owned `.json` and `.tmp`, `EditorMessage`, and full original process logs.

| Test ID | Fixture/actions in disposable Windows Maps | Required accept (fix may reject or require explicit consent) |
|---|---|---|
| CV02-N01 `trim-alias` | Save distinct A as `Tor 1`; edit map to distinct B and type ` Tor 1 ` then select **Zapisz** without consenting to replacement | A bytes/hash unchanged. No silent alias replace; explicitly rejected or consciously confirmed overwrite policy only |
| CV02-N02 `casefold-alias` | Commit A as `Tor Case`; edit to B, attempt `tor case` | A bytes unchanged without affirmative user consent; never silently overwrite due to NTFS case-fold alias |
| CV02-N03 `reserved` | Enter `CON`, `NUL`, `COM1`, `LPT9` sequentially; press **Zapisz** each time | Validation explains invalid platform filename; no successful map file, no stray `.tmp`, no ambiguous 'Zapisano' |
| CV02-N04 `cancel/invalid` | Type invalid path name `../escape`; also type a valid different name and leave the editor **without pressing Zapisz** | Saved canonical A remains unchanged; canceled edit does not call `MapStorage.Save` |
| CV02-N05 `corrupt-import` | Commit valid A; write malformed JSON into different test filename; use **Wczytaj** for malformed file | Error visible and existing in-memory editor map remains unchanged; no save side effect |
| CV02-P01 `normal-replace` | Commit A under `Legal Map`; intentionally replace by B under the *same exact name* with explicit permitted user action (per HQ-approved policy), then reload | B persists; SHA-256/bytes change only when intended; no `.tmp` leftovers; source policy recorded, no invented approval |
| CV02-P02 `legacy-native` | Place a valid **v1** `MapDocument` JSON with Ground/Spawn/Finish in test folder, load using actual `UnityEngine.JsonUtility` via `MapStorage.Load`, save as v2, then reload | v1 sizes normalize to 1, v1 GlobalFloor=false, v2 version 2, geometry/markers preserved; check original Unity JSON semantics (not the .NET shim) |
| CV02-P03 `Windows-sharing-failure` | Commit A; hold exclusive `FileStream(FileShare.None)` on canonical map, invoke original `MapStorage.Save` with distinct B and release lock | The failed replace must leave A byte-identical and reloadable and no `.tmp` or partial JSON; corresponding real non-Unity Windows C# [B09 proof](https://github.com/C8LyNn/RoboRougelike/pull/316) exists |
| CV02-R01 `interrupted-staging` | Using an **owner-controlled diagnostic/fault-injection at actual original Save transaction boundary** (if available), interrupt after fully staging tmp but before move/replace; restart original app; repeat with initially absent and with previously committed canonical | Report exact crash point, old canonical hash, orphaned temp inventory and whether original UI recovers. **No automatic recovery success requirement without HQ data-recovery policy.** If diagnostic hook unavailable, `NOT_RUN`—do not relabel a manual `.tmp` simulation or a child process outside `Save` as an injected engine crash |

**Important:** An overwrite confirmation choice is pending HQ product policy. Until explicitly decided, acceptance means **do not silently destroy an existing distinct saved map**, not that a particular modal dialog is already approved.

## Mandatory authentic evidence
1. Exact Codex local source SHA and relevant Git blobs, Unity version/license **status only** (never secrets), physical OS, Windows filesystem type and canonical `persistentDataPath`; isolated test-owned file manifest.
2. Original `NUnit XML` (if authoring engine tests), `Editor.log`, Windows Player log where applicable, complete test names/counts, process exit codes, screenshots or observable `EditorMessage` for GUI paths.
3. For every file test: canonical map name, before/after SHA256 and byte count, presence/list of `.GUID.tmp`, operation attempted, expected and actual outcomes, any native exceptions. On failed Save **never** accept a changed previously committed map unless explicit owner consent was recorded.
4. `PASS/FAIL/NOT_RUN` per case: required `CV-00` environment acknowledgement before scheduling. Native Unity/Windows Player result `NOT_RUN` until executable original logs are produced. Existing QA-only .NET8 [PR #316](https://github.com/C8LyNn/RoboRougelike/pull/316) **is not native Unity evidence**.
5. Codex owns any corrective game patch. B01-62 reruns source-original independent negative suite on **new published SHA**, B01-60 receives true native logs; B01-40 handles provenance. This does not close CHL/PARTSIZE independent G0/G1/G2.

## Existing evidence not to re-count
- Windows+Ubuntu .NET original-C# method execution, casefold and trim overwrite **known RED**, original UnityEngine JsonUtility replaced with .NET shim.
- QA-only process-boundary staging `Environment.Exit(73)` occurs **outside** unmodified `MapStorage.Save`, not injected process crash inside Save or actual power loss.
- Positive Windows locked-canonical B09 is an authentic original C# file I/O case but does not close D02 aliases.

**CV-02 execution ownership:** Codex. **CV-02 state:** QUEUED / CODEX_ACK_PENDING / NATIVE_NOT_RUN until Codex supplies a genuine engine receipt.