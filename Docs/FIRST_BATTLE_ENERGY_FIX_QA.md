# First-battle energy after range or save

Base: `87a4a1ea` (enemy energy refresh). A newly prepared Standard vehicle has 600 energy. On the base build, both (1) visiting the test range and returning and (2) saving and restoring before the first battle marked the real vehicle's initial charge as already prepared. After adding a battery, the first encounter could begin at 600/700.

The range now prepares only its disposable vehicle copy. Checkpoints store whether the first deployment has been prepared; older checkpoints without the marker keep their existing no-refill behavior. A previously saved checkpoint cannot reveal whether its original first deployment occurred, so it intentionally keeps that conservative legacy behavior.

## Evidence

- Native Unity 6000.3.14f1 EditMode runner, original code: range / return / add battery / first battle FAILED, expected 700 and observed 600; `Artifacts/Local/InitialChargeAfterRange-Red-20261108/`.
- Native Unity EditMode, original checkpoint behavior: save / restore / add battery / first-deployment preparation FAILED, expected 700 and observed 600; `Artifacts/Local/InitialChargeCheckpoint-Red-20261108/`.
- Native Unity EditMode after fixes: 13/13 PASS, exit 0; `Artifacts/Local/InitialCharge-Focused-20261108/results.xml` and `Editor.log`. This includes real PlayMode entry and range-to-encounter transition, all three starting vehicles, JSON round trip, depleted and old checkpoints, plus run checkpoint regression.
- Windows x64 Unity Player build: SUCCESS, exit 0, zero errors, 12 warnings; `Artifacts/Local/InitialCharge-Build-20261108/Editor.log`. The final player build has not been manually tested in this QA cycle.

Logs and XML are local QA artifacts and are not included in the source commit. No claim is made that existing old saves recover information that they never stored.
