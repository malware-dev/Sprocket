using System.Collections.Generic;
using System.Globalization;
using Lumel.Sim;
using VRageMath;

namespace Sprocket.Sim.Measuring;

/// <summary>Checks of the words a frame draws: runs cut off by the side of their clip, and runs drawn over each other.</summary>
/// <remarks>
/// Only as exact as the widths it measures with: the game's own where the frame has them, a stand-in face a few percent out
/// otherwise. A run that starts outside its clip is scrolled away rather than cut, and is left out.
/// </remarks>
public static class TextFit
{
    // A pixel either way, for rounding between the layout's units and the device's pixels.
    const float Rounding = 1f;

    // How far down its line a run's letters start: the game's blank over every line, 3 x 23 / 18 of the 37 units of a line
    // (ScreenSpaceGraphics.TopSideBearing; Fonts/white/FontDataPA.xml, height 37).
    const float Over = 3f * 23f / 18f / 37f;

    // How far down its line a run's baseline is: 30 of the 37 units (Fonts/white/FontDataPA.xml, base 30, height 37).
    const float Baseline = 30f / 37f;

    /// <summary>Each run in <paramref name="commands" /> cut short by its clip, measured with <paramref name="fonts" />, said as the words and how far past.</summary>
    public static List<string> Cut(IList<DrawCommand> commands, IFontMetrics fonts, float width, float height)
    {
        var cut = new List<string>();
        var clip = new RectangleF(0f, 0f, width, height);
        foreach (var command in commands)
        {
            switch (command.Kind)
            {
                case DrawKind.SetClip:
                    clip = new RectangleF(command.Clip.X, command.Clip.Y, command.Clip.Width, command.Clip.Height);
                    continue;
                case DrawKind.ClearClip:
                    clip = new RectangleF(0f, 0f, width, height);
                    continue;
                case DrawKind.Text:
                    break;
                default:
                    continue;
            }

            if (string.IsNullOrEmpty(command.Text))
                continue;

            var start = command.Position.X;
            var clipEnd = clip.X + clip.Width;
            if (start < clip.X - Rounding || start > clipEnd + Rounding)
                continue;

            var end = start + fonts.MeasurePixels(command.Font, command.Text, command.Scale).X;
            if (end > clipEnd + Rounding)
                cut.Add($"\"{command.Text}\" runs {(end - clipEnd).ToString("0", CultureInfo.InvariantCulture)} px past its clip");
        }

        return cut;
    }

    /// <summary>Each pair of runs in <paramref name="commands" /> drawn over each other, measured with <paramref name="fonts" />, said as both runs.</summary>
    /// <remarks>Only runs that start inside their clip are weighed, each to the end of its clip at most, since past that the game draws nothing.</remarks>
    public static List<string> Overlapping(IList<DrawCommand> commands, IFontMetrics fonts, float width, float height)
    {
        var runs = new List<KeyValuePair<string, RectangleF>>();
        var clip = new RectangleF(0f, 0f, width, height);
        foreach (var command in commands)
        {
            if (command.Kind == DrawKind.SetClip)
                clip = new RectangleF(command.Clip.X, command.Clip.Y, command.Clip.Width, command.Clip.Height);
            else if (command.Kind == DrawKind.ClearClip)
                clip = new RectangleF(0f, 0f, width, height);
            else if (command.Kind == DrawKind.Sprite && command.Color.A == 255)
                Cover(runs, command.Destination, clip);

            if (command.Kind != DrawKind.Text || string.IsNullOrWhiteSpace(command.Text))
                continue;

            var start = command.Position;
            if (!clip.Contains(new Vector2(start.X + Rounding, start.Y + Rounding)))
                continue;

            // The letters stand between the blank the game leaves over every line and the baseline: the rest of the line is room
            // for descenders, which most words leave empty.
            var size = fonts.MeasurePixels(command.Font, command.Text, command.Scale);
            var right = System.Math.Min(start.X + size.X, clip.X + clip.Width);
            var top = start.Y + size.Y * Over;
            var bottom = System.Math.Min(start.Y + size.Y * Baseline, clip.Y + clip.Height);
            runs.Add(new KeyValuePair<string, RectangleF>(command.Text, new RectangleF(start.X, top, right - start.X, bottom - top)));
        }

        var over = new List<string>();
        for (var i = 0; i < runs.Count; i++)
        {
            for (var j = i + 1; j < runs.Count; j++)
            {
                var a = runs[i].Value;
                var b = runs[j].Value;
                var across = System.Math.Min(a.X + a.Width, b.X + b.Width) - System.Math.Max(a.X, b.X);
                var down = System.Math.Min(a.Y + a.Height, b.Y + b.Height) - System.Math.Max(a.Y, b.Y);
                if (across > Rounding && down > Rounding)
                    over.Add($"\"{runs[i].Key}\" over \"{runs[j].Key}\"");
            }
        }

        return over;
    }

    /// <summary>Takes out of <paramref name="runs" /> every run a solid sprite at <paramref name="area" />, inside <paramref name="clip" />, paints over.</summary>
    static void Cover(List<KeyValuePair<string, RectangleF>> runs, Rectangle area, RectangleF clip)
    {
        var left = System.Math.Max(area.X, clip.X);
        var top = System.Math.Max(area.Y, clip.Y);
        var right = System.Math.Min(area.X + area.Width, clip.X + clip.Width);
        var bottom = System.Math.Min(area.Y + area.Height, clip.Y + clip.Height);
        if (right <= left || bottom <= top)
            return;

        runs.RemoveAll(run => run.Value.X < right && run.Value.X + run.Value.Width > left
            && run.Value.Y < bottom && run.Value.Y + run.Value.Height > top);
    }
}
