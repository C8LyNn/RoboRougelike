# INSTALL_FOR_CODEX — isolation + rollback

1. Check manifest.json and SHA256SUMS. Obtain BOTH exact GLBs from pinned Git branch M01 and successful Actions artifact M04. The full Codex inbox ZIP is only available after a real successful run of b01-vis-legendary-w12.yml.
2. Open a disposable Unity 6000.3.14f1 URP preview project, record target commit and snapshot. Install a verified GLTF2 animation-capable importer or convert .blend to animated FBX separately; a .glb rename is not FBX.
3. Import two GLBs only to isolated preview folders. Confirm M01 has MOUNT_TOP_SOCKET, visual WeaponBase/weak rear breech; M04 ROOT_UNDERSIDE_MOUNT_Z0, four N01..04_GIMBAL_PIVOT and inert MobilityHub/weak emitter markers. Y up in GLB, M04 exhaust -Y, M01 nominal weapon +Z.
4. Make **saved URP Lit/SimpleLit material assets** (not just Shader.Find), map original 7/8 PBR materials, validate emissive/no magenta in Windows Player. Original materials cannot guarantee player build shader retention.
5. Inspect 0%,25%,50%,75%,100% and intermediate animation frames; perform narrowphase Physics.ComputePenetration and true mounted footprint checks, 720p/1080p low/wide/tall chassis camera/reticle, green/red invalid socket ghosts. Decorative visual marker MUST NOT create BuildSocket or authorize physical attachment. MobilityHub unsupported unless actual authoritative adapter exists.
6. M01 nominal depth is too small; BLOCK mounting until B01-10/22/00 approve real geometry. Do not pick 220 vs650kg or150 vs780kg in visuals. B01-60 independent G1 and B01-40 target/ZIP gate required before B01-00 G2.
7. Rollback: delete imports/materials and generated .meta only in disposable preview project; restore snapshot; never alter active game, original model factory claims, main/develop, Unity ProjectSettings or packages. No merge.

## R1.1 import delta

Use the SHA-verified **R1.1** GLB from the latest Actions archive (M04 `c40ce2c090b232833340e3135d75184d01eb7f81c072ab07c80d46508f2af0d8`, not older `ab26...`). For `LEG-MOV-001` select clip **`LEG-MOV-001_ALL_FOUR_GIMBALS`**; four older clips only drive each gimbal separately. Inspect raw `qa/M04-024_SWEEP.json` and verify all four animated nodes moved at 25%/75%, then check target Unity importer retention, real collider/clearances, 720p/1080p. Do not equate glTF pose bounds to a Unity physical mount approval.

## W13 / RC1 mandatory footprint gate — STOP before mounting

Read `qa/FOOTPRINT_FIT_W13.json` and verify both GLB SHA256; expected `approvedToMount=false` for **BOTH** models. For canonical GLB + W12 typed build local axes **X→Width, Y→Height, Z→Depth**: LEG-WPN-001 requires 3.672m Z vs proposed 1.10m, LEG-MOV-001 requires 1.57756m Z vs 1.10m. Do not copy the former R1.1 `sampledWithinProposedExtent=true` field; it was incorrect and is superseded. Do not rotate M04 to transpose Depth with Height (its physical exhaust is -Y) without authorizing a design rework. No fake visual BuildSocket or decorative collider fallback. Ask owners to approve revised geometric contract, then retest sampled vertex bounds, full continuous narrowphase and both Unity rigs. Use **preview-only isolated import**, NOT mounted gameplay prefab. Unity Player/PhysX still NOT_RUN.
