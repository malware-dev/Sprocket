using System.Collections.Generic;
using VRageMath;

namespace Sprocket.Sim.Content;

/// <summary>One of the game's screens, as a block definition declares it: the resolution of its texture and the shape of its face.</summary>
/// <remarks>
/// The game makes a screen's texture and the part of it the face shows out of these three numbers alone, by the rules
/// <see cref="TextureSize" /> and <see cref="SurfaceSize" /> follow, so a screen here measures what the same screen measures
/// in the game.
/// </remarks>
public readonly struct ScreenShape
{
    /// <summary>A screen named <paramref name="name" />, its texture <paramref name="resolution" /> on its short side, its face <paramref name="wide" /> by <paramref name="high" />, on a block whose longest side is <paramref name="metres" />.</summary>
    public ScreenShape(string name, int resolution, int wide, int high, float metres)
    {
        Name = name;
        Resolution = resolution;
        Wide = wide;
        High = high;
        Metres = metres;
    }

    /// <summary>
    /// The reference set: a screen of each shape a player meets, the two large programmable blocks' first.
    /// </summary>
    /// <remarks>
    /// Transcribed from the <c>ScreenArea</c> elements of the block definitions under <c>Content/Data/CubeBlocks</c>: each
    /// is named for the block it comes from and carries that screen's <c>TextureResolution</c>, <c>ScreenWidth</c> and
    /// <c>ScreenHeight</c>, and the block's longest side from its <c>CubeSize</c> and <c>Size</c>, 2.5 metres a cell on a
    /// large grid and 0.5 on a small one. A panel turned on its side is the same block with the face's width and height
    /// swapped.
    /// </remarks>
    public static readonly IReadOnlyList<ScreenShape> Reference =
    [
        new ScreenShape("programmable-block", 512, 16, 10, 2.5f),
        new ScreenShape("programmable-block-automatons", 512, 4, 5, 2.5f),
        new ScreenShape("programmable-block-keyboard", 256, 5, 2, 2.5f),
        new ScreenShape("small-programmable-block", 256, 1, 1, 0.5f),
        new ScreenShape("small-programmable-block-automatons", 256, 10, 7, 0.5f),
        new ScreenShape("lcd-panel", 512, 1, 1, 2.5f),
        new ScreenShape("wide-lcd-panel", 512, 2, 1, 5f),
        new ScreenShape("wide-lcd-panel-on-end", 512, 1, 2, 5f),
        new ScreenShape("text-panel", 512, 5, 3, 2.5f),
        new ScreenShape("rover-cockpit-side", 256, 4, 3, 1.5f),
        new ScreenShape("corner-lcd", 128, 7, 1, 2.5f),
        new ScreenShape("small-corner-lcd", 256, 22, 7, 0.5f),
        new ScreenShape("small-flat-corner-lcd", 256, 25, 10, 0.5f)
    ];

    /// <summary>What the screen is called here, which names its picture.</summary>
    public string Name { get; }

    /// <summary>How many texture pixels the screen's short side has, as its definition says.</summary>
    public int Resolution { get; }

    /// <summary>How wide the face is, against <see cref="High" />.</summary>
    public int Wide { get; }

    /// <summary>How high the face is, against <see cref="Wide" />.</summary>
    public int High { get; }

    /// <summary>How long the block's longest side is, in metres, which the panel takes for its screen's long side.</summary>
    public float Metres { get; }

    /// <summary>
    /// The size of the texture the game draws the screen into.
    /// </summary>
    /// <remarks>
    /// Square for a square face. For any other, the long side is the resolution times the largest power of two that fits in
    /// how many times longer the face is, counted in whole times: a 16:10 face is once longer and gets a square texture, a
    /// 7:1 face four times.
    /// </remarks>
    public Vector2I TextureSize
    {
        get
        {
            if (Wide == High)
                return new Vector2I(Resolution, Resolution);

            var times = Wide > High ? Wide / High : High / Wide;
            var power = 1;
            while (power * 2 <= times)
                power *= 2;

            return Wide > High ? new Vector2I(Resolution * power, Resolution) : new Vector2I(Resolution, Resolution * power);
        }
    }

    /// <summary>
    /// The part of the texture the face shows, centered in it, which is what a screen's apps lay themselves out in.
    /// </summary>
    /// <remarks>The texture's long side whole, and the short side cut to the face's shape.</remarks>
    public Vector2 SurfaceSize
    {
        get
        {
            Vector2I texture = TextureSize;
            return Wide >= High
                ? new Vector2(texture.X, texture.X * (float)High / Wide)
                : new Vector2(texture.Y * (float)Wide / High, texture.Y);
        }
    }
}
