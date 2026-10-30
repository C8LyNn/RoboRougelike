# B01-40 / P17 W15 — Disposable Unity host migration and rollback

**Unique owner scope:** B01-40 only. This tool does NOT implement Save/Load gameplay, modify canonical Codex Unity project, rewrite frozen #510/#556, or claim G1. It creates a complete **new, disposable** Unity project candidate by combining EXACT original physical source ZIPs from frozen Secure V2 #510 (99,666 bytes, 35 members; SHA256 d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387) and frozen selected Save v2 #556 (67,581 bytes, 17 members; SHA256 20c3c17c252f084014e9766010ab4651770a41f73ce534fc2aa7fd2edc697060), and original W06 Unity asset dependency bytes from the original #510 Git commit. It invokes each ORIGINAL author package.py build/verify, selects ONLY its Unity Assets files, validates all original SHA/GUIDs, and leaves Git-owned Tools/workflows out of the Unity host.

**Adversarial read-only preflight:** Deny wrong Unity 6000.3.14f1/TestFramework 1.6.0, original or alternative path collision, duplicate original Unity GUID, symlink or Windows junction, serialized W07 legacy route and alternative script or folder (including empty), modified selected C# Git blob, invalid host meta, host/file casefold collision. Untouched unrelated host files and empty folders remain in NEW candidate. Existing shared live Codex host is NEVER scanned, staged, migrated or changed; tests use a real filesystem **synthetic disposable host**, not Unity Editor.

**Atomic preparation:** copy the authorized host to a fresh external temp clone; add verified original assets; census every file, directory and SHA receipt; rename **only** the new candidate directory. Reject existing destination, inject crash before publication, forbid overlapping source/destination, never overwrite host. This is NOT an atomic replacement of an existing game project, and is not a live in-game Save/Load restore.

**Strict rollback:** verify byte-for-byte all manifest-owned candidate files (including preserved host originals) plus every directory (including foreign EMPTY), symlinks/junctions, receipt. Refuse changed/missing/foreign content without deletion. For exact owned clone only, unlink listed files and rmdir listed directories. Original dangerous #510/#532 recursive rollback is prohibited for shared targets. As with previous #536/#542 standalone wrappers, hostile concurrent filesystem races without an exclusive host lock are not covered.

## Run and evidence

Clone separate pinned source commits as sibling folders game (#510) and save (#556), alongside own source, then run:

    python own/Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py --game game --save save --out evidence

The dedicated Windows2025 and Ubuntu24.04 workflow actually executes BOTH authors' original ZIP packagers, two full additive clone + rollback cycles, adversarial negatives for ID/code/GUID/foreign paths, incomplete prepare and occupied destination, original physical source bytes, and writes an inspectable JSON receipt. Actual symlink test is reported PASS only if OS permits it, otherwise NOT_RUN_NO_PERMISSION. Do not report authored source tests as native Unity.

**Release boundaries:** Source #510 original G0 accepted via #521, selected Save #556 original G0 accepted via #560; this new add-on is ONLY source-level disposable physical host intake. PRIVATE original #510 Rig/AtomicDockLedger cannot yet be legally captured/hydrated from outside, so a new B01-23/62 owner-approved Unity-facing bridge and independent B01-61 QA are required before any real scene save restore. B01-60 must execute existing native #532/#567 under legally activated Unity 6000.3.14f1 or G1 NOT_RUN. G2 HOLD, Codex ACK NONE, no offer/merge. B01-00 owns all future Codex inbox authorization.
