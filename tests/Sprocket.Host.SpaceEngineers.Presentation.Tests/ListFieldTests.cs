using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Lumel.Menus;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A value chosen from more words than fit on a line, from a list hanging under it.</summary>
public class ListFieldTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1000f, 800f);

    /// <summary>A list of as many words as a sound block has opens on the word already given, however far down it is.</summary>
    [Fact]
    public void AListOpensOnTheWordGiven()
    {
        var shown = Opened(73, "Sound 60");

        Assert.Contains("Sound 60", shown);
        Assert.DoesNotContain("Sound 0", shown);
    }

    /// <summary>With nothing given yet, it opens at the top.</summary>
    [Fact]
    public void WithNothingGivenItOpensAtTheTop()
    {
        var shown = Opened(73, "");

        Assert.Contains("Sound 0", shown);
        Assert.DoesNotContain("Sound 72", shown);
    }

    /// <summary>A drop-down asks for its longest word and its mark, never for the room it is offered.</summary>
    [Fact]
    public void TheFieldAsksForItsLongestWord()
    {
        var graphics = new Recording(Area);
        var field = new ListField(new[] { "a", "a much longer word" }) { Text = "a" };

        field.Measure(graphics, new Vector2(1000f, 40f));

        Assert.True(field.DesiredSize.X < 500f, "the drop-down asks for all the room it is offered");
    }

    /// <summary>The words a press reaches down the list hanging under a field holding <paramref name="given" />.</summary>
    static List<string> Opened(int count, string given)
    {
        var graphics = new Recording(Area);
        var layer = new WindowLayer();
        var words = Enumerable.Range(0, count).Select(i => "Sound " + i).ToList();
        var field = new ListField(words) { Text = given };
        layer.Covers(field);

        field.Measure(graphics, new Vector2(300f, 40f));
        field.Arrange(new RectangleF(0f, 100f, 300f, 40f));
        field.Command!.Execute();
        layer.Draw(graphics, Area);

        var shown = new List<string>();
        for (var y = 140f; y < Area.Height; y += 4f)
        {
            var row = layer.HitTest(new Vector2(20f, y), out _) as MenuItem;
            if (row != null && !shown.Contains(row.Text))
                shown.Add(row.Text);
        }

        return shown;
    }
}
