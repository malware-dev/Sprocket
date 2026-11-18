using Lumel;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    public sealed partial class PickPage
    {
        /// <summary>The strip of the pick, which this page shares with the pages before and after it.</summary>
        protected override Element CreateView()
        {
            return _strip.View;
        }
    }
}
