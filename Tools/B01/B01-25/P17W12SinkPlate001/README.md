# B01-25 SINKPLATE001 / R1 — mass-sensitive collapsing floor

W12 original executable C#9 game-rule engine and isolated Unity 6 URP physics demo. Distinct from SWEEPORB, HAZARDPULSE, CHL and reattach. No canonical game edits.

A vehicle entering the 6m central plate starts an irreversible warning: Light below 4kg has 0.8s; Heavy at least 4kg has 0.35s. Mass category is locked at entry. The actual BoxCollider disappears while the surface drops 3m for 1.2s and rises in 0.4s. It rearms only after full recovery AND the player leaves, preventing a falling-vehicle softlock. At any time the west and east sections are linked by an independent permanent northern bypass (world z=4), outside the collapsible region. Reset restores the plate, pause freezes the source phase, invalid/nonfinite or >0.1s dt fails closed. Real Unity Rigidbody and CapsuleCollider demo with Light=1.5kg or larger Heavy=12kg uses ForceMode.Force, displays warning/drop/recovery colors, auto-respawns falls at west spawn. Editor menu creates a new standalone additive scene with camera and light, never overwrites existing scenes.

Source paths: Research/B01-25/P17/SINKPLATE001 contains original C#9 core, .NET8 executable tests, two .csproj and authored arena JSON; Assets/B0125SinkPlate001 has BYTE-EXACT copy of core, a Unity MonoBehaviour, actual scene generator, 4 authored native Unity PlayMode cases, three isolated asmdefs and 12 stable .meta GUIDs. Native Unity tests are AUTHORED/NOT_RUN, not simulated as PASS.

Reproduce from repo root with .NET8, Python3, Git:
    P=Research/B01-25/P17/SINKPLATE001
    dotnet build "$P/SinkPlateRuntime.csproj" -c Release -p:LangVersion=9.0 -warnaserror
    dotnet run --project "$P/SinkPlateTests.csproj" -c Release -- "$P/.output/gameplay.json"
    python Tools/B01/B01-25/P17W12SinkPlate001/check.py --out Tools/B01/B01-25/P17W12SinkPlate001/.output/receipt.json

Dedicated Actions runs on Windows+Ubuntu, archives raw stdout, JSON and byte-deterministic original 20-member Unity-import SOURCE ZIP and third job byte-compares actual two OS output. Compiler/source QA success is NOT a real Unity Editor/PhysX PlayMode success.

B01-40 independently verifies exact manifest Git pins, 12 GUID/3 asmdefs, physical original source ZIP and rollback; B01-60 independently retests and if licensed host is available executes Unity6000.3.14f1 PlayMode/PhysX and collects original XML+Editor.log+exit. B01-00 owns source offer to #79; no Codex ACK/merge/integration is implied. Host must inspect true Rigidbody footprint in full real game; occupancy detection is simple center-XZ based and not a swept hull overlap. Safe bypass is authored collision geometry, not yet natively verified. Rollback only new SINKPLATE001 Research folder, Assets/B0125SinkPlate001 folder + its root meta, own Tools folder and W12 workflow on disposable branch. Keep Codex and previously frozen B01-25 SWEEPORB R2 #513 intact.
