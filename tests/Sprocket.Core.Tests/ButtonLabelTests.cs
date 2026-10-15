using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A button panel's labels, written and read by the button's number, and who may use it.</summary>
public class ButtonLabelTests
{
    /// <summary>A label is written on the button a step names, counted from 1, and an empty one gives back the game's own.</summary>
    [Fact]
    public void ALabelIsWrittenOnTheButtonNamed()
    {
        var ship = new Rigged();
        var panel = ship.Panel(1, "Panel", 4);

        Ran.Step(ship, panel, ButtonPanel.Label, Literal.Of(2L), Literal.Of("Open"));
        Assert.Equal("Open", panel.Label(1));
        Assert.Equal("Open", Ran.Read(ship, panel, ButtonPanel.LabelOf, Literal.Of(2L)).Text);

        Ran.Step(ship, panel, ButtonPanel.Label, Literal.Of(2L), Literal.Of(""));
        Assert.Equal("No action", panel.Label(1));
    }

    /// <summary>A button the panel does not have is never handed to the game, and a function refuses it.</summary>
    [Fact]
    public void AButtonThePanelLacksIsLeftAlone()
    {
        var ship = new Rigged();
        var panel = ship.Panel(1, "Panel", 4);

        Ran.Step(ship, panel, ButtonPanel.Label, Literal.Of(5L), Literal.Of("Open"));

        Assert.DoesNotContain(ship.Rig.Log, line => line.StartsWith("Panel:label"));
        var label = ship.Entity(panel.Id).Kind.FindAction(ButtonPanel.LabelOf, Nature.Value)!.Being!;
        Assert.Equal("button", label.Refused(new[] { Literal.Of(5L) }));
        Assert.Equal("button", label.Refused(new[] { Literal.Of(0L) }));
        Assert.Null(label.Refused(new[] { Literal.Of(4L) }));
    }

    /// <summary>Whether a button has anything on its slot is read by its number.</summary>
    [Fact]
    public void WhetherAButtonHasAnActionIsReadByItsNumber()
    {
        var ship = new Rigged();
        var panel = ship.Panel(1, "Panel", 4);
        var changes = panel.Toolbar.Changing();
        changes.Set(2, default(MyToolbarItemInfo));
        changes.Apply();

        Assert.Equal(Literal.Of(true), Ran.Read(ship, panel, ButtonPanel.Assigned, Literal.Of(3L)));
        Assert.Equal(Literal.Of(false), Ran.Read(ship, panel, ButtonPanel.Assigned, Literal.Of(1L)));
    }

    /// <summary>Each of the functions takes the button and nothing else.</summary>
    [Fact]
    public void TheFunctionsTakeTheButton()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Panel(1, "Panel", 4).Id).Kind;

        var label = Assert.Single(kind.FindAction(ButtonPanel.LabelOf, Nature.Value)!.Being!.Parameters);
        Assert.Equal("button", label.Name);
        Assert.Equal("4", label.Most);
        Assert.Single(kind.FindAction(ButtonPanel.Assigned, Nature.Value)!.Being!.Parameters);
    }

    /// <summary>
    /// A button panel vouches for its labels, whether a button has an action, and who may use it with its toggle, and not for
    /// the terminal's label box or the pair that sets who may use it.
    /// </summary>
    [Fact]
    public void APanelVouchesForItsButtonsAndAccessAndNotTheirPairs()
    {
        var ship = new Rigged();
        var panel = ship.Panel(1, "Panel", 4);
        panel.Carrying("AnyoneCanUse", "Anyone can use", ValueKind.Flag)
            .Carrying("ButtonName", "Button name", ValueKind.Text)
            .Offering("AnyoneCanUse", "Anyone can use On/Off")
            .Offering("AnyoneCanUse_On", "Anyone can use On")
            .Offering("AnyoneCanUse_Off", "Anyone can use Off");
        var kind = ship.Entity(panel.Id).Kind;

        Assert.True(kind.FindAction(ButtonPanel.Label, Nature.Native)!.Vetted);
        Assert.True(kind.FindAction(ButtonPanel.LabelOf, Nature.Value)!.Vetted);
        Assert.True(kind.FindAction(ButtonPanel.Assigned, Nature.Value)!.Vetted);
        Assert.True(kind.FindAction("AnyoneCanUse", Nature.Value)!.Vetted);
        Assert.True(kind.FindAction("AnyoneCanUse", Nature.Action)!.Vetted);

        Assert.False(kind.FindAction("ButtonName", Nature.Value)!.Vetted);
        Assert.False(kind.FindAction("AnyoneCanUse_On", Nature.Action)!.Vetted);
        Assert.False(kind.FindAction("AnyoneCanUse_Off", Nature.Action)!.Vetted);
    }

    /// <summary>A panel offers flipping who may use it on the value's own switch, its toggle kept to the terminal view, where every other block keeps it to the value.</summary>
    [Fact]
    public void APanelOffersFlippingWhoMayUseIt()
    {
        var ship = new Rigged();
        var panel = ship.Panel(1, "Panel", 4);
        panel.Offering("AnyoneCanUse", "Anyone can use On/Off").Carrying("AnyoneCanUse", "Anyone can use", ValueKind.Flag);
        var lamp = ship.Light(2, "Lamp");
        lamp.Offering("AnyoneCanUse", "Anyone can use On/Off").Carrying("AnyoneCanUse", "Anyone can use", ValueKind.Flag);

        var toggle = ship.Entity(panel.Id).Kind.FindAction("AnyoneCanUse", Nature.Action)!;
        Assert.True(toggle.Hidden);
        Assert.Equal("toggle anyone can use", toggle.Text);
        Assert.Equal("anyone can use", ship.Entity(panel.Id).Kind.FindAction("AnyoneCanUse", Nature.Value)!.Text);
        Assert.True(ship.Entity(panel.Id).Kind.FindAction("AnyoneCanUse", Nature.Value)!.Flips);
        Assert.True(ship.Entity(lamp.Id).Kind.FindAction("AnyoneCanUse", Nature.Action)!.Hidden);
    }
}
