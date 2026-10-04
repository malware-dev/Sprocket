using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Copied text goes into a file in storage, and what a player puts in that file outside the game is what pastes.</summary>
public class ClipboardFileTests
{
    /// <summary>A copy writes the file, and a paste reads whatever the file holds by then.</summary>
    [Fact]
    public void ACopyWritesTheFileAndAPasteReadsIt()
    {
        var folder = new Folder();
        var clipboard = new ClipboardFile(folder, delegate { });

        clipboard.Set("copied\nin the game");
        Assert.Equal("copied\nin the game", folder.Files["sprocket-clipboard.txt"]);

        folder.Write("sprocket-clipboard.txt", "pasted\r\nfrom outside");
        Assert.Equal("pasted\r\nfrom outside", clipboard.Get());
    }

    /// <summary>With no file, a paste gives nothing.</summary>
    [Fact]
    public void WithNoFileAPasteGivesNothing()
    {
        Assert.Equal("", new ClipboardFile(new Folder(), delegate { }).Get());
    }
}
