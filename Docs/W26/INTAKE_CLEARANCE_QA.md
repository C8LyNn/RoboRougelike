# W26 intake and projectile clearance — partial receiver report

Base: #383 `f92c873bb1e397a598b133e107ae704e15f86768`. Gameplay commit: `6b82dea3d76cd3a6671bbc375477ef491d6a0f80`. This is a partial change, not W26 acceptance or release.

## Actual code

The R3 player FireGroup now checks owned colliders along the gun-pivot-to-muzzle segment and forward from the muzzle before spawning Basic, Automatic or Heavy physical shells. A blocked gun does not start its cooldown. The forward query stops at the projectile's physical speed times lifetime and permits an external hit ahead of a more distant owned collider. Individual guns retain independent group firing. Specialized WPN projectiles, beam/saw, rotating-arc exclusion, visual grid masks, and the complete multi-cell placement system are not covered.

The existing 12-gun group fixture originally put synthetic barrels behind other guns and the two-gun fixture placed a gun intersecting another barrel. The test fixtures were changed to physically connected, clear firing lanes; the projectile count, group, damage and cooldown assertions were retained. A separate test checks actual obstruction, unblocking, external cover and range. Additional gameplay/visual review and performance profiling remain necessary.

## Executed

- Unity 6000.3.14f1 compile: PASS exit 0 on gameplay commit.
- Targeted Unity EditMode: PASS 20/20, exit 0; includes actual R3 gameplay after entering PlayMode, gun groups, 1/2/4/12 counts and two-type limit. These tests were executed on the same code before line-ending normalization and commit.
- Selected PlayMode tests: PASS 40/40, exit 0, original shotgun and railgun package tests; they do not test the new guard for those specialized guns.
- Windows x64 Unity build: PASS exit 0; build report 0 errors / 4 warnings.
- Extracted ZIP actual Windows Player batch smoke: PASS exit 0; two LD02 battles, two rewards, real enemy projectile and preserved IDs. Victories are API-controlled, not human play. Human GUI and subjective gameplay NOT_RUN.
- Earlier native test runs show RED for a shot through armor ahead of the muzzle and for armor intersecting the barrel. A wider intermediate regression found 3 failures using overlapping/disconnected test fixtures; after diagnosis these fixtures were corrected and the final 20/20 run passed. No assertion counts were weakened.
- Full EditMode, full PlayMode, blocked-fire Player scenario, performance profiling and all-weapon clearance remain NOT_RUN.

## Package gate

Selected exact source: #787 `f5db40bf293fe9582ec7c6fcb3515aeff6fcf937`, #797 `2b7983f0f9e09dd1d56906b379a3f7af7b02f21a`, #792 `16cb2a7b5158605b15191b6074f9e392e0d1ebd3`, #780 `b4df10a7ad7e6c758f8331013f6b216b3a3689a3`, #750 `60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb`. #787 is an ancestor of #797; the original #780 policy/section Git blob SHAs match #797 manifest. Composite G0 remains HOLD; none installed into the game. #797 contains old V1 material in its ancestry; do not import the whole tree or add a second writer.

A minimal Unity compiler probe for exact #797 API calls fails with CS0117 `SHA256.HashData`, CS1501 `File.Move(...,...,true)`, CS0117 `Convert.ToHexString`. This is a native compiler probe, not a complete package compilation. B01-62 was notified in #797 and must preserve atomicity/durability while supplying a compatible revision. Game-facing one-store physical/economy binder, legacy backup-first migration, G0, Unity G1 and full cold Save/Load are outstanding.

## Claude turret

Local v3 FBX SHA256 `4c22ce95971522d86b34e71532681d166e7780eb7744a40dab7d875483ea2f6e`, GLB `f88aafd53b66cf5c21f3467e99b195c17110791d81bbd025ff9ca9dbc863f76f`; source `.blend` and grid metadata are physically present. Default Unity import succeeds and assigns URP Lit. However its dual muzzles import at inverted Z and point +Y. With `bakeAxisConversion=true` positions match the metadata, but both forward vectors point -Y instead of +Z; the central muzzle position also disagrees. Independent screening found three triangles with squared cross-product below 1e-16; this is a threshold result, not proof of three exact zero-area triangles. Model import QA is isolated; no production prefab or model was installed. See #141 for exact reproduction. v2's green `Mount_Block_STATIC` is part of its exported model, whereas v3 has a separate `R41_Base` pedestal. No subjective visual acceptance, animated sweep, Unity physics, mounting or firing acceptance claimed.

## Local partial build

`RoboRougelike_W26_Clearance_PARTIAL_Windows_x64.zip`: 39,628,481 bytes; SHA256 `a95bad534b55ba58c4919d3661dcba3165cb281c2a7aa3ce9038960d9edbebd7`. This is a partial test artifact, not an accepted W26 release. Logs, XML, Unity FBX inspection and Player JSON are under the isolated checkout's `Artifacts/Local` directory.

Next receivers: B01-62 for Unity-compatible atomic Save implementation; B01-40/61/41 for exact composite G0; model producer for verified FBX axes; Codex for true binder, spatial masks and remaining weapon classes; B01-60 for independent native G1. No merge to main/develop and no release.
