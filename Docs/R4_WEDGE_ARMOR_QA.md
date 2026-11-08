# R4 wedge armor integration

Windows build source: `53d6d4c0` (based on wheel-catalog Draft PR #1041).
Shape source: PR #963, commit `03af5207143da76faeeb8004543a51f26c7f9c0d`.
The source collider bundle SHA-256 is
`768f45f036c407dc81656a72be3b85087d3586f69b797ef72a4bb6e27ec51f59`.

One-cell wedge is a separately purchased, owned and saved armor part. It uses
the supplied visual and convex physical mesh, the original damage/economy and
the existing mounting and repair systems. Old saves default to cube. A wedge
currently exposes only the two verified full faces; partial side sockets are
intentionally not exposed, including for weapons and mobility parts. The
remaining source block shapes are not claimed as integrated.

- Unity 6000.3.14f1 EditMode runner: **15 passed, 0 failed, 0 skipped**;
  native exit 0 on `dc6ed04bab6369bebd0ed6aa5a6ae550ddf48af2`. Tests enter
  Unity Play mode and exercise real garage purchase/mount, collider raycasts,
  rotation, damage, repair, inventory, save validation and catalog regression.
  Evidence: `Artifacts/Local/RC2-WedgeLifecycle-20261108T000022Z/`.
- Windows x64 build: **PASS**, Unity exit 0; actual Player opened the sandbox
  garage, bought and mounted a wedge, displayed its model, and exited through
  the game's menu. `Artifacts/Local/WedgeWheelPlayerBuild.log`.
- ZIP extraction and required files: **PASS**. Player was run from the build
  directory, not the extracted copy. The standalone PlayMode test suite and a
  full gameplay regression were **NOT RUN**.

Archive: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike_Klin_Kola_53d6d4c0_Windows_x64.zip`
(44,138,884 bytes), SHA-256
`8375a1853ff5af35a1ec0c50d9c819d0c8911a5090bebcdd5e13805ac8c2be4c`.
