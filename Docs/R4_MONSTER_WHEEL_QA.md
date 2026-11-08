# Monster XL side-mounted wheel

Windows build source `d2884db9`, stacked on Draft PR #1045. The #1018 source
FBX SHA-256 is `ebf87984232c71a465b1cc2f885c03d05bea08b007f74a722bfd393f6a62cf71`.
The wheel reserves one cell but its physical tire projects into adjacent space:
radius 0.8 m, width 0.58 m, mass 24 kg and 1.5× grounded traction contribution.
The socket and actual axle use the authored side-port offset. It must be removed
and remounted to change size; the installation gate checks the actual tire.
Its provisional purchase price remains the existing wheel price.

Unity 6000.3.14f1 EditMode runner: **18 passed, 0 failed, 0 skipped**, native
exit 0 on `136ef6f3bb5b321a347e1af806bafbaa33595c53`; tests enter Play mode.
They cover per-stack purchase, socket orientation, physical clearance against a
larger tire, mass, contact, grounded traction, save/restore and regressions of
medium/L wheels and armor. A four-wheel R6/Rigidbody fixture drove about 9.8 m
on flat ground in 1.6 seconds with at most one input per physics step. Evidence:
`Artifacts/Local/RC2-MonsterWheel-20261108T003455Z/`.

Windows build succeeded with Unity exit 0. In the actual Player, XL was bought
and mounted on the side of a construction, rendered, and the game exited through
its menu. ZIP extraction and required files passed; the Player was run from the
build folder rather than the extracted ZIP. Terrain comparisons, balance,
standalone PlayMode suite and full campaign remain **NOT RUN**.

Archive: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike_Kola_Monster_d2884db9_Windows_x64.zip`
(44,634,435 bytes), SHA-256
`bf1d62a00ca9bc4eb153c6d67bcb96a74fa23da72d123bc0fc4a22bb2a0d5f0b`.
