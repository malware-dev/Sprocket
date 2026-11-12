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
        // The mod's own names, which the harness's driver uses too (SprocketSession, SprocketEditor, Lumel's Element and Button).
        const string Session = "Sprocket.Host.SpaceEngineers.SprocketSession";

        // How far into a text its first words are rested on, in the editor's units: inside the shortest word a heading opens with.
        const float FirstWords = 24f;

        readonly dynamic _editor;
        object _altKey;

        InGameEditor(object editor)
        {
            _editor = editor;
        }

        /// <summary>The editor of the construct <paramref name="block" /> keeps its automation in, or null where Sprocket has none for it.</summary>
        public static InGameEditor Of(IMyTerminalBlock block)
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

        /// <summary>Goes back to the screen the editor opens on, as its back button does, answering any question back asks with its first answer.</summary>
        public void Home()
        {
            for (var guard = 0; _editor.Screen.CanGoBack && guard < 20; guard++)
            {
                Run(_editor.Screen.Back);
                if (_editor.Screen.HasQuestion)
                {
                    foreach (dynamic answer in _editor.Screen.Answers)
                    {
                        Run(answer.Command);
                        break;
                    }
                }
            }
        }

        static void Run(dynamic command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
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
        /// on, such as <c>Moves.ToLibrary</c>, or else the words of something in the editor, a text named by <c>has:</c> at its
        /// first words. Says whether it found it.
        /// </summary>
        public bool Hover(string target, EditorInput input)
        {
            object found = _editor.Current;
            foreach (var part in target.Split('.'))
                found = Get(found, part);

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
        static object Get(object thing, string name)
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
