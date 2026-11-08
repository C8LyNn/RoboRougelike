# Reward inventory dismantling feedback

Base: `56ca2b8e467f8f799761efd2a7c2fed2abe3c722` (Draft PR #1108).

Both reward-inventory layouts now use the same two-click dismantle action. After a successful dismantle the UI reports the actual change in scrap, and after a rejected operation it reports failure. Starting a different confirmation or closing the inventory clears the message. No economy rules were changed.

## Verification (2026-11-08)

- Roslyn compilation against the existing local Unity project references: runtime exit 0, editor/test assembly exit 0. This is not a Unity compilation.
- Three offline reflection checks of the compiled feedback formatter: PASS, including zero payout and failure. These do not exercise the Unity GUI or the transaction.
- Unity EditMode, PlayMode, actual game interaction and visual layout: NOT RUN, respecting the current request for lightweight work without taking control of the PC.
- Logs: `Artifacts/Local/CSharp-RewardDismantleFeedback-20261108/` in the local workspace (ignored by Git).

Manual follow-up: in both landscape and portrait, dismantle an inventory item from the reward screen, verify the scrap balance and message, cancel a different confirmation, and return to rewards. Check that the message does not overlap other UI.
