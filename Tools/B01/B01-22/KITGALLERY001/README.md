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
