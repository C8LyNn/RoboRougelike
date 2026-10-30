# B01-11 PASS17/W17 UXHAPTIC-001 R2 — new-run motor-stop contract

## Root cause in source
Prior immutable UXHAPTIC-001/R1 at `82cc7518b12ab530a3a819af0ffe9072c88c9675` accepted real host events and maintained `_activeUntilMs` to model scheduled pulse time. Its `StartNewRun(int)` returned `void` while clearing pulse state, making it impossible for a future Unity driver to know from that transition whether a still-active previous run motor must be physically stopped. This is a source-level integration contract defect; no physical motor has been installed or observed.

## Test-first source RED and repair
- 12 additional actual linked production C# tests at test-only source SHA `eacbbb35a440edb83405919ffd34a7f11b3fc9bb` without runtime modifications. Tests use reflection to validate return-type and behavior without failing the whole prior R1 compilation. [Workflow #41100770919](https://github.com/C8LyNn/RoboRougelike/actions/runs/41100770919), check both job results rather than guessing.
- Actual production source changed at `fc9046c022d1a0512b2858f43bc82cee41fef11a`: `StartNewRun(int)` now returns whether `_enabled && _activeUntilMs > _nowMs` **before resetting** the run. No changes to motor intensity, source event validation, event semantics or previous 114 checks. [GREEN workflow #41100849650](https://github.com/C8LyNn/RoboRougelike/actions/runs/41100849650); separate exact SHA final-package CI required.

## Explicit source limits, ownership, rollback
Host must consume returned stop flag and physically reset device motor speed, not just clear in-memory model. Neither a Unity Input System adapter nor physical device/Player tests exist in this package. Frozen game #510, Save #556, Codex sources, main/develop unchanged; branch owns only own W16 UXHAPTIC. Historical R1 immutable. Independent B01-40 G0, B01-60 source QA PENDING; native G1 NOT_RUN; Codex OFFER/ACK/INTEGRATION absent; no merge.
