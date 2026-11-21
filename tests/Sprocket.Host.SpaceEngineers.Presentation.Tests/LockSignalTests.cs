using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Machine;
using Xunit;
using LandingGearMode = SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode;
using MyShipConnectorStatus = Sandbox.ModAPI.Ingame.MyShipConnectorStatus;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A connector's and a magnetic plate's locking and unlocking start the sequences waiting on them.</summary>
public class LockSignalTests
{
    /// <summary>A plate that locks starts what waits on "locks", and one that unlocks what waits on "unlocks".</summary>
    [Theory]
    [InlineData("locks", LandingGearMode.Unlocked, LandingGearMode.Locked)]
    [InlineData("unlocks", LandingGearMode.Locked, LandingGearMode.Unlocked)]
    public void APlateStartsWhatWaitsOnIt(string signal, LandingGearMode from, LandingGearMode to)
    {
        var rig = Rig.Demo();
        var plate = rig.MagneticPlate(60, "Dock Plate");
        plate.Lock(from);
        Starts(rig, "Dock Plate", signal, () => plate.Lock(to));
    }

    /// <summary>A connector that locks starts what waits on "locks", and one that unlocks what waits on "unlocks".</summary>
    [Theory]
    [InlineData("locks", MyShipConnectorStatus.Connectable, MyShipConnectorStatus.Connected)]
    [InlineData("unlocks", MyShipConnectorStatus.Connected, MyShipConnectorStatus.Connectable)]
    public void AConnectorStartsWhatWaitsOnIt(string signal, MyShipConnectorStatus from, MyShipConnectorStatus to)
    {
        var rig = Rig.Demo();
        var connector = rig.Connector(60, "Dock Connector");
        connector.Lock(from);
        Starts(rig, "Dock Connector", signal, () => connector.Lock(to));
    }

    /// <summary>
    /// Builds, on <paramref name="rig" />, a logic system whose sequence turns a light off when <paramref name="block" /> gives
    /// <paramref name="signal" />, then has <paramref name="change" /> happen and checks the light went off then, and only then.
    /// </summary>
    static void Starts(Rig rig, string block, string signal, Action change)
    {
        var light = rig.Light(61, "Dock Light");
        var sim = Simulation.On(rig);

        sim.Editor.NewSystem("Dock");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock(block);
        sim.Editor.PressChoice(signal);
        sim.Editor.PressPrimary();
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Dock Light");
        sim.Editor.PressChoice("power");
        sim.Editor.Pick().Arguments!.Form.Fields[0].Text = "off";
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        Ticks(sim);

        Assert.True(light.Enabled, "the light went off before the block changed");

        change();
        Ticks(sim);

        Assert.False(light.Enabled, $"{block} changing never started what waits on {signal}");
    }

    static void Ticks(Simulation sim)
    {
        for (var i = 0; i < 5; i++)
            sim.Tick();
    }
}
