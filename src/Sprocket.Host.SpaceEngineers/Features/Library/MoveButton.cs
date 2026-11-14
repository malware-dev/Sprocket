using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>A button in the strip between the library's panes, showing the mark of what it does, dim while it cannot be pressed.</summary>
    public partial class MoveButton : ZStack
    {
        /// <summary>A button showing <paramref name="does" />, a symbol.</summary>
        public MoveButton(string does)
        {
            CreateView();
            _does.Symbol = does;
        }

        /// <summary>Whether it can be pressed, which its mark shows.</summary>
        public bool Live
        {
            set { _does.Set(LookStates.Off, !value); }
        }
    }
}
