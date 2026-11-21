using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Lumel.Text;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A slot expects one type of block, and the game's own tree of types says what that takes.</summary>
public class SlotTypeTests
{
    /// <summary>A kind stands in the block itself, its type, and each type the game builds that on, short of the terminal block.</summary>
    [Fact]
    public void AKindStandsInTheGamesTypes()
    {
        var sim = Lit();
        var lineage = Kind(sim, "Airlock Light").Lineage;

        Assert.StartsWith("InteriorLight/", lineage[0]);
        Assert.Equal(new[] { "InteriorLight", "LightingBlock", "FunctionalBlock" }, lineage.Skip(1).ToArray());
    }

    /// <summary>
    /// Blocks share the nearest type they all stand in, never one exact block: one light its type, a light and a spotlight a
    /// light, a light and a door a block that switches on and off, and nothing short of the terminal block is any block.
    /// </summary>
    [Fact]
    public void BlocksShareTheNearestType()
    {
        var sim = Lit();
        var light = Kind(sim, "Airlock Light").Lineage;
        var corner = Kind(sim, "Cockpit Light").Lineage;
        var spot = Kind(sim, "Hangar Spotlight").Lineage;
        var door = Kind(sim, "Airlock Inner Door").Lineage;

        Assert.Equal("InteriorLight", Part.Nearest(new[] { light }));
        Assert.Equal("LightingBlock", Part.Nearest(new[] { light, spot }));
        Assert.Equal("FunctionalBlock", Part.Nearest(new[] { light, spot, door }));
        Assert.Equal("", Part.Nearest(new[] { light, new[] { "CargoContainer/LargeBlockSmallContainer", "CargoContainer" } }));
        Assert.Equal("InteriorLight", Part.Nearest(new[] { light, corner }));
    }

    /// <summary>A block fills a slot expecting its own type or any the game builds that on, and an exact block fills only its own.</summary>
    [Fact]
    public void ABlockFitsAlongItsTypes()
    {
        var sim = Lit();
        var spot = Kind(sim, "Hangar Spotlight").Lineage;
        var door = Kind(sim, "Airlock Inner Door").Lineage;

        Assert.True(new Part("lamp") { Expects = "LightingBlock" }.Fits(spot));
        Assert.False(new Part("lamp") { Expects = "LightingBlock" }.Fits(door));
        Assert.True(new Part("anything").Fits(door));
        Assert.True(new Part("exact") { Expects = spot[0] }.Fits(spot));
        Assert.False(new Part("exact") { Expects = spot[0] }.Fits(Kind(sim, "Airlock Light").Lineage));
    }

    /// <summary>
    /// A signal slot takes any signal and its page says so; a block slot's page names the one type it expects, in Sprocket's
    /// own word for a type and the game's for one block.
    /// </summary>
    [Fact]
    public void TheSlotPageNamesTheType()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        editor.GoToRoot();
        screen.Run(5);
        editor.Program.Systems[0].Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(5);
        var part = Assert.Single(editor.Program.Systems[0].Parts);
        Assert.Equal("", part.Expects);
        editor.OpenSlot(part.Id);
        screen.Run(5);
        Assert.Contains(editor.Texts.SlotAnySignal, Said(editor));

        editor.Asked().Finish(true);
        screen.Run(5);
        part.IsSignal = false;
        part.Expects = "ButtonPanel";
        editor.OpenSlot(part.Id);
        screen.Run(5);
        Assert.Contains(editor.Texts.TypeButtonPanel, Said(editor));

        editor.Asked().Finish(true);
        screen.Run(5);
        part.Expects = "SomeModdedType";
        editor.OpenSlot(part.Id);
        screen.Run(5);
        Assert.Contains("SomeModdedType", Said(editor));
    }

    /// <summary>The demonstration construct with a spotlight on it.</summary>
    static Simulation Lit()
    {
        var rig = Rig.Demo();
        rig.Spotlight(90, "Hangar Spotlight");
        var sim = Simulation.On(rig);
        sim.Tick();
        return sim;
    }

    /// <summary>The kind of the block called <paramref name="name" />.</summary>
    static EntityKind Kind(Simulation sim, string name) => sim.Editor.World.Entities.First(entity => entity.Name == name).Kind;

    /// <summary>Every word the open panel says.</summary>
    static List<string> Said(SprocketEditor editor) =>
        editor.Asked().Window.Within().OfType<Label>().Where(label => label.IsVisible).Select(label => label.Text).ToList();
}
