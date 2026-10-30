# MOTION-TRACE-REPLAY-001/R1 — frozen-source C# replay

B01-13 HUMAN PLAYTESTING | PASS17 WAVE11. Original executable trace replayer plus six original synthetic 101-sample fixtures and three actually executed expected-RED negative controls. **Not another gameplay package / Unity HUD.** W10 original MotionStallLedger from Draft PR #495 exact Git commit e32d3199fa1fe6e9120df61a61ec02031441a2d3 is linked as source. No edits or copy commits to frozen W10 branch, Codex or canonical Assets.

## Behavior

Original 101-row synthetic technical replay fixture includes: blocked vehicle sampled at 100ms; equivalent 250ms segmentation; sample gap plus 60m teleport; slow creep then input release; travel without movement input; two sequential stalls and two recoveries. Every row independently stipulates expected current state, whether a new suspicion event fired, total suspected stalls, recoveries, discontinuities. The C# runner executes original W10 source for each trace, matches all fields, validates a strict CSV schema, monotonic times, contiguous trace names, and fails nonzero on any mismatch. A source regression or changed expected state cannot silently pass.

Independent Python adversarial script deliberately changes one expected state, invalidates the header and makes a trace timestamp non-monotonic. Each must return the exact expected nonzero code and specific diagnostic marker (three negative controls). No personal data, players, telemetry or gameplay changes.

**This is source behavioral replay evidence, NOT actual Unity native EditMode/PlayMode/PhysX, real Player observations, or a human experiment.** Does not prove that W/S moves the in-game vehicle; no assertion that the game can softlock. Native Unity G1 and independent approval remain NOT_RUN/PENDING.

## Exact original source setup

Prerequisites .NET 8, Python 3.11, Git, isolated clean checkout containing original W10 SHA. The dedicated Windows/Ubuntu Actions workflow automates this procedure:

1. mkdir -p Research/B01-13/P17I/Tests/PinnedOriginal
2. git show e32d3199fa1fe6e9120df61a61ec02031441a2d3:Research/B01-13/P17H/UnityPackage/Runtime/MotionStallLedger.cs > Research/B01-13/P17I/Tests/PinnedOriginal/MotionStallLedger.cs
3. python Research/B01-13/P17I/verify_package.py
4. dotnet build Research/B01-13/P17I/Tests/MotionTraceReplay.csproj -c Release --warnaserror
5. dotnet run --project Research/B01-13/P17I/Tests/MotionTraceReplay.csproj -c Release --no-build -- Research/B01-13/P17I/Fixtures/original_motion_trace_contract.csv
6. python Research/B01-13/P17I/Tests/adversarial.py Research/B01-13/P17I/Tests/bin/Release/net8.0/MotionTraceReplay.dll Research/B01-13/P17I/Fixtures/original_motion_trace_contract.csv Research/B01-13/P17I/Tests/Evidence/negative.txt

Pinned source physical SHA256 c391dfebd24621da9a2c23bbc722973b81258da2e490cc314cb4ebaccc5fce99, Git blob 77b74de9358a77b770d5e8134de431618a853a6e. Verify the source and physical byte-hash manifest before compiling. The input is synthetic, not inferred from actual participants; output is process stdout and optional Actions artifacts, no hidden participant logging.

## Owner, independent QA and rollback

Exclusive B01-13 W11 scope Research/B01-13/P17I/** + dedicated .github/workflows/b01-13-p17w11-motion-trace-replay.yml. This package adds only one original standalone replay executable, original fixture, adversarial controls, manifest, verifier, README and CI. No source files of W10 #495, earlier W09 #478, #458/#435/#426 or canonical game modified. Dependency W10 #495 is FROZEN and NOT independently accepted.

Receiver B01-40 checks immutable Git extraction SHA, scope, manifest and rollback. Receiver B01-60 independently executes original W10 code against the new six fixture scenarios plus all three mutation controls and investigates any failures. B01-00 decides whether this narrows W10 test intake, with separate native Unity test host still required. Codex SENT=NO / ACK=NONE / INTEGRATED=NO. No merge/force push.

Rollback: leave Draft unmerged and discard isolated W11 branch; any separately generated Tests/PinnedOriginal source and ephemeral CI stdout can be deleted without altering immutable upstream W10. If defect detected, request formal RETURNED_FOR_REWORK before producer code changes, in new version/SHA.
