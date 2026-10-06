using System;
using System.Collections.Generic;
using System.Linq;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Developing;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Lumel;
using Lumel.Controls;
using Lumel.Text;
using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Values;

namespace Sprocket.Sim.Driving
{
    /// <summary>Reads and drives the editor the way a target would.</summary>
    /// <remarks>
    /// Through exactly the surface a renderer sees - the screen's chrome, its commands, and each page's own
    /// view - so anything done from here, a Space Engineers host can do too. If a flow cannot be exercised
    /// from here, it cannot be implemented there.
    /// </remarks>
    public static class Target
    {
        /// <summary>The page of the sequence the editor is on, its view made.</summary>
        /// <remarks>Asks the page for its view first, which is what makes it where nothing has drawn the page yet.</remarks>
        public static SequencePage Sentence(this SprocketEditor editor)
        {
            var page = editor.Current as SequencePage
                       ?? throw new InvalidOperationException($"'{editor.Screen.Title()}' is not a sequence's page.");
            _ = page.View;
            return page;
        }

        /// <summary>The ways to add a step to the sequence the editor is on, in the order they stand, shown or not.</summary>
        public static List<Button> Adders(this SprocketEditor editor) =>
            Within(editor.Sentence().View).OfType<Button>().Where(b => b.Text.StartsWith(StatusRow.Plus, StringComparison.Ordinal)).ToList();

        /// <summary>The picking page the editor is on, or an exception naming the screen.</summary>
        public static PickPage Picking(this SprocketEditor editor) =>
            editor.Current as PickPage ?? throw new InvalidOperationException($"'{editor.Screen.Title()}' is not picking.");

        /// <summary>The page of the blueprint the editor is on, its view made.</summary>
        public static BlueprintPage Blueprint(this SprocketEditor editor)
        {
            var page = editor.Current as BlueprintPage
                       ?? throw new InvalidOperationException($"'{editor.Screen.Title()}' is not a blueprint's page.");
            _ = page.View;
            return page;
        }


        /// <summary>The lines under each blueprint on the root that add another installation, one for each blueprint.</summary>
        public static List<Button> RootAdds(this SprocketEditor editor) =>
            Within(Root(editor).View).OfType<SystemGroup>().Select(group => group.Adds).ToList();

        /// <summary>Adds another installation of the blueprint number <paramref name="group" /> on the root, by pressing its line.</summary>
        public static void AddInstallation(this SprocketEditor editor, int group) =>
            RootAdds(editor)[group].Command!.Execute();

        /// <summary>Opens the blueprint library, by pressing its button in the header.</summary>
        public static void OpenLibrary(this SprocketEditor editor) =>
            (editor.Screen.Library ?? throw new InvalidOperationException($"'{editor.Screen.Title()}' offers no library.")).Execute();

        /// <summary>Opens what the blueprint under the root's heading number <paramref name="group" /> does, by pressing that heading.</summary>
        public static void OpenBlueprintSystem(this SprocketEditor editor, int group) =>
            Within(Root(editor).View).OfType<SystemGroup>().ElementAt(group).Heading.Opens!.Execute();

        /// <summary>Opens the configuration panel of the logic system the editor is on, to be written, by pressing its button.</summary>
        public static void EditPanel(this SprocketEditor editor)
        {
            var page = editor.Current as LogicSystemPage
                       ?? throw new InvalidOperationException($"'{editor.Screen.Title()}' is not a logic system's page.");
            Within(page.View).OfType<Button>().First(button => button.Text == editor.Texts.SystemEditPanel).Command!.Execute();
        }

        /// <summary>The parts of the blueprint the editor is on, one row each.</summary>
        public static List<PartRow> Parts(this SprocketEditor editor) =>
            editor.Blueprint().Parts.OfType<PartRow>().ToList();

        /// <summary>The rows of the steps of the sequence the editor is on, in the order they stand.</summary>
        public static List<StepRow> StepRows(this SprocketEditor editor) =>
            editor.Sentence().Rows.OfType<StepRow>().ToList();

        /// <summary>Presses a block's row, which takes that block and drops whatever was taken before.</summary>
        public static void PressBlock(this SprocketEditor editor, string name) => Row(editor, name).Pick(Held.Alone, 1);

        /// <summary>Presses a block's row twice in quick succession, which takes that block alone and finishes where nothing is left to ask.</summary>
        public static void DoublePressBlock(this SprocketEditor editor, string name) => Row(editor, name).Pick(Held.Alone, 2);

        /// <summary>
        /// Ticks a block's box, which takes it as well as whatever was already taken.
        /// </summary>
        /// <remarks>
        /// The other of the two acts a row offers. A test building a set of blocks up is doing what the
        /// box is for, and pressing the rows would leave it with the last one only.
        /// </remarks>
        public static void TickBlock(this SprocketEditor editor, string name) => Row(editor, name).Pick(Held.Also, 1);

        /// <summary>
        /// Reaches from the last block taken on its own to this one, taking everything between.
        /// </summary>
        /// <remarks>
        /// In the order the blocks are listed rather than the order they were pressed, because a run is
        /// what somebody drew down the screen.
        /// </remarks>
        public static void ThroughBlock(this SprocketEditor editor, string name) => Row(editor, name).Pick(Held.Through, 1);

        /// <summary>The strip of the pick the editor is on, or an exception naming the screen.</summary>
        public static PickStrip Pick(this SprocketEditor editor)
        {
            var strip = (editor.Current as PickPage)?.Strip ?? (editor.Current as TimerPage)?.Strip;
            return strip ?? throw new InvalidOperationException($"'{editor.Screen.Title()}' is not a pick.");
        }

        /// <summary>The things a column offers, in the order they stand.</summary>
        public static List<PickRow> Entries(this StageColumn column) => column.Rows.OfType<PickRow>().ToList();

        /// <summary>The choices about the picking a column offers, in the order they stand.</summary>
        public static List<OptionRow> Choices(this StageColumn column) => column.Options.OfType<OptionRow>().ToList();

        /// <summary>The kinds a column can be narrowed to, in the order they stand.</summary>
        public static List<KindRow> Kinds(this StageColumn column) => column.Kinds.OfType<KindRow>().ToList();

        /// <summary>Types <paramref name="words" /> over whatever is in the box that narrows a column, as a keyboard would.</summary>
        public static void Search(this StageColumn column, string words) => Retype(column.Narrow, words);

        /// <summary>Types <paramref name="words" /> over whatever is in the box that narrows a column's kinds, as a keyboard would.</summary>
        public static void SearchKinds(this StageColumn column, string words) => Retype(column.KindNarrow, words);

        /// <summary>Presses the tab of a column's view saying <paramref name="text" />.</summary>
        public static void PressView(this StageColumn column, string text) =>
            column.Views.First(v => v.Text == text).Command!.Execute();

        static void Retype(TextBox box, string words)
        {
            box.SelectAll();
            if (string.IsNullOrEmpty(words))
                box.Backspace();
            else
                box.Insert(words);
        }

        /// <summary>
        /// The root's rows, as the controls it draws, in the order they stand.
        /// </summary>
        /// <remarks>The root draws itself, so these are its own rows, pressed through their own commands.</remarks>
        public static List<StatusRow> RootRows(this SprocketEditor editor)
        {
            var headings = RootHeadings(editor);
            return Within(Root(editor).View).OfType<StatusRow>().Where(row => !headings.Contains(row)).ToList();
        }

        /// <summary>The rows over each blueprint's logic systems on the root, one for each blueprint.</summary>
        public static List<StatusRow> RootHeadings(this SprocketEditor editor) =>
            Within(Root(editor).View).OfType<SystemGroup>().Select(group => group.Heading).ToList();

        /// <summary>The names the root's headings say, one for each blueprint.</summary>
        public static List<string> RootHeadingNames(this SprocketEditor editor) =>
            RootHeadings(editor).Select(heading => heading.AboutArguments!.Of("name")).ToList();

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
            PressButton(editor, editor.Texts.SystemsNew);
            editor.Screen.Heading[0].Renames!(name);
        }

        /// <summary>Presses New logic system and leaves the numbered one it makes as it is, untouched.</summary>
        public static void NewUntouchedSystem(this SprocketEditor editor) =>
            PressButton(editor, editor.Texts.SystemsNew);

        /// <summary>Makes a system with the custom name whose one trigger is the Airlock Button being pressed, and leaves the editor on the sequence it starts.</summary>
        public static void NewButtonSystem(this SprocketEditor editor, string name)
        {
            editor.NewSystem(name);
            editor.PressStart(editor.Texts.SystemWhen);
            editor.PressBlock("Airlock Button");
            editor.PressChoice("button 1 is pressed");
            editor.PressPrimary();
        }

        /// <summary>Answers the question this screen is asking, wherever it sits.</summary>
        /// <remarks>
        /// How a target asks is its own business, the same as naming a system: what a test says is
        /// what somebody typed.
        /// </remarks>
        public static void Typed(this SprocketEditor editor, string said)
        {
            var pick = editor.Current as PickPage;
            if (pick == null || pick.Question.Length == 0)
                throw new InvalidOperationException($"'{editor.Screen.Title()}' asks nothing to be typed.");

            Run(editor.Act(Cmd.Primary, () => pick.Give(said)));
        }

        /// <summary>Writes <paramref name="text" /> into the script page's listing in place of what it holds, as somebody editing it by hand.</summary>
        public static void Scripted(this SprocketEditor editor, string text)
        {
            var page = editor.Current as ScriptPage;
            if (page == null)
                throw new InvalidOperationException($"'{editor.Screen.Title()}' is not the script.");

            _ = page.View;
            page.Listing.Text = text;
        }

        /// <summary>Presses the script page's Apply button.</summary>
        public static void ApplyScript(this SprocketEditor editor)
        {
            var page = editor.Current as ScriptPage;
            if (page == null)
                throw new InvalidOperationException($"'{editor.Screen.Title()}' is not the script.");

            _ = page.View;
            Run(page.Apply.Command);
        }

        /// <summary>Presses one of the ways to start something on this screen, wherever it sits.</summary>
        public static void PressStart(this SprocketEditor editor, string label) => PressButton(editor, label);

        /// <summary>
        /// Switches on the logic system the editor is in, which a new one is not.
        /// </summary>
        /// <remarks>
        /// Building and running are separate acts. A system is switched off while it is written, both
        /// so a half-built one never fires and so its switch is what makes it safe to edit.
        /// </remarks>
        public static void SwitchOn(this SprocketEditor editor) => SwitchSystem(editor, true);

        /// <summary>Switches the logic system the editor is in to <paramref name="on" />, from its own screen or a sequence's, pressing the switch only where it says otherwise.</summary>
        public static void SwitchSystem(this SprocketEditor editor, bool on)
        {
            if (editor.Current is not SequencePage && editor.Current is not LogicSystemPage)
                throw new InvalidOperationException($"'{editor.Screen.Title()}' is not in a logic system.");

            if (editor.Screen.SwitchIsOn != on)
                editor.Screen.Flip!.Execute();
        }

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
                Run(answer.Command);
                return;
            }

            var row = editor.Choices().FirstOrDefault(r => r.Text == text);
            if (row != null)
            {
                row.Command!.Execute();
                return;
            }

            foreach (var pane in editor.Stages())
            {
                var option = pane.Choices().FirstOrDefault(e => e.Text == text);
                if (option != null)
                {
                    option.Command!.Execute();
                    return;
                }

                var entry = pane.Entries().FirstOrDefault(e => e.Text == text);
                if (entry != null)
                {
                    entry.Pick(Held.Alone, 1);
                    return;
                }
            }

            throw new InvalidOperationException($"No choice '{text}' on '{editor.Screen.Title()}'.");
        }

        /// <summary>Presses what a picker's column offers as <paramref name="text" /> twice in quick succession, which chooses it and moves on.</summary>
        public static void DoublePressChoice(this SprocketEditor editor, string text)
        {
            var entry = editor.Stages().SelectMany(pane => pane.Entries()).FirstOrDefault(e => e.Text == text)
                        ?? throw new InvalidOperationException($"No choice '{text}' on '{editor.Screen.Title()}'.");
            entry.Pick(Held.Alone, 2);
        }

        /// <summary>Whether anything on the screen offers <paramref name="text" /> to be chosen: an answer, a choice, or an option or block in a picker.</summary>
        public static bool HasChoice(this SprocketEditor editor, string text) =>
            editor.Screen.Answers.Any(e => e.Text == text)
            || editor.Choices().Any(r => r.Text == text)
            || editor.Stages().Any(p => p.Choices().Any(e => e.Text == text) || p.Entries().Any(e => e.Text == text));

        /// <summary>The answers on a question's page, in the order they are offered, or none where the page asks nothing.</summary>
        /// <remarks>Asks the page for its view first, which is what makes it where nothing has drawn the page yet.</remarks>
        public static IReadOnlyList<ChoiceRow> Choices(this SprocketEditor editor) =>
            Asking(editor)?.Answers ?? new ChoiceRow[0];

        /// <summary>The box an answer is typed into on a question's page, or null where there is none.</summary>
        public static TextBox? TypedBox(this SprocketEditor editor) =>
            Asking(editor)?.Box;

        /// <summary>The question's page the editor is on, its view made, or null.</summary>
        static ChoicePage? Asking(SprocketEditor editor)
        {
            var page = editor.Current as ChoicePage;
            if (page != null)
                _ = page.View;

            return page;
        }

        /// <summary>The rows of a logic system's sequences, as its page draws them, in the order they stand.</summary>
        /// <remarks>Positional because the screen is: a test reading a screen is allowed to know what is on it.</remarks>
        public static List<StatusRow> Rows(this SprocketEditor editor) =>
            Within(LogicSystem(editor).View).OfType<StatusRow>().ToList();

        /// <summary>The blockset whose blocks a blueprint's logic system screen names, as its list says.</summary>
        /// <remarks>The list at the top of the page, above every trigger's sentence.</remarks>
        public static string UsingName(this SprocketEditor editor) =>
            Within(LogicSystem(editor).View).OfType<ListField>().First().Text;

        /// <summary>The sentences of what starts things, as the rows the logic system's page draws them in.</summary>
        public static List<TriggerRow> Triggers(this SprocketEditor editor) =>
            Within(LogicSystem(editor).View).OfType<TriggerRow>().ToList();

        /// <summary>
        /// What a trigger's row reads as, joined the way a target draws it.
        /// </summary>
        /// <remarks>
        /// Out of the same parts and the same words a screen uses, because what is being asserted is
        /// that the parts say the right thing - not that a test can spell a sentence.
        /// </remarks>
        public static string Says(this SprocketEditor editor, int which)
        {
            var trigger = editor.Triggers()[which];
            var line = SentenceText.Lead(trigger.Kind, trigger.Starts.Count);

            foreach (SentenceSlot slot in trigger.Starts)
                line += " " + SentenceText.InTrigger(editor.World, slot);

            return line;
        }

        /// <summary>And what the second half of it reads as, with the words that join its parts.</summary>
        public static string Doing(this SprocketEditor editor, int which)
        {
            var trigger = editor.Triggers()[which];
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
            editor.Triggers()[which].Starts[0].Opens!.Execute();

        /// <summary>And the part that says what it does.</summary>
        public static void PressDoes(this SprocketEditor editor, int which) =>
            editor.Triggers()[which].Does[0].Opens!.Execute();

        /// <summary>The way back on a screen that carries one, which is screen furniture.</summary>
        public static EditorScreen Steps(this SprocketEditor editor) => editor.Screen;

        /// <summary>Opens a row of that list, by where it sits: on the home screen, a logic system by its heading.</summary>
        /// <remarks>
        /// By position rather than by name, because the names on these rows are the target's words and a
        /// test that matched on them would be asserting about English on its way somewhere else.
        /// </remarks>
        public static void PressRow(this SprocketEditor editor, int row)
        {
            var rows = editor.Current is HomePage ? editor.RootHeadings() : editor.Rows();
            rows[row].Opens!.Execute();
        }

        /// <summary>The short question hanging over the screen as it reads, or empty: its blocks a blank line apart, a list's items a line each and led by "- ".</summary>
        public static string Question(this SprocketEditor editor)
        {
            var arguments = editor.Screen.QuestionArguments;
            var question = editor.Screen.Question;
            var document = arguments == null ? Markdown.Parse(question) : Template.Read(question);

            var said = new System.Text.StringBuilder();
            Block last = null;
            foreach (var block in document.Blocks)
            {
                var listed = block.Kind == BlockKind.Bullet;
                if (last != null)
                    said.Append(listed && last.Kind == BlockKind.Bullet ? "\n" : "\n\n");
                if (listed)
                    said.Append("- ");

                foreach (var run in block.Runs)
                    said.Append(run.IsBreak ? "\n" : run.Slot != null ? arguments?.Of(run.Slot) : run.Text);

                last = block;
            }

            return said.ToString();
        }

        /// <summary>Whether the question hanging over the screen offers that answer.</summary>
        public static bool HasAnswer(this SprocketEditor editor, string text) =>
            editor.Screen.Answers.Any(a => a.Text == text);

        /// <summary>Answers the question hanging over the screen.</summary>
        public static void PressAnswer(this SprocketEditor editor, string text) =>
            Run(editor.Screen.Answers.First(a => a.Text == text).Command);

        /// <summary>Presses one of the ways to add a step to the sequence the screen shows.</summary>
        public static void PressAdd(this SprocketEditor editor, string text) => PressButton(editor, text);

        /// <summary>Presses one part of one step of the sequence the screen shows.</summary>
        public static void PressStepSlot(this SprocketEditor editor, int step, int slot) =>
            editor.StepRows()[step].Slots[slot].Opens!.Execute();

        /// <summary>Presses the screen's move-on action - "Next" or "Done".</summary>
        public static void PressPrimary(this SprocketEditor editor)
        {
            if (editor.Screen.Primary == null)
                throw new InvalidOperationException($"'{editor.Screen.Title()}' has nothing to move on to.");

            Run(editor.Screen.Primary);
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
                    Run(editor.Screen.Answers.First().Command);
            }
        }

        /// <summary>Presses one of the actions along the foot of the screen.</summary>
        public static void PressAction(this SprocketEditor editor, string text) =>
            Run(editor.Screen.Actions.First(a => a.Text == text).Command);

        /// <summary>Presses back.</summary>
        public static void Back(this SprocketEditor editor) => Run(editor.Screen.Back);

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

        /// <summary>Whether the part of the heading that takes a name has to have one.</summary>
        public static bool NameRequired(this EditorScreen screen) => screen.Nameable()?.NameRequired ?? false;

        /// <summary>Types a name into the part of the heading that takes one.</summary>
        public static void Name(this SprocketEditor editor, string name) =>
            editor.Screen.Nameable()?.Renames?.Invoke(name);

        /// <summary>Carries out <paramref name="command" />, as pressing what carries it would, where there is one that can be.</summary>
        static void Run(ICommand? command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }

        /// <summary>The columns of any pick on the screen, from the page it is on.</summary>
        static IEnumerable<StageColumn> Stages(this SprocketEditor editor)
        {
            var strip = (editor.Current as PickPage)?.Strip ?? (editor.Current as TimerPage)?.Strip;
            return strip == null ? Enumerable.Empty<StageColumn>() : strip.Stages.Skip(strip.Page);
        }

        /// <summary>The blocks the first column of the pick on the screen offers, as their rows.</summary>
        public static List<PickRow> Offered(this SprocketEditor editor) => editor.Stages().First().Entries().ToList();

        /// <summary>The row saying <paramref name="name" /> in a pick, or an exception naming the screen.</summary>
        static PickRow Row(SprocketEditor editor, string name)
        {
            return editor.Stages().SelectMany(p => p.Entries()).FirstOrDefault(e => e.Text == name)
                   ?? throw new InvalidOperationException($"No block '{name}' on '{editor.Screen.Title()}'.");
        }

        /// <summary>Presses the button saying <paramref name="label" />, after the plus of anything that adds, on the page the editor is on.</summary>
        static void PressButton(SprocketEditor editor, string label)
        {
            var view = editor.Current.View
                       ?? throw new InvalidOperationException($"'{editor.Screen.Title()}' is described in components and has no buttons.");

            var button = Within(view).OfType<Button>().FirstOrDefault(b => b.Text == StatusRow.Plus + label || b.Text == label)
                         ?? throw new InvalidOperationException($"No button '{label}' on '{editor.Screen.Title()}'.");

            button.Command!.Execute();
        }

        /// <summary>The page of the logic system the editor is on.</summary>
        static LogicSystemPage LogicSystem(SprocketEditor editor) =>
            editor.Current as LogicSystemPage ?? throw new InvalidOperationException($"'{editor.Screen.Title()}' is not a logic system's page.");

        /// <summary>The root, which is where the editor is.</summary>
        static HomePage Root(SprocketEditor editor) =>
            editor.Current as HomePage ?? throw new InvalidOperationException("the editor is not on its root");

        /// <summary>Every element under <paramref name="element" />, its own first, in the order they are drawn.</summary>
        public static IEnumerable<Element> Within(this Element element)
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
