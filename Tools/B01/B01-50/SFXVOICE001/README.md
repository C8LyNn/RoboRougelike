# B01-50 · SFXVOICE-001 R1 — optional audio voice budget, actual Unity AudioSource reuse

**Producer:** B01-50 Build Performance & Profiling. **Dispatch:** [#450](https://github.com/C8LyNn/RoboRougelike/issues/450). **Ownership:** only `Tools/B01/B01-50/SFXVOICE001/**` and one new dedicated `.github/workflows/b01-50-sfxvoice001.yml`. **Zero writes to canonical Codex Assets/**, gameplay, audio emitters, scenes, saves, models or other swarm packets. No merge.

## Original functionality, independent of FXPACE-001

The standalone pure C#9 `Core/OptionalSfxVoiceBudget.cs` has a **hard maximum of 1–32 simultaneous, dispensable audio voices**, a max audible distance, a bounded per-category replay cooldown, deterministic slot assignment and a deterministic optional `Accented` over `Ambient` priority. If all slots are occupied, a higher-priority *optional* event steals the **farthest lower-priority** voice (tie → first slot); equal-priority events do **not** steal, reducing audio clicks. Expired/finished slots are recycled; token matching prevents a stale completion from stopping a replacement. `Clear()` cancels active voices on scene disable. All request logic is deterministic at explicit monotonic timestamps; no heap allocation inside the core request path after construction. Cooldown history is intentionally capped to at most 128 entries (old keys may age out under heavy multi-category usage).

`Unity/B0150OptionalSfxVoices.cs` is an **actual optional Unity MonoBehaviour adapter**: on an authorized sandbox scene, it creates up to 32 child `AudioSource` instances **once**, reuses them for eligible clips, applies real 3D distance to an explicitly assigned listener transform, stops/replaces the old clip when core authorizes it, and releases finished/expired sources. `Unity/B0150OptionalSfxBurstDemo.cs` is an opt-in inspector context-menu burst demo; no automatic game integration or source edits.

**NO essential sounds.** This scheduler is restricted to *optional* ambience and foley. Never route attack warnings, danger telegraphs, core weapon cues, enemy detection warnings, aiming cues, accessibility information, dialogue, mission progress or hit/damage/physics events through it. Dropping an optional sound is intentional during overload; the model is not designed for gameplay-audio correctness. Sounds can stop on higher-priority optional replacement. Using the system for core gameplay audio requires new review and cannot be inferred from this isolated package. It does not modify any real hit timing.

## Execute original C# tests

```sh
dotnet run -c Release --project Tools/B01/B01-50/SFXVOICE001/tests/SFXVOICE.Tests.csproj -- --receipt /path/to/NEW-source-receipt.json
```

Only .NET 8 SDK is required; zero NuGet dependencies. The test project **links and compiles the exact same original source** from `Core/OptionalSfxVoiceBudget.cs` with compiler warnings as errors, and tests constructor validation, distance rejection, bounded active voices, cooldown, deterministic lower-tier preemption, ties, stop-token safety, expiry, clear, negative timestamps, and 500-request/20-preemption stress. Tests use synthetic times **only**; no audio hardware, Unity Editor or Player is executed by that .NET harness. The command refuses overwriting an existing receipt. The dedicated workflow uploads original stdout, JSON and SHA256SUMS. Verify actual Actions result for the exact final SHA rather than assuming success.

## Unity 6 URP opt-in smoke test (separate native gate)

In an **explicitly authorized disposable Unity 6000.3.14f1 URP scene/project only**, copy `Core/OptionalSfxVoiceBudget.cs`, `Unity/B0150OptionalSfxVoices.cs` and optionally `Unity/B0150OptionalSfxBurstDemo.cs` to a new independent sandbox folder, letting Unity generate fresh sandbox .meta. Add the optional audio adapter to a disposable GameObject, assign an **explicit listener Transform**, assign an optional AudioClip to the burst demo and invoke `BurstOptionalSounds` from its context menu. Observe actual `AudioSource` count <= configured cap, optional suppression and replacement. Collect Unity Editor/PlayMode log, source SHA, Audio Profiler voice count and hardware latency separately. This native execution **has not been run** by the producer; do not call it G1 PASS or claim measured FPS/audio latency improvement.

## Independent QA and rollback

B01-40 checks exact manifest, owned paths, source Git blob and true Actions JSON/ZIP artifact; B01-60 independently replays C# original tests and inspects adapter for accidental essential-sound routing, duplicate child AudioSource creation, reload behavior, mutable source and unsafe preemption. B01-00 controls any Codex routing after independent QA. Codex is busy; **CODEX_SENT and CODEX_ACK are not asserted**.

**Rollback:** remove only the isolated Tools folder and dedicated workflow from the draft; in an authorized disposable sandbox remove its added GameObjects/scripts and generated .meta. The original RoboRougelike Unity Assets, Prefabs, save files, scenes, canonical VFX/audio and Codex sources are never modified. **Native Unity, actual hardware output, FPS, GC, audio latency = NOT_RUN/NOT_MEASURED.**
