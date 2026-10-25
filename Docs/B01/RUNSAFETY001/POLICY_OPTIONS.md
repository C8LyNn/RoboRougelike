# RUN-SAFETY-001 — evaluation and HQ decision request
**HQ_DISPATCH_ID** HQ-B01-20261025-S04 · **P0** · source issue #152 · canonical ECON issue #136.
**Status** Recommended policy **not** yet approved for live game. No change in run-loss semantics.

| Policy | Can win with 0 HP gun/drive & scrap=0? | ECON one-win requirement | Economics and difficulty | Abuse risk | Decision |
| --- | --- | --- | --- | --- | --- |
| A. Immediately revive minimum destroyed parts by EmergencyRepair | Yes, free or subsidized | **Violated**: restores before next win | Trivializes damage/credit, devalues risk | Free slot bypass, reopening garage farm, infinite recoveries | REJECT as permanent |
| B. Special out-of-vehicle service challenge / puzzle for rebuild | Possibly, subject to special accessible stage | Win event can count if HQ defines it | High new activity/route/UI burden; challenge difficulty unrelated to current run | Farm easiest challenge, unintended loss/exploit semantics | P2 future if owner wants |
| C. **Temporary underpowered loaner rover for normal eligible encounter** | **Yes**, if a valid next challenge and living Core/run | **Preserved**: only real authoritative victory completes queued items | Short-term handicap; original owned items remain 0 HP/unavailable; no free item reward | Borrow-and-sell, extra inventory IDs, repeated wins, reward duplication, easy loaner farming | **RECOMMEND, HQ APPROVAL REQUIRED** |
| D. Reusable minimum free starter kit in garage | Yes | Violated unless kit quarantined like loaner | Permanent value injected into economy | Dismantle/resale/save dup | REJECT |

## Proposed C R1 gameplay contract
1. Offer only inside ongoing RUN-GARAGE, with next eligible challenge and alive Core, when canonical CanDeploy is **false because functional weapon OR propulsion OR structural support is missing**. Other start blockers are NOT bypassed. If full craft deployable do not offer.
2. Temp rover = **external lease for one real next challenge**, cannot join owned inventory, be sold, queued for rebuild, saved as a blueprint or salvaged into stock. It is an independent temporary active actor and cannot transfer components/HP.
3. Distinct encounter ID + lease ID minted atomically. One active lease; duplicate Start/Win/Defeat events idempotent. The real R1 run controller still owns the Garage → Challenge transition and victory/loss; never invent extra encounter, seed or auto-win.
4. Loaner has valid weak weapon, finite ammo/resource according to RES #139 when applicable and functional two-sided drive; tuning PENDING measured Light/Heavy and PR125 contact. Do not let a weak vehicle become stronger than an optimized starter.
5. Rewards, victory progression, and defeat settlement stay with **canonical existing R1**, and should occur **once per real challenge ID**. The loaner itself never produces salvageable player parts. Paid ECON orders still require valid cost and cannot be completed without win. A run-ending defeat remains run-ending exactly as before; safety state does not override it.
6. Preserve damaged-but-positive-HP repair path separately from 0HP queue; no old EmergencyRepair fallback may restore 0HP immediately when ECON is active.
7. Free slot once per **cycle between real wins**, not per Garage UI open. After victory complete only queued pieces with prior win serial, retain their original IDs/modifiers, keep unmounted until explicit garage action.
8. HQ must determine loaner eligibility for challenge types without weapons (future race/escape), specific reward caps or score flags and whether a user can choose loaner voluntarily even with working craft. R1 defaults to **no voluntary loaner** and **no rewards rule rewrite**.

## Explicit HQ policy approval requested
Approve A/B/C (recommend C), confirm whether lease should earn standard challenge rewards (recommend YES for real victory, to fund still-paid components) and whether future noncombat stages require a weapon (should be activity-specific). The isolated R1 is safe to QA without this approval. **No host integration until answer.**
