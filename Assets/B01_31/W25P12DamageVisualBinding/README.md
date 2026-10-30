# B01-31 W25 — P12 read-only visual adapter

This is the ORIGINAL W25 P12 health-to-3D visual-state binder source. Its dependency is the original [W24 visual component](../../W24ArmorStates/B0131W24ArmorDamageVisual.cs) and existing `Assets/Pack/P12_PipelineSafeDamageable.cs`. It NEVER calls `ApplyDamage`, owns no HP, Save/Load/IDs, currency, or gameplay decision. Binding the external P12 source is explicit and fail-closed; no scene auto-bind. Manual actual Unity import and G1 are NOT_RUN.
