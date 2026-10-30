# UXCOMFORT-001/R1 — read-only source receiver; no authorized canonical Unity integration

After **independent B01-40 source G0 and B01-60 source QA**, B01-00 may decide if/when to offer this packet to CODEX INBOX #79; ONLY Codex explicitly ACKs and owns actual `Assets/**` VFX integration. No parallel editing of already frozen #510 or other developers' effects. Source stays isolated and unchanged after OFFER.

## Reproduce
```sh
git fetch origin research/b01-11-p17-w14-uxcomfort-001
git worktree add --detach /tmp/uxcomfort origin/research/b01-11-p17-w14-uxcomfort-001
dotnet run --project /tmp/uxcomfort/Research/B01-11/P17/W14-UXCOMFORT-001/Tests/UXComfortTests.csproj -c Release
```

### Optional Codex adapter contract
Initialize `VisualStimulusGate(runId,mode,maxFlashesPerSecond,minFlashGapMs,maxShakeIntensity)`; convert B01-12 upstream preference labels to this module's enum explicitly (NOT imported, NEVER merge sibling packages implicitly). Feed authoritative `VisualCue` including stable runId, unique monotonically increasing sequence, nonnegative monotonic time in **milliseconds**, valid `VisualCueKind`, 0..100 intended strength. Each `Record` returns `VisualDecision`. Map `TimedEffect`/`ReducedEffect` to actual **Unity** VFX/Camera effects only in a Codex-owned adapter and `StaticIndicator` to an always-readable nonflashing icon/text alternative; **never drop critical feedback because a flash was rate-limited**. `SetMode` preserves cooldown timestamps to avoid bypass through rapid toggles. Call `StartNewRun(newId)` on a genuine new run. This is a scheduling advisory; URP post-processing, shader emissive peaks, ambient flicker, unrelated camera motion and external flashing sources are OUT OF SCOPE and must be independently audited. Not certified photosensitive-safe.

**Rollback:** remove own Research branch/workflow before any integration, or let Codex exclusively revert its own adapter later after an authorized integration. No canonical game files or assets modified in this package.


## W15 R2 host semantics: zero is no visual stimulus
The source R2 package changes no protected gameplay or Unity VFX. Future Codex-only authorized adapter MUST treat input intensity `0` as a **non-animated semantic indicator**, not a new flash, bloom pulse, particle emission or camera shake. A source event is still considered processed for monotonic run+sequence validation. Subsequent positive-intensity flashes may proceed subject to the original rolling budget and the earlier **actual nonzero** flash timestamps. `CriticalFlash` and `CriticalWarning` retain a static critical symbol even if all flashing is suppressed. Avoid inventing any photobiological certification: other shaders/HDR and motion sources can still flash. Native/Player validation NOT_RUN. Rollback remains discarding this own isolated W14 Research branch/workflow or selecting historical R1; Codex-owned future adapter may only be modified by its authorized owner after ACK.
