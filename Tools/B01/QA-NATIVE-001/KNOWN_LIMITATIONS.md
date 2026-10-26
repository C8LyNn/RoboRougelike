# Known limitations

- No licensed Windows Unity 6000.3.14f1 runner in this B01-60 environment: native Compile/Edit/Play/Player/manual **NOT_RUN**, no fabricated Editor.log/PNG/XML.
- QA script cannot authenticate who ran a test; any externally supplied receipts remain labeled external until B01-60 controls execution.
- PowerShell process wrapper must be exercised on a licensed Windows workstation. Windows invocation paths are strict; do not bypass checkout protection.
- Windows Player must itself support opt-in -qa-output and native telemetry/PNG. Missing mode leaves corresponding status NOT_RUN.
- PNG validation checks dimensions/chunk CRC/deflate, **not** subjective magenta, image UI or camera visual safety.
- The example plan has source_files {} intentionally, so unconfigured runs never PASS. Populate from immutable producer Git sources and check target head.
- Original five-stage gate is independent of MANUAL; manual defaults NOT_RUN. Raw full R6 wheel-contact time series and observation are needed for feature acceptance beyond minimum telemetry fields.
- This QA tool does not integrate packages, fix producer mechanisms, authorize G2, or merge.

- V2 full profiles reject omitted stages and empty Edit/Play case lists. `SCOPED_DIAGNOSTIC` is explicitly partial and cannot certify full G1.
- R6 receipts require wheelQueryCount, wheelContactSamples, zeroContactSamples, reachedCheckpoints, distanceMeters, stallCount, fallCount, flipCount and true finishReached. CAMERA requires finite maxAimDeltaDeg and maxContinuousSpeedMps. Native Player must actually measure these fields.
- Runner attribution metadata are NOT a cryptographic proof of execution; B01-60 native PASS requires separately witnessed controlled execution.
