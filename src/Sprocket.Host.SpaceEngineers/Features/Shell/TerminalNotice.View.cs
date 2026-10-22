using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    public partial class TerminalNotice
    {
        MessageBox _box;

        /// <summary>Builds the box, centered across the screen.</summary>
        void CreateView()
        {
            Align = Align.Fill;
            Children.Add(new Label { Placement = Flex.Grow });
            Children.Add(new MessageBox
            {
                Style = Styles.Dialog,
                Align = Align.Center,
                Placement = FixedSize.Wide(Sizes.Popup),
                MessageStyle = Styles.Question,
                ButtonStyle = Styles.Button,
                DefaultStyle = Styles.Primary,
                Default = MessageButton.Yes
            }.As(out _box));
            Children.Add(new Label { Placement = Flex.Grow });
        }
    }
}
