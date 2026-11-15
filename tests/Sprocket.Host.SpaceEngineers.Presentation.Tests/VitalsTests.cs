using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The editor's foot shows how the player stands: health and energy always, oxygen and food only where the world has them, a
/// bar amber when low and blinking red when nearly gone; nothing where there is no player to read.
/// </summary>
public class VitalsTests
{
    sealed class Standing : IVitals
    {
        public bool Present { get; set; } = true;
        public bool HasOxygen { get; set; } = true;
        public bool HasFood { get; set; }
        public float Health { get; set; } = 0.8f;
        public float Energy { get; set; } = 0.2f;
        public float Oxygen { get; set; } = 0.05f;
        public float Food { get; set; } = 1f;
    }

    /// <summary>Each bar shown says its share, and those the world does not have stand away.</summary>
    [Fact]
    public void TheBarsSayHowThePlayerStands()
    {
        var screen = Screen.Fresh();
        screen.Editor.Vitals = new Standing();
        screen.Run(60);

        var strip = Strip(screen);
        Assert.True(strip.IsVisible);
        var texts = screen.Editor.Texts;
        Assert.Equal(new[] { texts.VitalsHealth, texts.VitalsEnergy, texts.VitalsOxygen }, Words(strip));
        Assert.Equal(new[] { 0.8f, 0.2f, 0.05f }, Bars(strip).Select(bar => bar.Value));
    }

    /// <summary>Food shows where the world has hunger, and oxygen goes where it has none.</summary>
    [Fact]
    public void FoodAndOxygenFollowTheWorld()
    {
        var screen = Screen.Fresh();
        screen.Editor.Vitals = new Standing { HasFood = true, HasOxygen = false };
        screen.Run(60);

        var texts = screen.Editor.Texts;
        Assert.Equal(new[] { texts.VitalsHealth, texts.VitalsEnergy, texts.VitalsFood }, Words(Strip(screen)));
    }

    /// <summary>A bar under a quarter is amber, and one under a tenth blinks red, a reading at a time.</summary>
    [Fact]
    public void LowBarsWarnAndNearlyGoneOnesBlink()
    {
        var screen = Screen.Fresh();
        screen.Editor.Vitals = new Standing();
        screen.Run(60);

        var bars = Bars(Strip(screen));
        Assert.Equal(Palette.Ink, bars[0].Fill);
        Assert.Equal(Palette.Notice, bars[1].Fill);

        var seen = new HashSet<Color>();
        for (var i = 0; i < 4; i++)
        {
            screen.Run(30);
            seen.Add(bars[2].Fill);
        }

        Assert.Contains(Palette.Problem, seen);
        Assert.Contains(Palette.Notice, seen);
    }

    /// <summary>With no player to read, the foot shows no bars.</summary>
    [Fact]
    public void NoPlayerShowsNoBars()
    {
        var screen = Screen.Fresh();
        screen.Editor.Vitals = new Standing { Present = false };
        screen.Run(60);

        Assert.False(Strip(screen).IsVisible);
    }

    static VitalsStrip Strip(Screen screen) => screen.Scene.View.Within().OfType<VitalsStrip>().Single();

    static string[] Words(VitalsStrip strip) => strip.Within().OfType<Label>().Where(Shown).Select(label => label.Text).ToArray();

    static List<ProgressBar> Bars(VitalsStrip strip) => strip.Within().OfType<ProgressBar>().Where(Shown).ToList();

    /// <summary>Whether <paramref name="element" /> and what holds it, up to the strip, are visible.</summary>
    static bool Shown(Element element) => element.IsVisible && element.Parent!.IsVisible;
}
