using VRageMath;

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
