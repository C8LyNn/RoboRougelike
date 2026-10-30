# B01-60 P17/W21 — Windows Unity local operator handoff / #667 / #687

**Scope:** Owner-approved one-command MANUAL Windows Unity 6000.3.14f1 test runner, but only original V2 2 EditMode + 14 PlayMode/PhysX cases. This does NOT run R5 host #690, canonical API #691, or actual live game Save/Load. It does not access the owner's PC. Native Unity G1 = NOT_RUN until operator execution and raw evidence.

## Command at local repository root containing this W21 Draft branch

PowerShell 7:
~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W21WindowsUnityHandoff\Invoke-B0160W21.ps1 -RepoPath . -Run
~~~

For source-only preflight omit -Run. For Windows PowerShell 5.1 replace pwsh with powershell. Requires local Git, Python 3, Windows PowerShell and, for native, legitimately installed and activated Unity Hub 6000.3.14f1 Editor. Optional -UnityPath 'D:\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe' or -ScratchParent 'D:\PrivateScratch'. Native activation must be confirmed via your own Unity Hub. The frozen W20 script requires literal operator phrase B0160 RUN NATIVE before the Editor starts. No SSH, passwords, admin access, credentials or network fetch.

For fully offline execution, the checkout must already contain the original historical Git commit objects 510 d94400d95d13db7f27e1533df05c9292d5380431, W06 cb2b3a76e84266e2730fffc4ab048c1039f0652f, fixture 33a32b8bd9ab40467ab1a17b5684926a37dbd4d3, and Save 556 33094f154c7b91119b36d58aab285e6e83c5de57. Missing historical objects cause a clear HOLD; the operator NEVER fetches over the network. The CI-only Windows job explicitly obtains these objects into its ephemeral GitHub runner checkout. W21 safely carries those objects from the existing local checkout into its disposable nested clone using scratch-only tags. The original game checkout and its refs/tags remain unmodified.

W21 verifies its own launcher/verifier Git blobs and frozen W20 #684 @ c75cb6e52a0de11f27b570c34e08d32082af421f and old operator blob. Creates NEW GUID-named local scratch and detached local-only frozen W20 clone; source/results siblings prevent overlap. Refuses repo-contained/containing scratch, junction/reparse ancestors and UNC scratch. No deletion, auto-upload, GitHub fetch or protected push. Source-only exit0 is NOT native.

W21 independent read-only verifier checks every original evidence path, length, SHA256, manifest SHA256, symlinks, traversal, extra members, W20 source SHA, operator consent, and (if native claimed) exact 2+14 original NUnit identities and Editor.logs. Retains local console, verifier, handoff JSON, and original raw evidence at B0160W21-GUID/W20-private-results/B0160W20-GUID/evidence. Inspect/redact machine-specific paths before manual sharing. SHA256 is self-integrity, NOT cryptographic provenance/attestation. W20's legacy real_unity status is stale even after native; W21 does not treat it as proof. Structurally consistent V2 logs are NOT R5/canonical acceptance.

## Executable QA

~~~powershell
python -m unittest discover -s .\Tools\B01\B01-60\P17W21WindowsUnityHandoff -p test_verify_evidence.py -v
~~~

20 tests exercise SYNTHETIC positive/negative evidence only (false SHA, duplicated/missing NUnit, bad version/consent/hash, symlinks). CI runs them on Windows/Ubuntu and actual Windows PowerShell source-only plus missing-Editor negative without Unity. Native logs require actual licensed Editor execution on the user's PC.

**Dependencies/gates:** Frozen #510/#556/#596/#642/#647 and W20 #684 untouched; #652/#682 ALT_HOLD. Selected R5 #690, selected API #681 successor #691 and adapter #697 require independent B01-61/41 source QA, B01-40/62 physical G0 and a separate NATIVE R5 suite. W21 V2 cannot replace any gate. IMPLEMENTED=YES; TESTED=SOURCE_ONLY; QA_ACCEPTED=NATIVE_NO; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO; G2 HOLD; Codex ACK NONE; feedback NOT_MEASURED. No merge. Rollback: discard W21 additions/branch only.
