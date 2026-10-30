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
        # The final victory's new damage must wait for a NEXT victory.
        self.assertEqual((a["restored"], b["restored"]), (22, 22))
        self.assertEqual((a["reserved_final"], b["reserved_final"]), (2, 2))

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
        self.assertEqual((x["free"],x["paid"]), (2,0))

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
        self.assertEqual((x["free"],x["paid"],x["restored"]), (1,0,0))
        self.assertEqual(x["reserved_final"],1)

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
        self.assertEqual((x["free"],x["restored"]), (2,1))
        self.assertEqual(x["reserved_final"],1)

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


    def test_46_new_damage_on_victory_is_not_healed_by_its_own_win(self):
        x=run_plan(plan([attempt("v1",True,(part("wheel","mobility"),))]))
        self.assertEqual((x["wins"],x["restored"],x["reserved_final"]), (1,0,1))
        self.assertEqual(x["observations"][0]["unavailable_mobility"],1)

    def test_47_old_reservation_can_complete_on_a_later_victory(self):
        x=run_plan(plan([attempt("loss",False,(part("wheel","mobility"),)),
                         attempt("win",True)]))
        self.assertEqual((x["restored"],x["destroyed_final"]), (1,0))

    def test_48_old_part_can_be_destroyed_again_after_later_win(self):
        x=run_plan(plan([attempt("v1",True,(part("wheel","mobility"),)),
                         attempt("v2",True,(part("wheel","mobility"),))]))
        self.assertEqual((x["wins"],x["restored"],x["reserved_final"]), (2,1,1))

    def test_49_first_win_unlocks_first_coupon_when_bootstrap_false(self):
        x=run_plan(plan([attempt("first",True,(part("a"),part("b")))]),
                   bootstrap=False)
        self.assertEqual((x["free"],x["paid"],x["restored"]), (1,1,0))

    def test_50_no_bootstrap_loss_before_win_pays_then_wins_new_coupon(self):
        x=run_plan(plan([attempt("loss",False,(part("a"),)),
                         attempt("v1",True,(part("b"),))]),bootstrap=False)
        self.assertEqual((x["free"],x["paid"],x["restored"],
                          x["reserved_final"]), (1,1,1,1))

    def test_51_essential_deploy_flag_requires_boolean(self):
        p=plan([attempt("loss",False,({"id":"core","role":"other",
                                       "blocks_deploy":1},))])
        with self.assertRaises(InvalidScenario):
            validate(p)

    def test_52_essential_loss_blocks_scripted_win_under_gate(self):
        p=plan([attempt("loss",False,({"id":"wheel","role":"mobility",
                                       "blocks_deploy":True},)),
                attempt("scripted_win",True,earned=100)])
        x=run_plan(p,enforce_deploy_gate=True)
        self.assertEqual((x["wins"],x["restored"],x["blocked_deploy_attempts"],
                          x["contradicted_scripted_wins"]), (0,0,1,1))
        self.assertEqual(x["observations"][1]["blocked_reason"],
                         "ESSENTIAL_0HP_PREVENTS_DEPLOY")

    def test_53_blocked_attempt_cannot_collect_reward(self):
        p=plan([attempt("loss",False,({"id":"gun","role":"weapon",
                                       "blocks_deploy":True},)),
                attempt("scripted",True,earned=900)],cash=0)
        x=run_plan(p,enforce_deploy_gate=True)
        self.assertEqual((x["wallet_final"],x["earned_scrap"]), (0,0))

    def test_54_nonessential_mobility_loss_is_not_proven_softlock(self):
        p=plan([attempt("loss",False,(part("wheel","mobility"),)),
                attempt("win",True)])
        x=run_plan(p,enforce_deploy_gate=True)
        self.assertEqual((x["wins"],x["blocked_deploy_attempts"],
                          x["restored"]), (1,0,1))

    def test_55_essential_reserved_0hp_still_blocks(self):
        p=plan([attempt("loss",False,({"id":"wheel","role":"mobility",
                                       "blocks_deploy":True},)),
                attempt("win",True)])
        x=run_plan(p,enforce_deploy_gate=True)
        self.assertEqual(x["blocked_deploy_attempts"],1)
        self.assertEqual(x["observations"][0]["reserved_essential"],1)

    def test_56_one_unapproved_hypothetical_loaner_can_break_deadlock(self):
        p=plan([attempt("loss",False,({"id":"wheel","role":"mobility",
                                       "blocks_deploy":True},)),
                attempt("win",True)])
        x=run_plan(p,enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["hypothetical_loaner_used"],x["wins"],
                          x["restored"],x["destroyed_final"]), (1,1,1,0))
        self.assertTrue(x["observations"][1]["used_temporary_loaner"])

    def test_57_one_loaner_does_not_guarantee_future_win(self):
        p=plan([attempt("loss0",False,({"id":"wheel","role":"mobility",
                                        "blocks_deploy":True},)),
                attempt("loaner_loses",False),
                attempt("scripted_win",True)])
        x=run_plan(p,enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["hypothetical_loaner_used"],x["wins"],
                          x["blocked_deploy_attempts"],x["restored"]), (1,0,1,0))

    def test_58_successful_encounter_newly_breaking_essential_blocks_next(self):
        p=plan([attempt("victory",True,({"id":"wheel","role":"mobility",
                                        "blocks_deploy":True},)),
                attempt("next_victory",True)])
        x=run_plan(p,enforce_deploy_gate=True)
        self.assertEqual((x["wins"],x["blocked_deploy_attempts"],
                          x["reserved_final"]), (1,1,1))

    def test_59_blocked_no_new_damage_or_phantom_quota(self):
        p=plan([attempt("loss",False,({"id":"wheel","role":"mobility",
                                       "blocks_deploy":True},)),
                attempt("blocked",True,(part("new"),),earned=1000)])
        x=run_plan(p,enforce_deploy_gate=True,cadence="per_run")
        self.assertEqual((x["destroyed_final"],x["paid"],x["free"]), (1,0,1))
        self.assertEqual(x["earned_scrap"],0)

    def test_60_gate_parameter_requires_boolean(self):
        with self.assertRaises(InvalidScenario):
            run_plan(plan([attempt("v",True)]),enforce_deploy_gate=1)

    def test_61_loaner_budget_rejects_true_negative_and_multiple(self):
        p=plan([attempt("v",True)])
        for v in (True,-1,2,1.5,"1"):
            with self.subTest(v=v):
                with self.assertRaises(InvalidScenario):
                    run_plan(p,enforce_deploy_gate=True,loaner_challenges=v)

    def test_62_loaner_without_gate_rejected(self):
        with self.assertRaises(InvalidScenario):
            run_plan(plan([attempt("v",True)]),loaner_challenges=1)

    def test_63_essential_is_cleared_only_after_later_win(self):
        p=plan([attempt("loss",False,({"id":"wheel","role":"mobility",
                                       "blocks_deploy":True},)),
                attempt("win",True),
                attempt("next",False)])
        x=run_plan(p,enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["wins"],x["blocked_deploy_attempts"],
                          x["hypothetical_loaner_used"]), (1,0,1))
        self.assertEqual(x["observations"][2]["unavailable_essential"],0)

    def test_64_critical_essential_beats_noncritical_mobility_when_cash_zero(self):
        p=plan([attempt("loss",False,(part("wheel","mobility"),
                                       {"id":"gun","role":"weapon",
                                        "blocks_deploy":True}))],cash=0)
        x=run_plan(p)
        self.assertEqual((x["free"],x["denied"]), (1,1))
        self.assertEqual((x["observations"][0]["reserved_essential"],
                          x["observations"][0]["unreserved_essential"]), (1,0))

    def test_65_zero_scrap_essential_weapon_requires_loaner_option(self):
        p=plan([attempt("lost",False,({"id":"gun","role":"weapon",
                                      "blocks_deploy":True},)),
                attempt("win",True)],cash=0)
        hold=run_plan(p,enforce_deploy_gate=True)
        alt=run_plan(p,enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((hold["wins"],alt["wins"]), (0,1))

    def test_66_blocked_win_does_not_generate_free_coupon(self):
        p=plan([attempt("lost",False,({"id":"wheel","role":"mobility",
                                      "blocks_deploy":True},)),
                attempt("blocked",True)])
        x=run_plan(p,enforce_deploy_gate=True,bootstrap=False)
        self.assertEqual((x["wins"],x["free"],x["paid"]), (0,0,1))

    def test_67_loaner_can_be_used_for_one_loss_not_rearmed_by_retry(self):
        p=plan([attempt("loss",False,({"id":"drive","role":"mobility",
                                       "blocks_deploy":True},)),
                attempt("loaner_fail",False),
                attempt("retry_one",False),
                attempt("retry_two",True)])
        x=run_plan(p,enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["hypothetical_loaner_used"],
                          x["blocked_deploy_attempts"],
                          x["contradicted_scripted_wins"]), (1,2,1))

    def test_68_scripted_optimism_is_exposed_not_reported_as_genuine_win(self):
        p=plan([attempt("bad",False,({"id":"wheel","role":"mobility",
                                      "blocks_deploy":True},)),
                attempt("unreachable",True)])
        a=run_plan(p)
        b=run_plan(p,enforce_deploy_gate=True)
        self.assertEqual((a["wins"],b["wins"],b["contradicted_scripted_wins"]),
                         (1,0,1))

    def test_69_default_model_preserves_baseline_12win_cost(self):
        s=[attempt(f"w{i}",True,(part(f"a{i}"),part(f"b{i}"))) for i in range(12)]
        x=compare(plan(s,cash=4000))
        self.assertEqual(x["per_win"]["charged_scrap"],840)
        self.assertEqual(x["per_run"]["charged_scrap"],1610)

    def test_70_simulated_loaner_never_instant_heals_0hp_after_loss(self):
        p=plan([attempt("loss",False,({"id":"wheel","role":"mobility",
                                       "blocks_deploy":True},)),
                attempt("loaner_losses",False)])
        x=run_plan(p,enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["destroyed_final"],x["restored"]), (1,0))
        self.assertEqual(x["observations"][-1]["unavailable_essential"],1)


if __name__ == "__main__":
    unittest.main()
