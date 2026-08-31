using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Sim.Simulation
{
    /// <summary>Whatever can tell us how big text is. Supplied by the renderer, since only it has a font.</summary>
    public interface IFontMetrics
    {
        /// <summary>The size this run would occupy, in window pixels.</summary>
        Vector2 MeasurePixels(string font, string text, float scale);

        /// <summary>The height of one line, in window pixels, including leading.</summary>
        float LineHeightPixels(string font, float scale);
    }
}
