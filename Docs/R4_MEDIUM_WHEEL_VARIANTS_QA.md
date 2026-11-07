# Medium side wheel appearances — QA

Sources: PR #1018, `fb6910c29acd07989f1ad2a36b8ec5709ebfc9ce`; medium side offroad and slick FBX. Slick SHA-256 `ec3ac77d283752f2d1f2f745fda9a23addfb87ef8f6d77008ca901855bd8a6ca`. Imported through Unity 6000.3.14f1.

Select an installed, working wheel in the garage with Zaznacz; the wheel appearance button switches between offroad and road. Style is copied with the owned part and persisted through the existing checkpoint. Old saves default to offroad. Styles cannot be mixed when fusing. Original collider, R6 wheel contacts, cost, mass and traction remain the same. The 0.37 m slick visual is scaled to the existing 0.39 m axle radius. These are visual variants, not independently balanced wheel parts.

Unity EditMode at `f7a5ef276468c3984deb8f9fe6d5c15b3ad90fed`: 19/19 passed, exit 0, including actual garage switching, checkpoint round-trip, fusion, R6 drive and independent steering. Local evidence: `Artifacts/Local/RC2-WheelVariants-f7a5ef27-20261107T230006Z/`.
Windows Player `06a1c5b6` built successfully; opened garage, selected a wheel and switched from offroad to road through its button; the model changed. Returned to menu and exited. Cold restart of a prior save, performance profiling, arbitrary rotated mounts and distinct terrain behavior remain NOT RUN. The remaining ten source models and other block shapes are NOT INTEGRATED.
