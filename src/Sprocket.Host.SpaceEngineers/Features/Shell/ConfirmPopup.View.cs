using Lumel;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    partial class ConfirmPopup
    {
        RichText _question;

        /// <summary>Makes the question, which heads the answers.</summary>
        void CreateView()
        {
            Style = Styles.Menu;
            Base.Gap = 4f;

            Items.Add(new RichText { Style = Styles.MenuQuestion, Base = { Padding = new Thickness(0f, 0f, 0f, Gaps.Parts) } }.As(out _question));
        }
    }
}
