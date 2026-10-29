# W04 real original C# regression and recovery — UXHELP R2

**Package:** B01-11-P17-UXHELP-001/R2; producer C# content change; no Codex-owned Assets touched. Previous version R1 immutable at `08633b2dab05baaa929065483938d9c3568c4028`.

## Reproduced original R1 defect, unmodified runtime source
New original C# test cases committed first at `36ad23241d71319c0d287bf8d7e54265a83e06e2`, with old unchanged `Runtime/UXHelpCore.cs`. [GitHub Actions RED #40965728452](https://github.com/C8LyNn/RoboRougelike/actions/runs/40965728452) **Ubuntu 48 PASS/5 FAIL, Windows 48 PASS/5 FAIL** (exit failure). Exact failed cases: (1) inactive dual-gun must not recommend firing; (2) first combat inactive guns must not get firing controls; (3) one active of two assigned groups must not recommend both; (4) dismissed first-combat controls must fall back to valid dual-group hint; (5) critical core warning and optional fallback must preserve priority order. These are actual executable regressions, not source inspection guesses.

## Source repair and full retest
`Runtime/UXHelpCore.cs` fix `aa5269e96a8540c6b76f9fe41ab9b3c0274616b2`: only advertise a combat firing hint if at least one gun is operational, dual-group hint only if two guns operational; when first-combat control hint was dismissed select next eligible dual-group hint. No game-state change or Unity adapter.
Intermediate original source run [#40965823676](https://github.com/C8LyNn/RoboRougelike/actions/runs/40965823676) had a remaining **test fixture assumption**: pre-existing "Dual weapon group instruction" checked for two assigned groups with only one mounted/active gun; that is no longer a valid condition for advising two firing groups. Repaired same existing source test fixture to two mounted + two active without dropping any assertion, commit `9c490a8255fc298058aecd2e3ddfa96ad6baddcb`.

**[GitHub Actions GREEN #40965931366](https://github.com/C8LyNn/RoboRougelike/actions/runs/40965931366), original source exactly on `9c490a8255fc298058aecd2e3ddfa96ad6baddcb`**: **Ubuntu original .NET8 C# 53/53 PASS; Windows original .NET8 C# 53/53 PASS; 106/106 executed, 0 FAIL**. All previous 46 cases and all 7 new cases still enabled. Binary/runtime compiles on both CI OS, **this is not Unity Editor/Player native execution**.

## Scope and receiver
B01-40: validate all package SHA256/bytes/blob, exact changed paths, confirm adapter reliance on authoritative `ActiveWeapons`, `MountedWeapons`, `AssignedWeaponGroups`, inspect optional first-combat fallback and rollback.
B01-60: independently execute original 53 tests on exact R2 SHA, ensure RED mutants stay caught, inspect copy/actionable hints and no fake operational states; game native G1 NOT_RUN.
B01-00: only after independent G0/G1 may offer R2 to CODEX INBOX #79; no ACK/claim or integration currently.
