using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A line of values, as a form gives its answer back.</summary>
public class ValueLineTests
{
    /// <summary>Quoted text reads its entities back: a line break written as one is a break again, and an ampersand is one.</summary>
    [Fact]
    public void QuotedTextReadsItsEntities()
    {
        var values = new List<Literal>();
        ValueLine.Read("\"Cycling&br;Stand clear &amp;br; done\"", values);

        Assert.Equal("Cycling\nStand clear &br; done", Assert.Single(values).Text);
    }
}
