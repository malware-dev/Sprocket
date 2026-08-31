using Reactor.Editing;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Tests;

/// <summary>
/// A second set of words for a sequence, deliberately not the game's.
/// </summary>
/// <remarks>
/// The seam only proves something if the two implementations can disagree. If this said what the
/// Space Engineers host says, a test passing here would be evidence that core still holds the
/// English rather than that it has stopped.
/// <para>
/// So this is terse and machine-ish: ticks rather than minutes, an arrow rather than a guillemet,
/// and no sentences. A test reading these strings is reading the target's words, which is the point.
/// </para>
/// </remarks>
sealed class PaperSequenceText : ISequenceText
{
    public string Sequence(IWorld world, Sequence sequence) =>
        Trigger(world, sequence.Trigger) + " -> " + Doing(world, sequence);

    public string Trigger(IWorld world, Trigger trigger)
    {
        if (trigger is WhenTrigger when)
        {
            var events = Vocabulary.EventsFor(world, when.Sources);
            var text = "____";
            foreach (var happening in events)
            {
                if (happening.Id == when.EventId)
                {
                    text = happening.Text;
                    break;
                }
            }

            return "when " + Names(world, when.Sources, when.RequiresAll) + " " + text;
        }

        return trigger is EveryTrigger every ? "every " + every.IntervalTicks + " ticks" : "____";
    }

    public string Doing(IWorld world, Sequence sequence)
    {
        if (sequence.HasName)
            return sequence.Name;

        if (sequence.Steps.Count == 0)
            return "nothing";

        var first = Step(world, sequence.Steps[0]);
        return sequence.Steps.Count == 1 ? first : first + " +" + (sequence.Steps.Count - 1);
    }

    public string Status(SequenceStatus status) => status.ToString().ToLowerInvariant();

    public string Problem(SequenceProblem problem) =>
        problem == SequenceProblem.None ? "" : problem.ToString().ToLowerInvariant();

    static string Step(IWorld world, Step step)
    {
        if (!step.HasAction && step.Gate is AfterGate pause)
            return "wait " + pause.Ticks;

        if (step.Targets.Count == 0)
            return "____";

        var actions = Vocabulary.ActionsFor(world, step.Targets);
        var text = "____";
        foreach (var action in actions)
        {
            if (action.Id == step.ActionId)
            {
                text = action.Text;
                break;
            }
        }

        return text + " " + Names(world, step.Targets, true);
    }

    static string Names(IWorld world, System.Collections.Generic.IList<long> ids, bool all)
    {
        if (ids.Count == 0)
            return "____";

        var names = new System.Collections.Generic.List<string>();
        foreach (var id in ids)
        {
            var entity = world.FindEntity(id);
            names.Add(entity == null ? "(gone)" : entity.Name);
        }

        return string.Join(all ? " and " : " or ", names);
    }
}
