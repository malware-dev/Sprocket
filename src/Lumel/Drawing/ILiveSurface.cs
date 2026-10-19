using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using IMyCubeBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Lumel.Drawing
{
    /// <summary>A text surface drawn by a <see cref="SurfaceLoop" /> while something on it moves, faster than its app is run.</summary>
    public interface ILiveSurface
    {
        /// <summary>The block the surface is on.</summary>
        IMyCubeBlock Block { get; }

        /// <summary>The surface drawn.</summary>
        IMyTextSurface Surface { get; }

        /// <summary>Fills <paramref name="output" /> with the frame for <paramref name="tick" />, and answers false once nothing moves any more.</summary>
        bool Frame(List<MySprite> output, int tick);
    }
}
