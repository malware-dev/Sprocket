using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Lumel
{
    public partial class CodexTip
    {
        HStack _steps;
        Glyph _back;
        Glyph _forward;
        RichText _body;
        RichText _keepHint;

        /// <summary>Makes the row of steps back and forward at the top right, over the page's words.</summary>
        void CreateView()
        {
            Children.Add(new HStack
            {
                Align = Align.Right,
                IsVisible = false,
                Children =
                {
                    new Glyph { Align = Align.Center, Command = new Relay(GoBack) }.As(out _back),
                    new Glyph { Align = Align.Center, Command = new Relay(GoForward) }.As(out _forward)
                }
            }.As(out _steps));
            Children.Add(new RichText { Follow = link => Follow(link.Target) }.As(out _body));
            Children.Add(new RichText { IsVisible = false }.As(out _keepHint));
        }
    }
}
