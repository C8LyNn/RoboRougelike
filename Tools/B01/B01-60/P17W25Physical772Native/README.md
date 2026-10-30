# B01-60 P17/W25 — physical Rig[] / Rigidbody original #772 native Unity G1

**PACKAGE_ID:** B01-60-P17W25-772-RIG-NATIVE-G1/R1 | **HQ:** #731 | **authorization:** #667. New B01-60-only descendant of immutable #760 @ 6a3589ff23bd811fb456d375e14b45c22b93337d. Selected B01-23 authored Unity physical port **[#772 @ ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1](https://github.com/C8LyNn/RoboRougelike/pull/772)**. Original frozen #510/#556, selected #729, chosen branch #749, corrected fixture #750, prior owner runner #718/#760 stay byte-unchanged.

## Actual functionality added

Reuses original frozen B01-60 #718 archive, strict Unity GUID, safe scratch stage/rollback, native Editor.log/exit/NUnit validation. Selects **real #772-authored GameObject, Rigidbody, Collider, parent/pose/velocity, original #510 AtomicDockLedger and #556 consumed IDs** from the **exact Git SHA** (only its 21 owner-author paths; core dependencies pinned to #729/#510/#556). Adds one disposable QA-only NUnit private save-scope guard in the **existing #772 PlayMode assembly**. Stages each of its three original Unity [UnityTest] cases in **three separate temporary Unity projects**, each with its own product/checkpoint namespace. Source-only preflight physically verifies all 3 ZIP→Unity project→safe rollback contracts. Optional real Unity invokes separate guard and real PlayMode NUnit for each test (6 Editor runs) and preserves logs/hashes in owner-only scratch; no installed live project changes.

Original #772 is a NEW replacement physical rig implementation, **not a runtime bridge into the frozen private #510 Rig[]**. This package cannot attest native #773 Save (Tools-only, no integrated Unity-facing store assembly), original #750 R5 (separate W24 #760), economy/wallet/loaner persistence, or the release of a combined game. No coinstall of old #749 and proposed successor #773 or ALT #753/#741. Current #773 candidate is **not automatically selected**: B01-00/B01-23/62 must independently pass its original source ABI/security and explicitly choose it. All such gameplay product acceptance is HOLD.

## One command on owner's Windows machine

Requires local trusted checkout of W25 Draft branch; already locally available original Git #772 SHA (the script does **not fetch** on owner's PC); Windows PowerShell5.1/PowerShell7, Git, Python3, and optionally legitimate activated **Unity Hub 6000.3.14f1**. Default is SAFE SOURCE-ONLY:

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W25Physical772Native\Invoke-B0160W25Rig.ps1 -RepoPath .
~~~

After independent B01-40/41 accepts exact #772 G0 source and Unity activation is verified by owner, optional explicit native execution:

~~~powershell
pwsh -NoProfile -File .\Tools\B01\B01-60\P17W25Physical772Native\Invoke-B0160W25Rig.ps1 -RepoPath . -Run -SourceG0Approved
~~~

The script requires exact typed **B0160 W25 RIG NATIVE** and a valid Unity Hub Editor path; optional -UnityPath for nondefault Hub path. Source-only needs neither Editor nor consent. No automatic SSH/admin, persistent agent, network fetch, upload, unsafe file cleanup, merges or source overrides. Operator must preserve original receipts, Editor.log, Unity process stdout/exit and NUnit3 XML; SHA256 self-hash is **not independent execution attestation**. Sensitive paths should be redacted before any manual sharing.

## Source validation without Unity

~~~powershell
python -m unittest discover -s Tools\B01\B01-60\P17W25Physical772Native -p test_rig_native.py -v
~~~

CI uses original exact Git refs in GitHub-hosted disposable runner to validate source SHA, immutable selected API and frozen ledger, nine #772 GUID and two asmdef, strict three-case private physical source staging/rollback, missing G0/consent, wrong scratch. No Unity gets executed in GitHub Actions or ChatGPT. **No Editor.log/NUnit XML from owner's PC = NATIVE_G1_NOT_RUN**, no exceptions.

**Six stages:** IMPLEMENTED, SOURCE_TESTED, INDEPENDENT_QA, READY_FOR_INTEGRATOR, INTEGRATED, RELEASED separately reported in HQ. SOURCE_G0=INDEPENDENT_PENDING; NATIVE_G1=NOT_RUN; selected #773 source G0=INDEPENDENT_PENDING; actual canonical Save+Rig[] integration=HOLD; G2=HOLD; Codex ACK=NONE, integrator feedback=NOT_MEASURED. Receivers B01-40/41/61 QA + GUID/G0, B01-23 authored #772 physics, B01-62 selected #773 store, B01-00 owner/manual G1 handoff. Rollback discard only new B01-60 W25 branch/files; never delete owner/user foreign scratch. NO MERGE.
