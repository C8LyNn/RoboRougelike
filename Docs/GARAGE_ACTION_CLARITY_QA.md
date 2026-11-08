# Garage action and wheel clarity

- Disabled purchase captions now explain insufficient scrap or full storage; existing bonus-only and technology-locked captions remain. The same caption is used in both simple garage layouts and both full catalog layouts.
- Inventory repair buttons distinguish a healthy part, unavailable repair, missing scrap and an available paid repair. Repair and purchase enablement and transactions remain unchanged.
- Selecting a stack containing only destroyed items now directs the player to its instances and repair. It does not automatically spend scrap or repair an item.
- For an owned or inspected wheel, mass comes from the existing `PartMass` calculation and nominal force from `WheelTraction * TierWheelTraction` at 100% HP. This avoids showing T1 figures for T2/T3 wheels. It is a relative force value, not a guarantee of actual grip: damage, ground contact, mass and terrain still matter. Mounting hints for fork wheels remain available.

No Unity or Windows Player was launched in this lightweight pass. Roslyn C# syntax parsing: 0 errors in the three modified C# files; the four purchase-caption invocations have the expected argument counts; `git diff --check` clean. This is **not** a Unity semantic compile or visual/UI acceptance. Verify long names and button labels in both aspect ratios and at 1280×720 and 1920×1080 in the next Player. The previously distributed `5b21` ZIP does not contain these changes.
