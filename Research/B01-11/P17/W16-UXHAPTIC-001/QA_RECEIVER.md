# UXHAPTIC-001/R1 — B01-40 external G0 & B01-60 independent source QA

**Final exact SHA and 9 payload fingerprints** are frozen in `manifest.json` (SHA-256, UTF-8 byte counts, Git blob SHA-1; manifest excludes itself). PR diff must have **10 paths exclusively** inside `Research/B01-11/P17/W16-UXHAPTIC-001/**` plus the single dedicated `.github/workflows/b01-11-uxhaptic-w16.yml`; no `Assets/**`, Codex, #510, #556, save, rollback or foreign owner files.

**B01-40 independent SOURCE G0:** fresh exact SHA fetch, individually recompute all 9 payload digests/sizes/Git blobs and 10 owned paths, ensure original C# source and deterministic test project exist, no Unity install claim. Review haptic control signal `SetEnabled(false)` stop-motors contract, mandatory visual critical fallback, no state pollution by prior-run events, saturating time arithmetic and discard-only rollback. Report own standalone G0/HOLD with exact receipt; producer self-check cannot be external signoff.

**B01-60 independent source QA:** run actual `dotnet run --project Research/B01-11/P17/W16-UXHAPTIC-001/Tests/UXHapticTests.csproj -c Release` at EXACT SHA, expect **114 original executable checks**; independently challenge critical vs major immediate preemption, 180ms cooldown equality, disable while running, 1000-event flood, stale-run Critical visual false positive, hard zero intensity and invalid side/kind, long timestamp saturation, absent physical hardware. Source QA is not native G1.

**B01-00** coordinator owns optional source offer #79 after formal external G0/QA; **Codex** alone ACKs and authors any future Unity Input System gamepad motor adapter + native G1. External source QA/G0 and Unity G1 are all **PENDING/NOT_RUN** for this producer; no merge authorized. [INSTALL_FOR_CODEX.md](INSTALL_FOR_CODEX.md) contains the optional Codex-owned contract and rollback.


## W17 / R2 additional independent G0 and QA criteria

**B01-40:** pin final package SHA; verify 10 payload SHA256/UTF8 byte lengths/Git blob SHA1 + 11 own PR changed paths (own W16 research folder + dedicated workflow). Confirm `StartNewRun(int)` return bool contract is documented, old source invocation statements remain valid and old R1 SHA `82cc7518b12ab530a3a819af0ffe9072c88c9675` preserved. No imported Codex `Assets/**`, game #510 or Save #556 changes. Rollback discards producer R2 research changes; no real motor needs reverting in the game.

**B01-60:** independently execute all **126 original C# tests on final SHA** and specifically reproduce RED missing stop contract on prior runtime: 80/160/300ms active intervals, boundary exact expiry, mute/disable-before-new-run, repeat transitions, invalid next run leaves motor active, stale old-run critical no false HUD alarm. Verify source deliberately does NOT stop hardware, but reports STOP obligation. Native Input System/real controller/Unity 6000.3.14f1 G1 **NOT_RUN** until legal activated host with owner-authorized motor driver. External source G0/QA must not be inferred from producer CI or manifest self-checks.
