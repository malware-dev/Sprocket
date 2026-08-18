using Reactor.Editing;
using Reactor.Ui;

namespace Reactor.Host.Tests;

/// <summary>
/// Drives the editor through the same surface the renderer uses.
/// </summary>
static class Driving
{
    public static T Component<T>(this ReactorEditor editor) where T : EditorComponent =>
        editor.Screen.Components.OfType<T>().First();

    static IEnumerable<PathPane> Panes(this ReactorEditor editor) =>
        editor.Screen.Components.OfType<PathComponent>().SelectMany(p => p.Panes);

    public static void PressBlock(this ReactorEditor editor, string name)
    {
        var block = editor.Screen.Components.OfType<BlockListComponent>()
            .SelectMany(l => l.Entries).FirstOrDefault(e => e.Name == name);
        if (block != null)
        {
            editor.Invoke(block.Command);
            return;
        }

        var entry = editor.Panes().SelectMany(p => p.Entries).First(e => e.Text == name);
        editor.Invoke(entry.Command);
    }

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

        foreach (var pane in editor.Panes())
        {
            var option = pane.Options.FirstOrDefault(e => e.Text == text)
                         ?? pane.Entries.Select(e => new ChoiceEntry(e.Text, e.IsChosen, e.Command))
                             .FirstOrDefault(e => e.Text == text);
            if (option == null)
                continue;
            editor.Invoke(option.Command);
            return;
        }

        throw new InvalidOperationException($"No choice '{text}' on '{editor.Screen.Title}'.");
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

    public static void PressAction(this ReactorEditor editor, string text) =>
        editor.Invoke(editor.Screen.Actions.First(a => a.Text == text).Command);

    public static void Name(this ReactorEditor editor, string name) =>
        editor.SetText(editor.Component<SequenceComponent>().NameCommand, name);

    public static void Back(this ReactorEditor editor) => editor.Invoke(editor.Screen.BackCommand);

    /// <summary>Presses the screen's move-on action — "Next" or "Done".</summary>
    public static void PressPrimary(this ReactorEditor editor)
    {
        if (!editor.Screen.HasPrimary)
            throw new InvalidOperationException($"'{editor.Screen.Title}' has nothing to move on to.");
        editor.Invoke(editor.Screen.PrimaryCommand);
    }
}
