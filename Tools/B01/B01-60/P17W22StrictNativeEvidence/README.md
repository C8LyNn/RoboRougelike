# B01-60 — P17/W22 strict native evidence QA / #713

**Package:** B01-60-P17W22-STRICT-NATIVE-EVIDENCE/R1. Versioned **read-only** independent verification of frozen W21 [#711 @ 02272e8b0facc00d3943672770251b1d01182b85](https://github.com/C8LyNn/RoboRougelike/pull/711). Derived directly from W21, with **new B01-60 files only**. Does NOT modify the frozen W20/W21 launchers, QA test fixtures, Unity game #510, Save #556, selected R5 #690 or canonical #691, and does not install or run Unity.

## Distinct W22 safety fix

The original W21 verify_xml accepts each *required* NUnit identity, the Passed run flag and failed=0, but does not reconcile **all** raw NUnit test-case nodes against total/passed counters or reject unrelated skipped/failed cases when a contradictory root claims Passed. The source-only receipt also accepts a contradictory native_process_exit field. A dishonest/contradictory receipt is not an authenticated engine result, even with valid self-hashes. The W22 strict gate calls the original W21 verifier FIRST, then independently rejects these contradictions. It still permits extra valid PASS cases from original Unity assemblies; it does NOT require exactly 2/14 total tests, only the 2+14 required identities.

Native strict requirements: root NUnit total=passed=actual count, failed/skipped/inconclusive=0, all unique complete case identities Passed, W21 2 Edit+14 PlayMode V2 cases present, original verdict case counts match raw XML. Rejects malformed/tampered/uninventoried data by delegating to original W21 integrity gate. Real signed/externally attested execution is **NOT** established by self-hashed files; the output explicitly says UNATTESTED.

## Commands

First, existing W21 operator (from checkout containing this W22 descendant):

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W21WindowsUnityHandoff\Invoke-B0160W21.ps1 -RepoPath .
~~~

This is **source-only**; creates a GUID-named scratch folder and prints path. For genuine **V2-only** native testing the *owner alone* may append -Run and type B0160 RUN NATIVE after verifying legally activated Unity Hub 6000.3.14f1 Editor. No Unity access from ChatGPT. No automatic native launch, SSH, credentials, upload, merge, or deletion.

After W21 runs, use its printed B0160_W21_LOCAL_EVIDENCE_NO_UPLOAD_NO_DELETION directory (replace placeholder with actual local path):

~~~powershell
python .\Tools\B01\B01-60\P17W22StrictNativeEvidence\strict_evidence.py --evidence "C:\your-GUID-run\r\B0160W20-your-GUID\evidence"
~~~

This second command **only reads existing evidence**, never executes Editor. Success in source-only mode prints SOURCE_ONLY_PASS_NATIVE_NOT_RUN. Even a structurally valid native V2 receipt cannot attest actual Unity process authenticity, R5 host security, canonical Save/Load or G1 for latest host.

14 additional independent **SYNTHETIC** adversarial tests:

~~~powershell
python -m unittest discover -s Tools\B01\B01-60\P17W22StrictNativeEvidence -p test_strict_evidence.py -v
~~~

## Gates and provenance

- **Predecessors:** #684 frozen W20 @ c75cb6e52a0de11f27b570c34e08d32082af421f; #711 frozen W21 exact head above; authorization #667.
- **SOURCE_G0:** separate independent B01-40/41/61/62 source gate; W22 audit only, not acceptance of merged gameplay.
- **UNITY_G1:** NOT_RUN until owner-side legally activated Unity produces real Editor.log and NUnit XML. No R5 #690 / canonical #691 Unity tests covered by W21/W22.
- **Six stages:** IMPLEMENTED / TESTED / QA_ACCEPTED / READY_FOR_INTEGRATOR / INTEGRATED / RELEASED reported separately in #713.
- **Rollback:** discard only NEW W22-owned files / branch; no protected refs, original game, W21 source, or other agent files changed. No merge. Codex/owner ACK absent.
