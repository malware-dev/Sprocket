using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The marks found by locator, and every other locator handed on to the catalog the marks were given.</summary>
public class MarkImagesTests
{
    /// <summary>Each kind's mark is found by <c>mark:</c> and the kind, and is the same picture every time it is asked for.</summary>
    [Theory]
    [InlineData("mark:sequence")]
    [InlineData("mark:signal")]
    [InlineData("mark:action")]
    [InlineData("mark:awaited")]
    [InlineData("mark:value")]
    [InlineData("mark:block")]
    [InlineData("mark:blueprint")]
    [InlineData("mark:logic-system")]
    [InlineData("mark:installation")]
    public void EachKindsMarkIsFound(string locator)
    {
        var marks = new MarkImages(new Nothing());

        var mark = marks.Find(locator);

        Assert.NotNull(mark);
        Assert.Same(mark, marks.Find(locator));
    }

    /// <summary>A kind with no mark is no picture, and is never handed on.</summary>
    [Fact]
    public void AKindWithNoMarkIsNoPicture()
    {
        var rest = new Nothing();

        Assert.Null(new MarkImages(rest).Find("mark:nothing"));
        Assert.Empty(rest.Asked);
    }

    /// <summary>A locator that names no mark is found by the catalog given, as it was written.</summary>
    [Fact]
    public void AnyOtherLocatorIsHandedOn()
    {
        var rest = new Nothing();

        new MarkImages(rest).Find("Sprocket_Logo");

        Assert.Equal(new[] { "Sprocket_Logo" }, rest.Asked);
    }

    /// <summary>A catalog that finds nothing, and keeps what it was asked for.</summary>
    sealed class Nothing : IImageCatalog
    {
        public List<string> Asked { get; } = new List<string>();

        public IImageSource? Find(string name)
        {
            Asked.Add(name);
            return null;
        }
    }
}
