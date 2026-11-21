using System.Collections.Generic;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRageMath;

namespace Sprocket.Sim.Simulation;

/// <summary>Colors a player might give a screen's app, for pictures and tests of how the automation panel reads in each.</summary>
/// <remarks>
/// The game's own default first, then the plain choices a player reaches for, the two colors the panel's marks are drawn in as
/// a background, and a pair a player might pick that are close to each other.
/// </remarks>
public static class PanelColors
{
    /// <summary>Every pair, by the name it is shot under.</summary>
    public static readonly IReadOnlyList<KeyValuePair<string, Color[]>> All =
    [
        Pair("default", MyTextSurfaceScriptBase.DEFAULT_FONT_COLOR, MyTextSurfaceScriptBase.DEFAULT_BACKGROUND_COLOR),
        Pair("white-on-black", Color.White, Color.Black),
        Pair("black-on-white", Color.Black, new Color(240, 240, 240)),
        Pair("white-on-red", Color.White, new Color(170, 30, 30)),
        Pair("black-on-amber", Color.Black, new Color(235, 165, 30)),
        Pair("green-on-black", new Color(70, 255, 70), Color.Black),
        Pair("gray-on-gray", new Color(130, 130, 130), new Color(70, 70, 70))
    ];

    /// <summary>A pair called <paramref name="name" />: the app's text color, then its background.</summary>
    static KeyValuePair<string, Color[]> Pair(string name, Color ink, Color ground)
    {
        return new KeyValuePair<string, Color[]>(name, [ink, ground]);
    }
}
