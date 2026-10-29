using System.Text;
using BenchmarkDotNet.Attributes;
using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Text;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Sim.Content;
using Sprocket.Sim.Simulation;
using VRageMath;
using Lumel.Sim;

namespace Sprocket.Benchmarks;

/// <summary>Laying a document out afresh in a tooltip's style, 512 wide, as the profile does: the shapes it lays out, with the game's font widths.</summary>
[MemoryDiagnoser]
[InProcess]
public class LayoutBenchmarks
{
    const float Room = 512f;

    ScreenSpaceGraphics _graphics = null!;
    RichText _text = null!;
    Document _first = null!;
    Document _second = null!;

    /// <summary>Which text is laid out.</summary>
    [Params("help page", "stress", "links in text", "code in text", "one long paragraph")]
    public string Shape { get; set; } = "";

    /// <summary>Loads the fonts and reads the text twice, so each layout is of a document it has not laid out just before.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var device = new ScreenSpaceDevice(SprocketContent.Instance);
        var game = SeFontMetrics.Load(device);
        var fonts = new FontSet(game ?? (IFontMetrics)new FallbackFonts(device, "Segoe UI"));
        foreach (var font in SprocketContent.Instance.Fonts)
            fonts = fonts.And(font.Name, font.Metrics(device));

        device.Fonts = fonts;
        ScreenSession.Install(device);
        _graphics = new ScreenSpaceGraphics(device, MyScreenSpaceLayer.AboveHud, new MaterialImages(device.TextureSize), () => 0);

        var source = Source(Shape);
        _first = Markdown.Parse(source);
        _second = Markdown.Parse(source);
        _text = new RichText { Style = Styles.Tip };
    }

    /// <summary>Lays the shape out once, alternating between its two documents so nothing is kept from the last time.</summary>
    [Benchmark]
    public Vector2 LayOut()
    {
        _text.Document = _text.Document == _first ? _second : _first;
        _text.Measure(_graphics, new Vector2(Room, Element.Unbounded));
        return _text.DesiredSize;
    }

    static string Source(string shape)
    {
        switch (shape)
        {
            case "help page": return Mixed(40);
            case "stress": return Mixed(400);
            case "links in text": return Repeat("Each [logic system](logic-system) holds its [installations](installation), and runs them in turn.\n", 400);
            case "code in text": return Repeat("Press `+ installation`, pick `Airlock 1 Outer Door`, then `Open` it and `Close` it again.\n", 400);
            default: return Paragraph(8000);
        }
    }

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
}
