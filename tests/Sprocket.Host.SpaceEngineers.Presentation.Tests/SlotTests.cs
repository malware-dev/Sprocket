using Lumel.Input;
using Lumel.Text;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A part of a sentence, looking as its style says in each of its states.</summary>
public class SlotTests
{
    /// <summary>A part that can be pressed has the bright fill and the ink, and lights under the pointer.</summary>
    [Fact]
    public void APartThatCanBePressedLooksLikeOne()
    {
        var chip = Pressable();

        Assert.Equal(Palette.SlotFill, chip.Background);
        Assert.Equal(Palette.Ink, Words(chip).Color);

        chip.IsHot = true;
        Assert.Equal(Palette.Well, chip.Background);
    }

    /// <summary>A part nothing fills says so in the warning color.</summary>
    [Fact]
    public void AnEmptyPartReadsAsAnInvitation()
    {
        var chip = Pressable();
        chip.IsEmpty = true;

        Assert.Equal(Palette.Notice, Words(chip).Color);
    }

    /// <summary>A part that cannot be pressed sits a notch off the page and does not light under the pointer.</summary>
    [Fact]
    public void APartThatCannotBePressedStaysStill()
    {
        var chip = new SlotChip();
        chip.Say("Airlock");
        chip.IsHot = true;

        Assert.Equal(Palette.Chrome, chip.Background);
    }

    /// <summary>A part whose command cannot be carried out now is still as well.</summary>
    [Fact]
    public void APartWhoseCommandIsOffIsStill()
    {
        var chip = Pressable();
        ((Relay)chip.Command!).CanExecute = false;

        Assert.Equal(Palette.Chrome, chip.Background);
    }

    static SlotChip Pressable()
    {
        var chip = new SlotChip { Command = new Relay(() => { }) };
        chip.Say("Airlock");
        return chip;
    }

    static Label Words(SlotChip chip) => chip.Within().OfType<Label>().Single();
}
