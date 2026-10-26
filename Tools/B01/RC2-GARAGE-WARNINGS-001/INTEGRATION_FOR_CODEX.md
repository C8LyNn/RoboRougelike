# FOR CODEX / ONLY AFTER B01-00 AUTHORIZATION — tiny one-way UI hook

**DO NOT INTEGRATE from this Draft as if G2 approved.** Read-only C# `GarageWarningPolicy.Evaluate(GarageSnapshot)`; output is display-only message codes + optional suggestions. Production R1/R3 still owns `CanDeploy` and `StartNextEncounter`. Do **not** use `LaunchDecision` to set `CanDeploy` or override it, nor replace current repair/rebuild semantics.

1. Map actual `R3VehicleModel.Parts` **mounted** entries to `GaragePart`: preserve physical ID, kind, health and signed `Cell.x`; trusted Core connectivity from existing `Connected(alive)` plus real per-part graph. `Operational` for drive/weapon must be validated from actual R6/R4 capable state (not only `Health>0`, prefab presence, cooldown frame or camera). Unknown state -> `null` and explicit UNVERIFIED warning, never invent an operational pass. W14 current R3 `HasMobility` is a baseline physical proxy and still needs actual contact/drive verification.
2. Resolve current activity using authoritative challenge selection; `ActivityRequiresWeapon=true` for normal combat, `false` only if genuinely weaponless mode exists and HQ policy permits it, else `null`. This is presentation policy, not future feature authorization.
3. Wire `SuggestionsEnabled` from existing building-assistant toggle; render `Critical` **unconditionally** in visible garage area on both resolutions, allowing temporary overlays and repair progression to be legible. Show localized reason code, not a generic red icon. Draw suggestions only when enabled.
4. Recalculate after each mount/dismount/repair/0HP/drive selection/load, not only upon opening garage. Zero subscriptions/leaks after closing/scene switching.
5. Do not edit production start gate or loaner logic without separate HQ authority; evaluate warning and leave canon `CanDeploy` untouched.
6. Real full Windows Unity run required for three bounded cases in README; do not invent EditMode/PlayMode PASS or a weaponless race feature. Exact game RC2 source/target must be pinned and rollback-ready.

**Known limitation / safe interpretation:** This pure policy is not connected to existing R3/R6 by itself. It only verifies the desired display contract under explicit *supplied* canonical fields; G1 native and G2 all HOLD.
