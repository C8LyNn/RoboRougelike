# Rounded armor and preview build

Production source `8935f1922fb40e9c27ed71084167e94d81e0158a`; Windows
build source `fda37fa3`, based on Draft PR #1043. Adds the #963 round-edge,
half-wedge and dome visuals with their pinned convex colliders and full source
mounts. Partial ports and all multi-cell source parts remain disabled. Existing
cube, wedge, half-block and pyramid remain available.

Unity 6000.3.14f1 EditMode runner: **16 passed, 0 failed, 0 skipped**; native
exit 0. Tests enter Play mode for actual catalog purchases, mounting, physical
collider bounds, rendering components, save/restore and removal for each new
shape, plus wedge, wheel and mounting adapter regressions. Evidence:
`Artifacts/Local/RC2-RoundedArmor-20261108T001019Z/`.

Windows x64 build: exit 0; actual Player showed the new catalog, bought and
mounted a dome, rendered it on the vehicle, and exited through the menu. ZIP
extraction and required files checked; extracted Player itself was not run.
Standalone PlayMode suite, full gameplay and visual inspection of each model
in Windows remain **NOT RUN**.

Archive: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike_Klocki_Kola_fda37fa3_Windows_x64.zip`
(44,338,292 bytes), SHA-256
`9c6da7a6016d79604052c337af7500251273ce284c05a09c09b1034d5b663b4e`.
