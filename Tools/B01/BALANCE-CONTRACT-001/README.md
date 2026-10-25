# B01-10 W12 contract release R1 — runnable producer packet

Authorized role dispatch [#65](https://github.com/C8LyNn/RoboRougelike/issues/65#issuecomment-6400799836). **Execute:** `node Tools/B01/BALANCE-CONTRACT-001/validate.mjs`. Requires Node >=20; no external modules and no Unity. `contract.json` is canonical design decision; `fixtures.json` gives reproducible PASS/REJECT and rejection codes; `validate.mjs` checks contract invariants, 68 fixture scenarios and 3 negative mutation controls. `EVIDENCE.json` records actual V8 execution, never a Unity test. Workflow repeats Node validation on this Draft PR. Files only here and the owned CI workflow.

**Evidence provenance:** BUILD #239 original Unity6000.3.14f1 native compile PASS, 34/38 NUnit PASS + 4 actual FAIL each reproduced twice; 10/10 Player screenshot error shader, real R3/R6 driving NOT_RUN. R0 vs R1 legendary mass contradictions; CAMERA #234 actual ray/pivot failures. Fixture PASS is **portable policy result**, not PhysX/Unity/playable certificate.

**Flow:** B01-20 builds isolated BUILD candidate, B01-22 adapts LEGENDARY, B01-30 produces measured visual/socket assets, B01-21 handles MAPGEN. B01-60 independent Unity, B01-40 package and target gate, B01-00 G2. Codex only later integrates, never from this source-only design PR. **No Unity code, no main/develop merge, G2 HOLD.**
