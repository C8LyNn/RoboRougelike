# UXCAPTION-001 R1 — read-only producer-to-Codex receiver contract

**Source only; not approved to integrate until formal B01-40 G0, independent B01-60 QA and B01-00 #79 offer, then explicit Codex ACK.** No existing game `Assets/**` or audio source is modified by this Research PR.

## Reproduce on exact source SHA
```sh
git fetch origin research/b01-11-p17-w12-uxcaption-001
git worktree add --detach /tmp/b01-11-uxcaption origin/research/b01-11-p17-w12-uxcaption-001
dotnet run --project /tmp/b01-11-uxcaption/Research/B01-11/P17/W12-UXCAPTION-001/Tests/UXCaptionTests.csproj -c Release --no-launch-profile
```

## Future optional Codex-owned adaptation
When Codex authorizes and owns actual gameplay integration, on a verified *live authoritative sound event*, send one `SoundCueInput` with current run ID, strictly increasing sequence, event timestamp in ms, enum kind, whitelisted key from `Localization/` files (e.g. `cue.warning`), structured source `SourceId`, and a host-computed signed bearing relative to player forward in degrees (-90 left, +90 right). No sampling system, microphone, audio-file transcription, sound generation or source spoofing is provided.

A Codex-owned HUD/UI screen may read `CaptionTrack.Visible()` snapshots, translate `LocalizationKey` via game localization, use `Direction` to translate PL/EN direction label, and append `Occurrences` with locale's `repeatFormat`. **Never display untrusted sourceId** as prose. The host must perform event routing, Unity color/fonts/layout, visual captions, screen reader, sound-to-visual accessibility setting, pause/reset and actual Unity 6000.3.14f1 EditMode/PlayMode/Player QA. This producer has no Unity .meta/GUID/asmdef, prefab, sound hook, HUD input or authorization to edit canonical gameplay. No integration acknowledgment can be inferred from this draft.

## Risks and rollback
- If localized font shaping or text wrapping inflates HUD height, the player UI host is responsible for bounds: UXVIEWPORT-001 R2 (#498) is **separately pending source QA, not installed**. Do not assume it is available. Actual HUD layout requires separate Codex decision.
- Local time must be nonnegative/monotonic; drops in a fully urgent queue return `DroppedNormalForUrgent` but consume sequence and cannot be replayed. User controls or disable caption setting belong to host.
- Rollback now is dropping the entirely isolated `Research/B01-11/P17/W12-UXCAPTION-001/` and own `.github/workflows/b01-11-uxcaption-w12.yml` or deleting its branch; there are **zero** canonical game changes. After Codex integration, rollback the Codex-authored adapter/source independently.
