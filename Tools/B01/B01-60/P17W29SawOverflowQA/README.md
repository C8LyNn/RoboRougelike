# B01-60 PASS17/W29 — Rotary Saw true broadphase overflow diagnostic correction

PACKAGE B01-60-P17W29-SAW-OVERFLOW-CORRECTION-001/R1. Exact original Codex #828 887fd18bf0a518ee24776c660f5efa6e3a0491d8, original #824 e03d36f2b86f8c73a067e574b67237a8921f622a. Isolated new QA branch, original producer branch untouched, no merge.

## REAL W28 fault and narrowly implemented product fix

Independent native #848 proved original #828 WPN005 19/20 PlayMode, failing OverlapOverflowDoesNotPartiallyDamage. New first pre-motor IntersectsOtherOwnedPart Physics.OverlapSphereNonAlloc returns true on buffer saturation but did not set OverflowedLastTick. Result: motor/damage safely stopped, overflow diagnostic silently false. This W29 branch changes ONLY that one existing branch in original RotarySawModule.cs: record OverflowedLastTick=true BEFORE fail-closed return. It does NOT bypass owner collision, allow heat or energy on blocked motor, alter the target contact scan, damage, economy, Save, EMP, or any other game code.

New true Unity PlayMode test OwnerSafetyBroadphaseOverflowReportsAndResumesAfterClearing: actual WPN005 Rigidbody + 6 physical BoxCollider targets saturate 4-hit buffer, assert flag TRUE, no RPM/heat/contact damage; extra objects deactivate physically, next fixed tick checks flag FALSE and actual RPM/target damage resumes. All 20 original WPN005 tests retained byte-exact; 21 total. Real editor NOT RUN merely because C# test was authored.

## STILL P0 RED: actual default player and AI saw installation

Original actual native #848 diagnostics on #828 found FOUR physical owner armor cells intersect the mounted disc on Standard player vehicle and FOUR P12_EnemyBlock hull Colliders overlap the enemy's cutting disc. Standard case target HP remains 500; enemy saw RPM stays zero. This is valid product self-collision, not proof owner guard should be removed. Existing RRLW6SawPhysicalTests has 2/4 failing EditMode on exact #828. This W29 fix DOES NOT repair default mounting. Original Codex/owner must physically reposition allowed blade/mount/vehicle geometry and real Player-test before integration. Earlier failed blocked-then-clear minimal QA fixture accidentally intersected supporting Core; corrected separate fixture PASS, not a product failure. Never weaken self-cut protection.

## Actual source-only gates (not native)

Run from trusted clean W29 branch:
    python Tools/B01/B01-60/P17W29SawOverflowQA/verify.py
    python -m unittest discover -s Tools/B01/B01-60/P17W29SawOverflowQA -p test_verify.py -v

Source QA verifies immutable exact #828 original Git blobs, only one original code branch changed and one authentic PlayMode test appended, unchanged other 20 cases, six new owner QA/CI files, false/forged native receipt rejected. Executed outcomes must come from final exact-head CI logs; no fake XML or Windows build is produced.

## Actual Unity G1 only with owner manual opt-in (Windows)

After independently approved G0, use a brand-new clean isolated Unity checkout whose path contains B0160-W29-Disposable; NEVER point at active Codex worktree, existing owner Save, normal game checkout, or protected branch. Activated Unity Hub 6000.3.14f1 required.

SOURCE ONLY:
    pwsh -NoProfile -File .\Tools\B01\B01-60\P17W29SawOverflowQA\Invoke-B0160W29SawNative.ps1 -RepoPath .

NATIVE after legitimate approval:
    pwsh -NoProfile -File .\Tools\B01\B01-60\P17W29SawOverflowQA\Invoke-B0160W29SawNative.ps1 -RepoPath . -Run -SourceG0Approved -DisposableCheckoutAcknowledged

Type B0160 W29 SAW NATIVE manually. Runs entire WPN005 RotarySawPhysicsTests PlayMode 21/21 and original RRLW6SawPhysicalTests EditMode 4/4 (includes both still-RED default player/enemy scenarios), both actual Editor logs, two original NUnit XML, two process exit-code files plus unsigned SHA256 receipt. Native acceptance fails closed if any earlier red persists. Independent B01-41/61 review mandatory; SHA receipt alone does not authenticate the operator. No SSH, background PC access, automatic fetch/upload, merge, release, owner scratch deletion.

SIX STATUS FLAGS: IMPLEMENTED=YES_NARROW_RUNTIME_FIX; SOURCE_TESTED=PENDING_FINAL_EXACT_SHA; QA_ACCEPTED=NO_INDEPENDENT; READY_FOR_INTEGRATOR=NO_DEFAULT_GEOMETRY; INTEGRATED=NO; RELEASED=NO. Full W29 Windows x64 Player ZIP/anti-softlock/garage/Save are NOT produced/tested here. Rollback discards only own derived W29 branch (2 touched original source/fixture and six new B01-60 QA paths), leaving producer #828, Codex worktree and protected refs untouched. Receivers B01-40/41/61 independent QA, B01-00 owner policy/mounting, Codex original R3 gameplay owner.
