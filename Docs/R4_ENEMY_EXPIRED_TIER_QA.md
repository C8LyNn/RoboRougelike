# Enemy salvage after tiered wreck expiry

Follow-up: [native QA on a3d37d30](R4_BACKLOG_NATIVE_QA_20261107.md) passed
the targeted tier/expiry tests; Windows save/load and full regression remain pending.

The tier state for enemy wheels and secondary/elite weapons was attached to a
physical object that can be destroyed before the enemy's Core. The previous
fallback recreated a default state, losing that tier. The enemy now keeps the
same state reference by equipment slot when a tier is assigned; loot capture
still prefers the live binding and uses the retained state if the wreck expired.
Condition remains zero and existing loot ownership and repair rules are used.

`ExpiredEliteWheelAndWeaponKeepTierAndIdentityInSalvage` prepares an elite
with T3 wheels and weapons, destroys and expires one of each, then checks loot
identity, damage and tier when the Core is destroyed.

**NOT RUN:** compilation and the native test, Windows Player, save/load,
combat and full regression. The test and fix were prepared while the owner
requested no heavy background tasks. The change depends on the Unity-null
salvage fix in Draft #1021; it does not prove the whole salvage pipeline or
authorize a merge. Run the targeted tier and expiry suites before acceptance.
