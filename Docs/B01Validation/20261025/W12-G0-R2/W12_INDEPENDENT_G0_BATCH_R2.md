# B01-40 W12 / INDEPENDENT G0 BATCH R2

**Dispatch** `HQ-B01-20261025-W12-G0-BATCH-R2`. This is an evidence-only revision on top of PACKAGE-AUTO-001 #238, with **no modifications to existing installer source**. All five packages are pinned to real GitHub HEADs; no active Codex edit, merge, deployment or G2 approval.

## Independent producer ZIP checks (actual downloaded bytes)

| Source | HEAD SHA | Inner ZIP SHA256 | ZIP entries / bytes | Per-file SHA256 and size | Unity .meta GUID |
|---|---|---|---|---|---|
| MAPGEN R3.4.1 #237 | `5be31039411f89f17654b1c332f8692883fa03cc` | `2337cb85c090f194a7e10474e73bf3b6aa69f86eeabc74b3204a59d3a60bc623` | 63 / 89258 | 62/62 PASS | 22 |
| MAPGENWORLD #243 | `de1ce561d7e2ffac861c3e0fc76e5d722bdbb9f7` | `e5970c8f1f13837f9fe43e3d575c5ff4dc3a0dfbc1eb0174dbf08699aee1a491` | **68** /140144 | 67/67 PASS |14 |
| LEGENDARY UNITY #242 | `dffabbc6960304fdb33322b5a23c884feac804c6` | `14eccb49f69b2e67ecb34008fe19689b1b66e629ed94491224fb327216fc1fb2` |16 /10959 |16/16 PASS using actual external Actions source-manifest |11 |
| VIS LEGENDARY GLB #244 | `39adffe30ac19e17644dd9b72a0c700681928ce7` | `dabb89a4e14db1b1007bde3b18dde95abe7d09ed2e620e75614c25abc54e9209` |13 /208318 |2/2 verified binary GLB2 headers and hashes|N/A |
| QA-NATIVE #245 | `dc315c81645d40ede18ee53bdda927da44c595a2` | `533612d0d2020e76f8fd751a77b21935ce2a8642ceb68623a7456f50ff06e10b` |10 /31324|9/9 PASS|N/A |

**WORLD 67 vs 68:** 67 separately hashed producer files + embedded `META-INF/MAPGENWORLD001_MANIFEST.json` as the 68th physical archive entry. Real producer ZIP includes `Tools/B01/MAPGENWORLD001/obj/*` .NET build byproducts; hash retained, excluded from Assets-only import. WORLD references `B01.MAPGEN001.Runtime` and exact MAPGEN source SHA. PR #243 description has outdated appended `8ee378...` footer; actual HEAD is only `de1ce...`.

**LEGENDARY:** exact Actions external `source-manifest.json` was downloaded/checked because source-only 16-entry Unity ZIP contains no embedded manifest. Source parent GUID reused unchanged. Typed socket, atomic transfer, R3 0HP repair and mass/abilities unresolved by design. Producer 32 portable tests are not Unity native.

**VIS actual GLBs:** M01 387832B SHA256 `795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd` (53 meshes,7 materials,1 animation channel). M04 856744B SHA256 `ab26d649e31a5ea4d21320480c52b3b9781bd7f3752a687f265c24bbf473df4b` (105 meshes,8 materials,4 channels). They are **not** Unity-ready with stable import .meta; empty marker nodes are NOT BUILD-authoritative sockets. M01 length 3.672m exceeds proposed 1.10m footprint, DO NOT rescale or mount without design approval. QA toolkit producer tool CI 13/13 on each OS, licensed Unity not run.

## Existing PACKAGE-AUTO-001 invoked on real independent source bytes

Original frozen `package_auto.py` of #238 is **unchanged**. Actual inner artifacts were hashed against own producer manifests, then package-owned Unity Assets were copied byte-for-byte into **explicitly QA-only normalized fixtures** (not new approved producer archives) to exercise its unchanged generic embedded-manifest importer. New disposable Git worktrees used synthetic Unity6000.3.14f1/URP17.3.0 baseline and **foreign existing parent** `Assets/B01Packages.meta` GUID. Result:
- MAPGEN + MAPGENWORLD: **58 physical files staged / 58 rolled back PASS**.
- LEGENDARY UNITY: **15 staged /15 rolled back PASS**.
- Three Unity packages combined: **73 staged /73 rolled back PASS**.
- In **each** tested scope: wrong ZIP SHA → DENY; dirty worktree → DENY; colliding target GUID → DENY; injected fault after three staged files → **automatic rollback PASS**. Shared destination parent .meta and its GUID were preserved. Original producer MAPGEN full ZIP was also directly passed through unchanged PACKAGE-AUTO verify (62 source hashes PASS).
- Across all three Unity source archives: **0 duplicate GUIDs assigned to different paths, 0 differing bytes at identical paths**, asmdef cross-package links resolve at full stack.

**Caution:** This is an executed G0 **synthetic-disposable** target QA, **not live Codex G0_TARGET**. Current active Codex HEAD, clean/dirty status and path reservations cannot be observed from GitHub. Published OBJ #210 `4c9236a4cdc27814ea2e7679872ec4a524150cc8` is reference, not target ACK.

## Original native QA #241 independently checked, not reproduced

Codex QA HEAD `e4d67cb286499b9ac4aceec9818017cd110b14aa` tested original exact MAPGEN `5be31039411f89f17654b1c332f8692883fa03cc` against published OBJ reference `4c9236a4cdc27814ea2e7679872ec4a524150cc8`. Independently read committed original EditMode NUnit XML **24/24 passed**, PlayMode NUnit XML **19/19 passed**, process exits0; compile exit0. Material-results JSON 4/4 arena/course ×720/1080 PASS with retained Resources shader, no override and regenerated lifecycle. Two original R6 JSONs contain **4+8=12** PASS API-steered actual R3/R6 routes. **EXTERNAL_CODEX_NATIVE evidence**, **NOT B01-60 OWN_NATIVE_PASS**. Human gameplay, part destruction, turnover, non-floor collision and optional heavy ramp are not certified.

## Separate release gates

| Package | G0_SOURCE | G0_TARGET LIVE | G1_NATIVE | G1_INDEPENDENT_QA | G2_HANDOFF |
|---|---|---|---|---|---|
| MAPGEN #237 | PASS | PENDING | PASS_EXTERNAL_CODEX limited | PENDING B01-60 | HOLD |
| MAPGENWORLD #243 | PASS with build-object warning | PENDING | NOT_RUN | PENDING | HOLD |
| LEGENDARY UNITY #242 | PASS source with external manifest | PENDING | NOT_RUN | PENDING | HOLD |
| VIS #244 | PASS binary only; no Unity importer/meta/footprint | PENDING | NOT_RUN | PENDING | HOLD |
| QA-NATIVE #245 | PASS source tool | N/A game | Tool CI PASS; native Windows Unity runner NOT_RUN | PENDING | HOLD |

**CODEX queue prepared, NOT DISPATCHED:** 0) B01-60 review #241 and request actual target ACK, avoid repeat of the same 43 NUnit. 1) MAPGENWORLD+MAPGEN full stack native authored 6 Edit/3 Play, real builder/URP/lifecycle. 2) LEGENDARY standalone Unity edit negative tests, typed socket/atomic adapter blocked. 3) VIS isolated Unity GLB importer, 720/1080/animation/footprint without live mount. 4) BUILD R1.3 only when B01-23 publishes frozen source, CAMERA R1.2 same. No active integration ever before package-specific B01-00 G2 GO.

**Independent persisted issue receipt:** https://github.com/C8LyNn/RoboRougelike/issues/69#issuecomment-6401924073 . Full runnable local proof (source byte-audit Python + real git-sandbox tests + exact JSON results) was created in this ChatGPT turn and supplied as a downloadable ZIP, not represented as committed code or CI on this documentation-only branch. 
