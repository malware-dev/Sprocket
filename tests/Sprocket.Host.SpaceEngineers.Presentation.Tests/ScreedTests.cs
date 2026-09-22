using System.Text;
using Sprocket.Host.SpaceEngineers.Layout;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A listing of thousands of lines costs a window's worth of drawing, not a listing's worth.
/// </summary>
/// <remarks>
/// The defect this guards against draws the right picture: every line issued as a call, the scissor
/// throwing away all but a screenful. Nothing on screen says it is happening, so it is counted.
/// </remarks>
public class ScreedTests
{
    const float Width = 800f;
    const float Height = 600f;

    static string Lines(int count)
    {
        var text = new StringBuilder();
        for (var i = 0; i < count; i++)
            text.Append("line ").Append(i).Append('\n');

        return text.ToString(0, text.Length - 1);
    }

    static Screed Long(int count)
    {
        return new Screed { Text = Lines(count) };
    }

    /// <summary>
    /// Lays a control out and draws it, for a test that is about neither.
    /// </summary>
    /// <remarks>
    /// A control has no bounds until it has been arranged, so anything that presses at one has to
    /// have drawn it first even where the frame is not what is being asked about.
    /// </remarks>
    static void Laid(Screed screed)
    {
        new Easel(screed, Width, Height).Draw(Width, Height);
    }

    [Fact]
    public void ItHoldsEveryLineItWasGiven()
    {
        Assert.Equal(5000, Long(5000).Lines);
    }

    /// <summary>Its height is the line height times the count, which is arithmetic, not measurement.</summary>
    [Fact]
    public void ItStandsAsTallAsItsLines()
    {
        Screed screed = Long(200);
        var easel = new Easel(screed, Width, Height);

        Assert.Equal(200 * easel.LineHeight, screed.Extent, 2);
    }

    /// <summary>It takes the room it is given rather than growing to the text.</summary>
    [Fact]
    public void ItIsAWindowAndNotAColumn()
    {
        Screed screed = Long(5000);
        var easel = new Easel(screed, Width, Height);

        Assert.Equal(Height, screed.DesiredSize.Y, 2);
        Assert.True(screed.Extent > Height, "the listing is shorter than its own window, so this proves nothing");
        Assert.True(easel.Written.Count() * easel.LineHeight <= Height + easel.LineHeight,
            "it drew more lines than the window is tall");
    }

    /// <summary>A window held still measures nothing, however many frames go past.</summary>
    [Fact]
    public void AWindowHeldStillMeasuresNothing()
    {
        var easel = new Easel(Long(5000), Width, Height);

        int settled = easel.Measured;

        for (var i = 0; i < 60; i++)
            easel.Draw(Width, Height);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>
    /// Only the lines somebody has actually looked at are ever measured.
    /// </summary>
    /// <remarks>
    /// The number is the point, and it is the whole reason the sideways reach is learned rather than
    /// swept: measuring a thousand lines up front was timed at 22ms, which is a frame and a half of
    /// frozen game for text nobody has scrolled to yet.
    /// </remarks>
    [Fact]
    public void ItMeasuresOnlyTheLinesItHasShown()
    {
        var easel = new Easel(Long(1000), Width, Height);

        var fits = (int)(Height / easel.LineHeight);

        Assert.InRange(easel.Measured, fits - 1, fits + 2);
    }

    /// <summary>
    /// Moving down one line measures one line, because one line is all that came into view.
    /// </summary>
    /// <remarks>
    /// What it remembers is the band it showed last frame, not a note per line, so this is the number
    /// that says the remembering works.
    /// </remarks>
    [Fact]
    public void ScrollingMeasuresOnlyWhatComesIntoView()
    {
        Screed screed = Long(1000);
        var easel = new Easel(screed, Width, Height);

        int settled = easel.Measured;

        screed.Scrolled = easel.LineHeight;
        easel.Draw(Width, Height);

        Assert.Equal(settled + 1, easel.Measured);
    }

    /// <summary>The reach grows as longer lines are met, and never shrinks back.</summary>
    [Fact]
    public void TheReachGrowsAsLongerLinesAreMet()
    {
        var screed = new Screed { Text = Lines(500) + "\n" + new string('x', 400) + "\n" + Lines(500) };
        var easel = new Easel(screed, Width, Height);

        float known = screed.Widest;
        Assert.False(screed.CanMove(Way.Across), "the short lines at the top already reach past the window");

        screed.Scrolled = 500f * easel.LineHeight;
        easel.Draw(Width, Height);

        Assert.True(screed.Widest > known, "meeting the long line taught it nothing");
        Assert.True(screed.CanMove(Way.Across), "the long line is on screen and still cannot be reached");

        float met = screed.Widest;

        screed.Scrolled = 0f;
        easel.Draw(Width, Height);

        Assert.Equal(met, screed.Widest);
    }

    /// <summary>The whole point: the calls are what the window shows and nothing more.</summary>
    [Fact]
    public void ItDrawsOnlyWhatIsOnShow()
    {
        var easel = new Easel(Long(5000), Width, Height);

        var fits = (int)(Height / easel.LineHeight);

        Assert.InRange(easel.Written.Count(), fits - 1, fits + 2);
    }

    /// <summary>Scrolling moves the window rather than growing it.</summary>
    [Fact]
    public void ScrollingShowsOtherLinesAndNoMoreOfThem()
    {
        Screed screed = Long(5000);
        var easel = new Easel(screed, Width, Height);

        int before = easel.Written.Count();
        Assert.Contains(easel.Written, c => c.Text == "line 0");

        screed.Scrolled = 4000f * easel.LineHeight;
        easel.Draw(Width, Height);

        Assert.DoesNotContain(easel.Written, c => c.Text == "line 0");
        Assert.Contains(easel.Written, c => c.Text == "line 4000");
        Assert.InRange(easel.Written.Count(), before - 1, before + 1);
    }

    /// <summary>A line longer than the room is reachable, which is what the sideways axis is for.</summary>
    [Fact]
    public void ALineTooWideForTheRoomCanBeReached()
    {
        var screed = new Screed { Text = new string('x', 400) };
        var easel = new Easel(screed, Width, Height);

        Assert.True(screed.Widest > Width, "the line fits, so there is nothing to reach");
        Assert.True(screed.CanMove(Way.Across), "it cannot be moved sideways");

        float first = easel.Written.First().Position.X;

        screed.MoveBy(Way.Across, screed.Widest);
        easel.Draw(Width, Height);

        Assert.True(easel.Written.First().Position.X < first, "moving sideways drew it in the same place");
    }

    /// <summary>Sideways stops at the end of the longest line rather than running on into nothing.</summary>
    [Fact]
    public void SidewaysStopsAtTheLongestLine()
    {
        var screed = new Screed { Text = new string('x', 400) };
        var easel = new Easel(screed, Width, Height);

        screed.MoveBy(Way.Across, 100000f);
        easel.Draw(Width, Height);

        Assert.Equal(screed.Widest - Width, screed.Shifted, 1);
    }

    /// <summary>A thumb is drawn for each direction that has something out of sight, and no others.</summary>
    [Fact]
    public void AThumbIsDrawnForEachDirectionThatHasSomewhereToGo()
    {
        var screed = new Screed
        {
            Text = Lines(500),
            BarWidth = 8f,
            BarColor = Color.White
        };

        var easel = new Easel(screed, Width, Height);

        Assert.Single(easel.Sprites);

        // Put a line longer than the window at the top, where it is met straight away. Down at the
        // bottom it would not be, and that is the point of the test above rather than this one.
        screed.Text = new string('x', 400) + "\n" + Lines(500);
        easel.Draw(Width, Height);

        Assert.Equal(2, easel.Sprites.Count());
    }

    /// <summary>
    /// The text can be taken hold of and dragged, and goes where the hand goes.
    /// </summary>
    /// <remarks>
    /// Dragging upwards pushes the sheet up, which brings what is below into view - so the window moves
    /// down. Getting this the wrong way round is the classic bug and reads as the screen fighting you.
    /// </remarks>
    [Fact]
    public void TheTextCanBeDraggedAbout()
    {
        Screed screed = Long(5000);
        var easel = new Easel(screed, Width, Height);

        Assert.True(screed.Take(new Vector2(100f, 100f)), "it refused to be taken hold of");
        Assert.True(screed.IsPanning);

        screed.Carry(new Vector2(100f, 40f));
        easel.Draw(Width, Height);

        Assert.Equal(60f, screed.Scrolled, 1);

        screed.Drop();
        Assert.False(screed.IsPanning);

        screed.Carry(new Vector2(100f, 10f));
        Assert.Equal(60f, screed.Scrolled, 1);
    }

    /// <summary>Dragging sideways moves it sideways, and stops where the longest line stops.</summary>
    [Fact]
    public void DraggingSidewaysMovesItSideways()
    {
        var screed = new Screed { Text = new string('x', 400) };
        Laid(screed);

        screed.Take(new Vector2(100f, 10f));
        screed.Carry(new Vector2(40f, 10f));

        Assert.Equal(60f, screed.Shifted, 1);

        screed.Carry(new Vector2(-100000f, 10f));
        Assert.Equal(screed.Widest - Width, screed.Shifted, 1);
    }

    /// <summary>A press on a thumb is the thumb being dragged, not the text.</summary>
    [Fact]
    public void APressOnAThumbTakesTheThumb()
    {
        var screed = new Screed { Text = Lines(5000), BarWidth = 20f, BarColor = Color.White };
        Laid(screed);

        Assert.True(screed.Take(new Vector2(Width - 10f, 10f)));

        Assert.False(screed.IsPanning);
        Assert.Equal(Way.Down, screed.Held);
    }

    /// <summary>Text with nowhere to go is not taken hold of, so the press belongs to somebody else.</summary>
    [Fact]
    public void TextThatFitsIsNotTakenHoldOf()
    {
        var screed = new Screed { Text = "one\ntwo" };
        _ = new Easel(screed, Width, Height);

        Assert.False(screed.Take(new Vector2(100f, 10f)));
        Assert.False(screed.IsPanning);
    }

    static Screed Numbered(string text)
    {
        return new Screed { Text = text, Numbered = true, NumberColor = Color.White };
    }

    static float WhereIs(Easel easel, string said)
    {
        return easel.Written.First(c => c.Text == said).Position.X;
    }

    /// <summary>Lines are numbered from one, in a gutter, for the lines on show.</summary>
    [Fact]
    public void TheGutterNumbersTheLinesOnShow()
    {
        var easel = new Easel(Numbered(Lines(5000)), Width, Height);

        Assert.Contains(easel.Written, c => c.Text == "1");
        Assert.Contains(easel.Written, c => c.Text == "2");
        Assert.DoesNotContain(easel.Written, c => c.Text == "0");
        Assert.DoesNotContain(easel.Written, c => c.Text == "4000");
    }

    /// <summary>Nothing is numbered unless somebody asked for it.</summary>
    [Fact]
    public void ThereIsNoGutterUnlessItWasAskedFor()
    {
        var easel = new Easel(Long(5000), Width, Height);

        Assert.DoesNotContain(easel.Written, c => c.Text == "1");
    }

    /// <summary>
    /// The numbers are right against the words, whatever they run to.
    /// </summary>
    /// <remarks>
    /// Ragged on the left, flush on the right, which is what makes a column of numbers readable as a
    /// column rather than as a list of different-length things.
    /// </remarks>
    [Fact]
    public void TheNumbersAreFlushAgainstTheWords()
    {
        Screed screed = Numbered(Lines(5000));
        var easel = new Easel(screed, Width, Height);

        float nine = WhereIs(easel, "9");
        float digit = nine - WhereIs(easel, "10");

        Assert.True(digit > 0f, "a two-digit number did not start further left than a one-digit one");

        // Line 100 is not on the first screenful, so go and find it.
        screed.Scrolled = 95f * easel.LineHeight;
        easel.Draw(Width, Height);

        Assert.Equal(nine - 2f * digit, WhereIs(easel, "100"), 1);
    }

    /// <summary>
    /// The numbers end on one line, in a face whose digits are not all one width.
    /// </summary>
    /// <remarks>
    /// The game's own face has a <c>1</c> less than half the width of a <c>0</c>, so a number placed by
    /// counting its digits ends short of the margin by however many narrow ones it contains. The
    /// stand-in face cannot show that - its digits are all one width - so this asks for the real one,
    /// and says so rather than quietly passing where it cannot be had.
    /// </remarks>
    [Fact]
    public void TheNumbersEndOnOneLineWhateverDigitsTheyHold()
    {
        var easel = new Easel(Numbered(Lines(40)), Width, Height, true);

        if (!easel.IsExact)
            Assert.Skip("the game's own font is not here, and the stand-in's digits are all one width");

        float Ends(string said) => WhereIs(easel, said) + easel.WidthOf(said);

        // The narrowest digit alone, two of them, and one beside a wide one.
        Assert.Equal(Ends("9"), Ends("1"), 1);
        Assert.Equal(Ends("9"), Ends("11"), 1);
        Assert.Equal(Ends("9"), Ends("10"), 1);
    }

    /// <summary>
    /// Dragging the words sideways leaves the gutter where it is.
    /// </summary>
    /// <remarks>
    /// The one arrangement in which numbers are any use: somebody following a long line across is
    /// exactly the person who needs to know which line they are on.
    /// </remarks>
    [Fact]
    public void TheGutterHoldsStillWhileTheWordsMove()
    {
        Screed screed = Numbered(new string('x', 400) + "\n" + Lines(500));
        var easel = new Easel(screed, Width, Height);

        float gutter = WhereIs(easel, "2");
        float words = WhereIs(easel, "line 0");

        screed.MoveBy(Way.Across, 120f);
        easel.Draw(Width, Height);

        Assert.Equal(gutter, WhereIs(easel, "2"), 2);
        Assert.Equal(words - 120f * easel.Pixels, WhereIs(easel, "line 0"), 2);
    }

    /// <summary>The gutter takes its room from the words, so a long line cannot run under it.</summary>
    [Fact]
    public void TheGutterTakesItsRoomFromTheWords()
    {
        var wide = new string('x', 400);

        var plain = new Screed { Text = wide };
        var withGutter = Numbered(wide);

        var first = new Easel(plain, Width, Height);
        var second = new Easel(withGutter, Width, Height);

        Assert.True(WhereIs(second, wide) > WhereIs(first, wide), "the words did not move over for it");
        Assert.True(withGutter.CanMove(Way.Across));
    }

    /// <summary>An empty one draws nothing and still stands a line tall, so a box is not zero high.</summary>
    [Fact]
    public void EmptyItHasOneLineOfExtent()
    {
        var screed = new Screed();
        var easel = new Easel(screed, Width, Height);

        Assert.Empty(easel.Written);
        Assert.Equal(easel.LineHeight, screed.Extent, 2);
    }
}
