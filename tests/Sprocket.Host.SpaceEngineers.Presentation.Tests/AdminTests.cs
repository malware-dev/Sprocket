using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The world's settings, for an admin with creative tools on: a striped button on the home screen opens them, and each change
/// applies as it is made.
/// </summary>
public class AdminTests
{
    /// <summary>Nobody but an admin sees the button, asked each time the editor opens.</summary>
    [Fact]
    public void OnlyAnAdminSeesTheButton()
    {
        var screen = Screen.Fresh().Run(2);
        Assert.False(screen.Shows("Admin"));

        screen.Scene.Ship.Admin = true;
        screen.Scene.Reopen();
        screen.Run(2);
        Assert.True(screen.Shows("Admin"));

        screen.Scene.Ship.Admin = false;
        screen.Scene.Reopen();
        screen.Run(2);
        Assert.False(screen.Shows("Admin"));
    }

    /// <summary>The button stands on the home screen alone.</summary>
    [Fact]
    public void TheButtonIsOnTheHomeScreenAlone()
    {
        var screen = Screen.Fresh();
        screen.Scene.Ship.Admin = true;
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);

        Assert.False(screen.Shows("Admin"));
    }

    /// <summary>It opens on this world's tab, each rule left to every world, which gives what is built in.</summary>
    [Fact]
    public void ThisWorldLeavesEveryRuleToEveryWorld()
    {
        var screen = Opened(new Settings(SettingsLayer.Empty, SettingsLayer.Empty));

        Assert.True(screen.Shows("Default (every tick)"));
        Assert.True(screen.Shows("Default (off)"));
    }

    /// <summary>A choice under this world is this world's alone, and the world runs by it at once.</summary>
    [Fact]
    public void AChangeAppliesAtOnce()
    {
        var screen = Opened(new Settings(SettingsLayer.Empty, SettingsLayer.Empty));

        Choose(screen, "Default (every tick)", "Every 100 ticks (lightest)");
        Assert.Equal(100, screen.Scene.Ship.Settings.World.FastestUpdate);
        Assert.Null(screen.Scene.Ship.Settings.Global.FastestUpdate);
        Assert.Equal(100, screen.Scene.Ship.Runner.FastestUpdate);

        Choose(screen, "Default (off)", "On");
        Assert.True(screen.Scene.Ship.Settings.World.DeveloperMode);
        Assert.True(screen.Editor.DeveloperMode);
        Assert.Equal(100, screen.Scene.Ship.Settings.FastestUpdate);
    }

    /// <summary>The debug log is switched on and off like the other rules, and the world's runs write to it from the moment it is on until it is off.</summary>
    [Fact]
    public void TheDebugLogIsSwitchedOnAndOff()
    {
        var screen = Opened(new Settings(SettingsLayer.Empty, SettingsLayer.Empty));
        Assert.False(screen.Shows("World rules"));
        Assert.Null(screen.Scene.Ship.Runner.Log);

        // Developer mode given outright first, so the one default left saying off is the debug log's.
        Choose(screen, "Default (off)", "Off");
        Choose(screen, "Default (off)", "On");
        Assert.True(screen.Scene.Ship.Settings.World.DebugLog);
        Assert.NotNull(screen.Scene.Ship.Runner.Log);

        Choose(screen, "On", "Default (off)");
        Assert.Null(screen.Scene.Ship.Settings.World.DebugLog);
        Assert.Null(screen.Scene.Ship.Runner.Log);
    }

    /// <summary>The tip trial is chosen under the trials, for this world, and the editor's tips follow it at once.</summary>
    [Fact]
    public void TheTipTrialIsChosenForThisWorld()
    {
        var screen = Opened(new Settings(SettingsLayer.Empty, SettingsLayer.Empty));
        Assert.True(screen.Shows("Trials"));

        Choose(screen, "Always stay", "Hold Alt to keep open");

        Assert.Equal(TipKeeping.Alt, screen.Scene.Ship.Settings.World.TipKeeping);
        Assert.Null(screen.Scene.Ship.Settings.Global.TipKeeping);
        Assert.Equal(TipKeeping.Alt, screen.Editor.TipKeeping);
    }

    /// <summary>The default under this world gives the setting back to every world.</summary>
    [Fact]
    public void TheDefaultLeavesItToEveryWorld()
    {
        var screen = Opened(new Settings(new SettingsLayer(null, 10), new SettingsLayer(null, 100)));

        Choose(screen, "Every 100 ticks (lightest)", "Default (every 10 ticks)");

        Assert.Null(screen.Scene.Ship.Settings.World.FastestUpdate);
        Assert.Equal(10, screen.Scene.Ship.Settings.FastestUpdate);
        Assert.Equal(10, screen.Scene.Ship.Runner.FastestUpdate);
    }

    /// <summary>A choice under every world is every world's, and a world leaving the setting follows it, its default saying so.</summary>
    [Fact]
    public void EveryWorldIsFollowedWhereThisWorldLeavesIt()
    {
        var screen = Opened(new Settings(SettingsLayer.Empty, SettingsLayer.Empty));

        screen.Do("press", "Every world").Run(1);
        Choose(screen, "Every tick (most accurate)", "Every 10 ticks");

        Assert.Equal(10, screen.Scene.Ship.Settings.Global.FastestUpdate);
        Assert.Null(screen.Scene.Ship.Settings.World.FastestUpdate);
        Assert.Equal(10, screen.Scene.Ship.Runner.FastestUpdate);

        screen.Do("press", "This world").Run(1);
        Assert.True(screen.Shows("Default (every 10 ticks)"));
    }


    /// <summary>Close and Escape both put it away, keeping what was changed.</summary>
    [Fact]
    public void CloseKeepsWhatChanged()
    {
        var screen = Opened(new Settings(SettingsLayer.Empty, SettingsLayer.Empty));
        Choose(screen, "Default (every tick)", "Every 10 ticks");
        screen.Do("press", "Close").Run(1);

        Assert.False(screen.Shows("Admin settings"));
        Assert.Equal(10, screen.Scene.Ship.Settings.FastestUpdate);

        screen.Do("press", "Admin", "escape").Run(1);
        Assert.False(screen.Shows("Admin settings"));
    }

    /// <summary>Opens the list showing <paramref name="shown" /> and chooses <paramref name="word" /> from it.</summary>
    static void Choose(Screen screen, string shown, string word)
    {
        screen.Do("press", shown).Run(1);
        screen.Do("press", word).Run(1);
    }

    /// <summary>The settings open, for an admin, over a world running by <paramref name="settings" />.</summary>
    static Screen Opened(Settings settings)
    {
        var screen = Screen.Fresh();
        screen.Scene.Ship.Admin = true;
        screen.Scene.Ship.Settle(settings);
        screen.Scene.Reopen();
        screen.Run(2);
        screen.Do("press", "Admin").Run(2);
        Assert.True(screen.Shows("Admin settings"));
        return screen;
    }
}
