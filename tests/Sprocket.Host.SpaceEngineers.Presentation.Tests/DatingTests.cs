using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A system is dated with when it last changed, by the editor, however the change was made.</summary>
public class DatingTests
{
    static readonly DateTime Long = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>A change to what one system says dates that system and no other.</summary>
    [Fact]
    public void AChangeDatesOnlyTheSystemItChanges()
    {
        var rig = Two();
        var airlock = rig.Editor.Program.Systems[0];
        var hangar = rig.Editor.Program.Systems[1];
        var before = DateTime.UtcNow.AddSeconds(-1);

        airlock.Annotation = "Cycles the airlock.";
        rig.Editor.Program.Edited();
        rig.Editor.Stamp();

        Assert.True(airlock.Changed >= before);
        Assert.Equal(Long, hangar.Changed);
    }

    /// <summary>Switching a system off is not something it says, so it dates nothing.</summary>
    [Fact]
    public void SwitchingASystemDatesNothing()
    {
        var rig = Two();
        var airlock = rig.Editor.Program.Systems[0];

        airlock.Enabled = false;
        rig.Editor.Program.Edited();
        rig.Editor.Stamp();

        Assert.Equal(Long, airlock.Changed);
    }

    /// <summary>A system just made is dated as it is made.</summary>
    [Fact]
    public void ASystemJustMadeIsDated()
    {
        var rig = Simulation.Fresh();
        var before = DateTime.UtcNow.AddSeconds(-1);

        rig.Editor.NewSystem("Airlock");
        rig.Tick();

        Assert.True(rig.Editor.Program.Systems[0].Changed >= before);
    }

    /// <summary>A system renamed is dated at once, though a name says nothing to the counts the tick goes by.</summary>
    [Fact]
    public void ARenamedSystemIsDatedAtOnce()
    {
        var rig = Two();
        var hangar = rig.Editor.Program.Systems[1];
        rig.Editor.RootRows().Single(row => row.Summary == "Hangar").Opens!.Execute();
        rig.Tick();

        rig.Editor.Screen.Heading[0].Renames!("Bay");
        rig.Tick();

        Assert.Equal("Bay", hangar.Name);
        Assert.NotEqual(Long, hangar.Changed);
    }

    /// <summary>What was changed a moment ago is dated before the program is written out, with no tick between.</summary>
    [Fact]
    public void WritingTheProgramDatesWhatChanged()
    {
        var rig = Two();
        var airlock = rig.Editor.Program.Systems[0];

        airlock.Annotation = "Cycles the airlock.";
        rig.Editor.Program.Edited();

        rig.Unit.Written(out var program, out _);

        Assert.NotEqual(Long, airlock.Changed);
        Assert.Contains("changed \"" + airlock.Changed!.Value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"), program);
    }

    /// <summary>Two systems, both dated long ago, with the editor made after they were.</summary>
    static Simulation Two()
    {
        var made = Simulation.Fresh();
        made.Editor.NewButtonSystem("Airlock");
        made.Editor.GoToRoot();
        made.Tick();
        made.Editor.NewSystem("Hangar");
        made.Editor.GoToRoot();
        made.Tick();

        made.Editor.Stamp(true);
        foreach (var system in made.Editor.Program.Systems)
            system.Changed = Long;

        made.Unit.Written(out var program, out _);

        var rig = Simulation.Fresh();
        rig.Editor.Apply(program);
        rig.Tick();
        return rig;
    }
}
