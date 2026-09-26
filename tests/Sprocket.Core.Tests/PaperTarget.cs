using Sprocket.Host.SpaceEngineers.Editing;
using System.Text;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Tests;

/// <summary>
/// A screen written down as text.
/// </summary>
/// <remarks>
/// <b>A completeness check on the component surface, not a second product.</b> The editor says what
/// is on screen as components; the screens draw them. A component nothing draws is a component
/// nobody sees, and the drawing code is a long way from saying so - it lays out what it knows and
/// passes silently over what it does not.
/// <para>
/// So this is handed a screen and writes down what is on it, knowing nothing of layout or drawing,
/// and refuses anything it has not been told about - see the component-drawing method. A new
/// component type stops the tests until somebody has said what is shown for one.
/// </para>
/// </remarks>
static class PaperTarget
{
    public static string Draw(GridWorld world, SprocketEditor editor)
    {
        var paper = new StringBuilder();
        var screen = editor.Screen;

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

        if (editor.Notice != null && !editor.Notice.Leaving)
            paper.Append("notice (").Append(editor.Notice.Level).Append("): ").AppendLine(editor.Notice.Text);

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
    static string Draw(GridWorld world, EditorComponent component)
    {
        var paper = new StringBuilder();

        if (component is HeadingComponent heading)
        {
            paper.Append("heading: ").AppendLine(heading.Text);
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

        if (component is TriggerListComponent triggers)
        {
            paper.AppendLine("triggers:");

            foreach (SentenceTrigger trigger in triggers.Triggers)
                paper.Append("  ").AppendLine(Say(world, trigger));

            return paper.ToString();
        }

        if (component is SequenceComponent sequence)
        {
            paper.Append("sequence ").Append(sequence.SequenceId).AppendLine(":");

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
    /// One trigger, in this target's own words.
    /// </summary>
    /// <remarks>
    /// Both halves on one line, because that is the sentence: when it happens, and what it then
    /// does.
    /// </remarks>
    static string Say(GridWorld world, SentenceTrigger trigger)
    {
        var line = new StringBuilder();
        line.Append(trigger.Kind == TriggerKind.Every ? "Every" : "When");

        foreach (var slot in trigger.Slots)
            line.Append(' ').Append(Say(world, slot));

        if (trigger.DoesLead.Length > 0)
            line.Append(' ').Append(trigger.DoesLead);

        foreach (var slot in trigger.Does)
            line.Append(' ').Append(Say(world, slot));

        return line.ToString();
    }

    /// <summary>
    /// One numbered line, in this target's own words.
    /// </summary>
    /// <remarks>
    /// Its own words on purpose. Core hands over what the sentence refers to and never what it says,
    /// so a second target writing the same English as the first proves nothing - the point is that it
    /// could write something else and core would not know.
    /// <para>
    /// A step usually opens straight at its verb, so the space goes between words rather than in
    /// front of them.
    /// </para>
    /// </remarks>
    static string Say(GridWorld world, SentenceStep step)
    {
        var line = new StringBuilder();

        if (step.Kind == StepKind.Wait)
            line.Append("wait");

        foreach (var slot in step.Slots)
        {
            if (line.Length > 0)
                line.Append(' ');

            line.Append(Say(world, slot));
        }

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
    static string Say(GridWorld world, SentenceSlot slot)
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

        if (slot is SequenceSlot sequence)
            return sequence.Name.Length > 0 ? sequence.Name : "sequence " + sequence.Number;

        if (slot is ActionSlot action)
            return action.Action.Text;

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
