# Self-destruct near large vehicles

An unarmed enemy's trigger previously measured only the distance between vehicle origins. A large player's live part could be within the actual six-metre damage radius while its origin was outside the trigger; the enemy would keep pushing without detonating.

After the existing five-second warning the trigger now accepts either the original origin distance or a live player part inside the same six-metre radius used by the damage loop. The list of parts is reused and evaluated only after the warning has elapsed. Damage and the warning duration are unchanged; distant players do not trigger a blast.

Unity 6000.3.14f1 native EditMode runner, tests entering Play Mode:

- Reproduction on previous code: **FAIL**, an actual part was inside the blast radius and both origins outside it; the enemy had not detonated after seven seconds. `Artifacts/Local/EnemyBlastLargeVehicle-Red-20261108/`.
- With fix: **5/5 PASS**, exit 0, including large vehicle part, distant player, five-second timer, normal nearby player, and physically trapped unarmed enemy. `Artifacts/Local/EnemyBlastLargeVehicle-Focused-20261108/results.xml` and `Editor.log`.
- Windows Player and human gameplay: **NOT RUN**. This change does not solve physical retreat in a corridor too narrow for the enemy to turn, which remains documented in `ENEMY_BOUNDARY_QA.md`.

Test fixtures use real assembled vehicles and damageable parts but park rigidbodies for the proximity checks; this is not evidence of a driven collision or full encounter. XML and logs are local QA artifacts.
