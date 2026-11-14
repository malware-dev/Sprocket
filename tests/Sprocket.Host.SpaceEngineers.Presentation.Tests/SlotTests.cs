using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Lumel.Input;
using Lumel.Text;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A part of a sentence, a button looking as its style says in each of its states.</summary>
public class SlotTests
{
    /// <summary>A part that can be pressed is the game's button, with the ink, and lights under the pointer.</summary>
    [Fact]
    public void APartThatCanBePressedLooksLikeOne()
    {
        var chip = Pressable();

        Assert.Equal(Palette.Panel, chip.Background);
        Assert.Equal(Palette.ControlEdge, chip.Border);
        Assert.Equal(Palette.Ink, Words(chip).Color);

        ((IInputTarget)chip).IsPointerOver = true;
        Assert.Equal(Palette.PanelHot, chip.Background);
    }

    /// <summary>A part nothing fills says so in the warning color.</summary>
    [Fact]
    public void AnEmptyPartReadsAsAnInvitation()
    {
        var chip = Pressable();
        chip.IsEmpty = true;

        Assert.Equal(Palette.Notice, Words(chip).Color);
    }

    /// <summary>A part that cannot be pressed keeps its box on a quiet ground with no border, and does not light under the pointer.</summary>
    [Fact]
    public void APartThatCannotBePressedStaysStill()
    {
        var chip = new SlotChip();
        chip.Say("Airlock");
        chip.Restyle();
        ((IInputTarget)chip).IsPointerOver = true;

        Assert.Equal(Palette.Well, chip.Background);
        Assert.Equal(Palette.None, chip.Border);
    }

    /// <summary>A part whose command cannot be carried out now is still as well.</summary>
    [Fact]
    public void APartWhoseCommandIsOffIsStill()
    {
        var chip = Pressable();
        ((RelayCommand)chip.Command!).CanExecute = false;

        Assert.Equal(Palette.Well, chip.Background);
        Assert.Equal(Palette.None, chip.Border);
    }

    static SlotChip Pressable()
    {
        var chip = new SlotChip { Command = new RelayCommand(() => { }) };
        chip.Say("Airlock");
        chip.Restyle();
        return chip;
    }

    static Label Words(SlotChip chip) => chip.Within().OfType<Label>().Single();
}
