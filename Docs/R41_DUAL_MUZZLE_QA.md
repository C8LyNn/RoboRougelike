# R41 Automatic: visible dual muzzles

The supplied `R41_Turret_v6.1.fbx` has `Muzzle_L` and `Muzzle_R`. Previously both player and enemy art adapters used only the centre `Muzzle` transform, so the physical shell appeared between the barrels. Automatic now starts with the left barrel and switches after a successful shot; it keeps the same interval, damage and energy cost. Missing side nodes keep the existing centre fallback. Actual projectile path and own-armor checks use the selected muzzle. Existing conventional projectiles and rockets owned by the firing vehicle no longer conceal own armor from the path check.

Normal enemies' additional Automatic also uses the supplied R41 art and its physical muzzle. The original damageable object and collider remain authoritative; missing art retains the previous appearance and muzzle. Elite and primary Automatic appearances use the same adapter.

Unity 6000.3.14f1, EditMode runner entering actual Play Mode:

- `AutomaticDualMuzzle-Red-20261108`: 0/1, exit 2. Existing shot appeared 0.06 m away from a barrel.
- `AutomaticDualMuzzle-Green-20261108`: 5/5, exit 0, including visible alternating muzzles, real Automatic hit at 30 m, own-armor and elite art regressions.
- `AutomaticDualMuzzle-Final-20261108`: 2/3, exit 2; the extended test exposed an own projectile masking own armor. Retained as a genuine failure.
- `AutomaticDualMuzzle-ArmorFix-20261108`: 3/3, exit 0; includes alternating shots, armor blocking and not switching on a blocked shot, elite Automatic's alternating shot origins, and a real Automatic hit at 30 m.
- `EnemyExtraAuto-Red-20261108`: 0/1, exit 2; normal enemy extra gun had no R41 visual.
- `EnemyExtraAuto-Green-20261108`: 3/3, exit 0; the extra gun uses R41 and fires a physical tier-2 projectile with existing damage and energy accounting, player blocked-path regression, elite alternating physical muzzles.

Raw XML and Editor.log are retained locally under `Artifacts/Local/`. Windows Player/manual visual assessment of alternating muzzles, both aim reticles after every switch, broad regression and full expedition: NOT RUN. The corridor limitation in `ENEMY_BOUNDARY_QA.md` is unchanged.
