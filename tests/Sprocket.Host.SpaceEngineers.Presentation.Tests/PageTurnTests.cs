using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A pick that goes on to another page slides there, moving on every tick of the way.
/// </summary>
/// <remarks>
/// A slide that stops short and jumps the rest looks like the game hitching, and every frame of it
/// still takes no time at all - so the movement itself is what is held here, a column heading
/// followed across the page turn.
/// </remarks>
public class PageTurnTests
{
    const string Chosen = "60 196 type Airlock enter 60 163 200 351 560 333 1000 207";

    const string Fold = "47 500";

    static List<float> Followed(Screen screen, string heading, int ticks)
    {
        var at = new List<float>();
        for (var i = 0; i < ticks; i++)
        {
            screen.Run(1);
            var drawn = screen.Drawings.Where(d => d.IsText && d.Text == heading).ToList();
            if (drawn.Count == 0)
                break;

            at.Add(drawn[0].At.X);
        }

        return at;
    }

    [Fact]
    public void GoingOnMovesEveryTickUntilThePageIsGone()
    {
        var screen = Screen.Fresh(Chosen.Split(' '));

        var at = Followed(screen, "WHAT HAPPENS", 30);

        Assert.True(at.Count > 10, "the heading left before the slide was under way");
        for (var i = 1; i < at.Count; i++)
            Assert.True(at[i] < at[i - 1], $"the slide stood still at tick {i}, at {at[i]}");
    }

    [Fact]
    public void GoingBackMovesEveryTick()
    {
        var screen = Screen.Fresh((Chosen + " tick 30 " + Fold).Split(' '));

        var at = Followed(screen, "WHAT HAPPENS", 30);

        Assert.True(at.Count > 10, "the heading never came back");
        var moving = at.TakeWhile((x, i) => i == 0 || x != at[i - 1]).Count();
        Assert.True(moving > 10, $"the slide stood still after {moving} ticks");
        for (var i = 1; i < moving; i++)
            Assert.True(at[i] > at[i - 1], $"the slide went the wrong way at tick {i}");
    }
}
