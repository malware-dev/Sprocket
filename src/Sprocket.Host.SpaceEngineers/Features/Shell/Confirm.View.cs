using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    partial class Confirm
    {
        RichText _question;
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
                    new RichText { Style = Styles.MenuQuestion }.As(out _question),
                    new VStack().As(out _answers)
                }
            };
        }
    }
}
