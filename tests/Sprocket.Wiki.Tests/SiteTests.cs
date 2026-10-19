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

    /// <summary>The wiki's own pages are copied as they are and listed after the codex in the sidebar, each named for its file.</summary>
    [Fact]
    public void TheWikisOwnPagesAreWrittenAndListed()
    {
        var folder = Given.Folder();
        try
        {
            new Site(Given.Codex, Given.Catalog(), Path.Combine(Given.Repository, "docs", "wiki")).Write(folder);

            Assert.Equal(File.ReadAllText(Path.Combine(Given.Repository, "docs", "wiki", "Settings.md")), File.ReadAllText(Path.Combine(folder, "Settings.md")));
            var sidebar = File.ReadAllText(Path.Combine(folder, "_Sidebar.md"));
            Assert.EndsWith("**[Settings](Settings)**\n", sidebar);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>A page of the wiki's own named as a page of the codex stops the wiki being written, rather than one losing to the other.</summary>
    [Fact]
    public void AnOwnPageNamedAsATermStops()
    {
        var folder = Given.Folder();
        var own = Given.Folder();
        try
        {
            File.WriteAllText(Path.Combine(own, "home.md"), "Mine.");

            var stopped = Assert.Throws<InvalidDataException>(() => new Site(Given.Codex, Given.Catalog(), own).Write(folder));
            Assert.Contains("home.md", stopped.Message);
        }
        finally
        {
            Directory.Delete(folder, true);
            Directory.Delete(own, true);
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
