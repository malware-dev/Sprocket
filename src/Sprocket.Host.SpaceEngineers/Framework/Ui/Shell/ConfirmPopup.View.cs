using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
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
