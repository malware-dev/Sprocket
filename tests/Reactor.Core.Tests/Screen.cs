using Reactor.Host.SpaceEngineers.Editing;
using Reactor.Host.SpaceEngineers.Lists;
using Reactor.Host.SpaceEngineers.Picking;
using Reactor.Host.SpaceEngineers.Screen;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.Words;

namespace Reactor.Tests;

/// <summary>
/// Reads and drives the editor the way a target would.
/// </summary>
/// <remarks>
/// These tests go through exactly the surface a renderer sees - components out,
/// command ids back - so anything they can do, a Space Engineers host can do too. If a
/// flow cannot be exercised from here, it cannot be implemented there.
/// </remarks>
static class Screen
{
    public static T Component<T>(this ReactorEditor editor) where T : EditorComponent
    {
        foreach (var component in editor.Screen.Components)
        {
            if (component is T match)
                return match;
        }

        throw new InvalidOperationException(
            $"No {typeof(T).Name} on '{editor.Screen.Title()}'. Present: " +
            string.Join(", ", editor.Screen.Components.Select(c => c.GetType().Name)));
    }

    public static SequenceComponent Sentence(this ReactorEditor editor) =>
        editor.Component<SequenceComponent>();

    static IEnumerable<PickOption> PathEntries(this ReactorEditor editor) =>
        editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages).SelectMany(p => p.Entries);

    public static void PressBlock(this ReactorEditor editor, string name)
    {
        var entry = editor.PathEntries().FirstOrDefault(e => e.Text == name)
                    ?? throw new InvalidOperationException(
                        $"No block '{name}' on '{editor.Screen.Title()}'.");
        editor.Invoke(entry.Command);
    }

    /// <summary>
    /// Makes a system with the custom name.
    /// </summary>
    /// <remarks>
    /// How a target asks for the name is its own business, so a test says what the answer was
    /// and nothing about the asking.
    /// </remarks>
    public static void NewSystem(this ReactorEditor editor, string name)
    {
        var start = editor.Screen.Components.OfType<StartComponent>().First();
        editor.SetText(start.Options.First(o => o.Asks).Command, name);
    }

    /// <summary>Presses one of the ways to start something on this screen.</summary>
    public static void PressStart(this ReactorEditor editor, string label)
    {
        var option = editor.Screen.Components.OfType<StartComponent>()
            .SelectMany(s => s.Options)
            .First(o => o.Label == label);

        editor.Invoke(option.Command);
    }

    /// <summary>
    /// Switches a sequence on, which a new one is not.
    /// </summary>
    /// <remarks>
    /// Building and running are separate acts now. A sequence is parked while it is being
    /// written, both so a half-built one never fires and so that its own switch can be what
    /// makes it safe to edit.
    /// </remarks>
    public static void SwitchOn(this ReactorEditor editor) =>
        editor.SetToggle(editor.Sentence().ToggleCommand, true);

    public static void PressChoice(this ReactorEditor editor, string text)
    {
        // A question hanging over the screen is answered by pressing its words, which is the same
        // act from where a test stands: something on screen said this, and it was pressed.
        var answer = editor.Screen.Answers.FirstOrDefault(e => e.Text == text);
        if (answer != null)
        {
            editor.Invoke(answer.Command);
            return;
        }

        foreach (var list in editor.Screen.Components.OfType<ChoiceListComponent>())
        {
            var entry = list.Entries.FirstOrDefault(e => e.Text == text);
            if (entry == null)
                continue;
            editor.Invoke(entry.Command);
            return;
        }

        foreach (var pane in editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages))
        {
            var option = pane.Options.FirstOrDefault(e => e.Text == text);
            if (option != null)
            {
                editor.Invoke(option.Command);
                return;
            }

            var entry = pane.Entries.FirstOrDefault(e => e.Text == text);
            if (entry != null)
            {
                editor.Invoke(entry.Command);
                return;
            }
        }

        throw new InvalidOperationException($"No choice '{text}' on '{editor.Screen.Title()}'.");
    }

    public static bool HasChoice(this ReactorEditor editor, string text) =>
        editor.Screen.Components.OfType<ChoiceListComponent>().Any(l => l.Entries.Any(e => e.Text == text))
        || editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages)
            .Any(p => p.Options.Any(e => e.Text == text) || p.Entries.Any(e => e.Text == text));

    /// <summary>
    /// The rows of the things on this screen, below whatever starts them.
    /// </summary>
    /// <remarks>
    /// The last list, because a system's screen holds two: what starts things above, and the things
    /// themselves below. Positional because the screen is - a test reading a screen is allowed to
    /// know what is on it.
    /// </remarks>
    public static ObservableList<StatusEntry> Rows(this ReactorEditor editor) =>
        editor.Screen.Components.OfType<StatusListComponent>().Last().Entries;


    /// <summary>The sentences of what starts things, where the screen has them.</summary>
    public static ObservableList<SentenceTrigger> Triggers(this ReactorEditor editor) =>
        editor.Component<TriggerListComponent>().Triggers;

    /// <summary>
    /// What a trigger's row reads as, joined the way a target draws it.
    /// </summary>
    /// <remarks>
    /// Out of the same parts and the same words a screen uses, because what is being asserted is
    /// that the parts say the right thing - not that a test can spell a sentence.
    /// </remarks>
    public static string Says(this ReactorEditor editor, int which)
    {
        SentenceTrigger trigger = editor.Triggers()[which];
        var line = SentenceText.Lead(trigger);

        foreach (SentenceSlot slot in trigger.Slots)
            line += " " + SentenceText.InTrigger(editor.World, slot);

        return line;
    }

    /// <summary>And what the second half of it reads as.</summary>
    public static string Doing(this ReactorEditor editor, int which)
    {
        SentenceTrigger trigger = editor.Triggers()[which];
        var line = trigger.DoesLead;

        foreach (SentenceSlot slot in trigger.Does)
            line += (line.Length > 0 ? " " : "") + SentenceText.InTrigger(editor.World, slot);

        return line;
    }

    /// <summary>
    /// Presses the cross beside what a trigger does, which asks the whole question again.
    /// </summary>
    /// <remarks>
    /// Found rather than counted: which part of the sentence carries it is core's decision, and a
    /// test holding a number would be asserting that decision on its way somewhere else.
    /// </remarks>
    public static void PressClear(this ReactorEditor editor, int which)
    {
        SentenceTrigger trigger = editor.Triggers()[which];
        SentenceSlot slot = trigger.Does.First(s => s.ClearCommand != 0);

        editor.Invoke(slot.ClearCommand);
    }

    /// <summary>Presses the part of a trigger that says when it happens.</summary>
    public static void PressWhen(this ReactorEditor editor, int which) =>
        editor.Invoke(editor.Triggers()[which].Slots[0].Command);

    /// <summary>And the part that says what it does.</summary>
    public static void PressDoes(this ReactorEditor editor, int which) =>
        editor.Invoke(editor.Triggers()[which].Does[0].Command);

    /// <summary>The way back on a screen that carries one, which is screen furniture.</summary>
    public static EditorScreen Steps(this ReactorEditor editor) => editor.Screen;


    /// <summary>Opens a row of that list, by where it sits.</summary>
    /// <remarks>
    /// By position rather than by name, because the names on these rows are the target's words and a
    /// test that matched on them would be asserting about English on its way somewhere else.
    /// </remarks>
    public static void PressRow(this ReactorEditor editor, int row) =>
        editor.Invoke(editor.Rows()[row].Command);

    /// <summary>The short question hanging over the screen, or empty.</summary>
    public static string Question(this ReactorEditor editor) => editor.Screen.Question;

    public static bool HasAnswer(this ReactorEditor editor, string text) =>
        editor.Screen.Answers.Any(a => a.Text == text);

    public static void PressAnswer(this ReactorEditor editor, string text) =>
        editor.Invoke(editor.Screen.Answers.First(a => a.Text == text).Command);

    public static void PressAdd(this ReactorEditor editor, string text) =>
        editor.Invoke(editor.Sentence().AddOptions.First(o => o.Text == text).Command);

    public static void PressStepSlot(this ReactorEditor editor, int step, int slot) =>
        editor.Invoke(editor.Sentence().Steps[step].Slots[slot].Command);

    public static void PressPrimary(this ReactorEditor editor)
    {
        if (!editor.Screen.HasPrimary)
            throw new InvalidOperationException($"'{editor.Screen.Title()}' has nothing to move on to.");
        editor.Invoke(editor.Screen.PrimaryCommand);
    }

    /// <summary>Back out to the front door, however deep we are.</summary>
    /// <remarks>
    /// Answering anything asked on the way, with the first answer - which is the safe one by
    /// convention. A page may ask a question when it is left, and a question stands until it is
    /// answered, so a walk that only pressed back would alternate between asking and dismissing.
    /// </remarks>
    public static void GoToRoot(this ReactorEditor editor)
    {
        var guard = 0;
        while (editor.Screen.CanGoBack && guard++ < 20)
        {
            editor.Back();

            if (editor.Screen.HasQuestion)
                editor.Invoke(editor.Screen.Answers.First().Command);
        }
    }

    public static void PressAction(this ReactorEditor editor, string text) =>
        editor.Invoke(editor.Screen.Actions.First(a => a.Text == text).Command);

    public static void Back(this ReactorEditor editor) => editor.Invoke(editor.Screen.BackCommand);

    /// <summary>
    /// The whole heading as one string, for an assertion that does not care how it is made of parts.
    /// </summary>
    /// <remarks>
    /// A test convenience and deliberately only that. <c>EditorScreen</c> publishes no joined-up version
    /// of its heading, because a target handed one would draw one - and the one thing a heading has to
    /// show is which part of it somebody can change. Here there is no such risk: a test is asserting on
    /// words, not drawing them.
    /// </remarks>
    public static string Title(this EditorScreen screen)
    {
        var joined = "";
        for (var i = 0; i < screen.Heading.Count; i++)
        {
            var part = screen.Heading[i];

            if (joined.Length > 0)
                joined += part.Lead.Length > 0 ? " " : " » ";

            if (part.Lead.Length > 0)
                joined += part.Lead + " ";

            joined += part.Text;
        }

        return joined;
    }

    /// <summary>The first part of the heading anybody can rename, or null when none can be.</summary>
    static HeadingPart? Nameable(this EditorScreen screen)
    {
        for (var i = 0; i < screen.Heading.Count; i++)
        {
            if (screen.Heading[i].CanRename)
                return screen.Heading[i];
        }

        return null;
    }

    /// <summary>What somebody typed for the part of the heading that takes a name.</summary>
    public static string CustomName(this EditorScreen screen) => screen.Nameable()?.Name ?? "";

    public static int RenameCommand(this EditorScreen screen) => screen.Nameable()?.RenameCommand ?? 0;

    public static bool NameRequired(this EditorScreen screen) => screen.Nameable()?.NameRequired ?? false;

    public static void Name(this ReactorEditor editor, string name) =>
        editor.SetText(editor.Screen.RenameCommand(), name);

    /// <summary>Sets a sequence off the way a player would: through its real trigger.</summary>
    /// <remarks>
    /// There is no longer a run-by-hand button. Nobody would open the terminal and walk down
    /// to a sequence in order to use their airlock - they would press the button they built -
    /// so the only motive for one was watching it, which is diagnosis and waits for R28.
    /// </remarks>
    public static void Fire(this ReactorEditor editor) { }
}
