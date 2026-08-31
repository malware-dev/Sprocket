using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Input;

// Two interfaces share this name. VRage.Input's is the engine's own and is not whitelisted; the mod-facing
// one is what MyAPIGateway.Input hands back. Saying which removes an ambiguity that otherwise resolves to
// the wrong one and fails only at the whitelist gate.
using IMyInput = VRage.ModAPI.IMyInput;
using Reactor.Host.SpaceEngineers.Presentation.Input;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;
using Reactor.Host.SpaceEngineers.Terminal;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Input
{
    /// <summary>
    /// Types into whichever box the screen says has the keyboard.
    /// </summary>
    /// <remarks>
    /// This replaced a text editor that owned its box: it was handed one, opened an edit on it, remembered
    /// the original text and put it back on cancel. That shape belonged to an overlay where exactly one
    /// thing could be edited at a time. A screen has focus of its own, and <c>EditorView</c> already decides
    /// what has the keyboard - a question while one is open, otherwise whatever was last focused - so this
    /// is handed the answer rather than keeping one.
    /// <para>
    /// Characters come from the game's own text buffer, which has already dealt with the keyboard layout,
    /// dead keys and modifiers, so what arrives is what the player meant to type. Everything else is read as
    /// keys, because a caret movement is not a character.
    /// </para>
    /// <para>
    /// <b>Escape is deliberately not here.</b> The screen owns it: the game routes it through
    /// <c>OnCancelling</c>, which is where a question gets to refuse the close and dismiss itself instead.
    /// Reading it here as well would act on it twice.
    /// </para>
    /// </remarks>
    public sealed class ScreenKeyboard
    {
        readonly KeyRepeater _repeat = new KeyRepeater();

        /// <summary>Forgets any key held down, for when the screen opens or the focus moves.</summary>
        public void Clear()
        {
            _repeat.Clear();
        }

        /// <summary>
        /// Reads one frame of the keyboard into <paramref name="box" />.
        /// </summary>
        /// <remarks>
        /// Call only while the screen has focus. There is no sweep here watching for the world to move on
        /// underneath an edit, which the overlay needed and which would now be actively wrong: it abandoned
        /// on the mouse cursor becoming visible, and on a screen the cursor is always visible.
        /// </remarks>
        public KeyboardOutcome HandleInput(TextBox box)
        {
            KeyboardOutcome outcome = new KeyboardOutcome();

            if (box == null || MyAPIGateway.Input == null)
                return outcome;

            IMyInput input = MyAPIGateway.Input;
            bool shift = input.IsAnyShiftKeyPressed();

            if (input.IsAnyCtrlKeyPressed())
            {
                // Handled before characters, and instead of them: a keyboard usually suppresses the letter
                // under Ctrl, but taking both would insert a 'v' into whatever was just pasted.
                if (input.IsNewKeyPressed(MyKeys.A))
                {
                    box.SelectAll();
                    outcome.Changed = true;
                }
                else if (input.IsNewKeyPressed(MyKeys.C))
                {
                    Clipboard.Set(box.SelectedText());
                }
                else if (input.IsNewKeyPressed(MyKeys.X))
                {
                    Clipboard.Set(box.SelectedText());
                    box.Backspace();
                    outcome.Changed = true;
                }
                else if (input.IsNewKeyPressed(MyKeys.V))
                {
                    box.Insert(Clipboard.Get());
                    outcome.Changed = true;
                }
            }
            else
            {
                ListReader<char> typed = input.TextInput;
                for (int i = 0; i < typed.Count; i++)
                {
                    char c = typed[i];
                    if (c < 32 || c == 127)
                        continue;

                    box.InsertChar(c);
                    outcome.Changed = true;
                }
            }

            if (input.IsNewKeyPressed(MyKeys.Enter))
            {
                outcome.Commit = true;
                return outcome;
            }

            int now = Tick();

            if (Fires(input, MyKeys.Back, now)) { box.Backspace(); outcome.Changed = true; }
            if (Fires(input, MyKeys.Delete, now)) { box.DeleteForward(); outcome.Changed = true; }
            if (Fires(input, MyKeys.Left, now)) { box.MoveCaretLeft(shift); outcome.Changed = true; }
            if (Fires(input, MyKeys.Right, now)) { box.MoveCaretRight(shift); outcome.Changed = true; }
            if (Fires(input, MyKeys.Home, now)) { box.MoveCaretHome(shift); outcome.Changed = true; }
            if (Fires(input, MyKeys.End, now)) { box.MoveCaretEnd(shift); outcome.Changed = true; }

            return outcome;
        }

        bool Fires(IMyInput input, MyKeys key, int now)
        {
            return _repeat.Fires((int)key, input.IsNewKeyPressed(key), input.IsKeyPress(key), now);
        }

        static int Tick()
        {
            return MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;
        }
    }
}
