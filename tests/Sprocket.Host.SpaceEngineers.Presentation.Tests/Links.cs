using Lumel;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Where the links in the rich text of a screen sit.</summary>
static class Links
{
    /// <summary>The first point on the screen where a link to <paramref name="target" /> sits inside <paramref name="element" />, and the text it sits in.</summary>
    public static Vector2 First(Element element, string target, out RichText text)
    {
        foreach (var rich in Within(element))
        {
            for (var y = rich.Bounds.Y; y < rich.Bounds.Y + rich.Bounds.Height; y += 2f)
            {
                for (var x = rich.Bounds.X; x < rich.Bounds.X + rich.Bounds.Width; x += 2f)
                {
                    var point = new Vector2(x, y);
                    if (rich.LinkAt(point) is { } link && link.Target == target)
                    {
                        text = rich;
                        return rich.ToScreen(point);
                    }
                }
            }
        }

        text = null!;
        Assert.Fail("no link to " + target + " on the screen");
        return default;
    }

    /// <summary>Every rich text inside <paramref name="element" />.</summary>
    static IEnumerable<RichText> Within(Element element)
    {
        if (element is RichText rich)
            yield return rich;

        for (var i = 0; i < element.VisualChildCount; i++)
        {
            foreach (var inner in Within(element.GetVisualChild(i)))
                yield return inner;
        }
    }
}
