using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel.Layout;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    public partial class TerminalNotice
    {
        /// <summary>Gives the box the editor's look for a question, as wide as a popup, with OK as what Enter presses.</summary>
        void CreateView()
        {
            Style = Styles.Asking;
            Placement = FixedSize.Width(Sizes.Popup);
            DefaultButton = MessageBoxAnswer.Yes;
        }
    }
}
