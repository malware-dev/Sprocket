using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Lumel.Text;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A slot whose block was found by its name alone says so, and takes that block in one press.</summary>
public class ReplacedBlockTests
{
    /// <summary>A system on a door that is then rebuilt somewhere else under the same name.</summary>
    static Simulation Rebuilt(out string declared)
    {
        var rig = new Rig();
        var door = rig.Door(2, "Airlock Inner Door");
        rig.Panel(3, "Airlock Button");
        var sim = Simulation.On(rig);
        sim.Tick();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("open");
        sim.Editor.PressPrimary();
        sim.Tick();
        var runner = sim.Runner;
        declared = sim.Editor.Program.Refs.First(one => runner.Bindings.Entity(one.Name)?.Id == door.Id).Name;

        rig.Door(9, "Airlock Inner Door");
        rig.Grind(door);
        sim.Tick();
        return sim;
    }

    /// <summary>Taking the block binds it, and undoing goes back to not being sure.</summary>
    [Fact]
    public void ABlockFoundByNameIsTakenAndGivenBack()
    {
        var sim = Rebuilt(out var declared);
        Assert.Equal(RefState.Adrift, sim.Runner.Bindings.State(declared));

        var entity = sim.Runner.Bindings.Entity(declared)!;
        var refs = new[] { sim.Editor.Program.FindRef(declared) };
        var system = sim.Editor.Program.Systems[0];
        sim.Editor.Do(new AcceptBlocks(system.Instances[0], "slot", refs, new[] { entity }, sim.Editor.World));
        sim.Tick();
        Assert.Equal(RefState.Bound, sim.Runner.Bindings.State(declared));

        sim.Editor.UndoConstruct();
        sim.Tick();
        Assert.Equal(RefState.Adrift, sim.Runner.Bindings.State(declared));
    }

    /// <summary>A slot whose block was found by name alone is marked as replaced, ahead of anything else about it.</summary>
    [Fact]
    public void TheSlotIsMarkedAsReplaced()
    {
        var sim = Rebuilt(out var declared);
        var system = sim.Editor.Program.Systems[0];
        var instance = system.Instances[0];
        var part = system.Parts.First(one => instance.FilledWith(one.Id)?.Contains(declared) == true);

        Assert.Equal(SlotMark.Replaced, SlotMarks.Of(sim.Runner.Bindings, instance, part));
    }

    /// <summary>
    /// On the installation panel, in the game's fonts, the slot of a door rebuilt elsewhere offers Use this block on a row that holds
    /// it; one press takes the door, and the sequence runs on it.
    /// </summary>
    [Fact]
    public void TheInstallationPanelTakesTheBlockInOnePress()
    {
        var rig = new Rig();
        var door = rig.Door(2, "Airlock Inner Door");
        var button = rig.Panel(3, "Airlock Button");
        var screen = Screen.WithGameFonts(rig);
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Inner Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        screen.Run(2);
        editor.SwitchOn();
        screen.Run(2);
        var declared = editor.Program.Refs.First(one => editor.Runner.Bindings.Entity(one.Name)?.Id == door.Id).Name;

        var rebuilt = rig.Door(9, "Airlock Inner Door");
        rig.Grind(door);
        screen.Run(2);
        button.Press(0);
        screen.Run(120);
        Assert.Equal(Sandbox.ModAPI.Ingame.DoorStatus.Closed, rebuilt.Status);
        editor.GoToRoot();
        screen.Run(2);
        screen.Do("press", "Airlock").Run(2);
        screen.Do("press", editor.Texts.SystemInstallationPanel).Run(2);
        var problem = Template.Fill(editor.Texts.StoppedBlockReplaced, "block", "Airlock Inner Door");
        Assert.True(screen.Shows(problem), screen.Dump);

        // Changing a logic system waits for it to be switched off.
        editor.SwitchSystem(false);
        screen.Run(2);
        // The switch slides across as it flips, its word clipped until it rests, so the fit is read once it has.
        screen.Do("press", editor.Texts.BlueprintSwitchThemOff).Run(60);
        Assert.True(screen.Shows(editor.Texts.SlotUseBlock), screen.Dump);
        Assert.Empty(screen.Overlapping);
        Assert.Empty(screen.Cut);

        screen.Do("press", editor.Texts.SlotUseBlock).Run(2);
        Assert.Equal(RefState.Bound, editor.Runner.Bindings.State(declared));
        Assert.False(screen.Shows(editor.Texts.SlotUseBlock), screen.Dump);

        editor.SwitchOn();
        screen.Run(2);
        button.Press(0);
        screen.Run(120);
        Assert.NotEqual(Sandbox.ModAPI.Ingame.DoorStatus.Closed, rebuilt.Status);
    }

    /// <summary>Choosing the block in the picker is choosing it: the name stops being a guess.</summary>
    [Fact]
    public void ChoosingTheBlockInThePickerBindsIt()
    {
        var sim = Rebuilt(out var declared);
        var entity = sim.Runner.Bindings.Entity(declared)!;

        sim.Editor.Program.Declare(sim.Editor.World, entity);
        sim.Tick();

        Assert.Equal(RefState.Bound, sim.Runner.Bindings.State(declared));
    }
}
