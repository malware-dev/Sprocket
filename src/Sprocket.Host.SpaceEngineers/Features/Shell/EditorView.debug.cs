namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    // How often the chrome writes onto its controls, kept for the tests that hold an idle screen to none.
    //
    // A .debug.cs file, so this is compiled into local builds only and never packed for the game, where the
    // call to Wrote compiles to nothing.
    public sealed partial class EditorView
    {
        int _wires;

        /// <summary>How many times the chrome has written what the screen says onto its controls.</summary>
        public int Wires => _wires;

        partial void Wrote()
        {
            _wires++;
        }
    }
}
