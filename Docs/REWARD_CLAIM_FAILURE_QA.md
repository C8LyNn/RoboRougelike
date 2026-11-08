# Reward claim failure feedback

Base: `37b26d18` (Draft PR #1111).

If the existing reward transaction rejects an otherwise enabled confirmation, the reward screen now reports the failure and permits another attempt. Button and keyboard confirmations share the same path. Selecting a different bonus or basket clears the message; a new ledger resets it. The transaction itself is unchanged.

Verification on 2026-11-08: runtime C# compiled using Roslyn with local Unity references (exit 0). Actual failed/successful reward transactions, Unity EditMode/PlayMode and both UI layouts were NOT RUN under the current lightweight-work constraint. Local ignored log: `Artifacts/Local/CSharp-RewardClaimFeedback-20261108/runtime.log`.

Manual follow-up: use a reproducible rejected transaction to check the message in landscape and portrait, verify selections remain available, then retry or alter the selection. Confirm a normal reward claim still reaches the garage. Visual fit is unverified.
