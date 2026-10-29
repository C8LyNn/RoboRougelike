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
