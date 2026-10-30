# B01-25 — W17 original MAGNETLANE R3 level scenario data

SOURCE DATA ONLY; no Unity runtime integration. Seven distinct authored JSON scenarios for the frozen original R3 source @ c9663a8c50599bdb0f8954c42737d8e0d8afcaec. No new Unity gameplay source/production stream, no modifications to existing R3 or to Codex/Save #510/#556/CHL-003. Game source remains immutable and in its existing Draft PR #598.

The level data covers fast crossing, bypass, edge taper, reverse polarity, partial warning transition, south lane, reverse crossing. The executable CSharp9 source data oracle imports real original R3 MagnetLaneCore.cs unchanged; it drives real Light 1.5kg and Heavy 12kg through routes and compares exact core output with an independent 20,000-sample numerical midpoint approximation, deterministic replay and eight malformed scenario rejects. This is source validation, not actual Unity/PhysX gameplay or a scene loader.

Commands from checkout root with .NET8 and Python3:
dotnet run --project Research/B01-25/P17/W17_MAGNETLANE_SCENARIOS/Scenarios.csproj -c Release -- Research/B01-25/P17/W17_MAGNETLANE_SCENARIOS/.output/results.json
python Tools/B01/B01-25/W17Scenarios/check.py --out Tools/B01/B01-25/W17Scenarios/.output/receipt.json

GitHub Actions executes original source on Windows and Ubuntu and checks exact original bytes, no foreign file edits, physical source ZIP, byte parity and foreign-empty-folder-safe scratch rollback. Native Unity EditMode, PlayMode, PhysX and human routes NOT_RUN; G0 formal independent B01-40 pending, G1 B01-60 NOT_RUN, G2 HOLD, Codex ACK none, integration none, no merge. Remove only the new W17 Research/Tools files and unique workflow from a disposable checkout; do not delete unrelated dirs, files, or historical R3 source.
