using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.ObjectBuilders;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Sensing and automation blocks on the screen: what a picker offers of them, in Sprocket's words.</summary>
public class SensingTests
{
    /// <summary>A block of the game's interface <typeparamref name="T" /> with a toolbar, answering nothing more.</summary>
    sealed class Standing<T> : Fitting where T : class, IMyTerminalBlock
    {
        public Standing(long id, string name, MyObjectBuilderType type, string subtype)
            : base(id, name, type, subtype, name)
        {
            Toolbar = new Toolbar(9);
            Fit(Pretend.To<T>());
        }
    }

    /// <summary>The demo construct with a timer and a sensor on it, each declaring the terminal members the game gives it.</summary>
    static Screen WithTimerAndSensor()
    {
        var rig = Rig.Demo();
        rig.Timer(30, "Hold Timer")
            .Carrying("TriggerDelay", "Delay", ValueKind.Real, "1", "3600")
            .Carrying("Silent", "Silent", ValueKind.Flag)
            .Offering("Start", "Start")
            .Offering("TriggerNow", "Trigger now")
            .Offering("Silent", "Silent On/Off");
        rig.Weld(new Standing<IMySensorBlock>(31, "Bay Sensor", typeof(MyObjectBuilder_SensorBlock), "BaySensor"))
            .Carrying("Detect Players", "Detect Players", ValueKind.Flag)
            .Offering("Detect Players", "Toggle Detect Players");

        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Bay");
        return screen.Run(2);
    }

    /// <summary>A timer's task picker offers its countdown and its beeps in Sprocket's words.</summary>
    [Fact]
    public void ATimersTasksAreSaidInSprocketsWords()
    {
        var screen = WithTimerAndSensor();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Hold Timer");
        screen.Run(2);

        Assert.True(screen.Shows("Start the countdown"), "the timer does not offer starting its countdown");
        Assert.True(screen.Shows("Go off now"), "the timer does not offer going off now");
        Assert.True(screen.Shows("Toggle silent"), "the timer does not offer flipping its beeps");
        Assert.True(screen.Shows("Delay"), "the timer does not offer its delay");
    }

    /// <summary>A sensor's task picker offers what it watches for, and the toggle that flips it.</summary>
    [Fact]
    public void ASensorsTasksSayWhatItWatchesFor()
    {
        var screen = WithTimerAndSensor();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Bay Sensor");
        screen.Run(2);

        Assert.True(screen.Shows("Watching for people"), "the sensor does not offer what it watches for");
        Assert.True(screen.Shows("Toggle watching for people"), "the sensor does not offer flipping what it watches for");
    }

    /// <summary>A trigger on a sensor starts as it starts detecting, which is its toolbar's first slot.</summary>
    [Fact]
    public void ASensorStartsATriggerAsItStartsDetecting()
    {
        var rig = new Rig();
        rig.Weld(new Standing<IMySensorBlock>(1, "Bay Sensor", typeof(MyObjectBuilder_SensorBlock),
            "BaySensor"));

        var sim = Simulation.On(rig);
        sim.Editor.NewSystem("Bay");
        sim.Editor.PressStart("when");
        sim.Editor.PressBlock("Bay Sensor");
        Assert.True(sim.Editor.HasChoice("stops detecting"), "the sensor does not offer its second signal");
        sim.Editor.PressChoice("starts detecting");
        sim.Editor.PressPrimary();

        var trigger = Assert.IsType<SignalTrigger>(sim.Editor.Program.Sequences[0].Trigger);
        var at = sim.Editor.Program.Systems[0].First!.Positions(trigger.Part)![0];
        Assert.Equal(1, at.Slot);
    }
}
