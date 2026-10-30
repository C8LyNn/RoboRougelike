# B01-11 / PASS17 WAVE12 — UXCAPTION-001 R1: spatial sound accessibility captions

**Original executable C# sound-caption cue lifecycle and real PL/EN localization data**, standalone in an isolated Research directory, no canonical Unity `Assets/**` changes.

## UX purpose and concrete gameplay contract
Visual sound captions improve accessibility to off-screen audible cues without relying solely on audio playback. **This is not the UXFEED #438 combat-damage history**, UXFOCUS #424 keyboard focus, or UXVIEWPORT #498 panel geometry. Unlike UXFEED, this track takes **host-authoritative spatial audio events** and generates caption snapshots with 8-way relative direction, localization keys and repeated sound counts; it never reads microphone/audio samples, infers events from sound, or mutates weapons, vehicles or the game economy. A game host must instrument **actual** sound events after any Codex-approved integration.

### Original `CaptionTrack` API
- `Record(SoundCueInput)` requires `RunId`, strictly increasing `Sequence`, monotonic `TimeMs`, typed `SoundCueKind`, validated `cue.*` localization key, non-PII structured source ID and a bearing in [-180,+180] degrees relative to player forward (**positive clockwise/right**).
- 8 sectors `Front, FrontRight, Right, BackRight, Back, BackLeft, Left, FrontLeft` with exact +/-22.5° boundaries. Bearing is supplied by host/camera; no claims of native positional audio math.
- A bounded (1–32 items, default 6) transient track, (1–6 visible, default 4). `CriticalWarning` and `PartDetached` cues are Urgent (6000ms TTL); others Normal (2400ms). Urgent comes first in `Visible()`; an ordinary cue cannot evict an unexpired urgent when all slots are urgent. Strict capacity and deterministic newest-first tie-break. `Visible(true)` requests urgent only. This is separate from canonical damage HUD.
- Same kind, localization key, source ID, and directional sector within 450ms coalesce into one new immutable caption snapshot with `Occurrences` updated; previously returned snapshots are never mutated. Expiration, new-run reset and sequence replay defense are explicit.
- PL/EN data in `Localization/pl.json` and `Localization/en.json`: 7 captions, 8 bearing labels and repetition placeholders. The host owns actual localization rendering, font/UI accessibility, announcements, on-screen output, audio event wiring and safe-area geometry.

## Actual execution
[W12 original C# + PL/EN data validation CI #41073897570 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/41073897570): **150/150 PASS on Ubuntu, 150/150 PASS on Windows**, original .NET8 C# source compiled directly (not a shim or mock), **300 real assertions, 0 FAIL**. First original source CI [#41073393017 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/41073393017) compiled **115/115** on each OS before 35 extra localization checks. The final packaged SHA must rerun same 150 tests.
 
**Operational limits:** source-only design; Unity scene, .meta/GUID, sound triggers, UI render, gamepad/keyboard, human accessibility and real Unity Editor/PlayMode/Player **NOT_RUN**. There is no current integrated sound caption HUD. B01-00 owns only future [Codex Inbox #79](https://github.com/C8LyNn/RoboRougelike/issues/79) OFFER after independently verified G0/QA; Codex alone ACKs and integrates.

**Scope & safety:** New `research/b01-11-p17-w12-uxcaption-001` branch from unchanged `main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`; only `Research/B01-11/P17/W12-UXCAPTION-001/**` + own `.github/workflows/b01-11-uxcaption-w12.yml` changes. Secure Reattach V2 [#510](https://github.com/C8LyNn/RoboRougelike/pull/510) reserved for B01-23/40/60/61/62; no code or status change to #510, #511 or #494. No unauthorized merge. Rollback is discarding isolated source directory/workflow or, after a later Codex integration, reverting the Codex-owned adapter separately.
