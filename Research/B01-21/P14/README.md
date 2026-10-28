# B01-21 / PASS14 — CV-05 / D06 exact MapDocument v2 native acceptance fixture

**Status:** research/QA-only data. **Not an authorized gameplay patch, package, native test result, merged map, or G1/G2 approval.**

### Input and strict identity
- Original editor Codex PR [#304](https://github.com/C8LyNn/RoboRougelike/pull/304) `7249f8459c0313827472f31867ebbe62030339e6`, stacked [#310](https://github.com/C8LyNn/RoboRougelike/pull/310) `231005c6d0351945a8ee6684f3856af118f0e94f`.
- Original `Assets/MapEditor/MapDraft.cs` blob `b5f8f395f93972c0ede000701dbbada8fcfcdfdb`, `MapWorld.cs` blob `8928908db123b35dcab4b324fe9d3a7a8fceee4e`.
- Fixture [CV05_D06_GlobalFloor_2000Ground.json](CV05_D06_GlobalFloor_2000Ground.json) immutable original Git blob `4a8efed0b4dc688cb4cb541868f61fbf5500c446`: exactly **2,000 unique** `kind=0 (Ground)` cells; `x=-25..24`, `z=-20..19`; all unit scale and yaw0; `version=2`, `globalFloor=true`, no Spawn or Finish.
- It is **intentionally** full of previously authored Ground. Do not normalize away the tiles while preparing the repro or the bug disappears.
- Existing [CV-05 CodeX validation queue](https://github.com/C8LyNn/RoboRougelike/issues/79#issuecomment-6455303223) already includes hidden-GlobalFloor/map-item cap: **do NOT create another CV task**.

### Independent original-source execution evidence (NOT Unity)
- B01-21 actual original `MapDraft.cs` compiled in .NET8 **with Vector2Int and JsonUtility shims**; [Actions #40653654296](https://github.com/C8LyNn/RoboRougelike/actions/runs/40653654296) **Windows 14/14 PASS and Ubuntu 14/14 PASS, plus D06 exactly one EXPECTED KNOWN_PRODUCT_DEFECT_RED per OS**.
- Direct original-source D06 test constructs the same 2,000 Ground coordinates and toggles GlobalFloor ON. `Place(Spawn,30,30,0)` fails due `MaxItems=2000`; `Remove` Ground under GlobalFloor fails; turning floor OFF and removing 2 old Grounds permits Spawn+Finish and `TestError()==null`. **No corrected game code, no real Unity or PhysX run**.

### One scoped **native** operator reproduction, only if Codex has real authorized Unity

1. Establish genuine Unity version/license status and exact integrated source SHA first. If no legal Unity available, `BLOCKED/NOT_RUN`; do not synthesize GUI shots.
2. Use **disposable, isolated user profile/data directory**. Backup any existing map saves. Write the exact fixture bytes to `Application.persistentDataPath/Maps/B01_21_CV05_D06_P14.json`; ensure no existing file at that unique name (D02 alias/overwrite remains unresolved). In map-editor UI set `EditorName=B01_21_CV05_D06_P14` and select **Wczytaj**. Alternatively use an isolated EditMode NUnit calling `new MapDraft().TryLoad(originalFixtureJson,out error)`. **Do not overwrite user files.**
3. Expected loaded state: `TryLoad=true`, `GlobalFloor=true`, `Items.Count=2000`, `Spawn==null`, `Finish==null`. Actual `MapWorld.Build` should show one continuous GlobalFloor, not 2,000 visible per-cell Ground meshes.
4. In Unity/GUI attempt `Place(MapItemKind.Spawn,30,30,0)`. **Observed bug at the pinned original source SHA**: returns `false` despite valid free cell. Ground cannot be removed while GlobalFloor ON. Screenshot/method trace should capture **logical item count and visible free tile** to avoid confusing this with physical obstruction.
5. **Non-destructive workaround control:** on disposable map only, set GlobalFloor OFF, remove Ground(-25,-20) and Ground(-25,-19), set ON, place Spawn(30,30) and Finish(31,31); `TestError()==null`. This demonstrates two old Ground entries improperly consume the available capacity. No assertion of real vehicle deploy/drive.
6. **Pass threshold for future Codex FIX on a NEW SHA:** A valid legacy map with hidden retained Ground under GlobalFloor can place both markers into visibly/logically free cells, persist/reload losslessly, and Undo/Redo without losing existing Ground data or violating a documented resource cap. Test `GlobalFloor OFF` still enforces normal item limits and collisions. **Native regression target FAIL→GREEN**; a workaround is *not* a fix.

### Evidence per run
`CV_ID=CV-05/D06 | STATUS=EXECUTED_FAIL/EXECUTED_PASS/BLOCKED | CODEX_SHA | UNITY_VERSION+LICENSE_STATUS | FIXTURE_GIT_BLOB | TEST_LOG + NUNIT_XML + EXIT_CODE | GUI_VISIBLE_FREE_CELL | DRAFT_ITEMS_COUNT | CANPLACE_RESULT | ACTUAL_FILE_HASH_BEFORE_AFTER | FIX_SHA_IF_ANY | B01-60_RECEIVER`.

**Owners:** Codex implements and runs native diagnostic within permission; B01-60 independent review. B01-21 owns fixture/source geometry only. `HQ` owns authorization/policy; G1/G2 not implied.
