# Reward confirmation consistency

Base: `a95ba02d` (Draft PR #1109).

The reward screen now checks the same selection and inventory-capacity conditions for the Enter shortcut and for the confirmation button in both landscape and portrait. An out-of-range bonus or basket is also rejected. The existing transactional settlement remains the authority for granting rewards.

## Verification (2026-11-08)

- Roslyn compilation with existing local Unity references: runtime exit 0, editor/test assembly exit 0. This is not a Unity compilation.
- Nine offline checks of the compiled eligibility predicate: PASS, covering missing/invalid bonus or basket, no loot and the exact inventory-capacity boundary. These do not simulate keyboard input or claim rewards.
- Unity EditMode, PlayMode, Windows Player and actual keyboard interaction: NOT RUN; current instruction is lightweight work without taking over the computer.
- Local ignored evidence: `Artifacts/Local/CSharp-RewardConfirmConsistency-20261108/`.

Manual follow-up: on the reward screen press Enter with no bonus, no basket, and insufficient inventory space; then make a valid selection and confirm using both the button and Enter.
