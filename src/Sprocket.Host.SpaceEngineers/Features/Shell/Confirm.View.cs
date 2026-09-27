using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    partial class Confirm
    {
        Label _question;
        VStack _answers;

        /// <summary>Makes the question, and under it the column its answers go in.</summary>
        void CreateView()
        {
            Style = Styles.Menu;
            Gap = 4f;

            Content = new VStack
            {
                Spacing = 8f,
                Children =
                {
                    new Label { Style = Styles.ColumnNote }.As(out _question),
                    new VStack().As(out _answers)
                }
            };
        }
    }
}
