using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Copied text goes into a file in storage, and what a player puts in that file outside the game is what pastes.</summary>
public class ClipboardFileTests
{
    /// <summary>With no file, a paste gives nothing; a copy writes the file, and a paste reads whatever the file holds by then.</summary>
    [Fact]
    public void ACopyWritesTheFileAndAPasteReadsIt()
    {
        var folder = new Folder();
        var clipboard = new ClipboardFile(folder, delegate { }, null);

        Assert.Equal("", clipboard.Get());

        clipboard.Set("copied\nin the game");
        Assert.Equal("copied\nin the game", folder.Files["sprocket-clipboard.txt"]);

        folder.Write("sprocket-clipboard.txt", "pasted\r\nfrom outside");
        Assert.Equal("pasted\r\nfrom outside", clipboard.Get());
    }

    /// <summary>A copy also reaches the computer's own clipboard, and one it refuses is said while the file is still written.</summary>
    [Fact]
    public void ACopyAlsoReachesTheComputersClipboard()
    {
        var folder = new Folder();
        string? given = null;
        var clipboard = new ClipboardFile(folder, delegate { }, null, text => given = text);

        clipboard.Set("copied");
        Assert.Equal("copied", given);

        var said = "";
        var refusing = new ClipboardFile(folder, text => said = text, null, delegate { throw new InvalidOperationException("busy"); });
        refusing.Set("again");
        Assert.Equal("again", folder.Files["sprocket-clipboard.txt"]);
        Assert.Contains("the clipboard will not take the copy", said);
    }

}
