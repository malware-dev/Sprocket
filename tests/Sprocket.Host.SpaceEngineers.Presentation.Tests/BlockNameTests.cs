using Lumel.Layout;
using Sprocket.Sim.Machine;
using VRage.Utils;
using Xunit;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A block wherever it is named: its icon, then its name, in the parts of a sentence as well.</summary>
public class BlockNameTests
{
    static readonly MyStringId Picture = MyStringId.GetOrCompute("Picture");

    /// <summary>An image draws its material across the room its placement gives it.</summary>
    [Fact]
    public void AnImageDrawsItsMaterialAcrossItsBounds()
    {
        var image = new Image { Material = Picture, Placement = Fixed.Size(24f, 24f) };
        var easel = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, 400f, 200f);

        var drawn = easel.Sprites.Single(sprite => sprite.Material == Picture);
        Assert.Equal(24f, image.Bounds.Width);
        Assert.Equal(24f, image.Bounds.Height);
        Assert.True(drawn.Destination.Width > 0);
    }

    /// <summary>An image given no material draws nothing.</summary>
    [Fact]
    public void AnImageWithNoMaterialDrawsNothing()
    {
        var easel = new Easel(new VStack { Children = { new HStack { Children = { new Image { Placement = Fixed.Size(24f, 24f) } } } } }, 400f, 200f);

        Assert.Empty(easel.Sprites);
    }

    /// <summary>A block's icon stands before its name, as tall as a line of it.</summary>
    [Fact]
    public void TheIconStandsBeforeTheNameAsTallAsALine()
    {
        var block = new BlockName { Name = "Airlock Door", Icon = "Door", FontSize = 32f };
        var easel = new Easel(new VStack { Children = { new HStack { Children = { block } } } }, 400f, 200f);

        var icon = block.Within().OfType<Image>().Single();
        var name = easel.Written.Single(text => text.Text == "Airlock Door");

        Assert.Equal(32f, icon.Bounds.Height);
        Assert.True(icon.Bounds.X + icon.Bounds.Width <= name.Position.X / easel.Pixels);
    }

    /// <summary>A block whose kind has no icon is shown by its name alone.</summary>
    [Fact]
    public void ABlockWithNoIconShowsItsNameAlone()
    {
        var block = new BlockName { Name = "Airlock Door", Icon = "" };
        var easel = new Easel(new VStack { Children = { new HStack { Children = { block } } } }, 400f, 200f);

        Assert.Empty(easel.Sprites);
        Assert.Single(easel.Written, text => text.Text == "Airlock Door");
    }

    /// <summary>The part of a sentence naming a block shows the block with its icon.</summary>
    [Fact]
    public void APartNamingABlockShowsItWithItsIcon()
    {
        var sim = Built();
        var chip = sim.Editor.Triggers()[0].Within().OfType<Slot>().First();

        var block = chip.Within().OfType<BlockName>().Single();
        Assert.Equal("Airlock Button", block.Name);
        Assert.False(string.IsNullOrEmpty(block.Icon));
        Assert.Equal("Airlock Button", chip.Text);
    }

    /// <summary>
    /// Named in pieces, a selection reads word for word as it does named in one piece, each block with its icon
    /// and a block that is gone with none.
    /// </summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    public void PiecesReadAsTheWholeDoes(int count, bool requiresAll)
    {
        var sim = Built();
        var button = ((EntitiesSlot)sim.Editor.Triggers()[0].Starts[0]).Entities[0];

        var ids = new List<long> { -1 };
        while (ids.Count < count)
            ids.Add(button);

        var pieces = new List<Mention>();
        SentenceText.Entities(sim.World, ids, requiresAll, pieces);

        Assert.Equal(SentenceText.Entities(sim.World, ids, requiresAll), string.Concat(pieces.Select(piece => piece.Text)));
        Assert.Null(pieces[0].Icon);
        Assert.All(pieces.Where(piece => piece.Text == "Airlock Button"), piece => Assert.False(string.IsNullOrEmpty(piece.Icon)));
    }

    /// <summary>A part that says new words over the same blocks keeps the elements it had.</summary>
    [Fact]
    public void NewWordsOverTheSameBlocksMakeNothingNew()
    {
        var chip = new Slot();
        chip.Say(new List<Mention> { new("Inner Door", "Door"), new(" or ", null), new("Outer Door", "Door") });
        var before = chip.Within().Skip(1).ToList();

        chip.Say(new List<Mention> { new("Inner Door", "Door"), new(" and ", null), new("Outer Door", "Door") });

        Assert.Equal(before, chip.Within().Skip(1).ToList());
        Assert.Equal("Inner Door and Outer Door", chip.Text);
    }

    /// <summary>A logic system with a trigger started by the airlock's button, on its own page.</summary>
    static Simulation Built()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.Back();
        sim.Tick();
        return sim;
    }
}
