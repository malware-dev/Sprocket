using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What every block shares is chosen and read like any block's own member.</summary>
public class CommonMembersTests
{
    /// <summary>A system with one named sequence, made the ordinary way.</summary>
    static Screen WithOne()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.Back();
        return screen.Run(2);
    }

    static AutomationSystem System(Screen screen) => screen.Editor.Program.Systems[0];

    /// <summary>A door's stopping working is chosen by its words and read back as a sentence.</summary>
    [Fact]
    public void StopsWorkingIsChosenAndSaid()
    {
        var screen = WithOne();

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("stops working");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var trigger = Assert.IsType<SignalTrigger>(System(screen).Triggers[0]);
        Assert.Equal("stops-working", trigger.EventId);
        Assert.Equal("When Airlock Inner Door stops working", screen.Editor.Says(0));
    }
}
