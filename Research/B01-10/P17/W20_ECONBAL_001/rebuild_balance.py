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
    purchase = _number(plan.get("purchase_price"), "purchase_price", 2)
    percent = _number(plan.get("paid_quote_percent"), "paid_quote_percent", 1)
    if percent >= 100:
        raise InvalidScenario("discounted rebuild must cost less than purchase")
    quote = purchase * percent // 100
    if not 0 < quote < purchase:
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
            "steps": tuple(normalized)}


def run_plan(plan: dict, *, cadence: str = "per_win",
             bootstrap: bool = True, priority: str = "critical_first",
             enforce_deploy_gate: bool = False,
             loaner_challenges: int = 0) -> dict:
    """Model post-encounter destroyed-part requests; never write a game host.

    Win first completes only preexisting reservations, THEN new event damage
    can queue for a FUTURE victory. Optional essential deploy requirements are
    explicit synthetic input assumptions, not Unity CanDeploy assessment.
    """
    if cadence not in CADENCES or priority not in PRIORITIES:
        raise InvalidScenario("unsupported design policy")
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
    reserved: dict[str, tuple[str, int]] = {}
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
            reserved[pid] = (eid, charge)
            if free:
                free_count += 1
                if cadence == "per_win":
                    free_epoch = wins
                else:
                    free_run_used = True
            else:
                paid_count += 1
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
    return {"cadence": cadence, "bootstrap": bootstrap, "priority": priority,
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
            priority: str = "critical_first") -> dict:
    a = run_plan(plan, cadence="per_win", bootstrap=bootstrap, priority=priority)
    b = run_plan(plan, cadence="per_run", bootstrap=bootstrap, priority=priority)
    return {"per_win": a, "per_run": b,
            "delta_scrap_per_run_minus_per_win":
                b["charged_scrap"] - a["charged_scrap"]}


def synthetic_batch(*, seed: int = 20261030, episodes: int = 200,
                    win_percent: int = 55, quote_percent: int = 70,
                    bootstrap: bool = True) -> dict:
    """Fixed-seed invented traces for sensitivity exploration, NOT playtests."""
    _number(seed, "seed")
    _number(episodes, "episodes", 1)
    if episodes > 1000:
        raise InvalidScenario("too many synthetic episodes")
    _number(win_percent, "win_percent")
    if win_percent > 100:
        raise InvalidScenario("win_percent exceeds 100")
    _number(quote_percent, "quote_percent", 1)
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
            outcome = run_plan(plan, cadence=policy, bootstrap=bootstrap)
            for key in keys:
                totals[policy][key] += outcome[key]
    return {"notice": "SYNTHETIC_ONLY; NOT a real human/Unity gameplay experiment",
            "seed": seed, "episodes": episodes, "attempts_each": 12,
            "win_percent": win_percent, "quote_percent": quote_percent,
            "bootstrap": bootstrap, "aggregate": totals}


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
            output = {"notice": "MODEL ONLY; deploy gate and loaner are "
                                 "UNAPPROVED synthetic counterfactuals",
                      "cases": {name: {
                          "bootstrap_epoch_zero": compare(plan, bootstrap=True),
                          "no_bootstrap_epoch_zero": compare(plan, bootstrap=False),
                          "deploy_gate_no_loaner": run_plan(
                              plan, enforce_deploy_gate=True),
                          "deploy_gate_one_hypothetical_loaner": run_plan(
                              plan, enforce_deploy_gate=True,
                              loaner_challenges=1)
                      } for name, plan in items["cases"].items()}}
    except (InvalidScenario, ValueError, OSError) as exc:
        print("MODEL_INPUT_REJECTED: " + str(exc), file=sys.stderr)
        return 2
    print(json.dumps(output, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
