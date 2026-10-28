# PASS16 — Atomic terminal victory reward

Base `589b60a912096d1e9917cb0bbac73a53e7543b4f`. B01-22 reported that a corrupted reward catalog causes WinEncounter to throw after scrap is awarded; because the run remains in Combat, future ticks can pay repeatedly. Actual Unity Editor real-scene regression reproduced: after reducing the three-choice catalog to two entries and defeating enemies, one tick changed wallet from 0 to 80 even though victory failed (red XML, exit 2). This is a deliberately corrupted configuration, not a claim of ordinary-player reachability.

The R1 controller now completes catalog validation/victory transition before changing LastScrapReward, wallet, EarnedScrap or DefeatedEnemies. On invalid catalog it stops simulation and displays an error, leaving the run and wallet unawarded; repeated ticks do not pay. No reward amount, defeat priority, economy or valid bonus selection changed.

Unity 6000.3.14f1 Windows targeted EditMode including actual Play Mode: **32/32 PASS**, exit 0, including corrupted-catalog scene, ordinary rewards, duplicate claim prevention and invalid weapon integrity. Raw XML and exits in Evidence; Editor logs local `Artifacts/Local/P16Reward/`. Windows Player / independent B01-22 and B01-60 verification NOT_RUN; no G1/G2 or merge.
