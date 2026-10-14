using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Sandbox.ModAPI;
using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using Sprocket.Sim.Content;
using Sprocket.Sim.Simulation;
using VRageMath;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Sim.Measuring;

/// <summary>
/// What rich text costs to read, to lay out and to draw, at sizes from a tooltip to far beyond a help page.
/// </summary>
/// <remarks>
/// Measured with the game's own font widths where the game is installed, since measuring is most of what
/// laying out does. <b>These numbers are from this machine and this runtime, not from the game</b>; the
/// shape - whether a cost grows with the text or faster - is the thing worth trusting. Run it in Release.
/// </remarks>
public static class RichTextReport
{
    const int MinimumMilliseconds = 60;
    const int Windows = 7;
    const float Room = 512f;

    /// <summary>Every timing, as tables to print.</summary>
    public static string Render()
    {
        var device = new ScreenSpaceDevice();
        var game = SeFontMetrics.Load(device);
        var fonts = new FontSet(game ?? (IFontMetrics)new FallbackFonts(device, "Segoe UI"));
        foreach (var font in ModFonts.All)
            fonts = fonts.And(font, SeFontMetrics.LoadFrom(device, Path.Combine(ModFonts.Folder, font + ".xml"), "the mod's own content", true));

        device.Fonts = fonts;
        device.Install();
        var graphics = new ScreenSpaceGraphics(device, MyScreenSpaceLayer.AboveHud, Materials.Fill, new ScreenSpaceImages(device));

        var report = new StringBuilder();
        report.AppendLine("Rich text timing, Release only, " + (game != null ? "with the game's font widths." : "with a stand-in font (the game is not installed)."));
        report.AppendLine("Laid out " + Room.ToString(CultureInfo.InvariantCulture) + " wide unless the row says unbounded.");
        report.AppendLine();
        report.AppendLine("shape                 chars   parse us  layout us   frame us   measures  meas/char   KB parse  KB layout  B/frame");
        report.AppendLine(new string('-', 114));

        Row(report, device, graphics, "tooltip", Tooltip(), Room);
        Row(report, device, graphics, "annotation", Mixed(4), Room);
        Row(report, device, graphics, "help page", Mixed(40), Room);
        Row(report, device, graphics, "stress", Mixed(400), Room);
        Row(report, device, graphics, "stress, unbounded", Mixed(400), Element.Unbounded);
        Row(report, device, graphics, "stress, 2000 wide", Mixed(400), 2000f);
        Row(report, device, graphics, "one long paragraph", Paragraph(8000), Room);
        Row(report, device, graphics, "price table", PriceList(20), Room);
        Row(report, device, graphics, "long table", PriceList(400), Room);
        Row(report, device, graphics, "long table, narrow", PriceList(400), 160f);
        Row(report, device, graphics, "icons in text", Icons(400), Room);
        Row(report, device, graphics, "links in text", Linked(400), Room);
        Row(report, device, graphics, "code in text", Coded(400), Room);
        Row(report, device, graphics, "boxes", Boxed(400, true), Room);
        Row(report, device, graphics, "same, unboxed", Boxed(400, false), Room);
        report.AppendLine();

        report.AppendLine("Pathological marks, read only.");
        report.AppendLine();
        report.AppendLine("shape                 chars   parse us   us/KB");
        report.AppendLine(new string('-', 48));
        Parse(report, "unclosed closers", Repeat("a* ", 30000));
        Parse(report, "unclosed openers", Repeat(" *a", 30000));
        Parse(report, "nested to the hilt", Repeat("*a ", 15000) + Repeat("a* ", 15000));
        Parse(report, "code ticks unclosed", Repeat("`a ", 30000));
        Parse(report, "images unclosed", Repeat("![a](b ", 30000));
        Parse(report, "brackets unclosed", Repeat("![a ", 30000));
        Parse(report, "links unclosed", Repeat("[a](b ", 30000));
        Parse(report, "brackets only", Repeat("[a] ", 30000));
        Parse(report, "pipes, no table", Repeat("a | b | c\n", 20000));
        Parse(report, "table rows", "| a | b |\n|--|--|\n" + Repeat("| a | b |\n", 20000));
        Parse(report, "fences unclosed", Repeat("::: a\n", 20000));
        Parse(report, "fences closing none", Repeat(":::\n", 20000));
        Parse(report, "fences nested", Repeat("::: a\n", 10000) + Repeat(":::\n", 10000));
        report.AppendLine();

        Headroom(report, graphics);
        return report.ToString();
    }

    static void Row(StringBuilder report, ScreenSpaceDevice device, ScreenSpaceGraphics graphics, string name, string source,
        float room)
    {
        var first = Markdown.Parse(source);
        var second = Markdown.Parse(source);
        var text = new RichText { Style = Styles.Tip };

        var parse = Time(() => Markdown.Parse(source));
        var parseBytes = Allocated(() => Markdown.Parse(source));
        void LayOut()
        {
            text.Document = text.Document == first ? second : first;
            text.Measure(graphics, new Vector2(room, Element.Unbounded));
        }

        var layout = Time(LayOut);
        var layoutBytes = Allocated(LayOut);

        var before = device.Measured;
        LayOut();
        var measures = device.Measured - before;
        var characters = Characters(first);

        void Frame()
        {
            device.BeginFrame();
            graphics.Begin();
            text.Measure(graphics, new Vector2(room, Element.Unbounded));
            text.Arrange(new RectangleF(0f, 0f, text.DesiredSize.X, text.DesiredSize.Y));
            text.Draw(graphics);
        }

        var frame = Time(Frame);
        var frameBytes = Allocated(Frame);

        report.AppendLine(
            Pad(name, 20) +
            Pad(Whole(source.Length), 8) +
            Pad(Micro(parse), 11) +
            Pad(Micro(layout), 11) +
            Pad(Micro(frame), 11) +
            Pad(Whole(measures), 11) +
            Pad((measures / (double)Math.Max(1, characters)).ToString("0.00", CultureInfo.InvariantCulture), 11) +
            Pad((parseBytes / 1024d).ToString("0.0", CultureInfo.InvariantCulture), 11) +
            Pad((layoutBytes / 1024d).ToString("0.0", CultureInfo.InvariantCulture), 11) +
            Pad(Whole(frameBytes), 9));
    }

    static void Parse(StringBuilder report, string name, string source)
    {
        var parse = Time(() => Markdown.Parse(source));
        report.AppendLine(
            Pad(name, 20) +
            Pad(Whole(source.Length), 8) +
            Pad(Micro(parse), 11) +
            Pad((parse * 1000d / (source.Length / 1024d)).ToString("0.0", CultureInfo.InvariantCulture), 8));
    }

    /// <summary>The same text at doubling sizes, so the curve can be read: while us/KB holds steady, cost grows with the text.</summary>
    static void Headroom(StringBuilder report, ScreenSpaceGraphics graphics)
    {
        report.AppendLine("Headroom, mixed text at doubling sizes.");
        report.AppendLine();
        report.AppendLine("chars     parse us/KB   layout us/KB");
        report.AppendLine(new string('-', 38));

        for (var sentences = 8; sentences <= 1024; sentences *= 2)
        {
            var source = Mixed(sentences);
            var first = Markdown.Parse(source);
            var second = Markdown.Parse(source);
            var text = new RichText { Style = Styles.Tip };

            var parse = Time(() => Markdown.Parse(source));
            var layout = Time(() =>
            {
                text.Document = text.Document == first ? second : first;
                text.Measure(graphics, new Vector2(Room, Element.Unbounded));
            });

            var kilobytes = source.Length / 1024d;
            report.AppendLine(
                Pad(Whole(source.Length), 10) +
                Pad((parse * 1000d / kilobytes).ToString("0.0", CultureInfo.InvariantCulture), 14) +
                Pad((layout * 1000d / kilobytes).ToString("0.0", CultureInfo.InvariantCulture), 14));
        }
    }

    static string Tooltip() =>
        "Turn this into a **blueprint**, so more logic systems like it can be made, each using its own blocks.\n"
        + "This one becomes the first of them.";

    /// <summary>Sentences mixing every kind of block and run, the way a long annotation or help page would.</summary>
    static string Mixed(int sentences)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sentences; i++)
        {
            if (i % 10 == 0)
                text.Append("## Section ").Append(i / 10).Append("\n\n");

            text.Append("The **airlock** cycles *when* the `inner door` shuts, then the vent \\*depressurizes\\* the room.\n");

            if (i % 5 == 4)
                text.Append("\n- the inner door\n- the outer door\n1. seal\n2. vent\n\n---\n\n");
        }

        return text.ToString();
    }

    static string Paragraph(int words)
    {
        var text = new StringBuilder();
        for (var i = 0; i < words; i++)
            text.Append(i % 7 == 3 ? "**airlock** " : "airlock ");

        return text.ToString();
    }

    static string Repeat(string piece, int times)
    {
        var text = new StringBuilder(piece.Length * times);
        for (var i = 0; i < times; i++)
            text.Append(piece);

        return text.ToString();
    }

    /// <summary>A price list as a table, the names' column taking the spare width, with an icon in each row.</summary>
    static string PriceList(int rows)
    {
        var text = new StringBuilder("| | Item | Price |\n|--|:---*|---:|\n");
        for (var i = 0; i < rows; i++)
            text.Append("| ![](IconEnergy) | Power cell ").Append(i).Append(" | **").Append(i * 7).Append(" SC** |\n");

        return text.ToString();
    }

    /// <summary>Sentences with an icon in each, a taller picture now and then.</summary>
    static string Icons(int sentences)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sentences; i++)
        {
            text.Append("The ![](IconOxygen) vent fills the room, and the ![](IconEnergy) battery holds its charge.\n");
            if (i % 10 == 9)
                text.Append("![](LCD_SoF_CosmicTeam_Landscape =x96)\n\n");
        }

        return text.ToString();
    }

    /// <summary>Sentences with a link in each, the way a page of the codex reads.</summary>
    static string Linked(int sentences)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sentences; i++)
            text.Append("Each [logic system](logic-system) holds its [installations](installation), and runs them in turn.\n");

        return text.ToString();
    }

    /// <summary>Sentences with code in each, the way a page of the codex names what is on the screen.</summary>
    static string Coded(int sentences)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sentences; i++)
            text.Append("Press `+ installation`, pick `Airlock 1 Outer Door`, then `Open` it and `Close` it again.\n");

        return text.ToString();
    }

    /// <summary>Paragraphs in boxes nested three deep, the innermost centered, the way a codex page sets a note apart; or, where <paramref name="fenced" /> is false, the same paragraphs with no boxes, to tell what the boxes cost.</summary>
    static string Boxed(int paragraphs, bool fenced)
    {
        var text = new StringBuilder();
        for (var i = 0; i < paragraphs; i += 10)
        {
            if (fenced)
                text.Append("::: warning\n::: player\n::: center\n");

            for (var j = 0; j < 10; j++)
                text.Append("The airlock cycles when the inner door shuts, then the vent empties the room.\n\n");

            if (fenced)
                text.Append(":::\n:::\n:::\n\n");
        }

        return text.ToString();
    }

    static int Characters(Document document) => Characters(document.Blocks);

    static int Characters(IList<Block> blocks)
    {
        var count = 0;
        foreach (var block in blocks)
        {
            count += Characters(block.Blocks);
            foreach (var run in block.Runs)
                count += run.Text.Length;
        }

        return count;
    }

    /// <summary>The median, in milliseconds, of several windows each long enough for the clock not to matter.</summary>
    static double Time(Action work)
    {
        for (var i = 0; i < 3; i++)
            work();

        var windows = new double[Windows];
        for (var w = 0; w < Windows; w++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var clock = Stopwatch.StartNew();
            var runs = 0;
            while (clock.ElapsedMilliseconds < MinimumMilliseconds)
            {
                work();
                runs++;
            }

            windows[w] = clock.Elapsed.TotalMilliseconds / runs;
        }

        Array.Sort(windows);
        return windows[Windows / 2];
    }

    static long Allocated(Action work)
    {
        work();
        var before = GC.GetAllocatedBytesForCurrentThread();
        work();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    static string Micro(double milliseconds) => (milliseconds * 1000d).ToString("0.0", CultureInfo.InvariantCulture);

    static string Whole(long value) => value.ToString(CultureInfo.InvariantCulture);

    static string Pad(string text, int width) => text.PadLeft(width);
}
