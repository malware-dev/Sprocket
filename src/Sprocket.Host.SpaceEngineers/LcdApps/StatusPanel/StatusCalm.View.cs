using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    partial class StatusCalm
    {
        Glyph _check;
        VStack _underCheck;
        Label _says;
        VStack _underSays;
        Label _count;
        Label _fix;

        /// <summary>Makes the check, the words under it, and the count or what to do under them.</summary>
        void CreateView()
        {
            Children.Add(new Glyph { Symbol = Symbols.Check, Align = Align.Center }.As(out _check));
            Children.Add(new VStack().As(out _underCheck));
            Children.Add(new Label { TextWrapping = TextWrapping.Wrap, Base = { TextAlignment = TextAlignment.Center } }.As(out _says));
            Children.Add(new VStack().As(out _underSays));
            Children.Add(new Label { TextWrapping = TextWrapping.Wrap, Base = { TextAlignment = TextAlignment.Center } }.As(out _count));
            Children.Add(new Label { TextWrapping = TextWrapping.Wrap, Base = { TextAlignment = TextAlignment.Center } }.As(out _fix));
        }
    }
}
