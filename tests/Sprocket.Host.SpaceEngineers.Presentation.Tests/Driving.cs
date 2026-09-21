using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Drives the editor through the same surface the renderer uses.
/// </summary>
static class Driving
{
    public static T Component<T>(this SprocketEditor editor) where T : EditorComponent =>
        editor.Screen.Components.OfType<T>().First();

    /// <summary>The sentences of what starts things, where the screen has them.</summary>
    public static ObservableList<SentenceTrigger> Triggers(this SprocketEditor editor) =>
        editor.Component<TriggerListComponent>().Triggers;

    /// <summary>
    /// What a trigger's row reads as, joined the way a target draws it.
    /// </summary>
    /// <remarks>
    /// Out of the same parts and the same words a screen uses, because what is being asserted is
    /// that the parts say the right thing - not that a test can spell a sentence.
    /// </remarks>
    public static string Says(this SprocketEditor editor, int which)
    {
        SentenceTrigger trigger = editor.Triggers()[which];
        var line = SentenceText.Lead(trigger);

        foreach (SentenceSlot slot in trigger.Slots)
            line += " " + SentenceText.InTrigger(editor.World, slot);

        return line;
    }

    /// <summary>And what the second half of it reads as.</summary>
    public static string Doing(this SprocketEditor editor, int which)
    {
        SentenceTrigger trigger = editor.Triggers()[which];
        var line = trigger.DoesLead;

        for (var i = 0; i < trigger.Does.Count; i++)
        {
            var join = SentenceText.Joining(trigger.Does, i);
            if (join.Length > 0)
                line += (line.Length > 0 ? " " : "") + join;

            line += (line.Length > 0 ? " " : "") + SentenceText.InTrigger(editor.World, trigger.Does[i]);
        }

        return line;
    }

    /// <summary>The list of the things themselves, below whatever starts them.</summary>
    public static StatusListComponent Rows(this SprocketEditor editor) =>
        editor.Screen.Components.OfType<StatusListComponent>().Last();

    static IEnumerable<PickStage> Stages(this SprocketEditor editor) =>
        editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages);

    /// <summary>Presses a block's row, which takes that block and drops whatever was taken before.</summary>
    public static void PressBlock(this SprocketEditor editor, string name)
    {
        editor.SetNumber(Row(editor, name), (int)Held.Alone);
    }

    /// <summary>
    /// Ticks a block's box, which takes it as well as whatever was already taken.
    /// </summary>
    /// <remarks>
    /// The other of the two acts a row offers. A test building a set of blocks up is doing what the
    /// box is for, and pressing the rows would leave it with the last one only.
    /// </remarks>
    public static void TickBlock(this SprocketEditor editor, string name)
    {
        editor.SetNumber(Row(editor, name), (int)Held.Also);
    }

    static int Row(SprocketEditor editor, string name)
    {
        return editor.Stages().SelectMany(p => p.Entries).First(e => e.Text == name).Command;
    }

    /// <summary>
    /// Makes a system with the custom name.
    /// </summary>
    /// <remarks>
    /// How a target asks for the name is its own business, so a test says what the answer was
    /// and nothing about the asking.
    /// </remarks>
    public static void NewSystem(this SprocketEditor editor, string name)
    {
        var start = editor.Screen.Components.OfType<StartComponent>().First();
        editor.SetText(start.Options.First(o => o.Asks).Command, name);
    }

    /// <summary>Answers the question this screen is asking, wherever it sits.</summary>
    /// <remarks>
    /// How a target asks is its own business, the same as naming a system: what a test says is
    /// what somebody typed.
    /// </remarks>
    public static void Typed(this SprocketEditor editor, string said)
    {
        if (editor.Screen.PrimaryAsks)
        {
            editor.SetText(editor.Screen.PrimaryCommand, said);
            return;
        }

        var option = editor.Screen.Components.OfType<StartComponent>()
            .SelectMany(s => s.Options)
            .First(o => o.Asks);

        editor.SetText(option.Command, said);
    }

    /// <summary>Presses one of the ways to start something on this screen, wherever it sits.</summary>
    public static void PressStart(this SprocketEditor editor, string label)
    {
        var option = editor.Screen.Components.OfType<StartComponent>()
            .SelectMany(s => s.Options)
            .First(o => o.Label == label);

        editor.Invoke(option.Command);
    }

    public static SequenceComponent Sentence(this SprocketEditor editor) =>
        editor.Component<SequenceComponent>();

    /// <summary>
    /// Switches a sequence on, which a new one is not.
    /// </summary>
    /// <remarks>
    /// Building and running are separate acts. A sequence is parked while it is written, both
    /// so a half-built one never fires and so its own switch is what makes it safe to edit.
    /// </remarks>
    public static void SwitchOn(this SprocketEditor editor) =>
        editor.SetToggle(editor.Sentence().ToggleCommand, true);

    public static void PressChoice(this SprocketEditor editor, string text)
    {
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

        foreach (var pane in editor.Stages())
        {
            var option = pane.Options.FirstOrDefault(e => e.Text == text)
                         ?? pane.Entries.Select(e => new ChoiceEntry(e.Text, e.IsChosen, e.Command))
                             .FirstOrDefault(e => e.Text == text);
            if (option == null)
                continue;
            editor.Invoke(option.Command);
            return;
        }

        throw new InvalidOperationException($"No choice '{text}' on '{editor.Screen.Title()}'.");
    }

    public static void PressAdd(this SprocketEditor editor, string text)
    {
        var sentence = editor.Component<SequenceComponent>();
        editor.Invoke(sentence.AddOptions.First(o => o.Text == text).Command);
    }

    public static void PressStepSlot(this SprocketEditor editor, int step, int slot)
    {
        var sentence = editor.Component<SequenceComponent>();
        editor.Invoke(sentence.Steps[step].Slots[slot].Command);
    }

    /// <summary>The way back on a screen that carries one, which is screen furniture.</summary>
    public static EditorScreen Steps(this SprocketEditor editor) => editor.Screen;

    /// <summary>
    /// Presses the cross beside what a trigger does, which asks the whole question again.
    /// </summary>
    /// <remarks>
    /// Found rather than counted: which part of the sentence carries it is core's decision, and a
    /// test holding a number would be asserting that decision on its way somewhere else.
    /// </remarks>
    public static void PressClear(this SprocketEditor editor, int which)
    {
        SentenceTrigger trigger = editor.Triggers()[which];
        SentenceSlot slot = trigger.Does.First(s => s.ClearCommand != 0);

        editor.Invoke(slot.ClearCommand);
    }

    /// <summary>Presses the part of a trigger that says when it happens.</summary>
    public static void PressWhen(this SprocketEditor editor, int which) =>
        editor.Invoke(editor.Triggers()[which].Slots[0].Command);

    /// <summary>And the part that says what it does.</summary>
    public static void PressDoes(this SprocketEditor editor, int which) =>
        editor.Invoke(editor.Triggers()[which].Does[0].Command);

    /// <summary>Whether the question hanging over the screen offers that answer.</summary>
    public static bool HasAnswer(this SprocketEditor editor, string text) =>
        editor.Screen.Answers.Any(a => a.Text == text);

    /// <summary>Answers the question hanging over the screen.</summary>
    public static void PressAnswer(this SprocketEditor editor, string text) =>
        editor.Invoke(editor.Screen.Answers.First(a => a.Text == text).Command);

    /// <summary>Back out to the front door, however deep we are.</summary>
    /// <remarks>
    /// Answering anything asked on the way with the first answer, which is the safe one by
    /// convention. A page may ask a question when it is left, and a question stands until answered.
    /// </remarks>
    public static void GoToRoot(this SprocketEditor editor)
    {
        var guard = 0;
        while (editor.Screen.CanGoBack && guard++ < 20)
        {
            editor.Back();

            if (editor.Screen.HasQuestion)
                editor.Invoke(editor.Screen.Answers.First().Command);
        }
    }

    public static void PressAction(this SprocketEditor editor, string text) =>
        editor.Invoke(editor.Screen.Actions.First(a => a.Text == text).Command);

    public static void Name(this SprocketEditor editor, string name) =>
        editor.SetText(Nameable(editor.Screen)?.RenameCommand ?? 0, name);

    /// <summary>The whole heading as one string. A test convenience; core publishes no such thing.</summary>
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

    public static HeadingPart? Nameable(this EditorScreen screen)
    {
        for (var i = 0; i < screen.Heading.Count; i++)
        {
            if (screen.Heading[i].CanRename)
                return screen.Heading[i];
        }

        return null;
    }

    public static string CustomName(this EditorScreen screen) => screen.Nameable()?.Name ?? "";

    public static void Back(this SprocketEditor editor) => editor.Invoke(editor.Screen.BackCommand);

    /// <summary>Presses the screen's move-on action - "Next" or "Done".</summary>
    public static void PressPrimary(this SprocketEditor editor)
    {
        if (!editor.Screen.HasPrimary)
            throw new InvalidOperationException($"'{editor.Screen.Title()}' has nothing to move on to.");
        editor.Invoke(editor.Screen.PrimaryCommand);
    }
}
