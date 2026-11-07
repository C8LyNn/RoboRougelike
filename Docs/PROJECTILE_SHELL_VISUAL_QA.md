# Physical shell visual — QA

The canonical P17 physical shell keeps its existing sphere collider, Rigidbody, collision handling and damage. Its visible sphere is replaced by an elongated capsule mesh without a collider; a cached built-in mesh is shared and each appearance follows actual flight velocity. Rocket appearances remain on the separate existing rocket path.

Unity 6000.3.14f1 EditMode at `e0d18f45a55fe7c2ac1238ccae93d94cbaa0d707`: 15/15 passed, exit 0, including Basic projectile damage at 10/30/60/100 m, Automatic at 30 m, real enemy fire, railgun penetration and projectile collisions with wrecks. `505732ae954ce82050c6f5ced3a0f19069d4d90b`: 1/1 passed, checking the mesh follows a changed velocity and that only the original collider remains enabled. Local evidence: `Artifacts/Local/RC2-ShellVisual-e0d18f45-20261107T233332Z/` and `Artifacts/Local/RC2-ShellVisualDirection-505732ae-20261107T233544Z/`.
Windows Player for this exact revision, subjective visual assessment and high-count performance profiling: NOT RUN.
