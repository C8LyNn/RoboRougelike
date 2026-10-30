# B01-25 / P17 W11 — SWEEPORB001 R2 actual Unity 6 demo

Source base: immutable B01-25 R1 #499 at fc09d2d36b1d26d163347c94b978ca7730cf8891. This NEW VERSION adds a usable isolated Unity 6 demo without modifying R1, any canonical Codex assets, saved maps or other swarm packages.

## Actual functionality
- Copied original R1 pure C#9 SweepController exactly byte-for-byte into Runtime/SweepCore.cs (original Git blob ca98d0aa967d9e6267a5901304155a7a684620d7).
- Added Unity6 SweepOrbUnityDemo MonoBehaviour, actual Rigidbody with CapsuleCollider, gravity and ContinuousDynamic collision, grounded on BoxCollider floor. Light 1.5kg or physically wider/deeper Heavy 12kg; ForceMode.Force movement. Unity source moving sphere is visual only with disabled Collider: continuous XZ hazard contact comes from actual immutable SweepController using previous/current real Rigidbody positions. This is a demonstrator for host-owned damage policy, not real game's canonical HP integration.
- Demo HP starts 100, loses 10 only when immutable engine grants exactly one DamageRequest in an active sweep. Yellow warning, red active moving orb, green safe-return period. IMGUI buttons for drive, change Light/Heavy, pause, restart. Values come from actual controller and body state.
- Editor menu: B01 / PASS17 W11 / Create SWEEPORB R2 Demo Scene. It creates a NEW additive empty Unity scene, original camera, light and demo root; saves to Assets/B0125SweepOrbR2/Scenes/SWEEPORB_R2_Demo.unity and refuses to overwrite any existing scene. It restores original active scene. Generated scene is NOT committed; it is created locally on a licensed Editor host.
- Four original Unity Test Framework PlayMode tests authored for native Rigidbody+Collider creation, two physical vehicle footprints/masses, FixedUpdate damage once, pause/restart. No native Unity 6000.3.14f1 execution has been observed. Native G1=NOT_RUN; source compilation/ZIP tests never imply PhysX PlayMode success.
- Stable 12 Unity meta GUIDs; 3 distinct asmdefs (runtime, editor-only builder, PlayMode tests). Runtime autoReferenced:false; optional install does not rewrite or shadow existing game systems.

## Execute exact source gate
Checkout this PR in a disposable repo, with .NET8, Python3 and git. GitHub Actions runs these commands on Windows and Ubuntu, pins immutable R1 source and both unchanged maps:
    git fetch --no-tags origin fc09d2d36b1d26d163347c94b978ca7730cf8891 ad52a604ffa4f143d98e607049d6f79e0c7e580f f565e250659692541d672cdab108c4cfb8cfdd31
    P=Research/B01-25/P17/SWEEPORB001
    mkdir -p "$P/.output"
    git show ad52a604ffa4f143d98e607049d6f79e0c7e580f:Research/B01-25/P17/CLEARANCELADDER001/arena.json > "$P/.output/clearance.json"
    git show f565e250659692541d672cdab108c4cfb8cfdd31:Research/B01-25/P17/SLALOM001/arena.json > "$P/.output/slalom.json"
    dotnet build "$P/SweepUnityCompat.csproj" -c Release -p:LangVersion=9.0 -warnaserror
    dotnet run --project "$P/SweepOrb.csproj" -c Release -- "$P/profiles.json" "$P/.output/clearance.json" "$P/.output/slalom.json" "$P/.output/receipt.json"
    python Tools/B01/B01-25/P17W11SweepOrbUnityR2/verify.py --output Tools/B01/B01-25/P17W11SweepOrbUnityR2/.output/receipt.json

Verifier checks 19 actual Unity import files, original source Git blobs, all GUIDs and asmdef names, exactly owned git paths, 19 positive and negative source/packaging assertions, deterministic physical 20-member inner ZIP from raw Git objects, canonical ZIP creator metadata on all platforms, third cross-OS byte-for-byte parity job, safe isolated scratch stage/rollback. It does not call Unity and must not claim Unity native G1.

## Native test recipe for authorized receiver
B01-40 inspect exact Git SHA/inner physical ZIP/asmdefs/GUIDs/source rollback before source G0. B01-60 on a licensed Unity 6000.3.14f1 disposable checkout: open this new demo scene by Editor menu, enter Play Mode, drive both physical mass classes, inspect collision/HP/phase/telegraph, run B0125.SweepOrb.R2.PlayModeTests, retain actual Editor stdout, Test Results XML, exit code, Player logs if any. B01-00 controls #79 and any future offer. Codex must explicitly ACK before integration into canonical game. This package is not integrated, not OFFERED_FROZEN, and no merge is authorized.

Known limitations: 2D center-point swept hazard circle is not a genuine compound-collider sweep; robot hull and Armor/HP rules require host adapter. Fixed timestep above .25s fails closed rather than silently skipping phases. GameObject geometry is procedural graybox (no Blender model, polished VFX, or canonical game prefabs). Test scripts authored NOT_RUN in native Unity. To roll back on disposable branch, remove only Assets/B0125SweepOrbR2/, Assets/B0125SweepOrbR2.meta, Tools/B01/B01-25/P17W11SweepOrbUnityR2/ and unique W11 workflow. Do not modify earlier frozen R1 #499 or Codex files.

## Reproducibility incident, tracked RED
The first full dual-OS CI on the original W11 head had functional and source tests green but produced different physical ZIP digests because Python zipfile defaults the central-directory creator operating-system field differently on Windows and Ubuntu. The separate ZIP test was initially same-OS only. R2 later adds canonical create_system=3 to all ZIP entries, an explicit verifier regression, and a third GitHub Actions job comparing the actual Windows+Ubuntu artifact bytes with cmp. Do not claim physical byte-identical parity until that final third job returns PASS. Initial Windows also caught incorrectly mixed host path separators in manifest/GUID/Git lookups (corrected with as_posix), preserving the genuine negative evidence.
