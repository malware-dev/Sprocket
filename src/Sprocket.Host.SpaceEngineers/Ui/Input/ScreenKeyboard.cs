using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Input;
// VRage.Input also declares an IMyInput, the engine's own, which the whitelist rejects. MyAPIGateway.Input
// returns this one.
using IMyInput = VRage.ModAPI.IMyInput;

namespace Sprocket.Host.SpaceEngineers.Ui.Input
{
    /// <summary>Types into whichever box the screen says has the keyboard.</summary>
    /// <remarks>
    ///     Characters come from the game's text buffer, which has already applied the keyboard layout, dead
    ///     keys and modifiers.
    ///     <para>
    ///         Escape must not be read here: the game routes it to the screen's <c>OnCancelling</c>, and
    ///         reading it here as well would act on it twice.
    ///     </para>
    /// </remarks>
    public sealed class ScreenKeyboard
    {
        readonly KeyRepeater _repeat = new KeyRepeater();

        /// <summary>Forgets any key held down, for when the screen opens or the focus moves.</summary>
        public void Clear()
        {
            _repeat.Clear();
        }

        /// <summary>Reads one frame of the keyboard into <paramref name="box" />.</summary>
        /// <remarks>Call only while the screen has focus.</remarks>
        public KeyboardOutcome HandleInput(TextBox box)
        {
            var outcome = new KeyboardOutcome();

            if (box == null || MyAPIGateway.Input == null)
                return outcome;

            var input = MyAPIGateway.Input;
            var shift = input.IsAnyShiftKeyPressed();

            if (input.IsAnyCtrlKeyPressed())
            {
                // Instead of the typed characters, which can still carry the letter under Ctrl.
                if (input.IsNewKeyPressed(MyKeys.A))
                {
                    box.SelectAll();
                    outcome.Changed = true;
                }
                else if (input.IsNewKeyPressed(MyKeys.C))
                    Clipboard.Set(box.SelectedText());
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
                var typed = input.TextInput;
                for (var i = 0; i < typed.Count; i++)
                {
                    var c = typed[i];
                    if (c < 32 || c == 127)
                        continue;

                    box.InsertChar(c);
                    outcome.Changed = true;
                }
            }

            if (input.IsNewKeyPressed(MyKeys.Tab))
            {
                outcome.Onward = true;
                return outcome;
            }

            if (input.IsNewKeyPressed(MyKeys.Enter))
            {
                outcome.Commit = true;
                return outcome;
            }

            var now = Tick();

            if (Fires(input, MyKeys.Back, now))
            {
                box.Backspace();
                outcome.Changed = true;
            }

            if (Fires(input, MyKeys.Delete, now))
            {
                box.DeleteForward();
                outcome.Changed = true;
            }

            if (Fires(input, MyKeys.Left, now))
            {
                box.MoveCaretLeft(shift);
                outcome.Changed = true;
            }

            if (Fires(input, MyKeys.Right, now))
            {
                box.MoveCaretRight(shift);
                outcome.Changed = true;
            }

            if (Fires(input, MyKeys.Home, now))
            {
                box.MoveCaretHome(shift);
                outcome.Changed = true;
            }

            if (Fires(input, MyKeys.End, now))
            {
                box.MoveCaretEnd(shift);
                outcome.Changed = true;
            }

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