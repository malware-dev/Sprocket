using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Lumel.Text;
using VRage.Input;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// How often a timer starts is given in the same panel as a wait's length: on the timer's picker when it is made, and hanging
/// from its rate on its row afterwards.
/// </summary>
public class RateTests
{
    /// <summary>A new timer starts at a second, in seconds, and its row says so.</summary>
    [Fact]
    public void ANewTimerStartsAtASecond()
    {
        var screen = Made();

        var every = Assert.IsType<EveryTrigger>(System(screen).Triggers[0]);
        Assert.Equal(Duration.OfSeconds(1), every.Interval);
        Assert.Equal("Every second", screen.Editor.Says(0));
    }

    /// <summary>A rate short enough to be costly says so under the number, and one that is not says nothing.</summary>
    [Fact]
    public void ACostlyRateIsCautioned()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Housekeeping");
        screen.Editor.PressStart("every");
        screen.Run(2);

        Assert.True(screen.Shows("seconds"), "the picker does not show the panel");

        var rate = screen.Editor.TimerRate();
        rate.Give("1", true);
        screen.Run(1);
        Assert.Equal("Sixty times a second is costly; worth it only where nothing slower will do.", Caution(rate));

        rate.Give("10", true);
        screen.Run(1);
        Assert.Equal("Six times a second is still costly; try something slower first.", Caution(rate));

        rate.Give("30", true);
        screen.Run(1);
        Assert.Null(Caution(rate));
    }

    /// <summary>Everything in the timer's panel stands inside its column, in either unit.</summary>
    [Fact]
    public void TheTimerPanelFitsItsColumn()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Housekeeping");
        screen.Editor.PressStart("every");
        screen.Run(2);

        var rate = screen.Editor.TimerRate();
        AssertInsideItsColumn(rate);

        rate.Give("100", true);
        screen.Run(2);
        AssertInsideItsColumn(rate);
    }

    static void AssertInsideItsColumn(DurationPanel rate)
    {
        var column = rate.Parent!;
        var edge = column.ToScreen(new VRageMath.Vector2(column.Bounds.Right, 0f)).X;
        foreach (var part in rate.Within().Where(part => part.IsVisible))
            Assert.True(part.ToScreen(new VRageMath.Vector2(part.Bounds.Right, 0f)).X <= edge, part.GetType().Name + " stands past the column's edge");
    }

    /// <summary>What the panel cautions, or null where it says nothing.</summary>
    static string? Caution(DurationPanel rate) =>
        rate.Within().OfType<Label>().Where(label => label.IsVisible && label.Text.Contains("times a second")).Select(label => label.Text).SingleOrDefault();

    /// <summary>A rate given in seconds on the row is kept in seconds, said in seconds, and taken back as one change.</summary>
    [Fact]
    public void ARateChangedOnItsRowIsOneChange()
    {
        var screen = Made();
        var every = Assert.IsType<EveryTrigger>(System(screen).Triggers[0]);

        screen.Editor.PressWhen(0);
        screen.Run(2);
        var rate = screen.Editor.RateLengths();
        rate.Give("2.5", false);
        rate.Box.Submit!.Execute();
        screen.Run(2);

        Assert.Equal(Duration.OfSeconds(2.5), every.Interval);
        Assert.Equal("Every 2.5 seconds", screen.Editor.Says(0));

        screen.Editor.Undo(System(screen), System(screen));
        Assert.Equal(Duration.OfSeconds(1), every.Interval);
    }

    /// <summary>A repeat that comes round at a setting's rate says the setting; a fixed value chosen under the mark and a rate given take the setting's place, and undo gives it back.</summary>
    [Fact]
    public void ARatesSettingIsShownAndAFixedRateTakesItsPlace()
    {
        var screen = Made();
        var every = Assert.IsType<EveryTrigger>(System(screen).Triggers[0]);
        Assert.True(System(screen).IsBlueprint);
        screen.Editor.MakeSetting(System(screen), new Part("Pace") { Setting = "duration", Optional = true, Default = Literal.Of(Duration.OfSeconds(2)), HasDefault = true });
        every.IntervalSetting = "Pace";
        screen.Editor.Invalidate();
        screen.Run(2);
        Assert.Equal("Every Pace", screen.Editor.Says(0));

        screen.Editor.PressWhen(0);
        screen.Run(2);
        var rate = screen.Editor.RateLengths();
        Assert.Equal("Pace", rate.Setting);
        rate.Mark.Command!.Execute();
        screen.Run(2);
        screen.Editor.PressAnswer(screen.Editor.Texts.ValueFixed);
        screen.Run(2);
        rate.Give("1", false);
        rate.Box.Submit!.Execute();
        screen.Run(2);

        Assert.Equal("", every.IntervalSetting);
        Assert.Equal("Every second", screen.Editor.Says(0));

        screen.Editor.Undo(System(screen), System(screen));
        Assert.Equal("Pace", every.IntervalSetting);
    }

    /// <summary>Escape on a row's rate leaves it as it was.</summary>
    [Fact]
    public void EscapeLeavesARateAsItWas()
    {
        var screen = Made();
        var every = Assert.IsType<EveryTrigger>(System(screen).Triggers[0]);

        screen.Editor.PressWhen(0);
        screen.Run(2);
        screen.Editor.RateLengths().Give("7", false);
        screen.Editor.Input.Key(MyKeys.Escape, false, false);
        screen.Run(2);

        Assert.Equal(Duration.OfSeconds(1), every.Interval);
    }

    /// <summary>A timer made after ticks were chosen starts at a second, in ticks.</summary>
    [Fact]
    public void ANewTimerStartsInTheUnitLastChosen()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Housekeeping");
        screen.Editor.PressStart("every");
        screen.Run(2);
        screen.Editor.TimerRate().Give("30", true);
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressStart("every");
        screen.Run(2);
        Assert.False(screen.Editor.TimerRate().InSeconds);
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        screen.Run(2);

        var every = Assert.IsType<EveryTrigger>(System(screen).Triggers[1]);
        Assert.False(every.Interval.InSeconds);
        Assert.Equal(60, every.Interval.Ticks);
    }

    /// <summary>A logic system with a timer made at its starting rate, back on the system's page.</summary>
    static Screen Made()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Housekeeping");
        screen.Editor.PressStart("every");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        return screen.Run(2);
    }

    static AutomationSystem System(Screen screen) => screen.Editor.Program.Systems[0];
}
