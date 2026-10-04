using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using VRage.Utils;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Pictures: an image element sized by its picture's shape, and images in markdown-lite.</summary>
public class ImageTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);

    /// <summary>A picture twice as wide as it is tall.</summary>
    static readonly Sprite Banner = new Sprite(MyStringId.GetOrCompute("Banner"), new Vector2(200f, 100f));

    /// <summary>A picture whose size nobody knows.</summary>
    static readonly Sprite Unknown = new Sprite(MyStringId.GetOrCompute("Unknown"), Vector2.Zero);

    /// <summary>An image given its width takes its height from the picture's shape.</summary>
    [Fact]
    public void AnImageGivenAWidthKeepsItsShape()
    {
        var image = new Image { Source = Banner, Placement = Fixed.Wide(100f) };
        _ = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, Wide, High);

        Assert.Equal(new Vector2(100f, 50f), new Vector2(image.Bounds.Width, image.Bounds.Height));
    }

    /// <summary>An image given its height takes its width from the picture's shape.</summary>
    [Fact]
    public void AnImageGivenAHeightKeepsItsShape()
    {
        var image = new Image { Source = Banner, Placement = Fixed.High(30f) };
        _ = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, Wide, High);

        Assert.Equal(new Vector2(60f, 30f), new Vector2(image.Bounds.Width, image.Bounds.Height));
    }

    /// <summary>An image given no size fills the width it is offered, in the picture's shape.</summary>
    [Fact]
    public void AnImageGivenNoSizeFillsItsRoomInShape()
    {
        var image = new Image { Source = Banner };
        _ = new Easel(new VStack { Children = { image } }, Wide, High);

        Assert.Equal(new Vector2(Wide, Wide / 2f), new Vector2(image.Bounds.Width, image.Bounds.Height));
    }

    /// <summary>A picture of no known size is taken as square.</summary>
    [Fact]
    public void APictureOfNoKnownSizeIsSquare()
    {
        var image = new Image { Source = Unknown, Placement = Fixed.Wide(40f) };
        _ = new Easel(new VStack { Children = { new HStack { Children = { image } } } }, Wide, High);

        Assert.Equal(40f, image.Bounds.Height);
    }

    /// <summary>An image in markdown names its picture, describes it, and may say how big it is.</summary>
    [Theory]
    [InlineData("![The logo](Logo)", float.NaN, float.NaN)]
    [InlineData("![](Logo =64)", 64f, float.NaN)]
    [InlineData("![](Logo =64x)", 64f, float.NaN)]
    [InlineData("![](Logo =64x32)", 64f, 32f)]
    [InlineData("![](Logo =x32.5)", float.NaN, 32.5f)]
    public void AnImageIsReadWithItsSize(string source, float width, float height)
    {
        var run = Assert.Single(Markdown.Parse(source).Blocks[0].Runs);

        var image = Assert.IsType<ImageRun>(run);
        Assert.Equal("Logo", image.Picture.Name);
        Assert.Equal(width, image.Picture.Width);
        Assert.Equal(height, image.Picture.Height);
    }

    /// <summary>An image's description is its words, kept for when the picture is missing.</summary>
    [Fact]
    public void AnImageKeepsItsDescription()
    {
        Assert.Equal("The logo", Assert.Single(Markdown.Parse("![The logo](Logo)").Blocks[0].Runs).Text);
    }

    /// <summary>An image whose size is not a size, or whose brackets do not close, is the text it is.</summary>
    [Theory]
    [InlineData("![](Logo =big)")]
    [InlineData("![](Logo =0)")]
    [InlineData("![](Logo 64)")]
    [InlineData("![](Logo")]
    [InlineData("![]()")]
    public void AMalformedImageIsText(string source)
    {
        var runs = Markdown.Parse(source).Blocks[0].Runs;

        Assert.DoesNotContain(runs, r => r is ImageRun);
        Assert.Equal(source, string.Concat(runs.Select(r => r.Text)));
    }

    /// <summary>An image stands among words and in a table's cell.</summary>
    [Fact]
    public void AnImageStandsAmongWordsAndInCells()
    {
        var runs = Markdown.Parse("Fuel ![](Tank) low").Blocks[0].Runs;
        Assert.Equal(new[] { "Fuel ", "", " low" }, runs.Select(r => r is ImageRun ? "" : r.Text));

        var table = Markdown.Parse("| | |\n|--|--|\n| ![](Tank) | low |").Blocks[0];
        Assert.IsType<ImageRun>(Assert.Single(table.Blocks[0].Blocks[0].Runs));
    }

    /// <summary>An image with no size written is as tall as its line, and as wide as its picture's shape makes it.</summary>
    [Fact]
    public void AnImageIsAsTallAsItsLine()
    {
        var easel = Laid("Fuel ![](Banner) low");

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner").Destination;
        Assert.Equal(20f * easel.Pixels, drawn.Height, 1f);
        Assert.Equal(2f * drawn.Height, drawn.Width, 1f);
        Assert.True(Word(easel, "low").Position.X >= drawn.X + drawn.Width - 1f, "the words after the image overlap it");
    }

    /// <summary>An image taller than its line makes the line taller, its words standing on the line's foot.</summary>
    [Fact]
    public void ATallImageGrowsItsLine()
    {
        var easel = Laid("Fuel ![](Banner =x60) low\nnext");

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner").Destination;
        var low = Word(easel, "low").Position.Y;
        Assert.Equal(drawn.Y + drawn.Height - 20f * easel.Pixels, low, 2f);
        Assert.True(Word(easel, "next").Position.Y >= drawn.Y + drawn.Height - 1f, "the next line overlaps the image");
    }

    /// <summary>An image that does not fit after the words before it starts the next line, against the left edge.</summary>
    [Fact]
    public void AnImageThatDoesNotFitStartsALine()
    {
        var easel = Laid("Fuel tanks ![](Banner =400x100)");

        var drawn = Assert.Single(easel.Sprites, s => s.Material.String == "Banner").Destination;
        var words = Word(easel, "Fuel tanks").Position;
        Assert.Equal(words.X, drawn.X, 1f);
        Assert.True(drawn.Y >= words.Y + 20f * easel.Pixels - 1f, "the image stayed on the words' line");
    }

    /// <summary>A centered column sets each cell's lines in its middle.</summary>
    [Fact]
    public void ACenteredColumnCentersItsCells()
    {
        var easel = Laid("| | |\n|:-:|--|\n| A much wider cell | x |\n| ab | y |");

        var wide = Word(easel, "A much wider cell");
        var narrow = Word(easel, "ab");
        Assert.True(narrow.Position.X > wide.Position.X + 1f, "the narrow cell is not set in from the column's edge");
        Assert.True(narrow.Position.X < Word(easel, "y").Position.X, "the narrow cell left its column");
    }

    /// <summary>An image whose picture is not found says its description in its place.</summary>
    [Fact]
    public void AMissingPictureSaysItsDescription()
    {
        var easel = Laid("Welcome ![to Gabbo's](Nowhere)");

        Assert.Empty(easel.Sprites);
        Assert.Equal(Body, Word(easel, "Welcome to Gabbo's").Color);
    }

    static Easel Laid(string markdown)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            Images = new Pictures(),
            FontSize = 20f,
            Color = Body,
            BlockSpacing = 8f,
            Indent = 20f,
            Placement = Fixed.Wide(Wide)
        };

        var easel = new Easel(new VStack { Children = { text } }, Wide, High);
        return easel;
    }

    static Sim.Simulation.DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text.Trim() == text);

    /// <summary>The two pictures above, by name.</summary>
    sealed class Pictures : IImageCatalog
    {
        public IImageSource? Find(string name) => name == "Banner" ? Banner : name == "Unknown" ? Unknown : null;
    }
}
