using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>What the check of the member texts found, said to the developer who asked for it.</summary>
    public sealed class MemberAuditDialog : Dialog
    {
        const float Wide = 480f;

        /// <summary>The dialog saying <paramref name="found" />, in the language of <paramref name="texts" />.</summary>
        public MemberAuditDialog(Texts texts, string found)
        {
            Style = Styles.Dialog;
            Placement = FixedSize.Width(Wide);
            var close = new RelayCommand(Close);
            CancelCommand = close;
            AcceptCommand = close;

            Content = new VStack
            {
                Base = { Spacing = Gaps.Items },
                Children =
                {
                    new Label { Style = Styles.Phrase, Text = texts.MemberAuditTitle },
                    new Label { Style = Styles.Detail, TextWrapping = TextWrapping.Wrap, Text = found },
                    new Button { Style = Styles.Primary, Text = texts.AdminClose, Align = Align.Right, Command = close }
                }
            };
        }
    }
}
