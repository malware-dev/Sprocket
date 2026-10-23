using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Lumel
{
    public partial class CodexTip
    {
        HStack _heading;
        RichText _title;
        Glyph _back;
        Glyph _forward;
        RichText _body;
        RichText _keepHint;

        /// <summary>Makes the title row, holding the page's subject and the steps back and forward at its right end, over the page's words.</summary>
        void CreateView()
        {
            Children.Add(new HStack
            {
                Children =
                {
                    new RichText { Placement = Flex.Grow }.As(out _title),
                    new Glyph { Align = Align.Center, Command = new Relay(GoBack) }.As(out _back),
                    new Glyph { Align = Align.Center, Command = new Relay(GoForward) }.As(out _forward)
                }
            }.As(out _heading));
            Children.Add(new RichText { Follow = link => Follow(link.Target) }.As(out _body));
            Children.Add(new RichText { IsVisible = false }.As(out _keepHint));
        }
    }
}
