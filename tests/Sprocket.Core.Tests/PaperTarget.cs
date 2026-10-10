using Sprocket.Host.SpaceEngineers.Features.Sequences;
using System.Text;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Tests;

/// <summary>
/// A screen written down as text.
/// </summary>
/// <remarks>
/// Knows nothing of layout or drawing: it writes down what the heading, the notice and each page say,
/// in its own words, so a test can hold what is on screen still without holding where it is drawn.
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

        if (editor.Current is SequencePage sequence)
            paper.Append(Draw(world, editor, sequence));

        if (editor.Current is PickPage || editor.Current is TimerPage)
            paper.Append(Draw(editor.Pick()));

        return paper.ToString();
    }

    /// <summary>A pick's columns on the page showing: what each asks, its choices about the picking, and what it offers, chosen or not.</summary>
    static string Draw(PickStrip pick)
    {
        var paper = new StringBuilder();
        foreach (var stage in pick.Stages.Skip(pick.Page))
        {
            paper.Append("pick: ").AppendLine(stage.Title);

            if (Some(stage.Subtitle))
                paper.Append("  ").AppendLine(stage.Subtitle);

            foreach (var option in stage.Choices())
                paper.Append("  option ").AppendLine(option.Text);

            foreach (var entry in stage.Entries())
            {
                paper.Append("  ").Append(entry.IsChosen ? "[x] " : "[ ] ").Append(entry.Text);

                if (Some(entry.Secondary))
                    paper.Append(" - ").Append(entry.Secondary);

                paper.AppendLine();
            }
        }

        return paper.ToString();
    }

    /// <summary>A sequence's page: its steps, one numbered line each, and what undo and redo would do.</summary>
    static string Draw(GridWorld world, SprocketEditor editor, SequencePage page)
    {
        var paper = new StringBuilder();
        paper.Append("sequence ").Append(page.SequenceId).AppendLine(":");

        foreach (var step in editor.StepRows())
            paper.Append("  ").Append(step.Number).Append(". ").AppendLine(Say(world, step));

        paper.Append("  undo: ").Append(Blank(editor.Screen.UndoText))
            .Append("  redo: ").AppendLine(Blank(editor.Screen.RedoText));

        return paper.ToString();
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
    static string Say(GridWorld world, StepRow step)
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

        if (slot is ArgumentsSlot arguments)
            return string.Join(", ", arguments.Arguments.Select(argument => argument.Said));

        throw new InvalidOperationException(
            $"Core put a {slot.GetType().Name} in a sentence and nothing here knows what words go on "
            + "one. Say what it reads as before shipping it to a real target, or the first anybody "
            + "hears of it is a hole in the middle of a sentence.");
    }

    static string Blank(string text) => Some(text) ? text : "-";

    /// <summary>Whether core actually said anything. It is free to leave a string unset.</summary>
    static bool Some(string text) => !string.IsNullOrEmpty(text);
}
