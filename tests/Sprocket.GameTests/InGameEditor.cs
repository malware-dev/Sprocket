using System;
using System.Collections.Generic;
using System.Reflection;
using Sandbox.ModAPI;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Sprocket's editor as it runs in the game, pressed by the words on its buttons as the harness's driver presses them
    /// (Driving/Target): found through the session that keeps it, and reached by name, since the game compiles the mod into an
    /// assembly of its own whose types no plugin can name.
    /// </summary>
    public sealed class InGameEditor
    {
        // The mod's own names, which the harness's driver uses too (SprocketSession, SprocketEditor, Lumel's Element and Button, PlusMenu).
        const string Session = "Sprocket.Host.SpaceEngineers.SprocketSession";
        const string PlusMenu = "PlusMenu";

        // How far into a text its first words are rested on, in the editor's units: inside the shortest word a heading opens with.
        const float FirstWords = 24f;

        readonly dynamic _editor;
        object _altKey;

        InGameEditor(object editor)
        {
            _editor = editor;
        }

        /// <summary>The editor of the construct <paramref name="block" /> keeps its automation in, or null where Sprocket has none for it.</summary>
        public static InGameEditor? Of(IMyTerminalBlock block)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var session = assembly.GetType(Session, false);
                var adopt = session?.GetMethod("Adopt", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (adopt == null)
                    continue;

                dynamic unit = adopt.Invoke(null, new object[] { block });
                return unit == null ? null : new InGameEditor(unit.Editor);
            }

            return null;
        }

        /// <summary>The editor of the copy the server lent this client, while it is open, or null where none is.</summary>
        /// <remarks>A client's session keeps the copy it shows in a private field, <c>_copy</c>, of the session in its <c>_current</c>.</remarks>
        public static InGameEditor? Lent()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var session = assembly.GetType(Session, false);
                var current = session?.GetField("_current", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                if (current == null)
                    continue;

                dynamic copy = current.GetType().GetField("_copy", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(current);
                return copy == null ? null : new InGameEditor(copy.Editor);
            }

            return null;
        }

        /// <summary>
        /// Switches the logic system called <paramref name="name" /> <paramref name="on" />, as its row's switch does, answering
        /// the question a blueprint asks before it is switched off with its last answer, and says whether there was one.
        /// </summary>
        public bool SwitchSystem(string name, bool on)
        {
            foreach (dynamic system in _editor.Program.Systems)
            {
                if (system.Name != name)
                    continue;

                _editor.SwitchSystem(system, on);
                Answer();
                return true;
            }

            return false;
        }

        /// <summary>Runs the subject of the page the editor is on, as its play button does, and says whether it could be run.</summary>
        public bool Run()
        {
            dynamic runs = _editor.Screen.Runs;
            if (runs == null || !runs.CanExecute)
                return false;

            runs.Execute();
            return true;
        }

        /// <summary>Whether a run of the first sequence met something it could not do and switched its first installation off, saying why.</summary>
        public bool Stopped()
        {
            dynamic installation = _editor.Program.Sequences[0].System?.First;
            return installation != null && !(bool)installation.Enabled && installation.Halt != null;
        }

        /// <summary>How the first sequence's last run stands, for a test's report.</summary>
        public string LastRun()
        {
            dynamic run = _editor.Runner.Get(_editor.Program.Sequences[0]);
            if (run == null)
                return "no run";

            dynamic fault = run.Faults.Fault;
            dynamic installation = _editor.Program.Sequences[0].System.First;
            return $"running {run.IsRunning}, fault {fault.Problem} on {fault.Which} at step {fault.Step}, installation {(installation.Enabled ? "on" : "off")}{(installation.Halt == null ? "" : " with a reason kept")}";
        }

        /// <summary>Whether the logic system called <paramref name="name" /> is switched on, or null where the editor has none by that name.</summary>
        public bool? IsOn(string name)
        {
            foreach (dynamic system in _editor.Program.Systems)
            {
                if (system.Name == name)
                    return system.Enabled;
            }

            return null;
        }

        /// <summary>
        /// Goes back to the screen the editor opens on, as its back button does, answering any question back asks with its last
        /// answer, which discards what the page holds and always goes back; keeping it stays on a page whose values are not all given.
        /// </summary>
        public void Home()
        {
            for (var guard = 0; _editor.Screen.CanGoBack && guard < 20; guard++)
            {
                Run(_editor.Screen.Back);
                if (_editor.Screen.HasQuestion)
                {
                    dynamic last = null;
                    foreach (dynamic answer in _editor.Screen.Answers)
                        last = answer;
                    if (last != null)
                        Run(last.Command);
                }
            }
        }

        static void Run(dynamic command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }

        /// <summary>
        /// Presses the first shown element of the type called <paramref name="type" /> on the page, such as a <c>PlusMenu</c>,
        /// which hangs its menu open, or a <c>ListField</c>, which opens its list, and says whether there was one to press.
        /// </summary>
        public bool Open(string type)
        {
            foreach (var element in Within(Root()))
            {
                if (element.GetType().Name != type || !(Get(element, "IsVisible") is bool shown && shown))
                    continue;

                dynamic command = Get(element, "Command");
                if (command == null || !command.CanExecute)
                    continue;

                command.Execute();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds what the entry saying <paramref name="text" /> in a plus menu on the page adds, such as <c>task</c> under a sequence's
        /// steps, as choosing it in the open menu does, or presses the add button saying it where no menu offers it. Says whether
        /// either was there.
        /// </summary>
        /// <remarks>The menu opens in a window of its own, out of the page's elements, so its entry is taken from what the menu offers.</remarks>
        public bool Add(string text)
        {
            foreach (var element in Within(Root()))
            {
                if (element.GetType().Name != PlusMenu)
                    continue;

                foreach (dynamic choice in ((dynamic)element).Choices)
                {
                    if (choice.Text == text && choice.Adds != null)
                    {
                        choice.Adds.Execute();
                        return true;
                    }
                }
            }

            // A list with one kind of thing to add has a button of its own, "+" and the word.
            return Press(text);
        }

        /// <summary>
        /// Presses what says <paramref name="label" /> anywhere in the editor, its frame included: a button by its words or name, a row by its
        /// summary, which it opens, or a tab by its header; written <c>last:</c> and its words, the last of several that say them. Says
        /// whether there was one.
        /// </summary>
        public bool Press(string label)
        {
            var last = label.StartsWith("last:");
            var found = new List<object>();
            foreach (var element in Within(Root()))
            {
                if (Matches(element, last ? label.Substring(5) : label))
                    found.Add(element);
            }

            if (last)
                found.Reverse();

            foreach (var element in found)
            {

                // A tab already showing is where pressing it leads.
                if (Get(element, "Header") != null && Get(element, "IsSelected") is bool selected && selected)
                    return true;

                dynamic command = Get(element, "Command") ?? Get(element, "Opens");
                if (command == null || !command.CanExecute)
                    continue;

                command.Execute();
                return true;
            }

            return false;
        }

        /// <summary>Switches the logic system the editor is in <paramref name="on" />, by the switch at the top of its screen, and says whether there was one.</summary>
        public bool Switch(bool on)
        {
            var screen = _editor.Screen;
            if (Get(screen, "Flip") == null)
                return false;

            if ((bool)screen.SwitchIsOn != on)
                Run(screen.Flip);

            // Switching a running system off asks first.
            Answer();

            return true;
        }

        /// <summary>
        /// Rests <paramref name="input" /> on the middle of what <paramref name="target" /> names: a property path from the page the editor is
        /// on, such as <c>Moves.ToLibrary</c>, the element of a type by its place among them, such as <c>is:DragHandle#3</c> for the
        /// third step's grip, or else the words of something in the editor, a text named by <c>has:</c> at its first words. Says
        /// whether it found it.
        /// </summary>
        public bool Hover(string target, EditorInput input)
        {
            object found = target.StartsWith("is:") ? Nth(target.Substring(3)) : _editor.Current;
            if (!target.StartsWith("is:"))
            {
                foreach (var part in target.Split('.'))
                    found = Get(found, part);
            }

            if (found == null)
            {
                foreach (var element in Within(Root()))
                {
                    if (Matches(element, target))
                    {
                        found = element;
                        break;
                    }
                }
            }

            if (found == null)
                return false;

            dynamic hovered = found;
            RectangleF bounds = hovered.Bounds;
            var across = target.StartsWith("has:") ? Math.Min(bounds.Width / 2f, FirstWords) : bounds.Width / 2f;
            Vector2 middle = hovered.ToScreen(new Vector2(bounds.X + across, bounds.Y + bounds.Height / 2f));
            input.Rest(middle);
            return true;
        }

        /// <summary>
        /// Opens the codex page called <paramref name="id" />, such as <c>Term.Branch</c>, in the tip open where <paramref name="input" />
        /// rests, as a link in it would. Says whether a tip was open and the codex has the page.
        /// </summary>
        public bool Page(string id, EditorInput input)
        {
            dynamic tip = input.ShownTip();
            object page = _editor.Codex.Find(id);
            if (tip == null || page == null || tip.GetType().GetMethod("Navigate") == null)
                return false;

            tip.Navigate((dynamic)page);
            return true;
        }

        /// <summary>
        /// Holds Alt down for the editor's tips while <paramref name="held" /> holds, so a tip that opens stays pinned, or hands the
        /// tips back to the game's own key. Says whether the editor has tips.
        /// </summary>
        public bool HoldAlt(bool held)
        {
            foreach (var element in Within(Root()))
            {
                var codex = Get(element, "Codex");
                var key = codex?.GetType().GetProperty("FnIsPinKeyHeld");
                if (key == null)
                    continue;

                if (_altKey == null)
                    _altKey = key.GetValue(codex);

                key.SetValue(codex, held ? (Func<bool>)(() => true) : _altKey);
                return true;
            }

            return false;
        }

        /// <summary>Answers the question the editor asks with the answer that goes ahead, its last, and says whether one was asked.</summary>
        public bool Answer()
        {
            var screen = _editor.Screen;
            if (!(bool)screen.HasQuestion)
                return false;

            dynamic last = null;
            foreach (dynamic answer in screen.Answers)
                last = answer;

            if (last != null)
                Run(last.Command);

            return true;
        }

        /// <summary>The glyph the mod's <c>Symbols</c> names <paramref name="name" />, such as <c>Share</c>, or null where it has none.</summary>
        public static string Symbol(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var symbols = assembly.GetType("Sprocket.Host.SpaceEngineers.Framework.Ui.Look.Symbols", false);
                var field = symbols?.GetField(name, BindingFlags.Static | BindingFlags.Public);
                if (field != null)
                    return (string)field.GetRawConstantValue();
            }

            return null;
        }

        /// <summary>The page the editor is on.</summary>
        public dynamic Current => _editor.Current;

        /// <summary>The editor's texts, in the language the game is in.</summary>
        public dynamic Texts => _editor.Texts;

        /// <summary>The program the editor shows.</summary>
        public dynamic Program => _editor.Program;

        /// <summary>The library of blueprints, or null where the editor has none.</summary>
        public dynamic Shelf => _editor.Shelf;

        /// <summary>Whether the screen the editor is on has one to go back to, which is false at home.</summary>
        public bool CanGoBack => (bool)_editor.Screen.CanGoBack;

        /// <summary>The id of the callout hanging now, or null where none is.</summary>
        public string ShownCallout()
        {
            dynamic shown = _editor.Callouts.Shown;
            return shown == null ? null : (string)shown.Id;
        }

        /// <summary>What the callout hanging now says, or null where none is.</summary>
        public string ShownCalloutSaid()
        {
            dynamic shown = _editor.Callouts.Shown;
            return shown == null ? null : (string)((Delegate)shown.Said).DynamicInvoke((object)_editor.Texts);
        }

        /// <summary>The ids of the callouts due in this editor's session, in the order they are offered.</summary>
        public List<string> DueCallouts()
        {
            var due = new List<string>();
            foreach (dynamic callout in _editor.Callouts.Due)
                due.Add((string)callout.Id);
            return due;
        }

        /// <summary>Whether the player has dismissed the callout <paramref name="id" />.</summary>
        public bool CalloutDismissed(string id)
        {
            return (bool)_editor.Callouts.Choices.HasDismissed(id);
        }

        /// <summary>The number of the release the player has dismissed every callout of, or null where there is none.</summary>
        public int? SeenRelease()
        {
            dynamic seen = _editor.Callouts.Choices.Seen;
            return seen == null ? (int?)null : (int)seen;
        }

        /// <summary>Answers the question the editor asks with the answer saying <paramref name="text" />, and says whether there was one.</summary>
        public bool Answer(string text)
        {
            if (!(bool)_editor.Screen.HasQuestion)
                return false;

            foreach (dynamic answer in _editor.Screen.Answers)
            {
                if ((string)answer.Text != text)
                    continue;

                Run(answer.Command);
                return true;
            }

            return false;
        }

        /// <summary>The words of the answers the editor offers now.</summary>
        public string Answers()
        {
            return string.Join(", ", AnswerTexts());
        }

        /// <summary>The words of each answer the editor's question offers, in order, whole, since an answer's words may hold a comma.</summary>
        public List<string> AnswerTexts()
        {
            var said = new List<string>();
            foreach (dynamic answer in _editor.Screen.Answers)
                said.Add((string)answer.Text);
            return said;
        }

        /// <summary>Presses the screen's primary button, as Done does, and says whether it could be pressed.</summary>
        public bool Done()
        {
            dynamic primary = _editor.Screen.Primary;
            if (primary == null || !primary.CanExecute)
                return false;

            primary.Execute();
            return true;
        }

        /// <summary>Whether the tab headed <paramref name="header" /> is showing on the page.</summary>
        public bool IsTabShown(string header)
        {
            foreach (var element in Within(Root()))
            {
                if (Get(element, "Header") as string == header && Get(element, "IsSelected") is bool selected)
                    return selected;
            }

            return false;
        }

        /// <summary>The words on everything in the editor that can be pressed, for a report where a press finds none.</summary>
        public string Buttons()
        {
            var said = new List<string>();
            foreach (var element in Within(Root()))
            {
                if ((Get(element, "Command") ?? Get(element, "Opens")) != null)
                    said.Add(element.GetType().Name + ":" + (Get(element, "AutomationId") as string ?? Get(element, "Text") as string ?? Get(element, "Summary") as string ?? Get(element, "Header") as string ?? ""));
            }

            return string.Join(", ", said);
        }

        /// <summary>
        /// Whether <paramref name="element" /> is what <paramref name="label" /> names: its words, its name, its summary or its
        /// header, after the plus of anything that adds; written <c>tip:</c> and the words its tip starts with, its tip; or, written
        /// <c>has:</c> and words its text holds, a text, such as a heading holding a term. Only what is showing matches.
        /// </summary>
        static bool Matches(object element, string label)
        {
            if (Get(element, "IsVisible") is bool visible && !visible)
                return false;

            if (label.StartsWith("has:"))
            {
                var markdown = Get(element, "Markdown") as string;
                return markdown != null && markdown.Contains(label.Substring(4));
            }

            if (label.StartsWith("tip:"))
            {
                var tip = Get(Get(element, "Tip"), "Text") as string;
                return tip != null && tip.StartsWith(label.Substring(4));
            }

            var said = Get(element, "AutomationId") as string ?? Get(element, "Text") as string ?? Get(element, "Summary") as string
                ?? Get(element, "Header") as string;
            return said == label || said == "+ " + label;
        }

        /// <summary>
        /// The visible element of the type <paramref name="kind" /> names, such as <c>DragHandle#3</c>, the first where no place is
        /// given and the last for <c>#last</c>; or null.
        /// </summary>
        object Nth(string kind)
        {
            var hash = kind.IndexOf('#');
            var type = hash < 0 ? kind : kind.Substring(0, hash);
            var last = hash >= 0 && kind.Substring(hash + 1) == "last";
            var place = hash < 0 || last ? 1 : int.Parse(kind.Substring(hash + 1));
            object found = null;
            foreach (var element in Within(Root()))
            {
                if (element.GetType().Name != type || Get(element, "IsVisible") is bool visible && !visible)
                    continue;

                found = element;
                if (!last && --place == 0)
                    return element;
            }

            return last ? found : null;
        }

        /// <summary>The top of the editor's elements, its frame, from the page it is on.</summary>
        object Root()
        {
            object element = _editor.Current?.View;
            for (var parent = element; parent != null; parent = Get(parent, "Parent"))
                element = parent;

            return element;
        }

        /// <summary>
        /// The public property <paramref name="name" /> of <paramref name="thing" />, the one its own type declares where it hides one
        /// of its base's, such as a slot chip's words over its button's text; or null where it has none.
        /// </summary>
        public static object? Get(object? thing, string name)
        {
            for (var type = thing?.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (property != null)
                    return property.GetValue(thing);
            }

            return null;
        }

        static IEnumerable<object> Within(dynamic element)
        {
            if (element == null)
                yield break;

            yield return element;

            int count = element.VisualChildCount;
            for (var i = 0; i < count; i++)
            {
                foreach (var inner in Within(element.GetVisualChild(i)))
                    yield return inner;
            }
        }
    }
}
