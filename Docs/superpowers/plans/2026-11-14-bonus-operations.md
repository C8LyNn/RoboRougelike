# Bonus Operations Implementation Plan

**Goal:** Add additive increased/reduced and independent multiplicative more/less operations to the existing physical reward modules without changing old saves or existing default effects.

**Architecture:** Store a zero-default operation on upgrade definitions, owned part states, mounted parts and blueprints. Calculate each supported stat from one additive group and independent per-copy multipliers; preserve the existing inverse meaning of reload bonuses. Use finite saturation as a numerical guard, not a gameplay stat cap. Repair modules keep their existing behavior until explicitly supported. New scoped/typed damage remains a separate subsequent step.

**Tech Stack:** Unity 6000.3, C#, Unity Test Framework.

1. Write failing tests for authored operations, 3:1 fusion identity and checkpoint/blueprint compatibility, plus a live mounted damage scenario combining additive and multiplicative modules.
2. Add the zero-default serialized operation and validate it consistently in reward intake, fusion, checkpoint and blueprint paths.
3. Apply supported operations to existing stat groups, respecting per-copy tiers and the reload direction; update visible descriptions.
4. Run the new scenarios and targeted legacy bonus, fusion and save tests; document actual results, remaining categories and limits.
