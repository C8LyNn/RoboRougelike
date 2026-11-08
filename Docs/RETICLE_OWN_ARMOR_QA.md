# Reticle and own-armor feedback

For projectile weapons and the beam, the per-gun marker and aim caption now consult the same own-armor clearance rule used by the actual weapon. An obstructed gun is marked without moving its marker onto the player's hull. This also uses the currently selected Automatic muzzle after the dual-barrel change. Radial EMP and saw retain their different hit rules.

The garage shows that Rocket Launcher reloads automatically when empty. `R4WeaponDefinition.Brief` is also corrected; no claim is made that this currently unused helper is shown in the UI.

Unity 6000.3.14f1 EditMode runner, no desktop input:

- `ReticleOwnArmor-Red-20261108`: fixture error, null controller. The test setup was corrected; not counted as a product failure.
- `ReticleOwnArmor-Red2-20261108`: 0/1, exit 2, proved actual firing path blocked while marker claimed it was unobstructed.
- `ReticleOwnArmor-Green-20261108`: 25/25, exit 0, including reticle/viewport cases, beam clearance and Automatic alternating barrels.
- `ReticleOwnArmor-Beam-20261108`: 2/2, exit 0, including actual beam blocked/unblocked with both marker and caption following the result.
- `ReticleOwnArmor-Final-20261108`: 2/2, exit 0; the requested rocket filter used the wrong class name, so no rocket test ran in this invocation.
- `RocketAutoreload-Check-20261108`: 1/1, exit 0, actual empty player launcher starts and completes reload without input.

Raw Editor.log, XML and exit codes remain in the named local `Artifacts/Local/` folders. Live Windows Player visual assessment and performance with many weapons are NOT RUN. The marker and caption each currently query the physical own-armor path, so cost under large builds warrants profiling before performance claims.
