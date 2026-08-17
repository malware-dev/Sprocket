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

    public static void PressBlock(this ReactorEditor editor, string name)
    {
        var entry = editor.Component<BlockListComponent>().Entries.First(e => e.Name == name);
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

    public static void PressTriggerSwitch(this ReactorEditor editor) =>
        editor.Invoke(editor.Component<SequenceComponent>().Trigger.SwitchCommand);

    public static void PressTriggerSlot(this ReactorEditor editor, int slot) =>
        editor.Invoke(editor.Component<SequenceComponent>().Trigger.Slots[slot].Command);

    public static void PressAction(this ReactorEditor editor, string text) =>
        editor.Invoke(editor.Screen.Actions.First(a => a.Text == text).Command);

    public static void Back(this ReactorEditor editor) => editor.Invoke(editor.Screen.BackCommand);
}
