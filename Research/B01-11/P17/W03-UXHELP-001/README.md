# B01-11-P17-UXHELP-001 / R1 — Polish & English game help

**Real new localized gameplay-content source and tested C# hint selector**, not another screenshot/HTML prototype. Original `Runtime/UXHelpCore.cs` holds 10 original Polish and English messages and a deterministic selector for the player. Distinct from [FRAME-UX #364](https://github.com/C8LyNn/RoboRougelike/pull/364) and [COMBAT-CUES #341](https://github.com/C8LyNn/RoboRougelike/pull/341). Existing Codex R1 HUD source reference: [#382](https://github.com/C8LyNn/RoboRougelike/pull/382) `a06dea84e11859a519a42f89c88b2dcdb055646f`.

Core contract: `UXHelpCore.Select(HelpSnapshot state, ISet<string> dismissed=null)`. Pure C# (no UnityEngine); results contain distinct IDs, severity, PL/EN text and critical-first deterministic order. Authoritative, host-supplied fields include stage, drive type, true selected-drive readiness, mounted/active weapons, core HP, actual checkpoint availability, first-combat marker, optional hints toggle and assigned weapon groups. **Never infer actual readiness from this tool.**

Safety: missing drive and weapon appear as TWO separately returned critical reasons even with optional hints disabled or IDs dismissed. In combat, low positive core HP and lost active guns appear as independent critical alerts. Only one optional hint is suggested (drive-specific controls, weapon-group instruction, checkpoint option, reward). Unsafe/incoherent source state is rejected rather than guessed. No gameplay economy, save, damage or UI mutation.

Run source-backed tests: `dotnet run --project Research/B01-11/P17/W03-UXHELP-001/Tests/UXHelpTests.csproj -c Release`. Dedicated GitHub Actions workflow builds/executed **original actual C#** on Ubuntu and Windows: [#40908953821 SUCCESS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40908953821), 46/46 per OS, zero FAIL. Native Unity/Player and human playtest NOT_RUN.

B01-11 only owns `Research/B01-11/P17/W03-UXHELP-001/**` and dedicated `.github/workflows/b01-11-uxhelp-w03.yml`. No `Assets/**`, Codex #382/#383, CHL, PARTSIZE or other producer paths changed. Base `main@5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`. Independent receiver: B01-40 G0 → B01-60 source QA → B01-00 → Codex #79 only after authorized offer. No merge or CODEX_ACK.

## WAVE04 / R2 — source bug fixed (2026-10-29)
Original runtime C# no longer claims inactive weapon groups can fire; optional hint selection falls back after dismissal. **[True original RED 48/5 each OS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40965728452) → [GREEN 53/53 each OS](https://github.com/C8LyNn/RoboRougelike/actions/runs/40965931366)**. R1 source SHA preserved. R2 **independent QA PENDING / Codex ACK PENDING**. See `EVIDENCE/W04_RED_GREEN.md` and manifest R2.
