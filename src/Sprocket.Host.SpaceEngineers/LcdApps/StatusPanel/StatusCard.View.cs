using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    partial class StatusCard
    {
        VStack _body;
        Glyph _mark;
        VStack _underMark;
        Label _system;
        VStack _underLabel;
        Label _name;
        VStack _underName;
        Label _sentence;

        /// <summary>Makes the card a little above the middle of its cell: two parts of the room above it to three below.</summary>
        void CreateView()
        {
            Placement = Flex.Grow;
            Columns.Add(GridLength.Flex(1f));
            Rows.Add(GridLength.Flex(2f));
            Rows.Add(GridLength.Auto);
            Rows.Add(GridLength.Flex(3f));
            Cells[1, 0] = new VStack
            {
                Children =
                {
                    new Glyph { Align = Align.Center }.As(out _mark),
                    new VStack().As(out _underMark),
                    new Label { Wraps = true, Centres = true }.As(out _system),
                    new VStack().As(out _underLabel),
                    new Label { Wraps = true, Centres = true }.As(out _name),
                    new VStack().As(out _underName),
                    new Label { Wraps = true, Centres = true }.As(out _sentence)
                }
            }.As(out _body);
        }
    }
}
