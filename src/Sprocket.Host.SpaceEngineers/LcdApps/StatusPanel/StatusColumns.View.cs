using Lumel;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    partial class StatusColumns
    {
        VStack _first;
        VStack _second;

        /// <summary>Makes the two columns, each standing its cards from the top down over its share of the width.</summary>
        void CreateView()
        {
            Children.Add(new VStack { Align = Align.Tall, Placement = Flex.Grow }.As(out _first));
            Children.Add(new VStack { Align = Align.Tall, Placement = Flex.Grow }.As(out _second));
        }
    }
}
