# B01-11 / P17 W14 — UXCOMFORT-001 R1 | original visual-stimulus governor

**Runnable original pure C# gameplay-accessibility source**, distinct from B01-11 UXCAPTION (#534), UXVIEWPORT (#498), UXFOCUS (#424), UXFEED (#438) and B01-12 A11Y-PREFS (#479). This is a runtime **effect recommendation** engine, not another accessibility settings UI, VFX asset or Unity camera controller. Does **not** guarantee photosensitive safety or medical accessibility, and must never be marketed as an epilepsy-safe mode.

## Actual behavior
The standalone [`Runtime/VisualStimulusGate.cs`](Runtime/VisualStimulusGate.cs) accepts host-authoritative `VisualCue` events with run ID, strictly increasing sequence, monotonic milliseconds, one of seven typed visual effect kinds and intensity 0..100. Each event produces immutable `VisualDecision` with `RenderMode` (`TimedEffect`, `ReducedEffect` or `StaticIndicator`), semantic glyph `Indicator`, severity/intensity and reason `CueOutcome`. It DOES NOT render Unity VFX, manipulate player/camera, create Unity components, trigger audio or store player profiles.

- **Flash events** (`ExplosionFlash`, `DamageFlash`, `CriticalFlash`): configurable cap 0..4 in an exact **rolling 1000ms window**, configurable minimum gap 0..1000ms; default at most **2 effects/sec and 250ms gap**. Exceeding the budget yields a **static semantic indicator**, including `Critical`, so the event remains expressible without a new flash. Standard mode clamps requested strength to 60.
- **ReducedMotion**: at most one flash per 1000ms, min 500ms gap, reduced strength <=20; camera/part shake becomes static glyph. **NoFlashes**: all flashes static while normal limited shake may still run; the mode's scope is explicit. These are conservative UX heuristics, NOT biological safety limits.
- **Shake effects** (`CollisionShake`, `PartDetachShake`): clamp intensity to <=45 by default and configured cap. Zero shakes produces a static symbol. Pickup and critical-warning non-flash events always yield a static glyph and never debit the flash budget.
- Switching mode via `SetMode` retains the prior flash history, so toggling modes cannot reset a cooldown. `AdvanceTo` expires old flash stamps. New run clears per-run history while retaining the chosen mode; old-run inputs ignored; malformed/future run, stale sequence, nonmonotonic time, invalid kind/intensity are rejected before state changes.
- Queue of accepted flash timestamps remains strictly bounded by cap <=4; 5000-event flood is covered by actual original C# tests. `long.MaxValue` timestamps and expiry boundaries are validated.

**State of game:** no `Assets/**`, no VFX prefabs, no Unity .meta/GUID/asmdef, no camera/weapon logic. Real Unity Editor/PlayMode/PhysX, Player effects, brightness/contrast measurement, HDR, post-processing, real user accessibility evaluations **NOT_RUN**. If adopted, only Codex may attach source to protected Unity VFX after B01-00 routing/ACK. Existing B01-12 preference engine (#479) is a separate possible upstream source of modes and is NOT imported here.

## Provenance
Own isolated `research/b01-11-p17-w14-uxcomfort-001` based on unchanged main SHA `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. [Real original linked-source GitHub CI #41087229667](https://github.com/C8LyNn/RoboRougelike/actions/runs/41087229667): **98/98 C# PASS Ubuntu and 98/98 C# PASS Windows**, actual .NET8 source and test compilation, 196 assertions, no fail. Re-run **final packaged SHA** separately before source release. All payload bytes/SHA256/Git blob SHA1 recorded in `manifest.json`, which deliberately excludes self-hash.

Rollback: drop only isolated `Research/B01-11/P17/W14-UXCOMFORT-001/` and `.github/workflows/b01-11-uxcomfort-w14.yml` or delete the branch; all canonical Unity gameplay, main/develop and frozen Secure V2 #510 remain unchanged.


## W15 R2 — zero-intensity effect budget and false-animation repair

**A real R1 defect** was found in the original `VisualStimulusGate.Record`: a `DamageFlash`, `ExplosionFlash`, or `CriticalFlash` event with `Intensity=0` was given a `TimedEffect` (or reduced mode's `ReducedEffect`) and consumed the rolling flash budget, even though no nonzero visual stimulus was requested. A subsequent meaningful flash could be rejected by the capacity/gap rules. `CollisionShake` and `PartDetachShake` at zero amplitude similarly generated a timed camera-shake directive.

**R2 corrective behavior**: after validating the input and advancing the authoritative event sequence/time, zero-strength flash and shake cues immediately return the same typed **`StaticIndicator`** semantic glyph and `ReplacedByStatic` outcome. They do **not** add a flash timestamp, block a later nonzero flash, or schedule camera shake. `CriticalFlash` at zero remains a static **Critical** indicator, not a silently lost warning. All existing settings, flash limits, positive-strength behavior, bounded memory, rejection rules and run isolation remain intact. A zero-strength cue still consumes its **source event sequence** and advances the clock; it cannot be replayed.

Reproduced in executed original source using [W15 RED #41091279821](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091279821) at test-only SHA `a06d5e0b59a9fb24c38de5b54ed63bef74717fd9`: **102 PASS / 14 FAIL on Ubuntu and Windows** (18 new test cases). Source change `2a2bd304454efc3cc3bb825e5617b669e29b59f5` produced [W15 GREEN #41091389393](https://github.com/C8LyNn/RoboRougelike/actions/runs/41091389393): **116 PASS / 0 FAIL each OS**, original linked production source and old 98 cases. This is source code validation, **not** actual Unity graphics, VFX, native EditMode/PlayMode or medical photosensitivity clearance. [Full evidence](EVIDENCE/W15_ZERO_STIMULUS_RED_GREEN.md).

Historical immutable R1 producer SHA `866ab291924be2449fe8b295a1c1291259183608` remains available; this R2 update is performed only in the **producer-owned isolated Draft #565**, not any Codex-claimed or offered version.
