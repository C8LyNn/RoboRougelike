# Camera ramp diagnostic: real fixed steps

On Unity's EditMode runner the previous `WaitForFixedUpdate` loop returned repeatedly without advancing `Time.fixedTime`. The original diagnostic reported PASS but its 100 samples represented only a few physics steps. That result is not evidence of a complete ramp traverse.

The test-only fixture now waits for actual fixed-time advancement and verifies elapsed fixed time. No gameplay camera, terrain or vehicle code is changed.

- Unity 6000.3.14f1, final retry: **1/1 PASS**, exit 0, `Artifacts/Local/CameraRamp18-FinalRetry-20261108/results.xml` and `Editor.log`.
- Real Light vehicle and R6 forces on the existing 18-degree solid test-range ramp: 100/100 sampled steps grounded; 8.478 m uphill; no camera-volume overlap, aim overlap or clipping in this specific sampled setup.
- Initial placement uses a controlled fixture pose and the orbit is fixed at -5 degrees pitch / 20 m distance. This is not a human-driven test, all-ramp coverage, or proof that the reported camera issue is fixed.
- The original fixture's nonrepresentative PASS is retained under `CameraRamp18-5b21-20261108`; the first updated diagnostic passed under `CameraRamp18-RealFixedSteps-20261108`.
- An intervening run `CameraRamp18-Final-20261108` failed before simulation when Unity reported insufficient disk space and produced an empty XML; the later run above was successful. Raw artifacts are local.
