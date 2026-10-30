"""B01-10 W20 original executable deterministic economy *design-model* tests."""
import json
import unittest
from pathlib import Path

from rebuild_balance import (
    InvalidScenario, compare, run_plan, synthetic_batch, validate,
)

ROOT = Path(__file__).resolve().parent


def plan(events, *, cash=1000, price=100, percent=70):
    return {"initial_scrap": cash, "purchase_price": price,
            "paid_quote_percent": percent, "attempts": events}


def part(i, role="other"):
    return {"id": i, "role": role}


def attempt(i, won=True, destroyed=(), earned=0):
    return {"id": i, "won": won, "earned_scrap": earned,
            "destroyed": list(destroyed)}


class RebuildModelTests(unittest.TestCase):
    def test_01_exact_12wins_two_parts_per_win_840_vs_1610(self):
        steps = [attempt(f"win{i}", destroyed=(part(f"{i}a"), part(f"{i}b")))
                 for i in range(12)]
        result = compare(plan(steps, cash=4000))
        a, b = result["per_win"], result["per_run"]
        self.assertEqual((a["free"], a["paid"], a["charged_scrap"]),
                         (12, 12, 840))
        self.assertEqual((b["free"], b["paid"], b["charged_scrap"]),
                         (1, 23, 1610))
        self.assertEqual(result["delta_scrap_per_run_minus_per_win"], 770)
        self.assertEqual((a["restored"], b["restored"]), (24, 24))

    def test_02_discount_65_percent_is_quote_65(self):
        self.assertEqual(run_plan(plan([attempt("w", destroyed=(part("a"),part("b")))],
                                       percent=65))["charged_scrap"], 65)

    def test_03_discount_75_percent_is_quote_75(self):
        self.assertEqual(run_plan(plan([attempt("w", destroyed=(part("a"),part("b")))],
                                       percent=75))["charged_scrap"], 75)

    def test_04_original_winner_id_is_unique_across_all_attempts(self):
        with self.assertRaises(InvalidScenario):
            run_plan(plan([attempt("victory", True),attempt("victory", True)]))

    def test_05_loss_does_not_refill_free_quota(self):
        x = run_plan(plan([attempt("a",False,(part("a"),)),
                           attempt("b",False,(part("b"),))]))
        self.assertEqual((x["free"], x["paid"], x["wins"]), (1,1,0))

    def test_06_paid_fee_charged_at_reservation_not_after_win(self):
        x = run_plan(plan([attempt("loss",False,(part("a"),part("b")))],cash=100))
        self.assertEqual((x["charged_scrap"],x["restored"],x["reserved_final"]),
                         (70,0,2))

    def test_07_next_authored_win_completes_pending_ticket_model(self):
        x = run_plan(plan([attempt("loss",False,(part("a"),)),
                           attempt("win",True)]))
        self.assertEqual((x["restored"],x["reserved_final"],x["destroyed_final"]),
                         (1,0,0))

    def test_08_no_win_means_pending_never_auto_heals(self):
        x = run_plan(plan([attempt("loss",False,(part("a","mobility"),))]))
        self.assertEqual((x["restored"],x["reserved_final"],
                          x["attempts_with_mobility_unavailable"]), (0,1,1))

    def test_09_no_scrap_denies_paid_request_not_negative_wallet(self):
        x = run_plan(plan([attempt("loss",False,(part("a"),part("b")))],cash=0))
        self.assertEqual((x["free"],x["denied"],x["paid"],x["wallet_final"]),
                         (1,1,0,0))

    def test_10_wallet_earnings_do_not_pay_past_rebuild_in_same_event(self):
        x = run_plan(plan([attempt("win",True,(part("a"),part("b")),earned=100)],
                          cash=0))
        self.assertEqual((x["free"],x["denied"],x["paid"],
                          x["wallet_final"]), (1,1,0,100))

    def test_11_next_attempt_can_use_scrap_earned_on_previous_win(self):
        x = run_plan(plan([attempt("win1",True,earned=70),
                           attempt("win2",True,(part("a"),part("b")))],cash=0))
        self.assertEqual((x["free"],x["paid"],x["charged_scrap"]),
                         (1,1,70))

    def test_12_priority_saves_mobility_with_zero_scrap(self):
        x = run_plan(plan([attempt("loss",False,(part("cosmetic","other"),
                                                  part("wheel","mobility")))],cash=0))
        self.assertEqual((x["free"],x["denied"],x["reserved_final"],
                          x["attempts_with_mobility_unavailable"]), (1,1,1,1))
        # Both are physically unavailable before the next genuine victory.
        self.assertEqual(x["observations"][0]["unavailable_mobility"],1)
        self.assertEqual(x["observations"][0]["reserved_mobility"],1)
        self.assertEqual(x["observations"][0]["unreserved_mobility"],0)

    def test_13_prioritization_prefers_mobility_even_if_listed_later(self):
        p = plan([attempt("lost",False,(part("paint","other"),
                                        part("wheel","mobility")))],cash=0)
        # Critical-first chooses the wheel; inspect by granting a later win
        # and rejecting mistaken claims that it already deployed on loss.
        a=run_plan(p,priority="critical_first")
        b=run_plan(p,priority="input_order")
        self.assertEqual(a["free"],1)
        self.assertEqual(b["free"],1)
        self.assertEqual(a["restored"],0)
        self.assertEqual(b["restored"],0)
        self.assertEqual(a["observations"][0]["reserved_mobility"],1)
        self.assertEqual(b["observations"][0]["reserved_mobility"],0)
        self.assertEqual(a["observations"][0]["unreserved_mobility"],0)
        self.assertEqual(b["observations"][0]["unreserved_mobility"],1)

    def test_14_no_bootstrap_requires_winning_before_first_coupon(self):
        s=[attempt("first",True,(part("a"),)),
           attempt("second",True,(part("b"),))]
        x=run_plan(plan(s),cadence="per_win",bootstrap=False)
        self.assertEqual((x["free"],x["paid"]), (1,1))

    def test_15_per_run_without_bootstrap_never_refills_after_second_win(self):
        s=[attempt("w1",True,(part("a"),)),
           attempt("w2",True,(part("b"),)),
           attempt("w3",True,(part("c"),))]
        x=run_plan(plan(s),cadence="per_run",bootstrap=False)
        self.assertEqual((x["free"],x["paid"]), (1,2))

    def test_16_per_win_without_bootstrap_has_no_free_on_first_loss(self):
        x=run_plan(plan([attempt("lost",False,(part("a"),))],cash=100,
                        ),bootstrap=False)
        self.assertEqual((x["free"],x["paid"]), (0,1))

    def test_17_no_destroyed_parts_consume_no_credit(self):
        x=run_plan(plan([attempt("w1",True),attempt("w2",True,(part("a"),))]))
        self.assertEqual((x["free"],x["paid"],x["restored"]), (1,0,1))

    def test_18_repeated_destroyed_id_is_not_second_bill(self):
        s=[attempt("a",False,(part("same"),)),
           attempt("b",False,(part("same"),))]
        x=run_plan(plan(s))
        self.assertEqual((x["free"],x["paid"],x["denied"],x["reserved_final"]),
                         (1,0,0,1))

    def test_19_same_part_can_be_destroyed_after_authoritative_new_win(self):
        s=[attempt("a",True,(part("wheel","mobility"),)),
           attempt("b",True,(part("wheel","mobility"),))]
        x=run_plan(plan(s))
        self.assertEqual((x["free"],x["restored"]), (2,2))

    def test_20_same_physical_id_twice_within_one_event_is_invalid(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt("w",True,(part("same"),part("same")))]))

    def test_21_non_integer_quote_is_invalid_not_coerced(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt("w")],percent=70.0))

    def test_22_boolean_wallet_rejected_as_int(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt("w")],cash=True))

    def test_23_nonboolean_result_rejected(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([{"id":"w","won":1,"earned_scrap":0,
                            "destroyed":[]}]))

    def test_24_unsafe_event_identifier_rejected(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt("../../victory")]))

    def test_25_nonfinite_input_rejected_by_integer_validation(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt("win")],cash=float("nan")))

    def test_26_quote_rounding_to_zero_rejected(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt("win")],price=2,percent=1))

    def test_27_purchase_price_must_exceed_quote(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt("win")],percent=100))

    def test_28_unknown_role_does_not_enter_model(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt("win",destroyed=(part("p","shield"),))]))

    def test_29_unsupported_cadence_rejected(self):
        with self.assertRaises(InvalidScenario):
            run_plan(plan([attempt("win")]),cadence="per_level")

    def test_30_unknown_priority_rejected(self):
        with self.assertRaises(InvalidScenario):
            run_plan(plan([attempt("win")]),priority="random")

    def test_31_bootstrap_must_be_boolean(self):
        with self.assertRaises(InvalidScenario):
            run_plan(plan([attempt("win")]),bootstrap=1)

    def test_32_empty_scenarios_rejected(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([]))

    def test_33_too_many_events_rejected(self):
        with self.assertRaises(InvalidScenario):
            validate(plan([attempt(f"id{i}") for i in range(10001)]))

    def test_34_wallet_conservation_exact(self):
        s=[attempt("lost",False,(part("a"),part("b")),earned=25),
           attempt("win",True,(part("c"),),earned=100)]
        p=plan(s,cash=500)
        x=run_plan(p)
        self.assertEqual(x["wallet_final"],
                         p["initial_scrap"]+x["earned_scrap"]-x["charged_scrap"])

    def test_35_all_hypotheses_fixture_json_parses_and_runs(self):
        data=json.loads((ROOT/"scenarios.json").read_text(encoding="utf-8"))
        for name,scenario in data["cases"].items():
            with self.subTest(name=name):
                report=compare(scenario)
                self.assertEqual(report["per_win"]["attempts"],
                                 len(scenario["attempts"]))

    def test_36_synthetic_seed_is_exactly_reproducible(self):
        self.assertEqual(synthetic_batch(seed=123,episodes=8),
                         synthetic_batch(seed=123,episodes=8))

    def test_37_distinct_synthetic_seeds_change_distributions(self):
        self.assertNotEqual(synthetic_batch(seed=123,episodes=8)["aggregate"],
                            synthetic_batch(seed=456,episodes=8)["aggregate"])

    def test_38_synthetic_is_honestly_labeled(self):
        self.assertIn("SYNTHETIC_ONLY", synthetic_batch(seed=1,episodes=1)["notice"])

    def test_39_invalid_synthetic_win_probability_rejected(self):
        with self.assertRaises(InvalidScenario):
            synthetic_batch(win_percent=101)

    def test_40_zero_win_probability_no_real_wins(self):
        p=synthetic_batch(seed=99,episodes=2,win_percent=0)
        self.assertEqual(p["aggregate"]["per_win"]["restored"],0)
        self.assertEqual(p["aggregate"]["per_run"]["restored"],0)

    def test_41_fixed_high_wallet_free_per_win_cannot_cost_more_than_per_run(self):
        p={"initial_scrap":100000,"purchase_price":100,"paid_quote_percent":70,
           "attempts":[attempt(f"v{i}",i%3!=1,
                               (part(f"p{i}a"),part(f"p{i}b"))) for i in range(60)]}
        c=compare(p)
        self.assertGreaterEqual(c["delta_scrap_per_run_minus_per_win"],0)

    def test_42_paid_cost_never_exceeds_purchase(self):
        for quote in (25,50,65,70,75,90,99):
            x=run_plan(plan([attempt("w",True,(part("a"),part("b")))],
                            percent=quote))
            self.assertLess(x["quote"],100)

    def test_43_no_authoritative_win_does_not_finish_queued_parts(self):
        s=[attempt("one",False,(part("a","weapon"),)),
           attempt("two",False),attempt("three",False)]
        x=run_plan(plan(s))
        self.assertEqual(x["restored"],0)
        self.assertEqual(x["reserved_final"],1)

    def test_44_earned_currency_not_retroactively_repricing_reserved_quotations(self):
        s=[attempt("one",False,(part("a"),part("b")),earned=500),
           attempt("two",True)]
        x=run_plan(plan(s,cash=70))
        self.assertEqual(x["charged_scrap"],70)
        self.assertEqual(x["restored"],2)

    def test_45_comparison_is_observation_not_real_gameplay(self):
        x=synthetic_batch(episodes=1)
        self.assertIn("NOT a real human/Unity gameplay experiment",x["notice"])


if __name__ == "__main__":
    unittest.main()
