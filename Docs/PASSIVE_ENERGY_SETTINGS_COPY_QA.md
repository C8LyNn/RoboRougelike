# Passive energy: settings descriptions

Base: `b30da559` (Draft PR #1126).

The motor and regenerator descriptions in the garage settings still displayed the old T1/T2/T3 reservation of 25/30/35 after the reservation rule changed. They now display the shared per-module reservation constant, and the regenerator strength values derive from the existing tier effect and regeneration constants. The underlying gameplay is unchanged by this follow-up.

Verification on 2026-11-08: runtime C# compiled with Roslyn and local Unity references (exit 0), and a source search found no remaining C# reservation labels containing 25/30/35. This is not native Unity or a visual check. Unity, Player and both layouts: NOT RUN under the lightweight-work constraint. Local ignored compiler log: `Artifacts/Local/CSharp-PassiveEnergySettings-20261108/runtime.log`.

Manual follow-up: open garage settings with a wheel motor and regenerator selected, in both layouts. Verify the 25-energy reservation and regeneration-rate labels are legible and match the actual vehicle's bank.
