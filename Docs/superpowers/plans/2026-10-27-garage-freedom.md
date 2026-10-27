# Garage editing freedom implementation plan

Goal: permit incomplete but structurally valid garage vehicles while preserving deployment checks and owned parts.

1. Reproduce blocked removal in the model and actual Unity garage before changing behavior.
2. Permit gun and mobility removal while retaining core, connectivity and inventory-cap guards; safely rebuild a garage vehicle with no gun.
3. Keep deployment gates and show a persistent reason in the garage; allow returning to map editing without deploying.
4. Verify zero guns/wheels, rebuilding and remounting, mobility modes, damage, bridge/core, inventory capacity, dual fire groups and map editor through real Unity tests.
5. Build Windows, test the unpacked Player without UI control, archive, report, publish a stacked Draft PR and accessible build.
