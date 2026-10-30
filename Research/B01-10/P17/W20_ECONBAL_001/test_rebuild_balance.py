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

    def test_26_quote_rounding_to_zero_allowed_for_discounted_paid_order(self):
        x=validate(plan([attempt("win")],price=2,percent=1))
        self.assertEqual(x["quote"],0)

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


    def test_71_core_destroyed_after_loss_blocks_all_later_wins(self):
        s=[dict(attempt("loss",False,(part("wheel","mobility"),)),
                core_alive_after=False),attempt("next",True,earned=100)]
        x=run_plan(plan(s))
        self.assertEqual((x["wins"],x["blocked_core_attempts"],
                          x["contradicted_scripted_wins"],x["earned_scrap"]),
                         (0,1,1,0))
        self.assertEqual(x["end_feasibility"],"RUN_CORE_DESTROYED_NO_LOANER")

    def test_72_core_destroyed_cannot_take_even_one_loaner(self):
        s=[dict(attempt("loss",False,({"id":"wheel","role":"mobility",
                                        "blocks_deploy":True},)),
                core_alive_after=False),
           attempt("next",True)]
        x=run_plan(plan(s),enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["hypothetical_loaner_used"],x["blocked_core_attempts"],
                          x["wins"]), (0,1,0))

    def test_73_no_challenge_after_win_blocks_later_winner(self):
        s=[dict(attempt("victory",True,(part("gun","weapon"),)),
                next_challenge_available_after=False),
           attempt("impossible",True,earned=99)]
        x=run_plan(plan(s))
        self.assertEqual((x["wins"],x["restored"],
                          x["blocked_no_challenge_attempts"]), (1,0,1))
        self.assertEqual(x["end_feasibility"],"NO_ELIGIBLE_NEXT_CHALLENGE")

    def test_74_one_loaner_never_creates_nonexistent_challenge(self):
        s=[dict(attempt("loss",False,({"id":"gun","role":"weapon",
                                        "blocks_deploy":True},)),
                next_challenge_available_after=False),
           attempt("fake_win",True)]
        x=run_plan(plan(s),enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["hypothetical_loaner_used"],
                          x["blocked_no_challenge_attempts"],
                          x["contradicted_scripted_wins"]), (0,1,1))

    def test_75_core_reason_has_priority_over_no_challenge(self):
        s=[dict(attempt("bad",False),core_alive_after=False,
                next_challenge_available_after=False), attempt("future",True)]
        x=run_plan(plan(s))
        self.assertEqual((x["blocked_core_attempts"],
                          x["blocked_no_challenge_attempts"]), (1,0))
        self.assertEqual(x["observations"][-1]["blocked_reason"],"RUN_CORE_DESTROYED")

    def test_76_malformed_core_flag_fails_preflight(self):
        s=[dict(attempt("victory",True),core_alive_after=1)]
        with self.assertRaises(InvalidScenario):
            validate(plan(s))

    def test_77_malformed_challenge_flag_fails_preflight(self):
        s=[dict(attempt("victory",True),next_challenge_available_after="false")]
        with self.assertRaises(InvalidScenario):
            validate(plan(s))

    def test_78_both_missing_end_flags_default_to_true(self):
        x=run_plan(plan([attempt("v",True)]))
        self.assertTrue(x["end_core_alive"] and x["end_next_challenge_available"])
        self.assertEqual(x["end_feasibility"],
                         "NORMAL_DEPLOY_ELIGIBLE_IN_MODEL_NOT_ACTUAL_WIN")

    def test_79_valid_explicit_true_flags_retain_baseline(self):
        a=run_plan(plan([attempt("v",True,(part("one"),))]))
        b=run_plan(plan([dict(attempt("v",True,(part("one"),)),
                              core_alive_after=True,
                              next_challenge_available_after=True)]))
        self.assertEqual(a,b)

    def test_80_core_destroyed_after_current_win_does_not_erase_that_win(self):
        s=[dict(attempt("last_win",True,earned=50),core_alive_after=False)]
        x=run_plan(plan(s),cadence="per_win")
        self.assertEqual((x["wins"],x["earned_scrap"],x["wallet_final"]),
                         (1,50,1050))
        self.assertFalse(x["end_core_alive"])

    def test_81_after_core_loss_no_extra_damage_from_phantom_attempt(self):
        s=[dict(attempt("loss",False),core_alive_after=False),
           attempt("unreachable",False,(part("spurious"),))]
        x=run_plan(plan(s))
        self.assertEqual((x["destroyed_final"],x["denied"],
                          x["blocked_deploy_attempts"]), (0,0,1))

    def test_82_after_no_challenge_no_phantom_victory_credit(self):
        s=[dict(attempt("last",True),next_challenge_available_after=False),
           attempt("phantom",True,(part("wheel","mobility"),),earned=100)]
        x=run_plan(plan(s),cadence="per_win")
        self.assertEqual((x["wins"],x["free"],x["earned_scrap"],
                          x["destroyed_final"]), (1,0,0,0))

    def test_83_existing_repair_queue_is_not_healed_after_no_challenge(self):
        s=[dict(attempt("loss",False,(part("wheel","mobility"),)),
                next_challenge_available_after=False),
           attempt("phantom_win",True)]
        x=run_plan(plan(s))
        self.assertEqual((x["restored"],x["reserved_final"]), (0,1))

    def test_84_existing_repair_queue_not_healed_by_coreless_attempt(self):
        s=[dict(attempt("loss",False,(part("weapon","weapon"),)),
                core_alive_after=False),attempt("phantom_win",True)]
        x=run_plan(plan(s))
        self.assertEqual((x["restored"],x["reserved_final"]), (0,1))

    def test_85_end_essential_assessment_marks_unapproved_loaner_option(self):
        s=[attempt("loss",False,({"id":"wheel","role":"mobility",
                                  "blocks_deploy":True},))]
        x=run_plan(plan(s),enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual(x["end_feasibility"],
                         "ONE_UNAPPROVED_LOANER_COUNTERFACTUAL_AVAILABLE")
        self.assertEqual(x["end_unavailable_essential"],1)

    def test_86_no_loaner_essential_0hp_end_is_inaccessible_in_model(self):
        s=[attempt("loss",False,({"id":"drive","role":"mobility",
                                  "blocks_deploy":True},))]
        x=run_plan(plan(s),enforce_deploy_gate=True)
        self.assertEqual(x["end_feasibility"],
                         "ESSENTIAL_0HP_NEXT_WIN_INACCESSIBLE")

    def test_87_without_authoritative_deploy_check_mark_unverified_not_green(self):
        s=[attempt("loss",False,({"id":"gun","role":"weapon",
                                  "blocks_deploy":True},))]
        x=run_plan(plan(s))
        self.assertEqual(x["end_feasibility"],
                         "ESSENTIAL_0HP_HOST_DEPLOY_UNVERIFIED")

    def test_88_one_loaner_lost_exhausts_last_recovery_option(self):
        s=[attempt("loss",False,({"id":"gun","role":"weapon",
                                  "blocks_deploy":True},)),
           attempt("loaner_loss",False)]
        x=run_plan(plan(s),enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual(x["hypothetical_loaner_used"],1)
        self.assertEqual(x["end_feasibility"],
                         "ESSENTIAL_0HP_NEXT_WIN_INACCESSIBLE")

    def test_89_one_loaner_win_recovers_queue_and_unlocks_normal(self):
        s=[attempt("loss",False,({"id":"gun","role":"weapon",
                                  "blocks_deploy":True},)),
           attempt("borrowed_victory",True)]
        x=run_plan(plan(s),enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["wins"],x["restored"],x["destroyed_final"]),
                         (1,1,0))
        self.assertEqual(x["end_feasibility"],
                         "NORMAL_DEPLOY_ELIGIBLE_IN_MODEL_NOT_ACTUAL_WIN")

    def test_90_terminal_core_overrides_still_available_loaner(self):
        s=[dict(attempt("loss",False,({"id":"gun","role":"weapon",
                                      "blocks_deploy":True},)),
                core_alive_after=False)]
        x=run_plan(plan(s),enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual(x["end_feasibility"],"RUN_CORE_DESTROYED_NO_LOANER")
        self.assertEqual(x["hypothetical_loaner_used"],0)

    def test_91_missing_challenge_overrides_still_available_loaner(self):
        s=[dict(attempt("loss",False,({"id":"gun","role":"weapon",
                                      "blocks_deploy":True},)),
                next_challenge_available_after=False)]
        x=run_plan(plan(s),enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual(x["end_feasibility"],"NO_ELIGIBLE_NEXT_CHALLENGE")
        self.assertEqual(x["hypothetical_loaner_used"],0)

    def test_92_alternating_eight_wins_preserves_previous_money_comparison(self):
        s=[attempt(f"v{i}",True,(part(f"wheel{i}","mobility"),
                                  part(f"gun{i}","weapon"))) for i in range(8)]
        y=compare(plan(s,cash=3000))
        self.assertEqual((y["per_win"]["charged_scrap"],
                          y["per_run"]["charged_scrap"],
                          y["per_win"]["restored"]), (560,1050,14))

    def test_93_critical_gate_does_not_invent_actual_player_observation(self):
        x=run_plan(plan([attempt("w",True)]),enforce_deploy_gate=True)
        self.assertNotIn("verified_native",x)
        self.assertIn("_IN_MODEL_",x["end_feasibility"])

    def test_94_invalid_late_flag_rejects_entire_scenario(self):
        s=[attempt("w",True), dict(attempt("later",False),core_alive_after=None)]
        with self.assertRaises(InvalidScenario):
            run_plan(plan(s))

    def test_95_persisted_no_challenge_flag_remains_fail_closed(self):
        s=[dict(attempt("no_next",False),next_challenge_available_after=False),
           dict(attempt("forged_reenable",True),
                next_challenge_available_after=True)]
        x=run_plan(plan(s),enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["wins"],x["blocked_no_challenge_attempts"],
                          x["hypothetical_loaner_used"]), (0,1,0))


    def test_96_same_reward_different_order_changes_same_won_garage_affordability(self):
        ev=[attempt("win",True,(part("drive","mobility"),part("gun","weapon")),earned=70)]
        a=run_plan(plan(ev,cash=0),reward_timing="after_reservation")
        b=run_plan(plan(ev,cash=0),reward_timing="before_reservation")
        self.assertEqual((a["free"],a["paid"],a["denied"]), (1,0,1))
        self.assertEqual((b["free"],b["paid"],b["denied"]), (1,1,0))
        self.assertEqual((a["wallet_final"],b["wallet_final"]),(70,0))
        self.assertEqual((a["restored"],b["restored"]),(0,0))

    def test_97_reward_before_order_never_uses_money_from_blocked_attempt(self):
        ev=[dict(attempt("loss",False),core_alive_after=False),
            attempt("phantom",True,(part("w"),),earned=700)]
        b=run_plan(plan(ev,cash=0),reward_timing="before_reservation")
        self.assertEqual((b["earned_scrap"],b["wallet_final"],b["wins"]),(0,0,0))

    def test_98_reward_before_order_preserves_next_win_gate(self):
        ev=[attempt("v1",True,(part("a"),),earned=100)]
        b=run_plan(plan(ev,cash=0),reward_timing="before_reservation")
        self.assertEqual((b["restored"],b["reserved_final"],b["free"]),(0,1,1))

    def test_99_earned_currency_conservation_both_reward_orders(self):
        ev=[attempt("v1",True,(part("a"),part("b")),earned=70),
            attempt("v2",True,(part("c"),part("d")),earned=110)]
        for timing in ("before_reservation","after_reservation"):
            b=run_plan(plan(ev,cash=50),reward_timing=timing)
            self.assertEqual(b["wallet_final"],50+b["earned_scrap"]-b["charged_scrap"])

    def test_100_unsupported_reward_timing_fails_closed(self):
        with self.assertRaises(InvalidScenario):
            run_plan(plan([attempt("v",True)]),reward_timing="during_combat")

    def test_101_reward_timing_not_injected_into_event_input(self):
        ev=[dict(attempt("v",True),reward_timing="before_reservation")]
        b=run_plan(plan(ev,cash=0))
        self.assertEqual(b["reward_timing"],"after_reservation")

    def test_102_empty_reward_preserves_identical_monetary_result(self):
        ev=[attempt("v",True,(part("a"),part("b")))]
        before=run_plan(plan(ev,cash=140),reward_timing="before_reservation")
        after=run_plan(plan(ev,cash=140),reward_timing="after_reservation")
        self.assertEqual((before["charged_scrap"],before["wallet_final"]),
                         (after["charged_scrap"],after["wallet_final"]))

    def test_103_reward_order_does_not_forge_free_epoch(self):
        ev=[attempt("lost",False,(part("a"),part("b")),earned=70)]
        x=run_plan(plan(ev,cash=0),bootstrap=False,
                   reward_timing="before_reservation")
        self.assertEqual((x["free"],x["paid"],x["wins"]),(0,1,0))

    def test_104_terminal_no_challenge_reports_actual_modeled_paid_exposure(self):
        ev=[dict(attempt("final_win",True,(part("a"),part("b"))),
                 next_challenge_available_after=False)]
        x=run_plan(plan(ev,cash=70))
        self.assertEqual((x["modeled_stranded_pending_count"],
                          x["modeled_stranded_paid_scrap"],
                          x["modeled_stranded_free_tickets"]),(2,70,1))

    def test_105_terminal_destroyed_core_reports_actual_modeled_paid_exposure(self):
        ev=[dict(attempt("terminal_loss",False,(part("a"),part("b"))),
                 core_alive_after=False)]
        x=run_plan(plan(ev,cash=70))
        self.assertEqual((x["modeled_stranded_pending_count"],
                          x["modeled_stranded_paid_scrap"]),(2,70))

    def test_106_essential_zero_hp_without_loaner_reports_stranded_paid(self):
        ev=[attempt("lost",False,(
            {"id":"gun","role":"weapon","blocks_deploy":True},
            part("other")))]
        x=run_plan(plan(ev,cash=70),enforce_deploy_gate=True)
        self.assertEqual((x["modeled_stranded_pending_count"],
                          x["modeled_stranded_paid_scrap"]),(2,70))

    def test_107_hypothetical_loaner_available_is_not_assumed_irrecoverable(self):
        ev=[attempt("lost",False,(
            {"id":"gun","role":"weapon","blocks_deploy":True},
            part("other")))]
        x=run_plan(plan(ev,cash=70),enforce_deploy_gate=True,
                   loaner_challenges=1)
        self.assertEqual(x["modeled_stranded_paid_scrap"],0)
        self.assertEqual(x["end_feasibility"],
                         "ONE_UNAPPROVED_LOANER_COUNTERFACTUAL_AVAILABLE")

    def test_108_loaner_defeat_makes_remaining_paid_reservation_stranded(self):
        ev=[attempt("lost",False,(
            {"id":"gun","role":"weapon","blocks_deploy":True},
            part("other"))),attempt("loaner_defeat",False)]
        x=run_plan(plan(ev,cash=70),enforce_deploy_gate=True,
                   loaner_challenges=1)
        self.assertEqual((x["hypothetical_loaner_used"],
                          x["modeled_stranded_paid_scrap"]),(1,70))

    def test_109_non_terminal_pending_has_zero_stranded_risk_indicator(self):
        ev=[attempt("loss",False,(part("a"),part("b")))]
        x=run_plan(plan(ev,cash=70))
        self.assertEqual((x["reserved_final"],x["modeled_stranded_paid_scrap"]),
                         (2,0))

    def test_110_unreserved_destroyed_items_are_not_counted_as_paid_tickets(self):
        ev=[dict(attempt("final",False,(part("a"),part("b"),part("c"))),
                 next_challenge_available_after=False)]
        x=run_plan(plan(ev,cash=0))
        self.assertEqual((x["modeled_stranded_pending_count"],
                          x["modeled_stranded_paid_scrap"],
                          x["denied"]),(1,0,2))

    def test_111_stranded_paid_only_charges_accepted_reservation(self):
        ev=[dict(attempt("final",False,(part("a"),part("b"),part("c"))),
                 next_challenge_available_after=False)]
        x=run_plan(plan(ev,cash=70))
        self.assertEqual((x["modeled_stranded_pending_count"],
                          x["modeled_stranded_paid_scrap"],x["denied"]),
                         (2,70,1))

    def test_112_last_victory_repairs_old_tickets_not_new_stranded_tickets(self):
        ev=[attempt("before",False,(part("old"),)),
            dict(attempt("last_win",True,(part("new1"),part("new2"))),
                 next_challenge_available_after=False)]
        x=run_plan(plan(ev,cash=140))
        self.assertEqual((x["restored"],x["modeled_stranded_pending_count"],
                          x["modeled_stranded_paid_scrap"]),(1,2,70))

    def test_113_wallet_paid_stranded_exposure_does_not_issue_refund(self):
        ev=[dict(attempt("final",True,(part("a"),part("b"))),
                 next_challenge_available_after=False)]
        x=run_plan(plan(ev,cash=70))
        self.assertEqual((x["wallet_final"],x["modeled_stranded_paid_scrap"]),(0,70))
        self.assertNotIn("refund_issued",x)

    def test_114_hypothetical_cost_grid_65_70_75_remains_discounted(self):
        ev=[attempt("w",True,(part("a"),part("b")))]
        for pct in (65,70,75):
            with self.subTest(pct=pct):
                x=run_plan(plan(ev,cash=1000,percent=pct))
                self.assertEqual(x["charged_scrap"],pct)

    def test_115_fixed_seed_synthetic_before_reward_is_reproducible(self):
        from rebuild_balance import synthetic_batch
        a=synthetic_batch(seed=98,episodes=3,
                          reward_timing="before_reservation")
        b=synthetic_batch(seed=98,episodes=3,
                          reward_timing="before_reservation")
        self.assertEqual(a,b)
        self.assertEqual(a["reward_timing"],"before_reservation")

    def test_116_invalid_synthetic_reward_order_is_rejected(self):
        from rebuild_balance import synthetic_batch
        with self.assertRaises(InvalidScenario):
            synthetic_batch(episodes=1,reward_timing="invalid")

    def test_117_reward_before_reservation_cannot_bypass_dead_core(self):
        ev=[dict(attempt("terminal",False),core_alive_after=False),
            attempt("fake_winner",True,earned=1000)]
        x=run_plan(plan(ev,cash=0),reward_timing="before_reservation",
                   enforce_deploy_gate=True,loaner_challenges=1)
        self.assertEqual((x["wins"],x["earned_scrap"],
                          x["hypothetical_loaner_used"]),(0,0,0))

    def test_118_reward_before_reservation_cannot_bypass_no_challenge(self):
        ev=[dict(attempt("last",True),next_challenge_available_after=False),
            attempt("fake_winner",True,earned=1000)]
        x=run_plan(plan(ev,cash=0),reward_timing="before_reservation")
        self.assertEqual((x["wins"],x["earned_scrap"]),(1,0))

    def test_119_after_reward_baseline_840_1610_not_regressed(self):
        ev=[attempt(f"v{i}",True,(part(f"a{i}"),part(f"b{i}")))
            for i in range(12)]
        x=compare(plan(ev,cash=4000),reward_timing="after_reservation")
        self.assertEqual((x["per_win"]["charged_scrap"],
                          x["per_run"]["charged_scrap"]),(840,1610))

    def test_120_reward_before_order_changes_delayed_ticket_count_not_win_count(self):
        ev=[attempt("v",True,(part("a"),part("b")),earned=70)]
        after=run_plan(plan(ev,cash=0))
        before=run_plan(plan(ev,cash=0),
                        reward_timing="before_reservation")
        self.assertEqual((after["wins"],before["wins"]), (1,1))
        self.assertEqual((after["reserved_final"],
                          before["reserved_final"]),(1,2))
        self.assertEqual((after["restored"],before["restored"]),(0,0))


    def test_121_owner_approved_exact_65_quote(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True,(part("a"),part("b")))]))
        self.assertEqual((x["quote"],x["charged_scrap"],x["free"],x["paid"]),
                         (65,65,1,1))
        self.assertEqual((x["cadence"],x["bootstrap"]),("per_win",False))

    def test_122_owner_approved_12wins_22_repaired_and_2_pending_780scrap(self):
        from rebuild_balance import run_approved_plan
        s=[attempt(f"v{i}",True,(part(f"a{i}"),part(f"b{i}")))
           for i in range(12)]
        x=run_approved_plan(plan(s,cash=4000))
        self.assertEqual((x["wins"],x["free"],x["paid"],x["charged_scrap"],
                          x["restored"],x["reserved_final"]),(12,12,12,780,22,2))

    def test_123_owner_approved_no_previctory_free_quota(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("loss",False,(part("a"),))],cash=65))
        self.assertEqual((x["wins"],x["free"],x["paid"],x["charged_scrap"]),
                         (0,0,1,65))

    def test_124_owner_approved_first_winner_gives_one_free(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("first",True,(part("a"),part("b")))],
                                 cash=65))
        self.assertEqual((x["free"],x["paid"],x["charged_scrap"]),(1,1,65))

    def test_125_owner_approved_does_not_stockpile_unused_wins(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("w1",True),attempt("w2",True),
                                  attempt("w3",True,(part("a"),part("b")))],
                                 cash=65))
        self.assertEqual((x["wins"],x["free"],x["paid"]),(3,1,1))

    def test_126_owner_approved_price_one_paid_zero_not_free(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True,(part("a"),part("b")))],
                                 price=1,cash=0))
        self.assertEqual((x["quote"],x["free"],x["paid"],x["denied"],
                          x["charged_scrap"]),(0,1,1,0,0))

    def test_127_owner_approved_price_two_floor_one(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True,(part("a"),part("b")))],
                                 price=2,cash=1))
        self.assertEqual((x["quote"],x["charged_scrap"]), (1,1))

    def test_128_owner_approved_price_three_floor_one(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True,(part("a"),part("b")))],
                                 price=3,cash=1))
        self.assertEqual((x["quote"],x["charged_scrap"]), (1,1))

    def test_129_zero_paid_ticket_never_mislabeled_free_when_terminal(self):
        from rebuild_balance import run_approved_plan
        s=[dict(attempt("last",True,(part("a"),part("b"))),
                next_challenge_available_after=False)]
        x=run_approved_plan(plan(s,price=1,cash=0))
        self.assertEqual((x["free"],x["paid"],x["modeled_stranded_free_tickets"],
                          x["modeled_stranded_pending_count"]),(1,1,1,2))

    def test_130_zero_paid_ticket_previctory_not_double_free(self):
        from rebuild_balance import run_approved_plan
        s=[attempt("loss",False,(part("a"),part("b")))]
        x=run_approved_plan(plan(s,price=1,cash=0))
        self.assertEqual((x["free"],x["paid"],x["wins"],
                          x["charged_scrap"]),(0,2,0,0))

    def test_131_approved_quoted_cost_always_less_than_positive_purchase(self):
        from rebuild_balance import run_approved_plan
        for price in (1,2,3,7,10,39,99,100,999,1000000000):
            with self.subTest(price=price):
                x=run_approved_plan(plan([attempt("v",True)],price=price))
                self.assertGreaterEqual(x["quote"],0)
                self.assertLess(x["quote"],price)
                self.assertEqual(x["quote"],
                                 price*65//100)

    def test_132_approved_duplicate_win_id_fails_preflight(self):
        from rebuild_balance import run_approved_plan
        with self.assertRaises(InvalidScenario):
            run_approved_plan(plan([attempt("same",True),attempt("same",True)]))

    def test_133_approved_lost_battle_never_grants_victory_credit(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("loss",False,(part("wheel"),))],
                                 cash=0))
        self.assertEqual((x["wins"],x["free"],x["denied"]),(0,0,1))

    def test_134_approved_broken_nonessential_part_does_not_block(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([
            attempt("loss",False,(part("visual","other"),)),
            attempt("victory",True)],cash=0))
        self.assertEqual((x["wins"],x["blocked_deploy_attempts"]),(1,0))

    def test_135_approved_loaner_win_with_zero_cash_exposes_blocker(self):
        from rebuild_balance import run_approved_plan
        s=[attempt("lost",False,({"id":"gun","role":"weapon",
                                 "blocks_deploy":True},)),
           attempt("loaner_win",True)]
        x=run_approved_plan(plan(s,cash=0))
        # REQUIRED OWNER FIX: cannot repair non-reserved 0HP from the loaner
        # victory itself; the new quota only reserves a future rebuild, but
        # the sole loaner is exhausted and CanDeploy is still blocked.
        self.assertEqual((x["hypothetical_loaner_used"],x["wins"],
                          x["restored"],x["destroyed_final"]),(1,1,0,1))
        self.assertEqual(x["end_feasibility"],
                         "ESSENTIAL_0HP_NEXT_WIN_INACCESSIBLE")
        self.assertEqual(x["reserved_final"],1)
        self.assertTrue(x["p0_post_loaner_win_deferred_rebuild_deadlock"])
        self.assertIn("OWNER_B01_22",x["p0_recovery_contract"])

    def test_136_approved_loaner_loss_exhausts_lease_per_run(self):
        from rebuild_balance import run_approved_plan
        s=[attempt("lost",False,({"id":"gun","role":"weapon",
                                 "blocks_deploy":True},)),
           attempt("borrowed_lost",False),attempt("another_win",True)]
        x=run_approved_plan(plan(s,cash=0))
        self.assertEqual((x["hypothetical_loaner_used"],x["wins"],
                          x["blocked_deploy_attempts"]),(1,0,1))

    def test_137_approved_dead_core_forbids_loaner_and_fake_win(self):
        from rebuild_balance import run_approved_plan
        s=[dict(attempt("lost",False,({"id":"gun","role":"weapon",
                                      "blocks_deploy":True},)),
                core_alive_after=False),attempt("fake_win",True,earned=500)]
        x=run_approved_plan(plan(s,cash=0))
        self.assertEqual((x["wins"],x["hypothetical_loaner_used"],
                          x["earned_scrap"]),(0,0,0))

    def test_138_approved_no_next_challenge_forbids_loaner(self):
        from rebuild_balance import run_approved_plan
        s=[dict(attempt("won",True,({"id":"gun","role":"weapon",
                                    "blocks_deploy":True},)),
                next_challenge_available_after=False),
           attempt("fake_challenge",True)]
        x=run_approved_plan(plan(s,cash=0))
        self.assertEqual((x["wins"],x["hypothetical_loaner_used"],
                          x["blocked_no_challenge_attempts"]),(1,0,1))

    def test_139_approved_same_win_cannot_repair_broken_new_part(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True,(part("a"),))],cash=0))
        self.assertEqual((x["wins"],x["restored"],x["reserved_final"]),(1,0,1))

    def test_140_approved_override_70_and_75_historical_quotes(self):
        from rebuild_balance import run_approved_plan
        for pct in (1,10,70,75,99):
            x=run_approved_plan(plan([attempt("v",True,(part("a"),part("b")))],
                                     cash=65,percent=pct))
            self.assertEqual((x["quote"],x["charged_scrap"]),(65,65))
            self.assertEqual(x["source_quote_overridden_from"],pct)

    def test_141_approved_does_not_mutate_caller_plan(self):
        from rebuild_balance import run_approved_plan
        p=plan([attempt("v",True,(part("a"),part("b")))],percent=70)
        before=json.dumps(p,sort_keys=True)
        run_approved_plan(p)
        self.assertEqual(before,json.dumps(p,sort_keys=True))

    def test_142_approved_model_does_not_claim_trusted_game_event(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True)]))
        self.assertEqual(x["actual_trusted_victory_attestation"],
                         "NOT_AVAILABLE_MODEL_ONLY")
        self.assertEqual(x["approved_loaner_weaker_asset"],
                         "DESIGN_CONTRACT_ONLY_NOT_SPAWNED")
        self.assertFalse(x.get("game_integrated",False))

    def test_143_owner_has_not_set_native_reward_settlement_order(self):
        from rebuild_balance import run_approved_plan
        s=[attempt("v",True,(part("a"),part("b")),earned=65)]
        p=plan(s,cash=0)
        late=run_approved_plan(p,reward_timing="after_reservation")
        early=run_approved_plan(p,reward_timing="before_reservation")
        self.assertEqual((late["free"],late["paid"],late["denied"]),(1,0,1))
        self.assertEqual((early["free"],early["paid"],early["denied"]),(1,1,0))
        self.assertEqual(late["live_reward_settlement"],
                         "OWNER_IMPLEMENTATION_UNVERIFIED")

    def test_144_invalid_zero_purchase_price_rejected(self):
        from rebuild_balance import run_approved_plan
        with self.assertRaises(InvalidScenario):
            run_approved_plan(plan([attempt("v",True)],price=0))

    def test_145_explicit_original_price_one_even_with_75pct_works(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True,(part("a"),part("b")))],
                                 price=1,percent=75,cash=0))
        self.assertEqual((x["quote"],x["paid"],x["free"]),(0,1,1))


    def test_146_v7_floor_5_15_30_100_parity(self):
        from rebuild_balance import run_approved_plan
        for purchase,expected in [(5,3),(15,9),(30,19),(100,65)]:
            with self.subTest(purchase=purchase):
                x=run_approved_plan(plan([attempt("win",True,
                     (part("a"),part("b")))],cash=1000,price=purchase))
                self.assertEqual((x["quote"],x["charged_scrap"]), (expected,expected))

    def test_147_owner_requirement_price_one_must_clamp_to_zero(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("win",True,
              (part("a"),part("b")))],cash=0,price=1))
        self.assertEqual((x["quote"],x["free"],x["paid"]),(0,1,1))

    def test_148_rounding_floor_old_15_is_nine_not_ten(self):
        x=run_plan(plan([attempt("v",True,(part("a"),part("b")))],
                         cash=1000,price=15,percent=65))
        self.assertEqual(x["quote_rounding"],"floor")
        self.assertEqual(x["quote"],9)

    def test_149_approved_now_uses_owner_v7_floor(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True,(part("a"),part("b")))],
                                     cash=100,price=15))
        self.assertEqual(x["quote_rounding"],"floor")
        self.assertEqual(x["quote"],9)
        self.assertIn("V7_PR780",x["quote_authority_reference"])

    def test_150_invalid_unknown_rounding_rejected(self):
        p=plan([attempt("v",True)])
        p["quote_rounding"]="ceil_no_clamp"
        with self.assertRaises(InvalidScenario):
            run_plan(p)

    def test_151_invalid_non_string_rounding_rejected(self):
        p=plan([attempt("v",True)])
        p["quote_rounding"]=True
        with self.assertRaises(InvalidScenario):
            run_plan(p)

    def test_152_price_one_approved_normal_and_reward_first_zero_paid(self):
        from rebuild_balance import run_approved_plan
        p=plan([attempt("v",True,(part("a"),part("b")),earned=5)],
               cash=0,price=1)
        for timing in ("after_reservation","before_reservation"):
            with self.subTest(timing=timing):
                x=run_approved_plan(p,reward_timing=timing)
                self.assertEqual((x["free"],x["paid"],x["charged_scrap"]),(1,1,0))

    def test_153_rounding_65_only_not_70_history(self):
        from rebuild_balance import run_approved_plan
        for pct in (1,65,70,75):
            p=plan([attempt("v",True,(part("a"),part("b")))],
                   cash=1000,price=15,percent=pct)
            x=run_approved_plan(p)
            self.assertEqual(x["quote"],9)

    def test_154_quote_one_scarp_five_consumes_paid_order_not_free(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v",True,
                    (part("a"),part("b"),part("c")))],cash=6,price=5))
        self.assertEqual((x["free"],x["paid"],x["charged_scrap"]),(1,2,6))

    def test_155_owner_approved_strictly_cheaper_full_quote_range(self):
        from rebuild_balance import run_approved_plan
        for price in list(range(1,150)) + [999,100000,1000000000]:
            x=run_approved_plan(plan([attempt("v")],price=price))
            self.assertLess(x["quote"],price)
            self.assertGreaterEqual(x["quote"],0)

    def test_156_approved_price_max_is_safe_from_overflow(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v")],price=1000000000))
        self.assertEqual(x["quote"],650000000)
        self.assertLess(x["quote"],1000000000)

    def test_157_price_one_zero_cost_paid_does_not_consume_free_win_quota(self):
        from rebuild_balance import run_approved_plan
        p=plan([attempt("loss",False,(part("a"),part("b"))),
                attempt("v",True,(part("c"),part("d")))],cash=0,price=1)
        x=run_approved_plan(p)
        self.assertEqual((x["wins"],x["free"],x["paid"],x["charged_scrap"]),
                         (1,1,3,0))
        self.assertEqual((x["restored"],x["reserved_final"]),(2,2))

    def test_158_newly_damaged_in_same_win_waits_for_next_distinct_win(self):
        from rebuild_balance import run_approved_plan
        p=plan([attempt("v1",True,(part("a"),part("b"))),
                attempt("v2",True,(part("c"),part("d")))],cash=100,price=15)
        x=run_approved_plan(p)
        self.assertEqual((x["free"],x["paid"],x["charged_scrap"]), (2,2,18))
        self.assertEqual((x["restored"],x["reserved_final"]),(2,2))

    def test_159_price_one_rebuild_satisfies_strict_less_even_below_one(self):
        from rebuild_balance import run_approved_plan
        x=run_approved_plan(plan([attempt("v")],price=1))
        self.assertEqual(x["quote"],0)
        self.assertNotIn("game_merged",x)

    def test_160_owner_v7_floor_is_single_accepted_design_quote(self):
        from rebuild_balance import run_approved_plan
        for price in (1,5,15,30,100):
            expected_floor=price*65//100
            x=run_approved_plan(plan([attempt("v")],price=price))
            self.assertEqual(x["quote"],expected_floor)
            self.assertLess(expected_floor,price)


if __name__ == "__main__":
    unittest.main()
