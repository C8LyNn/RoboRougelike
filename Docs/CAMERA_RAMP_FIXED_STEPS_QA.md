# Camera ramp diagnostic: real fixed steps

On Unity's EditMode runner the previous `WaitForFixedUpdate` loop returned repeatedly without advancing `Time.fixedTime`. The original diagnostic reported PASS but its 100 samples represented only a few physics steps. That result is not evidence of a complete ramp traverse.

The test-only fixture now waits for actual fixed-time advancement and verifies elapsed fixed time. No gameplay camera, terrain or vehicle code is changed.

- Unity 6000.3.14f1, final retry: **1/1 PASS**, exit 0, `Artifacts/Local/CameraRamp18-FinalRetry-20261108/results.xml` and `Editor.log`.
- Real Light vehicle and R6 forces on the existing 18-degree solid test-range ramp: 100/100 sampled steps grounded; 8.478 m uphill; no camera-volume overlap, aim overlap or clipping in this specific sampled setup.
- Initial placement uses a controlled fixture pose and the orbit is fixed at -5 degrees pitch / 20 m distance. This is not a human-driven test, all-ramp coverage, or proof that the reported camera issue is fixed.
- The original fixture's nonrepresentative PASS is retained under `CameraRamp18-5b21-20261108`; the first updated diagnostic passed under `CameraRamp18-RealFixedSteps-20261108`.
- An intervening run `CameraRamp18-Final-20261108` failed before simulation when Unity reported insufficient disk space and produced an empty XML; the later run above was successful. Raw artifacts are local.

## Light and Heavy follow-up

The same test-only fixture was subsequently run for both Light and Heavy with actual fixed-time progression: **2/2 PASS**, exit 0, `Artifacts/Local/CameraRamp-LightHeavy-20261108/results.xml` and `Editor.log`. Both reported 100/100 grounded samples and zero sampled camera-volume overlaps in this one controlled setup. Light progressed 8.478 m, Heavy 0.396 m. The test's movement assertion only requires 0.05 m: a PASS does **not** mean the Heavy completed or could reliably climb the ramp. The large difference merits an independently set-up drive test from the approach to the ramp; the fixture places each vehicle directly on the slope and does not establish the cause. Human camera comfort, different angles and actual Player remain NOT RUN.

## Prepared follow-up: drive telemetry

The existing diagnostic now records rigidbody mass, gravity, wheel contacts, nominal available traction force, drive-power units, slope direction and observed uphill/forward speed. The available-force value is a capacity derived from current contacts, not a measurement of applied force; braking, speed limits, adapters, friction and collisions can change the outcome. This instrumentation does not modify game physics, initial setup, the original assertions or the earlier results. Local C# compilation of the test assembly succeeded (exit 0, 12 warnings); the instrumented Unity diagnostic is **NOT RUN**. Repeat from the real approach to the ramp separately before drawing a conclusion about in-game climbability.
