using Reactor.Host.SpaceEngineers.Core.Editing;
using Reactor.Host.SpaceEngineers.Core.Runtime;
using Reactor.Host.SpaceEngineers.Core.Sequences;

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
    public string Sequence(Bindings bindings, Sequence sequence) =>
        Trigger(bindings, sequence.Trigger) + " -> " + Doing(bindings, sequence);

    public string Trigger(Bindings bindings, Trigger trigger)
    {
        if (trigger is WhenTrigger when)
        {
            var events = Vocabulary.EventsFor(bindings, when.Sources);
            var text = "____";
            foreach (var happening in events)
            {
                if (happening.Id == when.EventId)
                {
                    text = happening.Text;
                    break;
                }
            }

            return "when " + Names(bindings, when.Sources, when.RequiresAll) + " " + text;
        }

        if (trigger is EveryTrigger every)
            return "every " + every.IntervalTicks + " ticks";

        return trigger is RequiredTrigger ? "when told" : "____";
    }

    public string Doing(Bindings bindings, Sequence sequence)
    {
        if (sequence.HasName)
            return sequence.Name;

        if (sequence.Steps.Count == 0)
            return "nothing";

        var first = Step(bindings, sequence.Steps[0]);
        return sequence.Steps.Count == 1 ? first : first + " +" + (sequence.Steps.Count - 1);
    }

    public string Status(SequenceStatus status) => status.ToString().ToLowerInvariant();

    public string DefaultName(int number) => "sequence " + number;

    public string Problem(SequenceProblem problem) =>
        problem == SequenceProblem.None ? "" : problem.ToString().ToLowerInvariant();

    static string Step(Bindings bindings, Step step)
    {
        if (step is Wait pause)
            return "wait " + pause.Ticks;

        if (step is Run run)
            return "run " + run.Number;

        if (!(step is Act act))
            return "await";

        if (act.Targets.Count == 0)
            return "____";

        var actions = Vocabulary.ActionsFor(bindings, act.Targets);
        var text = "____";
        foreach (var action in actions)
        {
            if (action.Id == act.ActionId)
            {
                text = action.Text;
                break;
            }
        }

        return text + " " + Names(bindings, act.Targets, true);
    }

    static string Names(Bindings bindings, IList<string> refs, bool all)
    {
        if (refs.Count == 0)
            return "____";

        var names = new List<string>();
        foreach (var name in refs)
        {
            var entity = bindings.Entity(name);
            names.Add(entity == null ? "(gone)" : entity.Name);
        }

        return string.Join(all ? " and " : " or ", names);
    }
}
