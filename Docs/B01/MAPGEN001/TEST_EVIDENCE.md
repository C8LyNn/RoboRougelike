# MAPGEN001 R1 execution evidence log

Initial source creation:
- Git commit: see current Draft PR HEAD, preserve immutable SHA in #93 after upload.
- C# Unity compilation: **NOT_RUN** (no licensed Unity environment in this connected execution runtime).
- Unity EditMode: **NOT_RUN**, XML none, exit code N/A.
- Unity PlayMode: **NOT_RUN**, XML none, exit code N/A.
- Physics manual rover drive: **NOT_RUN**, screenshots/video none.
- Independent B01-60: **NOT_RUN**.
- Static structure and Git committed-file existence: can be checked independently from published Git tree; no implicit gameplay PASS.

After a future actual execution append new dated evidence, exact SHA, command, environment, PASS/FAIL/SKIP and hashes. Never edit this record to hide prior failures.

## R3 / B01-20 producer evidence (2026-10-24)
- GitHub source review confirmed R2 HEAD `a2993a4cb09275864a04c6eaf8bc7b9f4dfb9946`; R3 producer commits were written on the same isolated branch, preserving R1/R2 sources.
- Static code editing checks: 7 scoped C# updates were submitted with expected GitHub blob SHAs; each edited C# file passed a simple balanced-braces check **only**, which does not establish C# compilation or full syntax/semantic correctness. PR file scope and manifest verification: separately recorded after source freeze.
- C# compilation with Unity 6000.3.14f1: **NOT_RUN** (no installed activated Unity available in this execution environment).
- Unity EditMode (100 seed x 2 recipes and 2 reference profiles + negatives): **NOT_RUN**; Unity PlayMode physics (12 AddForce/AddTorque traversals plus lifecycle): **NOT_RUN**; XML, logs, exit: N/A.
- Manual 3 seeds x 2 recipes x 2 vehicle profiles: **NOT_RUN**; screenshot/video evidence: NONE.
- B01-60 independent R3 red/green review: **PENDING**. B01-40 integration/package gate: **PENDING**. G2 READY_FOR_GPT_OSS: **HOLD**.
- Source test definitions are not results; preserve previous R1/R2 NOT_RUN reports unchanged.
