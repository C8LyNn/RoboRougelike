"""B01-10 W20: independent deterministic rebuild-cadence design model.

NOT an authoritative economy implementation or a Unity/PhysX player study.
No interaction with B01-22 owner store, canonical #252, or save slots.
"""
from __future__ import annotations

import argparse
import json
import random
import sys
from pathlib import Path


ROLES = ("mobility", "weapon", "other")
CADENCES = ("per_win", "per_run")
PRIORITIES = ("critical_first", "input_order")
REWARD_TIMINGS = ("after_reservation", "before_reservation")
APPROVED_PAID_QUOTE_PERCENT = 65
QUOTE_ROUNDINGS = ("floor", "nearest_half_up_clamped")
APPROVED_POLICY_ID = "B01-P17-W24-OWNER-APPROVED-DESIGN-ORACLE"


class InvalidScenario(ValueError):
    pass


def _number(value: object, field: str, minimum: int = 0) -> int:
    if type(value) is not int or value < minimum or value > 1_000_000_000:
        raise InvalidScenario(f"{field} must be an integer in bounds")
    return value


def _identity(value: object, field: str) -> str:
    if not isinstance(value, str) or not 1 <= len(value) <= 96:
        raise InvalidScenario(f"{field} must be a nonempty bounded string")
    if not all(c.isascii() and (c.isalnum() or c in "-_:") for c in value):
        raise InvalidScenario(f"{field} contains unsafe characters")
    return value


def validate(plan: dict) -> dict:
    """Validate ALL input before applying even simulated balance operations."""
    if not isinstance(plan, dict):
        raise InvalidScenario("scenario must be an object")
    initial = _number(plan.get("initial_scrap"), "initial_scrap")
    purchase = _number(plan.get("purchase_price"), "purchase_price", 1)
    percent = _number(plan.get("paid_quote_percent"), "paid_quote_percent", 1)
    if percent >= 100:
        raise InvalidScenario("discounted rebuild must cost less than purchase")
    rounding = plan.get("quote_rounding", "floor")
    if rounding not in QUOTE_ROUNDINGS:
        raise InvalidScenario("unsupported paid-quote rounding")
    raw = purchase * percent
    quote = (raw // 100 if rounding == "floor" else
             min((raw + 50) // 100, purchase - 1))
    # A positive purchase of 1 Scrap necessarily clamps the approved 65%
    # quote to 0. Zero-cost PAID reservations still count as PAID, not
    # as an additional free-victory quota; preserve both distinct ledgers.
    if not 0 <= quote < purchase:
        raise InvalidScenario("rounded paid quote outside valid range")
    steps = plan.get("attempts")
    if not isinstance(steps, list) or not 1 <= len(steps) <= 10000:
        raise InvalidScenario("attempt count must be between 1 and 10000")
    seen_events = set()
    normalized = []
    for event in steps:
        if not isinstance(event, dict):
            raise InvalidScenario("attempt must be an object")
        eid = _identity(event.get("id"), "event id")
        if eid in seen_events:
            raise InvalidScenario("duplicate attempt/victory ID: " + eid)
        seen_events.add(eid)
        won = event.get("won")
        if type(won) is not bool:
            raise InvalidScenario("won must be a real boolean")
        earned = _number(event.get("earned_scrap"), "earned_scrap")
        items = event.get("destroyed")
        if not isinstance(items, list) or len(items) > 128:
            raise InvalidScenario("destroyed must be a list of <=128 items")
        within = set()
        parts = []
        for p in items:
            if not isinstance(p, dict):
                raise InvalidScenario("destroyed part must be an object")
            pid = _identity(p.get("id"), "part id")
            role = p.get("role")
            if role not in ROLES:
                raise InvalidScenario("unknown part role")
            if pid in within:
                raise InvalidScenario("same physical ID listed twice in one attempt")
            within.add(pid)
            essential = p.get("blocks_deploy", False)
            if type(essential) is not bool:
                raise InvalidScenario("blocks_deploy must be a real boolean")
            parts.append((pid, role, essential))
        core_alive_after = event.get("core_alive_after", True)
        challenge_after = event.get("next_challenge_available_after", True)
        if type(core_alive_after) is not bool:
            raise InvalidScenario("core_alive_after must be a real boolean")
        if type(challenge_after) is not bool:
            raise InvalidScenario("next_challenge_available_after must be a real boolean")
        normalized.append((eid, won, earned, tuple(parts),
                           core_alive_after, challenge_after))
    return {"initial_scrap": initial, "purchase_price": purchase,
            "paid_quote_percent": percent, "quote": quote,
            "quote_rounding": rounding,
            "steps": tuple(normalized)}


def run_plan(plan: dict, *, cadence: str = "per_win",
             bootstrap: bool = True, priority: str = "critical_first",
             enforce_deploy_gate: bool = False,
             loaner_challenges: int = 0,
             reward_timing: str = "after_reservation") -> dict:
    """Model post-encounter destroyed-part requests; never write a game host.

    Win first completes only preexisting reservations, THEN new event damage
    can queue for a FUTURE victory. Optional essential deploy requirements are
    explicit synthetic input assumptions, not Unity CanDeploy assessment.
    """
    if cadence not in CADENCES or priority not in PRIORITIES:
        raise InvalidScenario("unsupported design policy")
    if reward_timing not in REWARD_TIMINGS:
        raise InvalidScenario("unsupported reward settlement timing")
    if type(bootstrap) is not bool:
        raise InvalidScenario("bootstrap must be boolean")
    if type(enforce_deploy_gate) is not bool:
        raise InvalidScenario("enforce_deploy_gate must be boolean")
    # Borrowed rig under #154 is NOT approved; one hypothetical use maximum
    # here, NEVER a live inventory asset/reward authority.
    if type(loaner_challenges) is not int or loaner_challenges not in (0, 1):
        raise InvalidScenario("loaner_challenges must be zero or one")
    if loaner_challenges and not enforce_deploy_gate:
        raise InvalidScenario("loaner requires modeled deploy eligibility gate")
    p = validate(plan)
    wallet = p["initial_scrap"]
    quote = p["quote"]
    wins = 0
    free_epoch = -1
    free_run_used = False
    destroyed: dict[str, tuple[str, bool]] = {}
    reserved: dict[str, tuple[str, int, bool]] = {}
    free_count = paid_count = denied_count = restored_count = 0
    expense = reward = 0
    critical_at_end = 0
    no_budget_critical = 0
    loaner_used = blocked_deploy_attempts = contradicted_scripted_wins = 0
    blocked_core_attempts = blocked_no_challenge_attempts = 0
    core_alive = True
    next_challenge_available = True
    observations: list[dict] = []
    for (eid, won, earned, new,
         core_alive_after, challenge_after) in p["steps"]:
        # PRE-ENCOUNTER safe eligibility. Core loss and absence of an eligible
        # next challenge cannot be bypassed even by a hypothetical loaner.
        essential_before = sum(is_essential for _, is_essential in destroyed.values())
        started_with_loaner = False
        reason = ("RUN_CORE_DESTROYED" if not core_alive
                  else "NO_ELIGIBLE_NEXT_CHALLENGE" if not next_challenge_available
                  else "ESSENTIAL_0HP_PREVENTS_DEPLOY"
                       if enforce_deploy_gate and essential_before else None)
        if reason:
            if (reason == "ESSENTIAL_0HP_PREVENTS_DEPLOY"
                    and loaner_used < loaner_challenges):
                loaner_used += 1
                started_with_loaner = True
            else:
                blocked_deploy_attempts += 1
                blocked_core_attempts += int(reason == "RUN_CORE_DESTROYED")
                blocked_no_challenge_attempts += int(
                    reason == "NO_ELIGIBLE_NEXT_CHALLENGE")
                contradicted_scripted_wins += int(won)
                observations.append({
                    "attempt": eid, "win": False, "scripted_win_blocked": bool(won),
                    "blocked_reason": reason,
                    "wallet": wallet, "wins": wins,
                    "unavailable_mobility": sum(
                        role == "mobility" for role, _ in destroyed.values()),
                    "unavailable_essential": essential_before,
                    "core_alive": core_alive,
                    "next_challenge_available": next_challenge_available,
                    "still_destroyed": len(destroyed),
                    "reserved_waiting": len(reserved)})
                # Invalid planned attempts cannot earn currency, damage a
                # real rig, use an OS slot or consume a trusted victory.
                continue
        # IMPORTANT W21 CORRECTION: an actual win happens BEFORE damage from
        # that encounter can enter the POST-encounter queue. Only tickets that
        # existed *before this win* can be completed on this win.
        if won:
            restored_count += len(reserved)
            for pid in tuple(reserved):
                destroyed.pop(pid, None)
            reserved.clear()
            wins += 1
        for pid, role, essential in new:
            if pid not in destroyed:
                destroyed[pid] = (role, essential)
        # *Hypothesis only*: many garages credit encounter earnings BEFORE the
        # post-combat reservation screen. The historical default instead charges
        # first. Do not assume either sequence matches the native reward owner.
        if reward_timing == "before_reservation":
            wallet += earned
            reward += earned
        items = [(pid, role, essential)
                 for pid, (role, essential) in destroyed.items()
                 if pid not in reserved]
        if priority == "critical_first":
            items.sort(key=lambda pair: (not pair[2], ROLES.index(pair[1]), pair[0]))
        for pid, role, essential in items:
            has_credit = bootstrap or wins > 0
            free = has_credit and (free_epoch != wins if cadence == "per_win"
                                   else not free_run_used)
            charge = 0 if free else quote
            if wallet < charge:
                denied_count += 1
                continue
            wallet -= charge
            expense += charge
            reserved[pid] = (eid, charge, free)
            if free:
                free_count += 1
                if cadence == "per_win":
                    free_epoch = wins
                else:
                    free_run_used = True
            else:
                paid_count += 1
        if reward_timing == "after_reservation":
            wallet += earned
            reward += earned
        # These are explicitly modeled *AFTER* encounter conditions.
        # A destroyed core or no eligible next challenge remains blocked on
        # subsequent events; a synthetic script cannot silently revive it.
        core_alive = core_alive and core_alive_after
        next_challenge_available = (
            next_challenge_available and challenge_after)
        critical = sum(role == "mobility"
                       for role, _ in destroyed.values())
        essential = sum(is_essential for _, is_essential in destroyed.values())
        reserved_mobility = sum(
            role == "mobility" and pid in reserved
            for pid, (role, _) in destroyed.items())
        unreserved_mobility = critical - reserved_mobility
        reserved_essential = sum(pid in reserved for pid, (_, blocked) in
                                 destroyed.items() if blocked)
        critical_at_end += int(critical > 0)
        no_budget_critical += int(critical > 0 and wallet < quote)
        observations.append({"attempt": eid, "win": won, "wallet": wallet,
                             "wins": wins, "unavailable_mobility": critical,
                             "reserved_mobility": reserved_mobility,
                             "unreserved_mobility": unreserved_mobility,
                             "unavailable_essential": essential,
                             "reserved_essential": reserved_essential,
                             "unreserved_essential": essential - reserved_essential,
                             "used_temporary_loaner": started_with_loaner,
                             "core_alive": core_alive,
                             "next_challenge_available": next_challenge_available,
                             "still_destroyed": len(destroyed),
                             "reserved_waiting": len(reserved)})
    end_essential = sum(flag for _, flag in destroyed.values())
    if not core_alive:
        end_feasibility = "RUN_CORE_DESTROYED_NO_LOANER"
    elif not next_challenge_available:
        end_feasibility = "NO_ELIGIBLE_NEXT_CHALLENGE"
    elif end_essential and enforce_deploy_gate:
        end_feasibility = ("ONE_UNAPPROVED_LOANER_COUNTERFACTUAL_AVAILABLE"
                           if loaner_used < loaner_challenges else
                           "ESSENTIAL_0HP_NEXT_WIN_INACCESSIBLE")
    elif end_essential:
        end_feasibility = "ESSENTIAL_0HP_HOST_DEPLOY_UNVERIFIED"
    else:
        end_feasibility = "NORMAL_DEPLOY_ELIGIBLE_IN_MODEL_NOT_ACTUAL_WIN"
    # Delayed tickets may become impossible to complete. The debit was already
    # made, so expose real modeled economic exposure without a refund API.
    # These are modeled exposure amounts, NOT actual game losses/refunds.
    permanently_blocked_model = end_feasibility in (
        "RUN_CORE_DESTROYED_NO_LOANER",
        "NO_ELIGIBLE_NEXT_CHALLENGE",
        "ESSENTIAL_0HP_NEXT_WIN_INACCESSIBLE")
    stranded_tickets = len(reserved) if permanently_blocked_model else 0
    stranded_paid_scrap = (sum(charge for _, charge, _ in reserved.values())
                           if permanently_blocked_model else 0)
    stranded_free_tickets = (
        sum(is_free for _, _, is_free in reserved.values())
        if permanently_blocked_model else 0)
    return {"cadence": cadence, "bootstrap": bootstrap, "priority": priority,
            "reward_timing": reward_timing,
            "modeled_stranded_pending_count": stranded_tickets,
            "modeled_stranded_paid_scrap": stranded_paid_scrap,
            "modeled_stranded_free_tickets": stranded_free_tickets,
            "end_feasibility": end_feasibility,
            "end_core_alive": core_alive,
            "end_next_challenge_available": next_challenge_available,
            "end_unavailable_essential": end_essential,
            "quote": quote, "attempts": len(p["steps"]), "wins": wins,
            "enforce_deploy_gate": enforce_deploy_gate,
            "hypothetical_loaner_budget": loaner_challenges,
            "hypothetical_loaner_used": loaner_used,
            "blocked_deploy_attempts": blocked_deploy_attempts,
            "blocked_core_attempts": blocked_core_attempts,
            "blocked_no_challenge_attempts": blocked_no_challenge_attempts,
            "contradicted_scripted_wins": contradicted_scripted_wins,
            "free": free_count, "paid": paid_count, "denied": denied_count,
            "restored": restored_count, "charged_scrap": expense,
            "earned_scrap": reward, "wallet_final": wallet,
            "destroyed_final": len(destroyed),
            "reserved_final": len(reserved),
            "attempts_with_mobility_unavailable": critical_at_end,
            "attempts_with_mobility_and_low_wallet": no_budget_critical,
            "observations": observations}


def compare(plan: dict, *, bootstrap: bool = True,
            priority: str = "critical_first",
            reward_timing: str = "after_reservation") -> dict:
    a = run_plan(plan, cadence="per_win", bootstrap=bootstrap,
                 priority=priority, reward_timing=reward_timing)
    b = run_plan(plan, cadence="per_run", bootstrap=bootstrap,
                 priority=priority, reward_timing=reward_timing)
    return {"per_win": a, "per_run": b,
            "delta_scrap_per_run_minus_per_win":
                b["charged_scrap"] - a["charged_scrap"]}


def run_approved_plan(plan: dict, *,
                      reward_timing: str = "after_reservation") -> dict:
    """2026-10-30 OWNER APPROVED economic design policy, NOT a trusted host.

    One REAL owner-validated winner is assumed for each scripted won=True
    event. This function does NOT authenticate wins, leases, IDs, live Rig[],
    or the single durable currency transaction. Host must do that itself.
    No free epoch zero. One temporary loaner per run, only where explicit
    CanDeploy evidence says essential 0HP while Core alive and challenge
    available. Exactly 65% floor on positive purchase cost; paid-zero stays
    a paid order, not a second coupon. Reward settlement order NOT DECIDED.
    """
    parsed = validate(plan)
    approved = dict(plan)
    approved["paid_quote_percent"] = APPROVED_PAID_QUOTE_PERCENT
    # Preserve V6 (#764) half-up values 5->3,15->10,30->20,100->65.
    # Fix the strict-cheaper P=1 boundary: nearest(0.65)=1 -> clamp to 0.
    # This is a proposed inter-owner resolution, not a merged V6 change.
    approved["quote_rounding"] = "nearest_half_up_clamped"
    out = run_plan(approved, cadence="per_win", bootstrap=False,
                   priority="critical_first", enforce_deploy_gate=True,
                   loaner_challenges=1, reward_timing=reward_timing)
    out["owner_approved_policy"] = APPROVED_POLICY_ID
    out["actual_trusted_victory_attestation"] = "NOT_AVAILABLE_MODEL_ONLY"
    out["approved_one_free_per_verified_win_assumed"] = True
    out["approved_loaner_weaker_asset"] = "DESIGN_CONTRACT_ONLY_NOT_SPAWNED"
    out["source_quote_overridden_from"] = parsed["paid_quote_percent"]
    out["quote_policy_proposal"] = (
        "B01-10_PROPOSED_V6_HALF_UP_CLAMP_MAX_PURCHASE_MINUS_1")
    out["quote_formula"] = (
        "min((purchase_price*65+50)//100, purchase_price-1)" )
    out["live_reward_settlement"] = "OWNER_IMPLEMENTATION_UNVERIFIED"
    # A fully approved temporary loaner is NOT sufficient if it wins but no
    # damaged essential component had any accepted reservation BEFORE that
    # victory. Zero-Scrap cannot buy pre-win repairs, free credit is not
    # permitted before a verified victory, and the single loaner is exhausted.
    # Flag as a real missing GAME-DESIGN CONTRACT/owner handoff rather than
    # pretending a source-only model fixed this circular dependency.
    out["p0_post_loaner_win_deferred_rebuild_deadlock"] = bool(
        any(o.get("used_temporary_loaner") and o.get("win")
            for o in out["observations"])
        and out["end_feasibility"] ==
            "ESSENTIAL_0HP_NEXT_WIN_INACCESSIBLE")
    out["p0_recovery_contract"] = (
        "OWNER_B01_22_DECIDE_TRUSTED_CONDITIONAL_PREWIN_ORDER_OR_ALTERNATE"
        if out["p0_post_loaner_win_deferred_rebuild_deadlock"]
        else "NO_POST_LOANER_WIN_DEADLOCK_OBSERVED_IN_MODEL")
    return out


def synthetic_batch(*, seed: int = 20261030, episodes: int = 200,
                    win_percent: int = 55, quote_percent: int = 70,
                    bootstrap: bool = True,
                    reward_timing: str = "after_reservation") -> dict:
    """Fixed-seed invented traces for sensitivity exploration, NOT playtests."""
    _number(seed, "seed")
    _number(episodes, "episodes", 1)
    if episodes > 1000:
        raise InvalidScenario("too many synthetic episodes")
    _number(win_percent, "win_percent")
    if win_percent > 100:
        raise InvalidScenario("win_percent exceeds 100")
    _number(quote_percent, "quote_percent", 1)
    if reward_timing not in REWARD_TIMINGS:
        raise InvalidScenario("unsupported reward settlement timing")
    rng = random.Random(seed)
    keys = ("charged_scrap", "free", "paid", "denied", "restored",
            "destroyed_final", "attempts_with_mobility_unavailable",
            "attempts_with_mobility_and_low_wallet")
    totals = {p: {k: 0 for k in keys} for p in CADENCES}
    for run in range(episodes):
        events = []
        for j in range(12):
            destroyed = []
            for slot in range(rng.randrange(5)):
                destroyed.append({"id": f"p{run}_{j}_{slot}",
                                  "role": ROLES[rng.randrange(3)]})
            events.append({"id": f"e{run}_{j}",
                           "won": rng.randrange(100) < win_percent,
                           "earned_scrap": rng.choice((0, 25, 50, 100)),
                           "destroyed": destroyed})
        plan = {"initial_scrap": 400, "purchase_price": 100,
                "paid_quote_percent": quote_percent, "attempts": events}
        for policy in CADENCES:
            outcome = run_plan(plan, cadence=policy, bootstrap=bootstrap,
                               reward_timing=reward_timing)
            for key in keys:
                totals[policy][key] += outcome[key]
    return {"notice": "SYNTHETIC_ONLY; NOT a real human/Unity gameplay experiment",
            "seed": seed, "episodes": episodes, "attempts_each": 12,
            "win_percent": win_percent, "quote_percent": quote_percent,
            "bootstrap": bootstrap, "reward_timing": reward_timing,
            "aggregate": totals}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--scenarios", type=Path)
    ap.add_argument("--stress", action="store_true")
    ns = ap.parse_args(argv)
    if bool(ns.scenarios) == ns.stress:
        ap.error("choose exactly one of --scenarios or --stress")
    try:
        if ns.stress:
            output = synthetic_batch()
        else:
            items = json.loads(ns.scenarios.read_text(encoding="utf-8"))
            if not isinstance(items, dict) or not isinstance(items.get("cases"), dict):
                raise InvalidScenario("scenarios requires cases object")
            output = {"notice": "MODEL ONLY; reward order, deploy gate and "
                                 "loaner are UNAPPROVED synthetic alternatives",
                      "cases": {name: {
                          "bootstrap_epoch_zero": compare(plan, bootstrap=True),
                          "no_bootstrap_epoch_zero": compare(plan, bootstrap=False),
                          "deploy_gate_no_loaner": run_plan(
                              plan, enforce_deploy_gate=True),
                          "deploy_gate_one_hypothetical_loaner": run_plan(
                              plan, enforce_deploy_gate=True,
                              loaner_challenges=1),
                          "reward_credit_before_order": compare(
                              plan, reward_timing="before_reservation"),
                          "approved_owner_policy": run_approved_plan(plan),
                          "approved_owner_policy_reward_first_alternative":
                              run_approved_plan(
                                  plan, reward_timing="before_reservation")
                      } for name, plan in items["cases"].items()}}
    except (InvalidScenario, ValueError, OSError) as exc:
        print("MODEL_INPUT_REJECTED: " + str(exc), file=sys.stderr)
        return 2
    print(json.dumps(output, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
