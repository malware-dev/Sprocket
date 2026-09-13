using System.Text;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Tests;

/// <summary>
/// A target made of nothing but text.
/// </summary>
/// <remarks>
/// Core states capabilities and leaves presentation alone, and the only way to know that claim is
/// still true is for something other than the game to be able to act on it. The Avalonia prototype
/// used to be that second implementation; it was three and a half thousand lines and it drifted, and
/// a lagging second implementation gives confidence rather than pressure - it stops pushing back and
/// starts agreeing with whatever core does.
/// <para>
/// This is the honest cheap version. It knows nothing of the screens, of Space Engineers, or of any
/// drawing at all: it is handed a screen and writes down what is on it. What it cannot render, it
/// refuses to render - see the component-drawing method - so a new component type breaks it
/// until somebody has said what a target is meant to do with one.
/// </para>
/// </remarks>
static class PaperTarget
{
    public static string Draw(IWorld world, EditorScreen screen)
    {
        var paper = new StringBuilder();

        foreach (var part in screen.Heading)
        {
            if (Some(part.Lead))
                paper.Append(part.Lead).Append(' ');

            paper.Append(part.Text);

            if (part.CanRename)
                paper.Append(" [rename]");

            paper.Append(' ');
        }

        paper.AppendLine();

        if (Some(screen.Subtitle))
            paper.Append("  ").AppendLine(screen.Subtitle);

        foreach (var component in screen.Components)
            paper.Append(Draw(world, component));

        return paper.ToString();
    }

    /// <summary>
    /// Writes down one component.
    /// </summary>
    /// <remarks>
    /// The refusal at the end is the whole point of this class. Adding a component type to core and
    /// forgetting to say how a target shows it is silent everywhere else; here it stops the build's
    /// tests until somebody has answered.
    /// </remarks>
    static string Draw(IWorld world, EditorComponent component)
    {
        var paper = new StringBuilder();

        if (component is HeadingComponent heading)
        {
            paper.Append("heading: ").AppendLine(heading.Text);
            return paper.ToString();
        }

        if (component is NoticeComponent notice)
        {
            paper.Append("notice (").Append(notice.Level).Append("): ").AppendLine(notice.Text);
            return paper.ToString();
        }

        if (component is StartComponent start)
        {
            paper.AppendLine("start:");

            foreach (var option in start.Options)
                paper.Append("  ").Append(option.Label).AppendLine(option.Asks ? " (asks)" : "");

            return paper.ToString();
        }

        if (component is ChoiceListComponent choices)
        {
            paper.AppendLine("choices:");

            foreach (var entry in choices.Entries)
                paper.Append("  ").Append(entry.IsCurrent ? "* " : "- ").AppendLine(entry.Text);

            return paper.ToString();
        }

        if (component is StatusListComponent statuses)
        {
            paper.AppendLine("statuses:");

            foreach (var entry in statuses.Entries)
            {
                paper.Append(new string(' ', 2 + entry.Depth * 2)).Append(entry.Summary);

                if (Some(entry.Status))
                    paper.Append(" - ").Append(entry.Status);

                paper.AppendLine();
            }

            return paper.ToString();
        }

        if (component is PickComponent pick)
        {
            foreach (var stage in pick.Stages)
            {
                paper.Append("pick: ").AppendLine(stage.Title);

                if (Some(stage.Subtitle))
                    paper.Append("  ").AppendLine(stage.Subtitle);

                foreach (var option in stage.Options)
                    paper.Append("  option ").AppendLine(option.Text);

                foreach (var entry in stage.Entries)
                {
                    paper.Append("  ").Append(entry.IsChosen ? "[x] " : "[ ] ").Append(entry.Text);

                    if (Some(entry.Secondary))
                        paper.Append(" - ").Append(entry.Secondary);

                    paper.AppendLine();
                }
            }

            return paper.ToString();
        }

        if (component is SequenceComponent sequence)
        {
            paper.Append("sequence ").Append(sequence.SequenceId).AppendLine(":");
            paper.Append("  ").AppendLine(Say(world, sequence.Trigger));

            foreach (var step in sequence.Steps)
                paper.Append("  ").Append(step.Number).Append(". ").AppendLine(Say(world, step));

            paper.Append("  undo: ").Append(Blank(sequence.UndoText))
                .Append("  redo: ").AppendLine(Blank(sequence.RedoText));

            return paper.ToString();
        }

        throw new InvalidOperationException(
            $"Core produced a {component.GetType().Name} and nothing here knows what a target should "
            + "show for one. Say what it looks like on paper before shipping it to a real target, or "
            + "the first anybody hears of it is a blank space in the game.");
    }

    /// <summary>
    /// The first line, in this target's own words.
    /// </summary>
    /// <remarks>
    /// Its own words on purpose. Core hands over what the sentence refers to and never what it says,
    /// so a second target writing the same English as the first proves nothing - the point is that it
    /// could write something else and core would not know.
    /// </remarks>
    static string Say(IWorld world, SentenceTrigger trigger)
    {
        var line = new StringBuilder();
        line.Append(trigger.Kind == TriggerKind.Every ? "Every" : "When");

        foreach (var slot in trigger.Slots)
            line.Append(' ').Append(Say(world, slot));

        return line.ToString();
    }

    /// <summary>
    /// One numbered line.
    /// </summary>
    /// <remarks>
    /// A step usually opens straight at its verb, so the space goes between words rather than in
    /// front of them.
    /// </remarks>
    static string Say(IWorld world, SentenceStep step)
    {
        var line = new StringBuilder();

        if (step.Kind == StepKind.Wait)
            line.Append("wait");
        else if (step.Gate != null)
            line.Append(Say(world, step.Gate));

        foreach (var slot in step.Slots)
        {
            if (line.Length > 0)
                line.Append(' ');

            line.Append(Say(world, slot));
        }

        return line.ToString();
    }

    static string Say(IWorld world, SentenceGate gate)
    {
        var line = new StringBuilder();

        if (gate.Kind == GateKind.Once)
            line.Append("once");
        else if (gate.Kind == GateKind.OnlyIf)
            line.Append("only if");
        else
            line.Append("after");

        foreach (var slot in gate.Slots)
            line.Append(' ').Append(Say(world, slot));

        return line.ToString();
    }

    /// <summary>
    /// One slot, as the words this target would print for it.
    /// </summary>
    /// <remarks>
    /// The refusal at the end is the same pressure as the one on components. A slot kind core
    /// invented and nobody has words for is a gap in a sentence, and a gap in a sentence reads
    /// exactly like a slot somebody has not filled in yet.
    /// </remarks>
    static string Say(IWorld world, SentenceSlot slot)
    {
        if (slot.IsEmpty)
            return "____";

        if (slot is EntitiesSlot entities)
        {
            var names = new List<string>();
            foreach (var id in entities.Entities)
            {
                var entity = world.FindEntity(id);
                names.Add(entity == null ? "(gone)" : entity.Name);
            }

            return string.Join(entities.RequiresAll ? " and " : " or ", names);
        }

        if (slot is EventSlot happening)
            return happening.Event.Text;

        if (slot is ActionSlot action)
            return action.Action.Text;

        if (slot is StateSlot state)
            return state.State.Text;

        if (slot is DurationSlot duration)
            return duration.Ticks + " ticks";

        throw new InvalidOperationException(
            $"Core put a {slot.GetType().Name} in a sentence and nothing here knows what words go on "
            + "one. Say what it reads as before shipping it to a real target, or the first anybody "
            + "hears of it is a hole in the middle of a sentence.");
    }

    static string Blank(string text) => Some(text) ? text : "-";

    /// <summary>Whether core actually said anything. It is free to leave a string unset.</summary>
    static bool Some(string text) => !string.IsNullOrEmpty(text);
}
