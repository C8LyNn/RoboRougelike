# MAPSAFE-001 R1 — Offline backup and safe restore of RoboRougelike map-editor JSON

**Package owner:** B01-62 — Data Integrity & Save QA. **Purpose:** a NEW usable standalone offline tool to protect and restore map-editor files without touching Codex's gameplay, run-save feature or canonical Assets sources. All operations are opt-in and confined to operator-supplied paths.

## Functional commands

Python 3.11+ standard library, Windows 11 or Linux. Create separate EXISTING directories Maps and Vault. The tool never auto-discovers real user saves.

    python Tools/B01/B01-62/MapSafe001/mapsafe.py backup --maps-dir Maps --vault-dir Vault --name "Mapa 1"
    python Tools/B01/B01-62/MapSafe001/mapsafe.py list --vault-dir Vault
    python Tools/B01/B01-62/MapSafe001/mapsafe.py verify --vault-dir Vault --backup-id BACKUP_ID

After moving/deleting the map from a DISPOSABLE TEST directory, restore it with the NO-CLOBBER command:

    python Tools/B01/B01-62/MapSafe001/mapsafe.py restore --maps-dir Maps --vault-dir Vault --name "Mapa 1" --backup-id BACKUP_ID

When the map ALREADY EXISTS, the tool refuses restoration by default. To intentionally replace the map, obtain its current SHA-256 (Windows PowerShell: Get-FileHash "Maps/Mapa 1.json" -Algorithm SHA256), check the bytes and deliberately specify BOTH --replace and --expect-sha256 HASH. This creates a new exact-byte pre-restore undo snapshot in the vault BEFORE replacing the target; corrupted previous bytes are retained as forensic copies but cannot be re-applied with the safe CLI.

Outputs are JSON (stdout) and process exit 0 on success, or error JSON (stderr) with exit 2. Snapshot names are random immutable 32-hex IDs. Payloads have SHA-256, byte length, creation timestamp, source map name and schema. A staged snapshot is not published until both contents and manifest have been fully written. New-map restore uses same-volume hardlink publication, which refuses existing files instead of silently clobbering them; explicit replacement uses os.replace with a freshly written temporary file.

## Actual tests

    python -m unittest discover -s Tools/B01/B01-62/MapSafe001 -p test_mapsafe.py -v

Test cases include real temporary filesystem backup/verify/restore, invalid map schemas, aliases and reserved device names, corrupt vault payload, v1 migration fields, explicit replace with automatic undo, missing map no-clobber, checksum mismatch rejection, simulated failed atomic promotion and symlink refusal (only if supported). Scoped CI runs Windows+Ubuntu and additionally fetches the exact original 168/173-item Codex LD02 JSON maps, verifies Git blobs, and performs actual map backup/restore/undo smoke tests.

## Dependencies and ownership

Owned paths: Tools/B01/B01-62/MapSafe001/** and .github/workflows/b01-62-mapsafe-001.yml. No changes to Assets/**, CHL, PARTSIZE, the in-game MapStorage / MapDraft, or Codex's entire-run save. Input format is the Unity MapDocument v1/v2 JSON documented at Codex #310 commit a2ccd055070a963805cba0779b79f18489f4794f; two fixture Git blobs e39e2f174984832bd97311c050e3c22fd6fb1175 / 8586312b9606d94b088e4e7640c28105e2d6bac1.

**Install for Codex:** if accepted, copy the folder into a separate workspace and invoke mapsafe.py using Python. No new Unity asmdef, .meta, scene or auto-import. Any future in-game backup interface is owned exclusively by Codex and requires a separate explicit product decision. **Rollback:** remove this scoped source folder and workflow only; NEVER delete an operator's Vault copies automatically. B01-40 must check package provenance, B01-60 may independently rerun native/OS acceptance.

**Limits:** conservative schema validation may reject nonstandard files that Unity JsonUtility accepts, and does not demonstrate Unity native serialization; map editor gameplay and level physical reachability remain separate. There is no cross-process compare-and-swap with a concurrently writing Unity Editor/Player: close the game before any restore, use a disposable directory for first trials. Hardlink publication needs a local filesystem that supports it; otherwise the no-clobber restore fails safely. This is not whole-run save, power-loss journal, cloud synchronization or a finished integrated gameplay feature. Native Unity G1 = NOT_RUN, G2 = HOLD, Draft only; no main/develop merge authorized.
