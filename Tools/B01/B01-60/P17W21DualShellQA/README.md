# B01-60 W21 — actual Windows PowerShell 5.1 + 7 original #684 operator QA

HQ: https://github.com/C8LyNn/RoboRougelike/issues/687

## Distinct new functionality
W20 original manual operator #684 at c75cb6e52a0de11f27b570c34e08d32082af421f already runs in PowerShell7 on GitHub-hosted Windows, but no real Windows PowerShell **5.1** proof existed. This NEW versioned QA package runs the EXACT unchanged W20 script through **actual Windows PowerShell 5.1** and **PowerShell7** binaries, each with separate real local-only GUID scratch clones, never the owner's PC, NEVER a Unity Editor.

Four checks per actual Windows shell: (1) original source-only ZIP/preflight exit0 plus original stored per-file SHA256 receipts; (2) -Run with nonexistent Unity.exe must exit3 and never fake XML/G1; (3) modified operator script in a separate local Git detached clone must reject against original HEAD blob (exit2); (4) original repository selected as scratch must be rejected (exit2). The script does not delete scratch or user files; no SSH, unattended remote service, license workaround, upload or merge.

An independent Python source guard verifies original game #510 native V2 2 EditMode +14 PlayMode exact NUnit names, frozen W20 PowerShell/Python Git blobs, exactly FIVE new W21 QA paths, and 8 deliberately mutated dual-shell source security checks. Python is explicitly NOT a Windows shell or Unity simulator. The actual Windows CI job executes the real Windows shells. Independent B01-41 native evidence verifier #675 is a different workflow and not copied or reimplemented here.

## Owner Windows native operator — unchanged single command

From a checked-out copy of this Git version on the owner's Windows machine, with Git+Python3+Unity Hub legitimately activated at version 6000.3.14f1 and PowerShell7 installed:

    pwsh -NoProfile -File .\Tools\B01\B01-60\P17W20WindowsUnityOperator\Invoke-B0160WindowsUnity.ps1 -RepoPath . -Run

Type exactly B0160 RUN NATIVE when prompted. Replace pwsh with powershell for Windows PowerShell 5.1. Omit -Run for source preflight only, without Unity. The original hardened #532/#601 native runner checks full real Editor.log, XML, case identities and source after import, but true original 16/16 V2 PhysX success is ONLY V2, NOT native Save/Load R4 acceptance or canonical game G1.

Current one selected frozen R4: #647 at 09c1809b73324a6d31c13e5d0143de62ac1e3ca6; #652 ALT_HOLD, historical #644/#604 ALT_HOLD. Original frozen game #510, Save #556, host #596 and R3 #642 never changed. B01-61 #670 independently diagnosed a P0: component-only Destroy(host) leaves orphan Rigidbody world and releases lease. Thus R4 host SOURCE SECURITY HOLD until owner new versioned fix and independent G0; never run or claim accepted R4 native Save using this V2-only operator. New canonical #666 API #681/#682 selection is B01-23-owned, and #682 has independent #685 orphan-prepare source P0. Neither private #510 live Rig[]/ledger nor canonical Save is yet integrated.

## Evidence / review
CI job status must be checked at exact final SHA and original logs. All executed test steps are SOURCE QA on hosted Windows/Linux, not licensed Unity. Real Unity6000.3.14f1 / PhysX native G1 remains NOT_RUN until actual owner PC manual opt-in. Six lifecycle fields: IMPLEMENTED YES; TESTED pending exact W21 CI; QA_ACCEPTED NO_NATIVE; READY_FOR_INTEGRATOR NO; INTEGRATED NO; RELEASED NO. Actual integrator feedback NOT_MEASURED. Receiver: B01-00 for operator instruction, B01-41/40 for independent scratch/Windows source review, B01-23/62 for R5 after P0, B01-60 for actual owner Unity logs. Rollback: remove exactly FIVE new W21 own paths, never old frozen W20 or producer files.
