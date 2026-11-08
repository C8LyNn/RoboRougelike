# Battery tier labels

An inventory group can contain batteries of different tiers. The generic label `Bateria +100` and a T2/T3 item name based on it falsely implied that every instance adds only 100 energy. The group is now called `Bateria`; individual names and quick-selection messages calculate capacity using the existing `VehicleEnergyState.BatteryCapacity` and `R3VehicleModel.TierEffectUnits`. The purchase prompt explicitly describes the T1 battery. Energy accounting, prices and fusion are unchanged.

The current game values yield T1 +100, T2 +200 and T3 +400. Roslyn syntax parsing: zero errors for the three changed C# files; `git diff --check` clean. **Unity compilation, EditMode/PlayMode and rendered Windows UI: NOT RUN** during the requested lightweight work. The previously packaged `5b21` build predates this change; it is queued for the next Player check.
