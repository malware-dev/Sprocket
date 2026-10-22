using Lumel;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The installation panel lists a slot's blocks one under the other, as many as fit its limit.</summary>
public class SlotListTests
{
    /// <summary>A logic system whose one task acts on the first <paramref name="lamps" /> lamps, on its installation panel.</summary>
    static Screen WithLamps(int lamps)
    {
        var rig = Rig.Demo();
        for (var i = 1; i <= lamps; i++)
            rig.Light(40 + i, $"Lamp {i:00}");

        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Lamps");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Lamp 01");
        if (lamps > 1)
            screen.Editor.ThroughBlock($"Lamp {lamps:00}");

        screen.Editor.PressChoice("power");
        screen.Editor.PressPrimary();
        screen.Run(2);
        screen.Editor.BackKeeping();
        screen.Run(2);
        screen.Editor.PressTab(screen.Editor.Texts.SystemInstallationPanel);
        return screen.Run(2);
    }

    /// <summary>The lines of the slot holding the lamps: what each says.</summary>
    static string[] Lines(Screen screen)
    {
        var row = screen.Editor.Current!.View.Within().OfType<PartRow>().Single(part => part.Blocks.Count > 0 && part.Within().OfType<SlotChip>().First().Text.Contains("Lamp"));
        var chip = row.Within().OfType<SlotChip>().First();
        return chip.Children.Select(line => line is BlockName block ? block.Name : ((Lumel.Text.Label)line).Text).ToArray();
    }

    /// <summary>Every block stands on a line of its own, in the order they are listed, and the lines stand one under the other.</summary>
    [Fact]
    public void EachBlockHasALineOfItsOwn()
    {
        var screen = WithLamps(3);

        Assert.Equal(new[] { "Lamp 01", "Lamp 02", "Lamp 03" }, Lines(screen));
        var chip = screen.Editor.Current!.View.Within().OfType<PartRow>().SelectMany(row => row.Within().OfType<SlotChip>()).First(slot => slot.Text.Contains("Lamp"));
        var tops = chip.Children.Select(line => line.Bounds.Y).ToArray();
        Assert.True(tops[1] > tops[0] && tops[2] > tops[1], "the blocks do not stand one under the other");
        Assert.Equal(chip.Children[0].Bounds.X, chip.Children[2].Bounds.X, 1);
    }

    /// <summary>As many blocks as the limit all have their lines.</summary>
    [Fact]
    public void AsManyAsTheLimitAllShow()
    {
        var screen = WithLamps(PartRow.MostListed);

        var lines = Lines(screen);
        Assert.Equal(PartRow.MostListed, lines.Length);
        Assert.Equal($"Lamp {PartRow.MostListed:00}", lines[lines.Length - 1]);
    }

    /// <summary>More than the limit fill all but its last line, and the last says how many more there are.</summary>
    [Fact]
    public void MoreThanTheLimitSayHowManyMore()
    {
        var screen = WithLamps(PartRow.MostListed + 4);

        var lines = Lines(screen);
        Assert.Equal(PartRow.MostListed, lines.Length);
        Assert.Equal($"Lamp {PartRow.MostListed - 1:00}", lines[lines.Length - 2]);
        Assert.Equal("and 5 more", lines[lines.Length - 1]);

        // Its words stand at the right of the list.
        var chip = screen.Editor.Current!.View.Within().OfType<PartRow>().SelectMany(row => row.Within().OfType<SlotChip>()).First(slot => slot.Text.Contains("Lamp"));
        var more = (Lumel.Text.Label)chip.Children[chip.Children.Count - 1];
        Assert.Equal(Align.Right, more.Align);
    }

    /// <summary>A signal slot whose buttons all send the same signal says it once, beside the top of the list.</summary>
    [Fact]
    public void ASignalSharedByEveryBlockIsSaidOnce()
    {
        var rig = Rig.Demo();
        for (var i = 1; i <= 3; i++)
            rig.Panel(60 + i, $"Hangar Button {i}");

        var screen = Screen.On(rig);
        screen.Editor.NewSystem("Hangar");
        screen.Run(2);
        screen.Editor.PressStart(SentenceText.WhenWord);
        screen.Editor.PressBlock("Hangar Button 1");
        screen.Editor.ThroughBlock("Hangar Button 3");
        screen.Editor.TickBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Editor.PressPrimary();
        screen.Run(2);
        screen.Editor.BackKeeping();
        screen.Run(2);
        screen.Editor.PressTab(screen.Editor.Texts.SystemInstallationPanel);
        screen.Run(2);

        var row = screen.Editor.Current!.View.Within().OfType<PartRow>().Single(part => part.Signals.Count > 0);
        Assert.Equal(4, row.Signals.Count);
        var chips = row.Within().OfType<SlotChip>().ToArray();
        Assert.Equal("button 1 is pressed", chips[1].Text);
        Assert.Equal(chips[0].Bounds.Y, chips[1].Bounds.Y, 1);
    }
    /// <summary>A slot's name is a button where pressing it opens the slot, on the logic system's screen, and the name alone on an installation's own panel.</summary>
    [Fact]
    public void TheNameIsAButtonOnlyWhereItOpensTheSlot()
    {
        var screen = WithLamps(2);
        var editing = screen.Editor.Current!.View.Within().OfType<PartRow>().First();
        var name = editing.Within().OfType<Lumel.Controls.Button>().First();
        Assert.NotNull(name.Command);
        Assert.Same(Styles.SlotName, name.Style);

        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.RootRows()[0].Opens!.Execute();
        screen.Run(2);
        var installed = screen.Editor.Current!.View.Within().OfType<PartRow>().First();
        var still = installed.Within().OfType<Lumel.Controls.Button>().First();
        Assert.Null(still.Command);
        Assert.Same(Styles.SlotNameStill, still.Style);
    }

    /// <summary>A signal slot with a short name says it after its name, in brackets; one without says its name alone.</summary>
    [Fact]
    public void ASignalSlotsShortNameFollowsItsName()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.BackKeeping();
        screen.Run(2);
        screen.Editor.PressTab(screen.Editor.Texts.SystemInstallationPanel);
        screen.Run(2);

        var row = screen.Editor.Current!.View.Within().OfType<PartRow>().Single();
        var label = row.Within().OfType<Lumel.Controls.Button>().First().Within().OfType<Lumel.Text.Label>().First();
        Assert.Equal(row.Name, label.Text);

        var name = row.Name;
        screen.Editor.OpenSlot(name);
        screen.Run(2);
        var page = (SlotPage)screen.Editor.Current!;
        Assert.Equal(512, page.Short.Longest);
        screen.Editor.Input!.Focus(page.Short);
        page.Short.Insert("Call");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);
        screen.Editor.PressTab(screen.Editor.Texts.SystemInstallationPanel);
        screen.Run(2);

        row = screen.Editor.Current!.View.Within().OfType<PartRow>().Single();
        Assert.Equal(name + " (Call)", row.Within().OfType<Lumel.Controls.Button>().First().Within().OfType<Lumel.Text.Label>().First().Text);
    }

    /// <summary>A slot's name button, which shows its mark and name as content, is pressed by its automation id, the slot's name.</summary>
    [Fact]
    public void ASlotsNameButtonIsPressedByItsAutomationId()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.BackKeeping();
        screen.Run(2);
        screen.Editor.PressTab(screen.Editor.Texts.SystemInstallationPanel);
        screen.Run(2);

        var row = screen.Editor.Current!.View.Within().OfType<PartRow>().Single();
        var button = row.Within().OfType<Lumel.Controls.Button>().First();
        Assert.Equal("", button.Text);
        Assert.Equal(row.Name, button.AutomationId);

        screen.Do("press", row.Name);
        Assert.IsType<SlotPage>(screen.Editor.Current);
    }

    /// <summary>The line saying how many more there are is said in the player's language.</summary>
    [Fact]
    public void HowManyMoreIsSaidInThePlayersLanguage()
    {
        var screen = WithLamps(PartRow.MostListed + 4);
        screen.Scene.Speak("no");
        screen.Run(2);

        Assert.Equal("og 5 til", Lines(screen).Last());
    }
}
