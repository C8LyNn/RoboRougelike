# Passive energy and checkpoint: targeted test preparation

Base: `d428cf2d` (Draft PR #1129). No gameplay or storage code changes in this follow-up.

A focused Unity EditMode test has been prepared to mount a tier-2 battery and tier-3 wheel motor on the real R3 model, save and restore its in-memory checkpoint, and verify their IDs and tiers, the 675 maximum energy, 123 current energy, and the absence of a second initial refill. It uses no player checkpoint file and does not itself test a Windows Player restart or disk persistence.

Verification on 2026-11-08: the editor/test assembly compiled with Roslyn using the existing local Unity references and runtime from #1129 (exit 0). An attempt to call the real checkpoint restore through standalone .NET failed with `ECall methods must be packaged into a system module`, because this path needs Unity native calls. It did not establish whether the checkpoint is accepted or rejected. The new Unity test, native Editor, Player, and disk save/load remain NOT RUN under the lightweight-work constraint. Local ignored compiler logs: `Artifacts/Local/CSharp-PassiveCheckpointTests-20261108/`.

Follow-up: run `BacklogEnergyRegenTests.CheckpointPreservesTieredBatteryPassiveReservationAndCurrentCharge` in Unity and separately perform an isolated cold save/close/load with a real Windows Player if later requested. No build was prepared.
