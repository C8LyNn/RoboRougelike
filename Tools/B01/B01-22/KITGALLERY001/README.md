# B01-22 KITGALLERY-001 / PASS17 WAVE03

REAL Unity 6 EditorWindow under menu B01 / Equipment / Kit Gallery (Read Only).
Loads a physical Unity TextAsset with 10 original EK01-EK10 BOMs from producer Draft PR #333, source SHA 0d212a706c1e633b773d2e03b7b40c28bb63521b.
Features: search by name or role, per-kit reference cost and part list, copy TSV, fail-closed schema/source/price/family validation.

Import only Assets/B01KitGallery001/** and root .meta into an authorized disposable Unity project.
Rollback deletes only that isolated folder (and own root .meta) if it was not previously present.
Does NOT spend currency, create inventory, mount parts, grant loot or modify Codex save/garage/AI.
Native Unity Editor UI and Player tests: NOT_RUN until licensed editor and independent B01-60 receiver.
Producer Python source tests: python -m unittest discover -s Tools/B01/B01-22/KITGALLERY001 -p test_gallery.py -v
Original producer check: git fetch --depth=1 origin 0d212a706c1e633b773d2e03b7b40c28bb63521b then python Tools/B01/B01-22/KITGALLERY001/verify_gallery.py
No merge to main/develop. G0 independent and G1 still pending. Codex must explicitly ACK after B01-00 authorization.

## W06 / KITCOMPARE-001/R2 — Side-by-side editor tool, exact #411 provenance
- Versioned successor branch based on immutable #390 HEAD, own isolated Assets/B01KitGallery001/Editor + original owned tests only.
- Repinned to independently G0-accepted EQUIPMENT-KITS-001 v1.0.3 #411 @ b1d4e341d846c9397e236809a1d7e4c4fff9c935. Prices and BOMs unchanged from #333; original #333 SHA remains separate and old.
- New **functional** side-by-side LEFT/RIGHT selection for 10 physical kits, per-part count deltas, reference Scrap delta, physical piece delta and deterministic TSV comparison export. Compare is purely informational: NO purchase, wallet, save, live game call, mount, reward or garage mutation.
- 35 original C# source test cases with Unity API stand-ins, 15 Python source test cases, real ref/manifest+GUID gate, Windows+Ubuntu CI; native Unity Editor UI and Player = NOT_RUN until licensed runner.
- Install only after receiver source-G0 and GUI smoke: import own Assets/B01KitGallery001/** + root .meta into disposable authorized Unity project; check GUID collisions. Rollback remove isolated new folder/root meta if no prior content. Stacked PR depends on previous #390 ownership; NEVER merge without formal B01-40/B01-60/HQ.

## Physical W06 receiver
Run `python Tools/B01/B01-22/KITGALLERY001/pack_gallery.py --out /path/to/NEW_OUTPUT_DIR` after fetching exact accepted producer #411 source. Produces a deterministic nine-member Unity Editor source ZIP (three real assets and six matching `.meta`) with external SHA-256 receipt. Run `--rollback` only on unchanged two-file staged directory; refusing altered files or foreign contents. Own CI packages Windows+Ubuntu physical artifacts for B01-40; this is not automatic Unity installation or a Codex offer.

## PASS17 WAVE07 — v1.1.1: coherent-data forgery regression FIX

**Owner-reproduced RED on exact unchanged original Unity Editor C#**: #418 head `aab92e157433d0b57e38f36ccd1502e6a3fdc5bd` [Actions #41037567102](https://github.com/C8LyNn/RoboRougelike/actions/runs/41037567102) Windows/Ubuntu failed the new test `reject unknown valid-count part on unchanged source pin`. Original v1.1.0 validation verified arithmetic and the *printed* source SHA field but not the actual semantic content; a forged unit price with an adjusted total was accepted.

**Source fix:** [`KitGalleryRules.Validate`](../../../../Assets/B01KitGallery001/Editor/KitGalleryWindow.cs) now verifies a SHA-256 semantic snapshot of **all 207 ordered fields** from independently G0-accepted immutable #411 data: provenance, every kit's ID/title/role/description, all actual part kind/weapon/count/unit-scrap, calculated total and piece count. Canonical encoding is UTF-8 over length-prefixed UTF-16-character-count strings using invariant integer formatting. The expected digest `d748152edbc0c3837f870c9b8bdb3fb9e4b8dab147633d7abe1028b19356450a` is embedded in original Editor C#; a second independent Python algorithm verifies it and proves coherently forged sums change it.

**New negative regression cases:** unknown but arithmetically legal part, forged cost+total, changed nonempty title, fake valid-count gun, forged part count+total. All must fail closed in compiled original Editor source. Consumer GUI already invokes validation on Reload before exposing any offer. **This is not an authorization, encryption, wallet or network signature**, only source-pinned integrity of a fixed reference catalog. Intentional new catalog revisions require new immutable source version and a new expected digest.

Tests are original compiled Editor C# (now 40 checks) with test-only Unity stubs; Python catalog (now 17 tests), physical nine-member source ZIP (15 tests), 19 original Git blobs and 6 stable GUIDs. **Native Unity UI and PlayMode remain NOT_RUN**. Regenerate exact-head ZIP/receipt on Windows and Ubuntu using existing isolated `pack_gallery.py`; discard all older W06 ZIPs for W07 reception. Independent B01-40/60 review remains mandatory.
