using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Input;

// Two interfaces share this name. VRage.Input's is the engine's own and is not whitelisted; the mod-facing
// one is what MyAPIGateway.Input hands back. Saying which removes an ambiguity that otherwise resolves to
// the wrong one and fails only at the whitelist gate.
using IMyInput = VRage.ModAPI.IMyInput;

namespace Phosphor
{
    /// <summary>
    /// Drives one <see cref="TextBox" /> from the keyboard while an edit is open.
    /// <para>
    /// Characters come from the game's own text buffer, which has already dealt with the keyboard layout,
    /// dead keys and modifiers, so what arrives is what the player meant to type. Everything else - the
    /// caret, deleting, committing - is read raw, because those are keys rather than characters and because
    /// raw reads still work while the game's controls are suspended.
    /// </para>
    /// <para>
    /// Escape is deliberately not handled. Pressing it opens the game's menu, and the cancel sweep notices
    /// the cursor and abandons the edit, so handling it here would fight the game for a key it already owns.
    /// </para>
    /// </summary>
    public sealed class TextEditor
    {
        private const int BlinkTicks = 30;

        private readonly InputSuspension _suspension;
        private readonly KeyRepeater _repeat = new KeyRepeater();

        /// <summary>
        /// Whether aiming is suspended along with everything else. A property of how this target is pointed
        /// at rather than of any one edit, so it is settled once.
        /// </summary>
        private readonly bool _suspendLook;

        private TextBox _box;
        private string _original;

        public TextEditor(InputSuspension suspension, bool suspendLook)
        {
            _suspension = suspension;
            _suspendLook = suspendLook;
        }

        public bool IsEditing
        {
            get { return _box != null; }
        }

        public TextBox Editing
        {
            get { return _box; }
        }

        /// <summary>
        /// Opens an edit on <paramref name="box" />, remembering the text so cancelling can put it back.
        /// </summary>
        public void Begin(TextBox box)
        {
            if (box == null || _box != null)
                return;

            _box = box;
            _original = box.Text;

            box.IsEditing = true;
            box.CaretVisible = true;
            box.SelectAll();

            _repeat.Clear();
            _suspension.Begin(_suspendLook);
        }

        /// <summary>Accepts the text and tells anybody listening.</summary>
        public void Commit()
        {
            if (_box == null)
                return;

            TextBox box = _box;
            Close();
            box.RaiseCommitted();
        }

        /// <summary>Puts the text back as it was and closes without telling anybody.</summary>
        public void Cancel()
        {
            if (_box == null)
                return;

            _box.Text = _original;
            Close();
        }

        /// <summary>
        /// Reads the keyboard. Call from the session's input handling, which the game only runs on a client.
        /// </summary>
        public void HandleInput()
        {
            if (_box == null || MyAPIGateway.Input == null)
                return;

            if (ShouldAbandon())
            {
                Cancel();
                return;
            }

            IMyInput input = MyAPIGateway.Input;
            bool shift = input.IsAnyShiftKeyPressed();

            if (input.IsAnyCtrlKeyPressed())
            {
                // Handled before characters, and instead of them: a keyboard usually suppresses the letter
                // under Ctrl, but taking both would insert a 'v' into whatever was just pasted.
                if (input.IsNewKeyPressed(MyKeys.A))
                    _box.SelectAll();
                else if (input.IsNewKeyPressed(MyKeys.C))
                    Clipboard.Set(_box.SelectedText());
                else if (input.IsNewKeyPressed(MyKeys.X))
                {
                    Clipboard.Set(_box.SelectedText());
                    _box.Backspace();
                }
                else if (input.IsNewKeyPressed(MyKeys.V))
                    _box.Insert(Clipboard.Get());
            }
            else
            {
                ListReader<char> typed = input.TextInput;
                for (int i = 0; i < typed.Count; i++)
                {
                    char c = typed[i];
                    if (c < 32 || c == 127)
                        continue;

                    _box.InsertChar(c);
                }
            }

            if (input.IsNewKeyPressed(MyKeys.Enter))
            {
                Commit();
                return;
            }

            int now = Tick();

            if (Fires(input, MyKeys.Back, now)) _box.Backspace();
            if (Fires(input, MyKeys.Delete, now)) _box.DeleteForward();
            if (Fires(input, MyKeys.Left, now)) _box.MoveCaretLeft(shift);
            if (Fires(input, MyKeys.Right, now)) _box.MoveCaretRight(shift);
            if (Fires(input, MyKeys.Home, now)) _box.MoveCaretHome(shift);
            if (Fires(input, MyKeys.End, now)) _box.MoveCaretEnd(shift);
        }

        /// <summary>Call every tick, so the caret blinks and the suspension keeps being asked for.</summary>
        public void Update()
        {
            _suspension.Update();

            if (_box == null)
                return;

            _box.CaretVisible = Tick() / BlinkTicks % 2 == 0;
        }

        /// <summary>
        /// Whether the world has moved on in a way that means this edit cannot honestly continue - a menu or
        /// the chat opening, or the player no longer being there. Cheap enough to ask every tick, and the
        /// alternative is an edit that quietly stays open behind whatever the player is now doing.
        /// </summary>
        private static bool ShouldAbandon()
        {
            if (MyAPIGateway.Gui == null)
                return true;

            return MyAPIGateway.Gui.IsCursorVisible
                || MyAPIGateway.Gui.ChatEntryVisible
                || MyAPIGateway.Session == null
                || MyAPIGateway.Session.LocalHumanPlayer == null;
        }

        private bool Fires(IMyInput input, MyKeys key, int now)
        {
            return _repeat.Fires((int)key, input.IsNewKeyPressed(key), input.IsKeyPress(key), now);
        }

        private void Close()
        {
            _box.IsEditing = false;
            _box = null;
            _original = null;

            _repeat.Clear();
            _suspension.End();
        }

        private static int Tick()
        {
            return MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;
        }
    }
}
