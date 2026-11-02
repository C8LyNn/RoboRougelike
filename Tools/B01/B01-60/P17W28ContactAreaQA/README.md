# B01-60 — PASS17/W28: Rotary Saw + EMP contact/area original Unity QA

**PACKAGE:** B01-60-P17W28-CONTACT-AREA-NATIVE-001/R1 | **Owner:** B01-60 | HQ #800. **Parent:** Codex W27 [Draft #824](https://github.com/C8LyNn/RoboRougelike/pull/824) exact e03d36f2b86f8c73a067e574b67237a8921f622a. Source only from Codex; no original gameplay, active worktree, garage, weapon binding or Save modified.

## Actual gameplay source test coverage
Two newly authored genuine Unity [UnityTest]s in Assets/Tests/Editor/W28B0160ContactAreaClearanceTests.cs instantiate ACTUAL R3VehicleAssembly, R3VehicleModel, RRLSawBinding/RotarySawModule and RRLEmpBinding/EmpEmitter with real GameObjects, Collider/PhysX and R3 source:
- **Saw contact**: Collider attached after Bind must never become owner victim, including when motor is requested and physics fixed steps run; physical unparenting reverses ownership. This tests contact ownership and rotation, not fake projectile clearance.
- **EMP radial**: owner electronics immune, real wall occludes enemy, a successfully emitted pulse costs one real cooldown even with all targets shielded (NOT projectile semantics), second LPM group attempt cannot double-charge; after removing wall and explicit test-only cooldown advance, actual R3 FireGroup disrupts enemy but not owner.

These are authored Unity tests, **NATIVE NOT_RUN** until actual Editor.log and NUnit XML. They are not managed .NET/UnityEngine shims and are not a complete game smoke test. Other gaps: high-speed rotating barrels, contact damage/energy depletion, moving debris, all special direct/group paths, 0.55m 3D garage, canonical Save #797 API, #812 loaner softlock, whole Windows ZIP Player, actual two-win game loop, model muzzle axes. B01-22 #812 source witness means PRODUCT_ANTI_SOFTLOCK=RED until approved recovery tested. #824 reported targeted native Beam 5/5 and special projectile 6/6 but not complete Windows Player on its final SHA.

## Executed source checks (never Unity)
From clean trusted W28 branch:
~~~powershell
python -m unittest discover -s Tools\B01\B01-60\P17W28ContactAreaQA -p test_check.py -v
python Tools\B01\B01-60\P17W28ContactAreaQA\check.py
~~~

CI tests original exact Git W27 code and validates eight-only W28 QA additions, authentic Unity GUID, two new real authored NUnit identities, original product Saw/EMP physics/code route, source and deliberately invalid evidence rejection. Ten direct Python SOURCE cases per OS; source PASS cannot promote Unity.

## Manually invoking original Unity on Windows
Never run inside Codex active worktree or normal game checkout. AFTER independent G0, owner must create a **fresh clean isolated checkout** whose path contains B0160-W28-Disposable, with original W27 ancestry. Licensed activated Unity Hub 6000.3.14f1 required. Source-only preflight, no Unity:
~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W28ContactAreaQA\Invoke-B0160W28Native.ps1 -RepoPath .
~~~

Only after verified independent G0 and source preflight, opt in:
~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W28ContactAreaQA\Invoke-B0160W28Native.ps1 -RepoPath . -Run -SourceG0Approved -DisposableCheckoutAcknowledged
~~~

Type exact **B0160 W28 CONTACT AREA NATIVE** at interactive prompt. Optional -UnityPath for activated Editor and -ScratchParent outside checkout. Runs Unity -testPlatform EditMode, same actual EnterPlayMode test format as #824. Local nonce evidence: W28-contact-area-results.xml, W28-Editor.log, W28-process.log, W28-run.json with exact SHA and unsigned hashes. Machine verifier checks Unity exit0, EXACT TWO passed original NUnit3 case identities, Editor6000.3.14f1 marker and intact file hashes. Independent human/QA raw inspection still required; do not report G1 PASS merely because receipt self-validates. NO SSH, unattended PC access, automatic fetch, upload, global Editor install, merge, release or user data cleanup.

**Six gates:** IMPLEMENTED=YES QA SOURCE; TESTED=only executed CI source; QA_ACCEPTED=NO independent native; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO. G0 independent #824/W28 pending, Unity G1=NOT_RUN, Windows Player=NOT_RUN, anti-softlock=RED, G2=HOLD. Receivers B01-41/61 and B01-40 for independent G0/native evidence, B01-00 and Codex only after authorized G0. Rollback: discard only eight W28 own QA paths/branch; never modify owner Save, active Codex branch or protected refs.
