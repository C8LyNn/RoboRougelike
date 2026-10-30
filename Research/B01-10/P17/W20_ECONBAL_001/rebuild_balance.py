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
            parts.append((pid, role))
        normalized.append((eid, won, earned, tuple(parts)))
    return {"initial_scrap": initial, "purchase_price": purchase,
            "paid_quote_percent": percent, "quote": quote,
            "steps": tuple(normalized)}


def run_plan(plan: dict, *, cadence: str = "per_win",
             bootstrap: bool = True, priority: str = "critical_first") -> dict:
    """Model destroyed-part requests and next-WIN completion, never host writes.

    One global free reservation per genuine win epoch (optionally epoch-0
    bootstrap) versus one per run. Per-win FREE is NOT a per-purchase coupon.
    Losses neither restore parts nor refill the free allowance.
    """
    if cadence not in CADENCES or priority not in PRIORITIES:
        raise InvalidScenario("unsupported design policy")
    if type(bootstrap) is not bool:
        raise InvalidScenario("bootstrap must be boolean")
    p = validate(plan)
    wallet = p["initial_scrap"]
    quote = p["quote"]
    wins = 0
    free_epoch = -1
    free_run_used = False
    destroyed: dict[str, str] = {}
    reserved: dict[str, tuple[str, int]] = {}
    free_count = paid_count = denied_count = restored_count = 0
    expense = reward = 0
    critical_at_end = 0
    no_budget_critical = 0
    observations: list[dict] = []
    for eid, won, earned, new in p["steps"]:
        for pid, role in new:
            if pid not in destroyed:
                destroyed[pid] = role
        items = [(pid, role) for pid, role in destroyed.items()
                 if pid not in reserved]
        if priority == "critical_first":
            items.sort(key=lambda pair: (ROLES.index(pair[1]), pair[0]))
        for pid, role in items:
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
        if won:
            # A successful unique event authenticates the next cycle in this
            # *model*. Only a future trusted runtime owner can authenticate it.
            restored_count += len(reserved)
            for pid in tuple(reserved):
                destroyed.pop(pid, None)
            reserved.clear()
            wins += 1
        wallet += earned
        reward += earned
        critical = sum(role == "mobility" for role in destroyed.values())
        critical_at_end += int(critical > 0)
        no_budget_critical += int(critical > 0 and wallet < quote)
        observations.append({"attempt": eid, "win": won, "wallet": wallet,
                             "wins": wins, "unavailable_mobility": critical,
                             "still_destroyed": len(destroyed),
                             "reserved_waiting": len(reserved)})
    return {"cadence": cadence, "bootstrap": bootstrap, "priority": priority,
            "quote": quote, "attempts": len(p["steps"]), "wins": wins,
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
            output = {"notice": "MODEL ONLY, not host/native/player evidence",
                      "cases": {name: compare(plan)
                                for name, plan in items["cases"].items()}}
    except (InvalidScenario, ValueError, OSError) as exc:
        print("MODEL_INPUT_REJECTED: " + str(exc), file=sys.stderr)
        return 2
    print(json.dumps(output, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    sys.exit(main())
