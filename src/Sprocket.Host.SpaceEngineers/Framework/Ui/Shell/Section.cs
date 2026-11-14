using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>A heading with what it heads under it, the heading standing close over it.</summary>
    public class Section : VStack
    {
        /// <summary>An empty section; its heading is the first of its children.</summary>
        public Section()
        {
            Base.Spacing = Gaps.Within;
        }
    }
}
