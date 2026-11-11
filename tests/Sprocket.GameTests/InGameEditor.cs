using System;
using System.Collections.Generic;
using System.Reflection;
using Sandbox;
using Sandbox.ModAPI;
using VRage.Input;
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

        readonly dynamic _editor;

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
        /// summary, which it opens, or a tab by its header. Says whether there was one.
        /// </summary>
        public bool Press(string label)
        {
            foreach (var element in Within(Root()))
            {
                var said = Get(element, "AutomationId") as string ?? Get(element, "Text") as string ?? Get(element, "Summary") as string ?? Get(element, "Header") as string;
                if (said != label && said != "+ " + label)
                    continue;

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
        /// Rests the game's mouse on the middle of what <paramref name="target" /> names: a property path from the page the editor is
        /// on, such as <c>Moves.ToLibrary</c>, or else the words of something in the editor. Says whether it found it.
        /// </summary>
        public bool Hover(string target)
        {
            object found = _editor.Current;
            foreach (var part in target.Split('.'))
                found = Get(found, part);

            if (found == null)
            {
                foreach (var element in Within(Root()))
                {
                    var said = Get(element, "AutomationId") as string ?? Get(element, "Text") as string ?? Get(element, "Summary") as string ?? Get(element, "Header") as string;
                    if (said == target)
                    {
                        found = element;
                        break;
                    }
                }
            }

            if (found == null)
                return false;

            // The editor lays out in units of a 1080-high screen, over the safe area the game keeps: the window's full height,
            // four by three wide and centred (GameScreen, Lumel's Sim/Surface).
            dynamic hovered = found;
            RectangleF bounds = hovered.Bounds;
            Vector2 middle = hovered.ToScreen(new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f));
            var window = MySandboxGame.ScreenSize;
            var scale = window.Y / 1080f;
            var left = (window.X - Math.Min(window.X, window.Y * 4f / 3f)) / 2f;
            MyInput.Static.SetMousePosition((int)(left + middle.X * scale), (int)(middle.Y * scale));
            return true;
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
                    said.Add(Get(element, "AutomationId") as string ?? Get(element, "Text") as string ?? Get(element, "Summary") as string ?? Get(element, "Header") as string ?? "");
            }

            return string.Join(", ", said);
        }

        /// <summary>The top of the editor's elements, its frame, from the page it is on.</summary>
        object Root()
        {
            object element = _editor.Current?.View;
            for (var parent = element; parent != null; parent = Get(parent, "Parent"))
                element = parent;

            return element;
        }

        /// <summary>The public property <paramref name="name" /> of <paramref name="thing" />, or null where it has none.</summary>
        static object Get(object thing, string name)
        {
            return thing?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(thing);
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
