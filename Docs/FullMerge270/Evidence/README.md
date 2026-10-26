# Native evidence

Each `FM270-*` directory corresponds to one actual frozen-source native or Windows invocation. `*-process.json` records the source commit, arguments, wall-clock UTC timestamps and the native process exit code. NUnit XML is the actual Unity Test Framework output; the authored test count is not used as a substitute for results. Test-run XML durations can reset across domain reloads, so use process timestamps for elapsed wall time.

`native-summary.json` preserves every available test case and failure. `windows-summary.json` preserves process receipts, all result counters and screenshot hashes. Historical failures and superseded candidates remain visible; the final admitted runs are identified in `../RELEASE_QA.md` and `../delivery-verification.json`. A narrower harness status of `Passed` does not override an independently observed framing failure or a missing required scenario.

Logs and CSV telemetry are gzip compressed. CSV samples retain original bytes. Log lines containing machine, licensing or connection identifiers are redacted; `raw-file-hashes.json` records the SHA-256 and byte length of each original local file before redaction. Original unredacted logs remain in the isolated project's ignored `Artifacts/Local` directory.

PNG files are actual Player captures. Their native dimensions, SHA-256 and non-black image statistics are in the Windows summary; visual interpretation is documented separately. Camera timeline and consumed-target/local-turret provenance are distinct data streams. Temporary turret acquisition errors are retained rather than labelled all-frame tracking success.

Windows traversal uses real R3/R6 wheel contacts, Rigidbody forces, source map colliders and checkpoint observation. Activity selection and reward settlement are API calls. Combat outcome fixtures explicitly destroy real enemy cores after observing active AI/projectiles. These tests do not claim human or unassisted completion. The separately labelled camera stress fixture writes velocity for camera-follow diagnosis and is not used as driving acceptance.

An intentionally stopped native process has a termination receipt and no completed XML; it is not a PASS or proof of a native crash. Historical C0000005 is not declared fixed from successful controlled exits. No UI accessibility client is attached by these finite Player runners.

Legacy RC2 Input/Mapgen/Reactor hooks use the actual menu-default Standard vehicle internally. Earlier launch folders used the ignored requested Light/Heavy label; their raw receipts are preserved. The final launcher explicitly records the actual Standard robot and retains requestedVehicle separately. Camera/Race/road/Loop hooks select the named physical vehicle.
