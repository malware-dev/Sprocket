using System;
using System.Reflection;
using Sandbox.Graphics.GUI;
using VRage.Input;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The pointer and keys of the editor's screen, driven by a test: the pointer held where the test rests it, pressed there, and
    /// keys pressed, so the editor finds what the test points at with the game window out of focus or the PC locked, and Windows'
    /// own mouse and keyboard are left alone.
    /// </summary>
    /// <remarks>
    /// Out of focus the game clears its input on every frame and the editor reads none of it (Lumel's GameInput.IsCleared), and in
    /// focus the editor takes its pointer from Windows' mouse in the screen's input, which the game runs before it updates a
    /// plugin, so <see cref="Hold" /> called on every plugin update is the pointer the editor draws and opens tips by.
    /// </remarks>
    public sealed class EditorInput
    {
        // Lumel's screen, which keeps its workspace in a private field, the field of the game's host screen holding the mod's
        // screen it shows, and Lumel's pointer buttons; the mod's assembly is the game's own compile, so each is found by name.
        const string Screen = "Lumel.Screens.GameScreen";
        const string View = "_view";
        const string Owner = "m_owner";
        const string Button = "Lumel.Input.PointerButton";

        Vector2? _at;

        /// <summary>Rests the pointer at <paramref name="at" />, in the editor's units.</summary>
        public void Rest(Vector2 at)
        {
            _at = at;
        }

        /// <summary>Rests the pointer outside the editor, on nothing.</summary>
        public void Park()
        {
            _at = new Vector2(float.NegativeInfinity);
        }

        /// <summary>Writes the rested pointer into the editor's screen, where one is open and a pointer has been given.</summary>
        /// <remarks>Called on every plugin update, sixty a second on a client.</remarks>
        public void Hold()
        {
            if (!_at.HasValue)
                return;

            dynamic workspace = Workspace();
            if (workspace != null)
                workspace.Pointer = _at.Value;
        }

        /// <summary>Presses and releases the left button where the pointer rests, as a click does, and says whether an editor was open to take it.</summary>
        public bool Click()
        {
            dynamic workspace = Workspace();
            if (workspace == null || !_at.HasValue)
                return false;

            var left = Enum.Parse(((object)workspace).GetType().Assembly.GetType(Button, true), "Left");
            workspace.PointerDown(_at.Value, (dynamic)left, 1, false, false);
            workspace.PointerUp(_at.Value, (dynamic)left);
            return true;
        }

        /// <summary>Presses <paramref name="key" /> in the editor, with neither Shift nor Ctrl, and says whether an editor was open to take it.</summary>
        public bool Press(MyKeys key)
        {
            dynamic workspace = Workspace();
            if (workspace == null)
                return false;

            workspace.Key(key, false, false);
            return true;
        }

        /// <summary>Says where the pointer is held, what of the editor is under it and the tip open there, for a test's report.</summary>
        public string Describe()
        {
            dynamic workspace = Workspace();
            if (workspace == null)
                return $"no editor screen among {string.Join(", ", System.Linq.Enumerable.Select(MyScreenManager.Screens, screen => screen.GetType().FullName))}";

            object over = workspace.PointerOver;
            object tip = workspace.ShownTip;
            return $"pointer {(Vector2)workspace.Pointer}, over {over?.GetType().Name ?? "nothing"}, tip {tip?.GetType().Name ?? "none"}";
        }

        /// <summary>The workspace of the editor's screen, or null where none is open.</summary>
        static object Workspace()
        {
            foreach (var host in MyScreenManager.Screens)
            {
                var screen = host.GetType().GetField(Owner, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(host);
                for (var type = screen?.GetType(); type != null; type = type.BaseType)
                {
                    if (type.FullName == Screen)
                        return type.GetField(View, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(screen);
                }
            }

            return null;
        }
    }
}
