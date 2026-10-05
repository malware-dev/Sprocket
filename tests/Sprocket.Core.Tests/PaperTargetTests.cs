using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Tests;

/// <summary>
/// Everything the editor puts on a screen can be written down.
/// </summary>
public class PaperTargetTests
{
    [Fact]
    public void EveryPageCanBeWrittenDown()
    {
        var ship = Build();
        var editor = ship.Editor;
        var seen = new HashSet<string>();

        Note(seen, ship);

        editor.NewSystem("Airlock");
        Note(seen, ship);

        editor.PressStart("when");
        Note(seen, ship);

        editor.PressBlock("Airlock Button");
        Note(seen, ship);

        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        Note(seen, ship);

        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        Note(seen, ship);

        editor.GoToRoot();
        Note(seen, ship);

        editor.OpenLibrary();
        Note(seen, ship);

        Assert.Contains(nameof(PickPage), seen);
        Assert.Contains(nameof(SequencePage), seen);
    }

    /// <summary>The sentence survives the trip to paper, because the sentence is the product.</summary>
    [Fact]
    public void ASequenceReadsAsASentenceOnPaper()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewButtonSystem("Airlock");

        editor.PressAdd("task");
        editor.PressBlock("Inner Door");
        editor.PressChoice("Close");
        editor.PressPrimary();

        var paper = PaperTarget.Draw(ship.World, editor);

        Assert.Contains("1. close Inner Door", paper);
    }

    /// <summary>An unfinished step says so rather than saying nothing.</summary>
    [Fact]
    public void AnEmptySlotIsDrawnAsAGap()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("when");

        var paper = PaperTarget.Draw(ship.World, editor);

        Assert.Contains("pick:", paper);
        Assert.Contains("[ ] Airlock Button", paper);
    }

    static void Note(HashSet<string> seen, Ship ship)
    {
        // Written every time, not only at the end: a walk that wrote once would step straight past whatever it
        // could not have written down.
        Assert.NotEqual("", PaperTarget.Draw(ship.World, ship.Editor));

        seen.Add(ship.Editor.Current.GetType().Name);
    }

    static Ship Build()
    {
        var ship = new Ship();
        ship.Rig.Door(1, "Inner Door");
        ship.Rig.Door(2, "Outer Door");
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light");

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner, new Texts());
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
