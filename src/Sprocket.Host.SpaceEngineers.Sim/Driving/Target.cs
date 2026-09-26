using System;
using System.Collections.Generic;
using System.Linq;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Rich;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Sim.Driving
{
    /// <summary>Reads and drives the editor the way a target would.</summary>
    /// <remarks>
    /// Through exactly the surface a renderer sees - components out, command ids back, and a ported page's
    /// own view - so anything done from here, a Space Engineers host can do too. If a flow cannot be
    /// exercised from here, it cannot be implemented there.
    /// </remarks>
    public static class Target
    {
        /// <summary>The first component of its kind on the screen, or an exception naming what is there.</summary>
        public static T Component<T>(this SprocketEditor editor) where T : EditorComponent
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

        /// <summary>The sequence the screen shows.</summary>
        public static SequenceComponent Sentence(this SprocketEditor editor) =>
            editor.Component<SequenceComponent>();

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

        /// <summary>
        /// Reaches from the last block taken on its own to this one, taking everything between.
        /// </summary>
        /// <remarks>
        /// In the order the blocks are listed rather than the order they were pressed, because a run is
        /// what somebody drew down the screen.
        /// </remarks>
        public static void ThroughBlock(this SprocketEditor editor, string name)
        {
            editor.SetNumber(Row(editor, name), (int)Held.Through);
        }

        /// <summary>
        /// The root's rows, as the controls it draws, in the order they stand.
        /// </summary>
        /// <remarks>The root draws itself, so these are its own rows, pressed through their own commands.</remarks>
        public static List<StatusRow> RootRows(this SprocketEditor editor) =>
            Within(Root(editor).View).OfType<StatusRow>().ToList();

        /// <summary>The names the root's headings say, one for each blueprint.</summary>
        public static List<string> RootHeadingNames(this SprocketEditor editor) =>
            Within(Root(editor).View).OfType<RichText>()
                .Where(text => text.Arguments != null)
                .Select(text => text.Arguments!.Of("name"))
                .ToList();

        /// <summary>What the root's buttons for starting something say, in order.</summary>
        public static List<string> RootStarts(this SprocketEditor editor) =>
            Within(Root(editor).View).OfType<Button>().Select(button => button.Text).ToList();

        /// <summary>
        /// Makes a system with the custom name.
        /// </summary>
        /// <remarks>
        /// Pressing New logic system makes a numbered one and opens it, and the name is given where it is
        /// renamed, in the heading.
        /// </remarks>
        public static void NewSystem(this SprocketEditor editor, string name)
        {
            PressRootButton(editor, "New logic system");
            editor.SetText(editor.Screen.Heading[0].RenameCommand, name);
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
            if (editor.Current is HomePage)
            {
                PressRootButton(editor, label);
                return;
            }

            var option = editor.Screen.Components.OfType<StartComponent>()
                .SelectMany(s => s.Options)
                .First(o => o.Label == label);

            editor.Invoke(option.Command);
        }

        /// <summary>
        /// Switches a sequence on, which a new one is not.
        /// </summary>
        /// <remarks>
        /// Building and running are separate acts. A sequence is parked while it is written, both
        /// so a half-built one never fires and so its own switch is what makes it safe to edit.
        /// </remarks>
        public static void SwitchOn(this SprocketEditor editor) =>
            editor.SetToggle(editor.Sentence().ToggleCommand, true);

        /// <summary>Presses whatever on the screen says <paramref name="text" />: an answer, a choice, or an option or block in a picker.</summary>
        /// <remarks>
        /// A question hanging over the screen is answered by pressing its words, which is the same act from
        /// where a test stands: something on screen said this, and it was pressed.
        /// </remarks>
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

        /// <summary>Whether anything on the screen offers <paramref name="text" /> to be chosen.</summary>
        public static bool HasChoice(this SprocketEditor editor, string text) =>
            editor.Screen.Components.OfType<ChoiceListComponent>().Any(l => l.Entries.Any(e => e.Text == text))
            || editor.Stages().Any(p => p.Options.Any(e => e.Text == text) || p.Entries.Any(e => e.Text == text));

        /// <summary>
        /// The rows of the things on this screen, below whatever starts them.
        /// </summary>
        /// <remarks>
        /// The last list, because a system's screen holds two: what starts things above, and the things
        /// themselves below. Positional because the screen is - a test reading a screen is allowed to
        /// know what is on it.
        /// </remarks>
        public static ObservableList<StatusEntry> Rows(this SprocketEditor editor) =>
            editor.Screen.Components.OfType<StatusListComponent>().Last().Entries;

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

        /// <summary>And what the second half of it reads as, with the words that join its parts.</summary>
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

        /// <summary>Presses the part of a trigger that says when it happens.</summary>
        public static void PressWhen(this SprocketEditor editor, int which) =>
            editor.Invoke(editor.Triggers()[which].Slots[0].Command);

        /// <summary>And the part that says what it does.</summary>
        public static void PressDoes(this SprocketEditor editor, int which) =>
            editor.Invoke(editor.Triggers()[which].Does[0].Command);

        /// <summary>The way back on a screen that carries one, which is screen furniture.</summary>
        public static EditorScreen Steps(this SprocketEditor editor) => editor.Screen;

        /// <summary>Opens a row of that list, by where it sits.</summary>
        /// <remarks>
        /// By position rather than by name, because the names on these rows are the target's words and a
        /// test that matched on them would be asserting about English on its way somewhere else.
        /// </remarks>
        public static void PressRow(this SprocketEditor editor, int row)
        {
            if (editor.Current is HomePage)
                editor.RootRows()[row].Opens!.Execute();
            else
                editor.Invoke(editor.Rows()[row].Command);
        }

        /// <summary>The short question hanging over the screen, or empty.</summary>
        public static string Question(this SprocketEditor editor) => editor.Screen.Question;

        /// <summary>Whether the question hanging over the screen offers that answer.</summary>
        public static bool HasAnswer(this SprocketEditor editor, string text) =>
            editor.Screen.Answers.Any(a => a.Text == text);

        /// <summary>Answers the question hanging over the screen.</summary>
        public static void PressAnswer(this SprocketEditor editor, string text) =>
            editor.Invoke(editor.Screen.Answers.First(a => a.Text == text).Command);

        /// <summary>Presses one of the ways to add a step to the sequence the screen shows.</summary>
        public static void PressAdd(this SprocketEditor editor, string text) =>
            editor.Invoke(editor.Sentence().AddOptions.First(o => o.Text == text).Command);

        /// <summary>Presses one part of one step of the sequence the screen shows.</summary>
        public static void PressStepSlot(this SprocketEditor editor, int step, int slot) =>
            editor.Invoke(editor.Sentence().Steps[step].Slots[slot].Command);

        /// <summary>Presses the screen's move-on action - "Next" or "Done".</summary>
        public static void PressPrimary(this SprocketEditor editor)
        {
            if (!editor.Screen.HasPrimary)
                throw new InvalidOperationException($"'{editor.Screen.Title()}' has nothing to move on to.");

            editor.Invoke(editor.Screen.PrimaryCommand);
        }

        /// <summary>Back out to the front door, however deep the editor is.</summary>
        /// <remarks>
        /// Answering anything asked on the way with the first answer, which is the safe one by
        /// convention. A page may ask a question when it is left, and a question stands until it is
        /// answered, so a walk that only pressed back would alternate between asking and dismissing.
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

        /// <summary>Presses one of the actions along the foot of the screen.</summary>
        public static void PressAction(this SprocketEditor editor, string text) =>
            editor.Invoke(editor.Screen.Actions.First(a => a.Text == text).Command);

        /// <summary>Presses back.</summary>
        public static void Back(this SprocketEditor editor) => editor.Invoke(editor.Screen.BackCommand);

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
        public static HeadingPart? Nameable(this EditorScreen screen)
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

        /// <summary>The command that renames the part of the heading that takes a name, or zero where none does.</summary>
        public static int RenameCommand(this EditorScreen screen) => screen.Nameable()?.RenameCommand ?? 0;

        /// <summary>Whether the part of the heading that takes a name has to have one.</summary>
        public static bool NameRequired(this EditorScreen screen) => screen.Nameable()?.NameRequired ?? false;

        /// <summary>Types a name into the part of the heading that takes one.</summary>
        public static void Name(this SprocketEditor editor, string name) =>
            editor.SetText(editor.Screen.RenameCommand(), name);

        /// <summary>The panes of any picker on the screen, from the page it is on.</summary>
        static IEnumerable<PickStage> Stages(this SprocketEditor editor) =>
            editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages.Skip(p.Page));

        /// <summary>The command of the block row saying <paramref name="name" /> in a picker, or an exception naming the screen.</summary>
        static int Row(SprocketEditor editor, string name)
        {
            return (editor.Stages().SelectMany(p => p.Entries).FirstOrDefault(e => e.Text == name)
                    ?? throw new InvalidOperationException($"No block '{name}' on '{editor.Screen.Title()}'.")).Command;
        }

        /// <summary>Presses the root's button saying <paramref name="label" />.</summary>
        static void PressRootButton(SprocketEditor editor, string label)
        {
            var button = Within(Root(editor).View).OfType<Button>()
                .First(b => b.Text == StartView.Plus + label);

            button.Command!.Execute();
        }

        /// <summary>The root, which is where the editor is.</summary>
        static HomePage Root(SprocketEditor editor) =>
            editor.Current as HomePage ?? throw new InvalidOperationException("the editor is not on its root");

        /// <summary>Every element under <paramref name="element" />, its own first, in the order they are drawn.</summary>
        static IEnumerable<Element> Within(Element element)
        {
            yield return element;

            for (var i = 0; i < element.VisualChildCount; i++)
            {
                foreach (var inner in Within(element.GetVisualChild(i)))
                    yield return inner;
            }
        }
    }
}
