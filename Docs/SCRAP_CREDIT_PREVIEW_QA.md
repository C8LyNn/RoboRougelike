# Scrap award preview and accounting at wallet cap

Base: `3ce1dca9a92d0f7aeabc3c76173bc77e5da64387` (Draft PR #1120).

A local reproduction showed that dismantling a basic gun at 100,000 scrap had a nominal value of 7 while the wallet received 0. The existing wallet cap and payout rules remain unchanged. The garage and reward inventory now show the capped credit, garage feedback uses the actual wallet delta, reward previews account for the cap, and victory/ledger statistics report the credited amount rather than the nominal amount.

Verification on 2026-11-08:

- Roslyn runtime and editor/test assemblies compiled against local Unity references: exit 0 / 0. This is not native Unity compilation.
- Offline reflection checks using the compiled real R3 vehicle model and R4 salvage ledger: 3/3 claims at initial scrap 0, 99,999 and 100,000; credited values 7, 1 and 0, with the ledger matching each delta.
- Offline checks of actual model dismantling at the same three wallet balances: 3/3 previews matched wallet changes.
- Six targeted Unity EditMode cases were added to `RRLW34RewardTests` but NOT RUN. Actual GUI, encounter victory path, Windows Player and complete regression: NOT RUN under the current lightweight-work constraint.
- Local ignored logs: `Artifacts/Local/CSharp-ScrapCredit-20261108/`.

Manual follow-up: in a disposable profile near the scrap cap, inspect the dismantle quote, confirm dismantling, and verify the wallet and message match. Win a fight with scrap near the cap and verify the reward and run summary match the actual credit. Do not alter the real player save for this test.
