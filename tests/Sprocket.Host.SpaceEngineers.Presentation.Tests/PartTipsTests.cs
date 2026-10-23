using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A part of a sentence says in its tooltip what it holds, where its chip shortens it, above what pressing it does.</summary>
public class PartTipsTests
{
    /// <summary>A step's blocks are named in the tooltip under the slot they come from and a line saying what they are, one to a line, as many as fit, then how many more.</summary>
    [Fact]
    public void ABlockPartNamesItsBlocksAndItsSlot()
    {
        var rig = Rig.Demo();
        for (var i = 1; i <= HeldBlocks.Most + 2; i++)
            rig.Light(40 + i, $"Lamp {i:00}");

        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Lamps");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Lamp 01");
        screen.Editor.ThroughBlock($"Lamp {HeldBlocks.Most + 2:00}");
        screen.Editor.PressChoice("power");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var row = screen.Editor.StepRows()[0];
        var blocks = row.Slots.OfType<EntitiesSlot>().Single();
        var tip = row.ChipOf(blocks)!.Tip!.Text;
        Assert.Contains("- Lamp 01\n", tip);
        Assert.Contains($"- Lamp {HeldBlocks.Most - 1:00}\n", tip);
        Assert.DoesNotContain($"Lamp {HeldBlocks.Most:00}", tip);
        Assert.StartsWith("From the slot **", tip);
        Assert.Contains("\n" + screen.Editor.Texts.PartStepBlocks + "\n- Lamp 01\n", tip);
        Assert.EndsWith("- and 3 more\n", tip);
        Assert.DoesNotContain(screen.Editor.Texts.SequenceBlocksAbout, tip);
    }

    /// <summary>A slot's blocks on the installation panel say that pressing them chooses what fills the slot.</summary>
    [Fact]
    public void AnInstallationPanelsBlocksSayWhatPressingDoes()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.Back();
        screen.Run(2);
        screen.Editor.PressTab(screen.Editor.Texts.SystemInstallationPanel);
        screen.Run(2);

        var row = screen.Editor.Current!.View.Within().OfType<Features.Blueprints.PartRow>().First();
        var chips = row.Within().OfType<SlotChip>().Where(chip => chip.IsVisible).ToArray();
        Assert.NotEmpty(chips);
        Assert.All(chips, chip => Assert.Equal(screen.Editor.Texts.PartFillsAbout, chip.Tip?.Text));
    }

    /// <summary>A delay's tooltip gives its length in the unit it is not written in.</summary>
    [Fact]
    public void ALengthSaysItselfInTheOtherUnit()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("delay");
        screen.Editor.GiveLength("2");
        screen.Run(2);

        var row = screen.Editor.StepRows()[0];
        var length = row.Slots.OfType<DurationSlot>().Single();
        var tip = row.ChipOf(length)!.Tip!.Text;
        Assert.StartsWith(length.Length.InSeconds ? @"\= 120 ticks" : @"\= 2 seconds", tip);
    }
}
