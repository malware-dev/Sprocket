using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Xunit;

namespace Sprocket.Tests;

/// <summary>Files packed into the world's carrier come back as they were written, and a carrier that will not read is never written over.</summary>
public class CarrierFilesTests
{
    /// <summary>Every file unpacks to exactly what was written, whatever its lines start with or end in.</summary>
    [Fact]
    public void FilesUnpackAsTheyWereWritten()
    {
        var files = new Dictionary<string, string>
        {
            ["state"] = "running 3\nwaiting 2",
            ["empty"] = "",
            ["newlines"] = "\n\nlast\n",
            ["marks"] = "=\n= not a file\n== two\n=== three",
            ["windows"] = "a\r\nb\r\n",
            ["= odd name"] = "x"
        };

        var written = new CarrierFiles();
        foreach (var file in files)
            written.Write(file.Key, file.Value);

        var read = new CarrierFiles();
        Assert.Equal("", read.Unpack(written.Pack()));

        Assert.False(read.Changed);
        Assert.Equal(files.Count, read.Count);
        foreach (var file in files)
            Assert.Equal(file.Value, read.Read(file.Key));
    }

    /// <summary>A line of a file that starts with the mark is written with one more, so it never reads as the start of a file.</summary>
    [Fact]
    public void ALineStartingWithTheMarkIsWrittenWithOneMore()
    {
        var files = new CarrierFiles();
        files.Write("a", "= b");

        Assert.Equal("files 1\n= a\n== b\n", files.Pack());
    }

    /// <summary>A carrier with nothing in it holds no files.</summary>
    [Fact]
    public void AnEmptyCarrierHoldsNoFiles()
    {
        var files = new CarrierFiles();

        Assert.Equal("", files.Unpack(""));
        Assert.Equal(0, files.Count);
        Assert.False(files.Changed);
    }

    /// <summary>Text that will not read gives no files, says what was wrong, and is packed over at the next save.</summary>
    [Theory]
    [InlineData("files 2\n")]
    [InlineData("files 1\nno file\n")]
    [InlineData("backups 1\nbackup 2026-10-02T14:30:00.0000000Z 1 7 4 1\nShip\n")]
    public void FilesThatWillNotReadAreIgnoredAndWrittenOver(string text)
    {
        var files = new CarrierFiles();

        Assert.NotEqual("", files.Unpack(text));
        Assert.Equal(0, files.Count);
        Assert.True(files.Changed);

        files.Write("state", "x");
        Assert.Equal("files 1\n= state\nx\n", files.Pack());
    }

    /// <summary>Writing or deleting a file marks the files changed, and packing them marks them unchanged.</summary>
    [Fact]
    public void ChangesAreMarkedUntilPacked()
    {
        var files = new CarrierFiles();
        Assert.Equal("", files.Unpack("files 1\n= a\nx\n"));
        Assert.False(files.Changed);

        files.Delete("missing");
        Assert.False(files.Changed);

        files.Delete("a");
        Assert.True(files.Changed);

        files.Pack();
        Assert.False(files.Changed);

        files.Write("b", "y");
        Assert.True(files.Changed);
    }
}
