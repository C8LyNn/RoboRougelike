# Garage bonus-module wording

Bonus modules are rewards and `PurchaseToInventory` rejects buying them. The quick garage previously suggested buying a new part when only a destroyed bonus existed, and all four relevant buy buttons displayed a price despite being disabled.

The quick garage now directs the player to the owned instances and repair, and the disabled buy buttons say `Tylko z nagród`. Both landscape and portrait variants use the same caption. Purchase, repair, inventory and reward behavior are unchanged.

Verification without Unity: `git diff --check` clean; Roslyn C# syntax parsing reports zero errors for all three modified source files. This is not a Unity semantic compilation or a rendered UI test. **Unity EditMode/PlayMode and Windows Player: NOT RUN**. The older `5b21` ZIP predates this change; the follow-up check is recorded separately in `PAKIET_TESTOW_WINDOWS_5B21.md`.
