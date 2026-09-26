using Sprocket.Host.SpaceEngineers.Localization;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Texts read once per language and said on request: as written, or translated.</summary>
public class PhrasebookTests
{
    static readonly Phrase Empty = new Phrase(0, "T.Empty", "Nothing automated here yet.");
    static readonly Phrase Named = new Phrase(1, "T.Named", "Made from **{name}** blueprint");

    static readonly Phrase[] All = { Empty, Named };

    /// <summary>A text is said exactly as written, braces and all, until a language is loaded.</summary>
    [Fact]
    public void ATextIsSaidAsWritten()
    {
        var book = new Phrasebook(All);

        Assert.Equal("Nothing automated here yet.", book.Say(Empty));
        Assert.Equal("Made from **{name}** blueprint", book.Say(Named));
    }

    /// <summary>A translation's words are said as they are, and a text it leaves out is said as written.</summary>
    [Fact]
    public void ATranslationIsSaid()
    {
        var book = new Phrasebook(All);
        book.Load(new Source("T.Named", "Laget fra **{name}**-plantegningen"));

        Assert.Equal("Laget fra **{name}**-plantegningen", book.Say(Named));
        Assert.Equal("Nothing automated here yet.", book.Say(Empty));
    }

    /// <summary>Loading nothing goes back to the texts as written.</summary>
    [Fact]
    public void LoadingNothingGoesBack()
    {
        var book = new Phrasebook(All);
        book.Load(new Source("T.Empty", "Ingenting."));
        book.Load(null);

        Assert.Equal("Nothing automated here yet.", book.Say(Empty));
    }

    /// <summary>Saying a text hands back the one string read for it, so saying it again costs nothing.</summary>
    [Fact]
    public void SayingATextAgainIsTheSameString()
    {
        var book = new Phrasebook(All);
        book.Load(new Source("T.Empty", "Ingenting."));

        Assert.Same(book.Say(Empty), book.Say(Empty));
    }

    /// <summary>A list whose texts are not at their own index is refused where it is made, not where it is said.</summary>
    [Fact]
    public void TextsOutOfPlaceAreRefused()
    {
        Assert.Throws<ArgumentException>(() => new Phrasebook(new[] { Named }));
    }

    /// <summary>Texts keyed by the pairs given.</summary>
    sealed class Source : ITextSource
    {
        readonly Dictionary<string, string> _texts = new Dictionary<string, string>();

        public Source(params string[] pairs)
        {
            for (var i = 0; i + 1 < pairs.Length; i += 2)
                _texts[pairs[i]] = pairs[i + 1];
        }

        public string? Find(string key) => _texts.TryGetValue(key, out var text) ? text : null;
    }
}
