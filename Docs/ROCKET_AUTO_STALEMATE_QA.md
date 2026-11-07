# Rocket reload and unarmed enemy — native QA

Production commit: `3ad50a65fd1ccd6438280bb6d469a7daccfb57e8`. Base: camera/model fixes from PR #1027.

- Empty player rocket launchers begin the existing reload automatically, as enemy launchers already did. Existing reload duration, damage and projectile physics are unchanged.
- Rocket visuals use an elongated body and fins; visual child colliders are disabled and the existing projectile collider remains authoritative.
- An unarmed enemy starts its existing five-second self-destruct warning even if its wheels still work; detonation still requires a nearby player. This is one candidate for the reported stalemate, not a completed comparison against retreat behavior.
- Unity 6000.3.14f1 EditMode: 23 passed, 0 failed, exit code 0. Evidence: `Artifacts/Local/RC2-RocketStalemate-Green-3ad50a65-20261107T222212Z/` (local, ignored).
- Before the fix, targeted reproduction: 1 passed, 3 failed, documenting the player reload, projectile shape and mobile unarmed enemy defects. Evidence: `Artifacts/Local/RC2-RocketStalemate-Red-17df25f8-20261107T222039Z/`.
- Windows Player and subjective gameplay comparison for these changes: NOT RUN at this commit. No claim that either enemy behavior is more enjoyable.
