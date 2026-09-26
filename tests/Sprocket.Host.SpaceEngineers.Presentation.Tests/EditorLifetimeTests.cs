using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A Sprocket makes its editor when somebody opens it, and not before.</summary>
public class EditorLifetimeTests
{
    /// <summary>A Sprocket that runs, saves and changes language without being opened has no editor.</summary>
    [Fact]
    public void ASprocketNobodyOpensMakesNoEditor()
    {
        var sim = Simulation.Fresh();

        for (var i = 0; i < 120; i++)
            sim.Tick();

        sim.Save();
        sim.Unit.Reword();

        Assert.False(sim.Unit.HasEditor);
    }

    /// <summary>An editor made late shows the program as it stands when it is made.</summary>
    [Fact]
    public void AnEditorMadeLateShowsTheProgramAsItStands()
    {
        var first = Simulation.Fresh();
        first.Editor.NewSystem("Airlock");
        first.Unit.Written(out var program, out _);

        var store = Store.Nowhere();
        store.SetValue(SprocketUnit.ProgramKey, program);
        var sim = Simulation.On(store);
        sim.Tick();

        Assert.False(sim.Unit.HasEditor);

        var editor = sim.Unit.Editor;

        Assert.True(sim.Unit.HasEditor);
        Assert.Same(editor, sim.Unit.Editor);
        Assert.Contains(editor.RootRows(), row => row.Summary == "Airlock");
    }
}
