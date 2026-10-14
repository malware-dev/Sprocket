using System.Text.RegularExpressions;
using Sprocket.Wiki.Reading;
using Xunit;

namespace Sprocket.Wiki.Tests;

public class SiteTests
{
    /// <summary>The codex the mod ships is written whole: a page for every term, named for its title, the sidebar, and every picture a page shows.</summary>
    [Fact]
    public void TheCodexIsWrittenWhole()
    {
        var folder = Given.Folder();
        try
        {
            var codex = Given.Codex;
            new Site(codex, Given.Catalog()).Write(folder);

            foreach (var term in codex.Terms)
                Assert.True(File.Exists(Path.Combine(folder, Site.Name(term) + ".md")), term.Id + " has no page");

            Assert.True(File.Exists(Path.Combine(folder, "Home.md")));
            Assert.True(File.Exists(Path.Combine(folder, "_Sidebar.md")));

            foreach (var page in Directory.GetFiles(folder, "*.md"))
            {
                foreach (Match picture in Regex.Matches(File.ReadAllText(page), "images/[a-z/-]+\\.png"))
                    Assert.True(File.Exists(Path.Combine(folder, picture.Value)), Path.GetFileName(page) + " shows " + picture.Value + ", which was not written");
            }
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>A term whose id is not its title stops the wiki being written, since a link to it would find no page.</summary>
    [Fact]
    public void ATermNotNamedForItsTitleStops()
    {
        var folder = Given.Folder();
        try
        {
            var codex = new Codex(new[] { new Term("picking", "Selector", "# Selector\n", "A selector.", false) });

            var stopped = Assert.Throws<InvalidDataException>(() => new Site(codex, Given.Catalog()).Write(folder));
            Assert.Contains("picking", stopped.Message);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>A folder holding anything but a wiki is left as it is, so a mistyped folder is never emptied.</summary>
    [Fact]
    public void AFolderThatIsNoWikiIsLeftAlone()
    {
        var folder = Given.Folder();
        try
        {
            File.WriteAllText(Path.Combine(folder, "README.md"), "Mine.");

            Assert.Throws<InvalidDataException>(() => new Site(Given.Terms, Given.Catalog()).Write(folder));
            Assert.Equal("Mine.", File.ReadAllText(Path.Combine(folder, "README.md")));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
