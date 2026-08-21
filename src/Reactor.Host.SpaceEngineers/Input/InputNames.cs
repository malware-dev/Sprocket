using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Input;
using VRage.ModAPI;

namespace Phosphor
{
    /// <summary>
    /// Works out which game controls to suspend while somebody is typing.
    /// <para>
    /// Keyed by raw keys and mouse buttons rather than by control names, so a rebound key is still found: if
    /// forward has been moved to Z, pressing Z still resolves to the forward control and still gets
    /// suspended. A key bound to nothing resolves to nothing and is skipped, which is why typing an ordinary
    /// letter does not need the letter itself to be listed anywhere.
    /// </para>
    /// <para>
    /// <b>Client only.</b> It reads the local key bindings, which a dedicated server does not have. The
    /// resolved list is shipped to the server, which is the only place a blacklist can actually be applied.
    /// </para>
    /// </summary>
    public static class InputNames
    {
        /// <summary>
        /// The controls that aim. Whether these are suspended is the caller's decision and depends entirely
        /// on how the UI is pointed at: a mouse pointer needs them gone, because the movement that would turn
        /// the player's head is the movement that moves the pointer. A panel pointed at by looking needs them
        /// kept, because look is the only way to aim at it.
        /// </summary>
        private static readonly string[] Look =
        {
            "LOOKAROUND",
            "LOOK_UP",
            "LOOK_DOWN",
            "LOOK_LEFT",
            "LOOK_RIGHT",
        };

        private static readonly MyMouseButtonsEnum[] MouseButtons =
        {
            MyMouseButtonsEnum.Left,
            MyMouseButtonsEnum.Right,
            MyMouseButtonsEnum.Middle,
            MyMouseButtonsEnum.XButton1,
            MyMouseButtonsEnum.XButton2,
        };

        private static readonly MyKeys[] Keys =
        {
            MyKeys.Back, MyKeys.Tab, MyKeys.Enter, MyKeys.Shift, MyKeys.Control,
            MyKeys.Alt, MyKeys.Space, MyKeys.PageUp, MyKeys.PageDown, MyKeys.End,
            MyKeys.Home, MyKeys.Left, MyKeys.Up, MyKeys.Right, MyKeys.Down,
            MyKeys.Insert, MyKeys.Delete,
            MyKeys.D0, MyKeys.D1, MyKeys.D2, MyKeys.D3, MyKeys.D4,
            MyKeys.D5, MyKeys.D6, MyKeys.D7, MyKeys.D8, MyKeys.D9,
            MyKeys.A, MyKeys.B, MyKeys.C, MyKeys.D, MyKeys.E, MyKeys.F, MyKeys.G, MyKeys.H,
            MyKeys.I, MyKeys.J, MyKeys.K, MyKeys.L, MyKeys.M, MyKeys.N, MyKeys.O, MyKeys.P,
            MyKeys.Q, MyKeys.R, MyKeys.S, MyKeys.T, MyKeys.U, MyKeys.V, MyKeys.W,
            MyKeys.X, MyKeys.Y, MyKeys.Z,
            MyKeys.NumPad0, MyKeys.NumPad1, MyKeys.NumPad2, MyKeys.NumPad3, MyKeys.NumPad4,
            MyKeys.NumPad5, MyKeys.NumPad6, MyKeys.NumPad7, MyKeys.NumPad8, MyKeys.NumPad9,
            MyKeys.Multiply, MyKeys.Add, MyKeys.Separator, MyKeys.Subtract,
            MyKeys.Decimal, MyKeys.Divide,
            MyKeys.LeftShift, MyKeys.RightShift,
            MyKeys.LeftControl, MyKeys.RightControl,
            MyKeys.LeftAlt, MyKeys.RightAlt,
            MyKeys.OemSemicolon, MyKeys.OemPlus, MyKeys.OemComma, MyKeys.OemMinus,
            MyKeys.OemPeriod, MyKeys.OemQuestion, MyKeys.OemTilde, MyKeys.OemOpenBrackets,
            MyKeys.OemPipe, MyKeys.OemCloseBrackets, MyKeys.OemQuotes, MyKeys.Oem8,
            MyKeys.OemBackslash,
        };

        /// <summary>
        /// Fills <paramref name="into" /> with the deduped control ids to suspend.
        /// </summary>
        /// <param name="suspendLook">
        /// Whether aiming is suspended too. True for a mouse pointer, false for anything pointed at by
        /// looking.
        /// </param>
        public static void Collect(List<string> into, bool suspendLook)
        {
            into.Clear();

            if (MyAPIGateway.Input == null)
                return;

            HashSet<string> seen = new HashSet<string>();
            HashSet<string> keep = new HashSet<string>();

            if (!suspendLook)
                for (int i = 0; i < Look.Length; i++)
                    keep.Add(Look[i]);

            for (int i = 0; i < MouseButtons.Length; i++)
                Take(MyAPIGateway.Input.GetControl(MouseButtons[i]), seen, keep, into);

            for (int i = 0; i < Keys.Length; i++)
                Take(MyAPIGateway.Input.GetControl(Keys[i]), seen, keep, into);
        }

        private static void Take(IMyControl control, HashSet<string> seen, HashSet<string> keep, List<string> into)
        {
            if (control == null || !control.IsControlAssigned())
                return;

            string name = control.GetGameControlEnum().ToString();
            if (keep.Contains(name) || !seen.Add(name))
                return;

            into.Add(name);
        }
    }
}
