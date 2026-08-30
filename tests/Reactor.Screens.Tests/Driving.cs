using Reactor.Editing;
using Reactor.Ui;
using Phosphor.Ui;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Tests;

/// <summary>
/// Drives the editor through the same surface the renderer uses.
/// </summary>
static class Driving
{
    public static T Component<T>(this ReactorEditor editor) where T : EditorComponent =>
        editor.Screen.Components.OfType<T>().First();

    static IEnumerable<PickStage> Stages(this ReactorEditor editor) =>
        editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages);

    public static void PressBlock(this ReactorEditor editor, string name)
    {
        var entry = editor.Stages().SelectMany(p => p.Entries).First(e => e.Text == name);
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
        var start = editor.Screen.Components.OfType<StartComponent>().First();
        editor.Invoke(start.Options.First(o => o.Label == label).Command);
    }

    public static SequenceComponent Sentence(this ReactorEditor editor) =>
        editor.Component<SequenceComponent>();

    /// <summary>
    /// Switches a sequence on, which a new one is not.
    /// </summary>
    /// <remarks>
    /// Building and running are separate acts. A sequence is parked while it is written, both
    /// so a half-built one never fires and so its own switch is what makes it safe to edit.
    /// </remarks>
    public static void SwitchOn(this ReactorEditor editor) =>
        editor.SetToggle(editor.Sentence().ToggleCommand, true);

    public static void PressChoice(this ReactorEditor editor, string text)
    {
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

    public static void PressAdd(this ReactorEditor editor, string text)
    {
        var sentence = editor.Component<SequenceComponent>();
        editor.Invoke(sentence.AddOptions.First(o => o.Text == text).Command);
    }

    public static void PressStepSlot(this ReactorEditor editor, int step, int slot)
    {
        var sentence = editor.Component<SequenceComponent>();
        editor.Invoke(sentence.Steps[step].Slots[slot].Command);
    }

    public static void PressTriggerSlot(this ReactorEditor editor, int slot) =>
        editor.Invoke(editor.Component<SequenceComponent>().Trigger.Slots[slot].Command);

    /// <summary>Back out to the front door, however deep we are.</summary>
    public static void GoToRoot(this ReactorEditor editor)
    {
        var guard = 0;
        while (editor.Screen.CanGoBack && guard++ < 20)
            editor.Back();
    }

    public static void PressAction(this ReactorEditor editor, string text) =>
        editor.Invoke(editor.Screen.Actions.First(a => a.Text == text).Command);

    public static void Name(this ReactorEditor editor, string name) =>
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

    public static void Back(this ReactorEditor editor) => editor.Invoke(editor.Screen.BackCommand);

    /// <summary>Presses the screen's move-on action - "Next" or "Done".</summary>
    public static void PressPrimary(this ReactorEditor editor)
    {
        if (!editor.Screen.HasPrimary)
            throw new InvalidOperationException($"'{editor.Screen.Title()}' has nothing to move on to.");
        editor.Invoke(editor.Screen.PrimaryCommand);
    }
}
