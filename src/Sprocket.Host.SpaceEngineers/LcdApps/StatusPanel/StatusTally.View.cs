using Lumel;
using Lumel.Images;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    partial class StatusTally
    {
        Glyph _mark;
        Label _count;

        /// <summary>Makes the mark and the count beside it.</summary>
        void CreateView()
        {
            Align = Align.Center;
            Children.Add(new Glyph { Align = Align.Center }.As(out _mark));
            Children.Add(new Label { Align = Align.Center }.As(out _count));
        }
    }
}
