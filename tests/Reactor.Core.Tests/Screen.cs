using Reactor.Editing;
using Reactor.Ui;

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
            $"No {typeof(T).Name} on '{editor.Screen.Title}'. Present: " +
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
                        $"No block '{name}' on '{editor.Screen.Title}'.");
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

        throw new InvalidOperationException($"No choice '{text}' on '{editor.Screen.Title}'.");
    }

    public static bool HasChoice(this ReactorEditor editor, string text) =>
        editor.Screen.Components.OfType<ChoiceListComponent>().Any(l => l.Entries.Any(e => e.Text == text))
        || editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages)
            .Any(p => p.Options.Any(e => e.Text == text) || p.Entries.Any(e => e.Text == text));

    public static void PressAdd(this ReactorEditor editor, string text) =>
        editor.Invoke(editor.Sentence().AddOptions.First(o => o.Text == text).Command);

    public static void PressStepSlot(this ReactorEditor editor, int step, int slot) =>
        editor.Invoke(editor.Sentence().Steps[step].Slots[slot].Command);

    public static void PressPrimary(this ReactorEditor editor)
    {
        if (!editor.Screen.HasPrimary)
            throw new InvalidOperationException($"'{editor.Screen.Title}' has nothing to move on to.");
        editor.Invoke(editor.Screen.PrimaryCommand);
    }

    /// <summary>Back out to the front door, however deep we are.</summary>
    public static void GoToRoot(this ReactorEditor editor)
    {
        var guard = 0;
        while (editor.Screen.CanGoBack && guard++ < 20)
            editor.Back();
    }

    public static void PressAction(this ReactorEditor editor, string text) =>
        editor.Invoke(editor.Screen.Actions.First(a => a.Text == text).Command);

    public static void Back(this ReactorEditor editor) => editor.Invoke(editor.Screen.BackCommand);

    public static void Name(this ReactorEditor editor, string name) =>
        editor.SetText(editor.Screen.RenameCommand, name);

    public static void RunNow(this ReactorEditor editor) =>
        editor.Invoke(editor.Sentence().RunCommand);
}
