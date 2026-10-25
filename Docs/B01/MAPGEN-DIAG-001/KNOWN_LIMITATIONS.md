# Evidence bounds / honest gates
- The CI diagnostic suite is headless **real C# MapGen planner and route validator** compiled with an explicit Unity surrogate. This is not a licensed native Unity test, and does not execute physics. It must never claim to have tested R6 chassis or WheelContacts.
- Existing Codex R3.2 original native report at `6358dde` has Edit 22/23, Play 5/6 (exit 2 each), with 11 out of 12 cube drive configurations **NOT_RUN** after first failure.
- NEW diagnostic suite's 12 base cases and suspect seeds have physical drive status `NOT_RUN`, even when geometric source diagnostics pass.
- First Light/CombatArena seed7 physical stop is factual, but neither a generator defect nor a specific PID/steering defect is proven. Requires independent instrumented Unity run, actual grounded wheel vehicle, and QA log/exit.
- Workflow CI PASS/BLOCKED to be populated only after run exists; never infer workflow success from a committed workflow file.
- Source baseline branch is stacked on R3.2, *not main*: draft diff against #211 only. Cannot merge independently into main or deploy.
