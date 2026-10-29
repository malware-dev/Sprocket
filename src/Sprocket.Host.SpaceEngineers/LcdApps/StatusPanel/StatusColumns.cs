using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>One page of the status panel's cards, in one column or two.</summary>
    public partial class StatusColumns : HStack
    {
        /// <summary>A page holding no cards yet.</summary>
        public StatusColumns()
        {
            CreateView();
        }

        /// <summary>Takes its cards out, so another page may hold them.</summary>
        public void Empty()
        {
            _first.Children.Clear();
            _second.Children.Clear();
        }

        /// <summary>
        /// Holds <paramref name="cards" /> from <paramref name="from" /> up to <paramref name="to" />, in two columns where
        /// <paramref name="two" />, spaced <paramref name="between" /> apart: the first column takes cards until the next
        /// would crowd it, each column <paramref name="column" /> wide and <paramref name="high" /> tall.
        /// </summary>
        public void Fill(IGraphics graphics, List<StatusCard> cards, int from, int to, bool two, float between, float column, float high)
        {
            Base.Spacing = between;
            _second.IsVisible = two;

            var filled = 0f;
            var into = _first;
            for (var i = from; i < to; i++)
            {
                var tall = StatusLook.Tall(graphics, cards[i].Body, column);
                if (two && into == _first && filled > 0f && filled + tall * 2f > high)
                {
                    into = _second;
                    filled = 0f;
                }

                filled += filled > 0f ? tall * 2f : tall;
                into.Children.Add(cards[i]);
            }
        }
    }
}
