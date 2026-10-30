using System;
using Swarm.B01.Playtesting;
using Outcome = Swarm.B01.Playtesting.Ld02TechnicalTrial.Outcome;
using Variant = Swarm.B01.Playtesting.Ld02TechnicalTrial.Variant;
using Stage = Swarm.B01.Playtesting.Ld02TechnicalTrial.Stage;

internal static class Program
{
    private static int passed, failed;

    private static void Eq<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual))
            throw new Exception("expected " + expected + " actual " + actual);
    }
    private static void ShouldThrow<T>(Action test) where T : Exception
    {
        try { test(); }
        catch (T) { return; }
        throw new Exception("Expected exception: " + typeof(T).Name);
    }
    private static void Case(string name, Action test)
    {
        try { test(); Console.WriteLine("PASS " + name); ++passed; }
        catch (Exception e) { Console.Error.WriteLine("FAIL " + name + " : " + e.Message); ++failed; }
    }

    private static int Main()
    {
        Case("starts_at_first_task", () => Eq(0, new Ld02TechnicalTrial(0).CurrentStageIndex));
        Case("initially_not_finished", () => Eq(false, new Ld02TechnicalTrial(0).IsFinished));
        Case("initial_unmarked_count", () => Eq(0, new Ld02TechnicalTrial(0).MarkedCount));
        Case("first_stage_fight", () => Eq(Stage.Fight, new Ld02TechnicalTrial(0).GetStage(0)));
        Case("second_stage_garage", () => Eq(Stage.Garage, new Ld02TechnicalTrial(0).GetStage(1)));
        Case("third_stage_repair", () => Eq(Stage.Repair, new Ld02TechnicalTrial(0).GetStage(2)));
        Case("fourth_stage_checkpoint", () => Eq(Stage.Checkpoint, new Ld02TechnicalTrial(0).GetStage(3)));
        Case("all_objectives_nonempty", () =>
        {
            var x = new Ld02TechnicalTrial(0);
            for (int i=0; i<4; ++i) if (string.IsNullOrWhiteSpace(x.GetObjective(i))) throw new Exception("missing objective");
        });
        Case("all_assisted_hints_nonempty", () =>
        {
            var x = new Ld02TechnicalTrial(0);
            for (int i=0; i<4; ++i) if (string.IsNullOrWhiteSpace(x.GetAssistedHint(i))) throw new Exception("missing hint");
        });
        Case("neutral_trial0_stage0", () => Eq(Variant.Neutral, new Ld02TechnicalTrial(0).GetVariant(0)));
        Case("assisted_trial0_stage1", () => Eq(Variant.Assisted, new Ld02TechnicalTrial(0).GetVariant(1)));
        Case("assisted_trial1_stage0", () => Eq(Variant.Assisted, new Ld02TechnicalTrial(1).GetVariant(0)));
        Case("neutral_trial1_stage1", () => Eq(Variant.Neutral, new Ld02TechnicalTrial(1).GetVariant(1)));
        Case("balanced_each_stage_across_pair", () =>
        {
            var a=new Ld02TechnicalTrial(22);var b=new Ld02TechnicalTrial(23);
            for(int i=0;i<4;++i) if(a.GetVariant(i)==b.GetVariant(i))throw new Exception("not balanced");
        });
        Case("variant_period_two", () =>
        {
            var a=new Ld02TechnicalTrial(4);var b=new Ld02TechnicalTrial(6);
            for(int i=0;i<4;++i) Eq(a.GetVariant(i),b.GetVariant(i));
        });
        Case("outcomes_start_unmarked", () => Eq(Outcome.Unmarked, new Ld02TechnicalTrial(0).GetOutcome(0)));
        Case("pass_advances_to_garage", () =>
        {
            var x=new Ld02TechnicalTrial(0);x.MarkCurrent(Outcome.Passed);
            Eq(1,x.CurrentStageIndex);Eq(Outcome.Passed,x.GetOutcome(0));
        });
        Case("fail_advances_without_false_pass", () =>
        {
            var x=new Ld02TechnicalTrial(0);x.MarkCurrent(Outcome.Failed);
            Eq(1,x.CurrentStageIndex);Eq(0,x.Count(Outcome.Passed));Eq(1,x.Count(Outcome.Failed));
        });
        Case("skip_is_not_success", () =>
        {
            var x=new Ld02TechnicalTrial(0);x.MarkCurrent(Outcome.Skipped);
            Eq(0,x.Count(Outcome.Passed));Eq(1,x.Count(Outcome.Skipped));
        });
        Case("all_four_marked_complete", () =>
        {
            var x=new Ld02TechnicalTrial(0);
            for(int i=0;i<4;++i)x.MarkCurrent(Outcome.Passed);
            Eq(true,x.IsFinished);Eq(4,x.CurrentStageIndex);Eq(4,x.Count(Outcome.Passed));
        });
        Case("mixed_result_counts", () =>
        {
            var x=new Ld02TechnicalTrial(0);
            x.MarkCurrent(Outcome.Passed);x.MarkCurrent(Outcome.Failed);
            x.MarkCurrent(Outcome.Skipped);x.MarkCurrent(Outcome.Passed);
            Eq(2,x.Count(Outcome.Passed));Eq(1,x.Count(Outcome.Failed));Eq(1,x.Count(Outcome.Skipped));
        });
        Case("cannot_mark_ended_trial", () =>
        {
            var x=new Ld02TechnicalTrial(0);for(int i=0;i<4;++i)x.MarkCurrent(Outcome.Passed);
            ShouldThrow<InvalidOperationException>(()=>x.MarkCurrent(Outcome.Passed));
        });
        Case("unmarked_cannot_be_marked", () =>
        {
            var x=new Ld02TechnicalTrial(0);
            ShouldThrow<ArgumentOutOfRangeException>(()=>x.MarkCurrent(Outcome.Unmarked));
            Eq(0,x.CurrentStageIndex);
        });
        Case("invalid_enum_cannot_be_marked", () =>
        {
            var x=new Ld02TechnicalTrial(0);
            ShouldThrow<ArgumentOutOfRangeException>(()=>x.MarkCurrent((Outcome)999));
        });
        Case("invalid_negative_trial_rejected", () => ShouldThrow<ArgumentOutOfRangeException>(()=>new Ld02TechnicalTrial(-1)));
        Case("invalid_negative_stage_rejected", () => ShouldThrow<ArgumentOutOfRangeException>(()=>new Ld02TechnicalTrial(0).GetStage(-1)));
        Case("invalid_overflow_stage_rejected", () => ShouldThrow<ArgumentOutOfRangeException>(()=>new Ld02TechnicalTrial(0).GetVariant(4)));
        Case("invalid_objective_index_rejected", () => ShouldThrow<ArgumentOutOfRangeException>(()=>new Ld02TechnicalTrial(0).GetObjective(10)));
        Case("invalid_hint_index_rejected", () => ShouldThrow<ArgumentOutOfRangeException>(()=>new Ld02TechnicalTrial(0).GetAssistedHint(-5)));
        Case("cannot_count_unmarked", () => ShouldThrow<ArgumentOutOfRangeException>(()=>new Ld02TechnicalTrial(0).Count(Outcome.Unmarked)));
        Case("reset_clears_result_counts", () =>
        {
            var x=new Ld02TechnicalTrial(0);x.MarkCurrent(Outcome.Passed);x.Reset(0);
            Eq(0,x.CurrentStageIndex);Eq(0,x.Count(Outcome.Passed));Eq(Outcome.Unmarked,x.GetOutcome(0));
        });
        Case("reset_updates_counterbalance_variant", () =>
        {
            var x=new Ld02TechnicalTrial(0);x.Reset(1);
            Eq(1,x.TrialNumber);Eq(Variant.Assisted,x.GetVariant(0));
        });
        Case("reset_negative_rejected_preserves_old_state", () =>
        {
            var x=new Ld02TechnicalTrial(0);x.MarkCurrent(Outcome.Passed);
            ShouldThrow<ArgumentOutOfRangeException>(()=>x.Reset(-1));
            Eq(1,x.CurrentStageIndex);Eq(1,x.Count(Outcome.Passed));
        });
        Case("int_max_trial_deterministic", () =>
        {
            var x=new Ld02TechnicalTrial(int.MaxValue);
            Eq(Variant.Assisted,x.GetVariant(0));
            Eq(Variant.Neutral,x.GetVariant(1));
        });
        Case("overlay_type_compiles_with_mock_only", () =>
        {
            var t=typeof(Ld02TechnicalOverlay);
            Eq("Swarm.B01.Playtesting", t.Namespace);
        });
        Console.WriteLine("TOTAL " + passed + " PASS " + failed + " FAIL");
        return failed == 0 ? 0 : 1;
    }
}
