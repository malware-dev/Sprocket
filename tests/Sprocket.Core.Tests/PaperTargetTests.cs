using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Everything the editor puts on a screen can be shown.
/// </summary>
/// <remarks>
/// The editor states what is on screen as components and decides nothing about how they look. The
/// risk in that is a component that nothing shows: the screens lay out what they know and pass
/// silently over what they do not, so the failure is invisible exactly where it matters.
/// <para>
/// The pressure is in <see cref="PaperTarget" /> refusing to write down a component it has not been
/// told about. These tests walk the editor through every component type there is, so a new one
/// arrives here before it arrives in front of anybody.
/// </para>
/// </remarks>
public class PaperTargetTests
{
    [Fact]
    public void EveryComponentCoreProducesCanBeWrittenDown()
    {
        var ship = Build();
        var editor = ship.Editor;
        var seen = new HashSet<string>();

        Note(seen, ship);

        editor.NewSystem("Airlock");
        Note(seen, ship);

        editor.PressStart("When…");
        Note(seen, ship);

        editor.PressBlock("Airlock Button");
        Note(seen, ship);

        editor.PressChoice("is pressed");
        editor.PressPrimary();
        editor.PressChoice("Run a new sequence");
        Note(seen, ship);

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        Note(seen, ship);

        editor.GoToRoot();
        Note(seen, ship);

        editor.PressStart("Load system");
        Note(seen, ship);

        Assert.Contains(nameof(StartComponent), seen);
        Assert.Contains(nameof(StatusListComponent), seen);
        Assert.Contains(nameof(PickComponent), seen);
        Assert.Contains(nameof(ChoiceListComponent), seen);
        Assert.Contains(nameof(SequenceComponent), seen);
    }

    /// <summary>The sentence survives the trip to paper, because the sentence is the product.</summary>
    [Fact]
    public void ASequenceReadsAsASentenceOnPaper()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
        editor.PressChoice("Run a new sequence");

        editor.PressAdd("do something");
        editor.PressBlock("Inner Door");
        editor.PressChoice("Close");
        editor.PressPrimary();

        var paper = PaperTarget.Draw(ship.World, editor.Screen);

        Assert.Contains("1. close Inner Door", paper);
    }

    /// <summary>An unfinished step says so rather than saying nothing.</summary>
    [Fact]
    public void AnEmptySlotIsDrawnAsAGap()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");

        var paper = PaperTarget.Draw(ship.World, editor.Screen);

        Assert.Contains("pick:", paper);
        Assert.Contains("[ ] Airlock Button", paper);
    }

    static void Note(HashSet<string> seen, Ship ship)
    {
        // Drawn every time, not only at the end: the refusal is what is being exercised, and a walk
        // that renders once would step straight past whatever it could not have written down.
        Assert.NotEqual("", PaperTarget.Draw(ship.World, ship.Editor.Screen));

        foreach (var component in ship.Editor.Screen.Components)
            seen.Add(component.GetType().Name);
    }

    static Ship Build()
    {
        var ship = new Ship();
        ship.Rig.Door(1, "Inner Door");
        ship.Rig.Door(2, "Outer Door");
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light");

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner);
        return ship;
    }

    sealed class Ship
    {
        public Rigged Rig = new();
        public Automation Automation = new();
        public AutomationRunner Runner = null!;
        public SprocketEditor Editor = null!;

        public GridWorld World => Rig.World;
    }
}
