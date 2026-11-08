# Garage drive unlock guidance

Base: `2bb1cc51` (passive-energy checkpoint test).

The drive selection panel now explains the existing `R3VehicleModel.HasMobility` requirements: functioning wheels or tracks on both sides, or at least one functioning Omni or Hover module. The hover status no longer exposes the internal MOV009 identifier. This change does not alter eligibility, vehicle physics, or deployment rules.

## Evidence

- Runtime C# Roslyn compilation using existing Unity reference assemblies: PASS, exit code 0; local log `Artifacts/Local/CSharp-DriveUnlockHint-20261108/runtime.log`.
- Unity EditMode / PlayMode, GUI appearance, and Windows Player: NOT RUN in this light-work session.
- Suggested manual check: open garage vehicle settings, inspect the drive tab, remove one wheel or track from a side and confirm the relevant button is disabled; mount the required healthy parts and confirm selection is enabled. Repeat with Omni and Hover, and inspect the layout at the supported resolutions.
