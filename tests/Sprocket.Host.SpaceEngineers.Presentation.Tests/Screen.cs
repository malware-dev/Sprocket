using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Sim.Machine;
using Sprocket.Sim.Measuring;
using Sprocket.Sim.Simulation;
using VRageMath;
using Lumel.Drawing;
using Lumel.Sim;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The editor, driven and then read.
/// </summary>
/// <remarks>
/// The same offscreen frame the reference dumps are made from, so a test and a dump are looking at
/// one thing. Scripts are written the way they are written in tests/Reference/suite.txt: press by
/// what the screen says, coordinates in UI units, and the words move, drag, scroll, tick, type, enter,
/// escape and clear.
/// </remarks>
sealed class Screen
{
    const float Width = 1920f;
    const float Height = 1080f;

    readonly Offscreen _frame;

    Screen(Offscreen frame) => _frame = frame;

    public static Screen Fresh(params string[] script)
    {
        var frame = new Offscreen(Width, Height);
        frame.Play(Script.Parse(script, 0));

        return new Screen(frame);
    }

    /// <summary>The editor on a construct of <paramref name="rig" />'s blocks, after <paramref name="script" />.</summary>
    public static Screen On(Rig rig, params string[] script)
    {
        var frame = new Offscreen(Width, Height, rig);
        frame.Play(Script.Parse(script, 0));

        return new Screen(frame);
    }

    /// <summary>The editor on <paramref name="rig" />'s blocks, its letters measured with the game's own widths, after <paramref name="script" />.</summary>
    /// <remarks>Needs the game installed; <see cref="HasGameFonts" /> says whether it found it.</remarks>
    public static Screen WithGameFonts(Rig rig, params string[] script)
    {
        var frame = new Offscreen(Width, Height, rig, true);
        frame.Play(Script.Parse(script, 0));

        return new Screen(frame);
    }

    /// <summary>Whether the letters are measured with the game's own widths.</summary>
    public bool HasGameFonts => _frame.HasGameFonts;

    /// <summary>Every run of words the last frame drew past the side of its clip.</summary>
    public List<string> Cut => TextFit.Cut(_frame.Commands, _frame.Fonts, Width, Height);

    /// <summary>Every pair of runs of words the last frame drew over each other.</summary>
    public List<string> Overlapping => TextFit.Overlapping(_frame.Commands, _frame.Fonts, Width, Height, Symbols.Font);

    /// <summary>The editor on a block that already holds <paramref name="program" />, after <paramref name="script" />.</summary>
    public static Screen Holding(string program, params string[] script)
    {
        var frame = new Offscreen(Width, Height, program);
        frame.Play(Script.Parse(script, 0));

        return new Screen(frame);
    }

    /// <summary>Does more to the screen this one is already showing.</summary>
    public Screen Do(params string[] script)
    {
        Script.Parse(script, 0).Play(_frame.Scene, _frame.Draw);
        return this;
    }

    public EditorScene Scene => _frame.Scene;

    /// <summary>How much text this screen has measured since it was made.</summary>
    public int Measured => _frame.Measured;

    /// <summary>How many times it has written what its controls mean onto them.</summary>
    public int Wires => _frame.Scene.Wires;

    /// <summary>How many questions the mod has asked the game.</summary>
    public int Asked => _frame.Scene.Asked;

    /// <summary>The editor, for tests about what it does rather than where this target draws it.</summary>
    public SprocketEditor Editor => _frame.Scene.Editor;

    /// <summary>
    /// Advances the clock, drawing every tick.
    /// </summary>
    /// <remarks>
    /// Drawn every time rather than once at the end, because the fault this exists to catch is work
    /// done per frame, and a run that draws once cannot see it.
    /// </remarks>
    public Screen Run(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            _frame.Scene.Tick();
            _frame.Draw();
        }

        return this;
    }

    public string Ended => _frame.Scene.OnScreen;

    public string Dump => _frame.Describe();

    /// <summary>
    /// Every call the last frame made, with the clip that was standing when it was made.
    /// </summary>
    /// <remarks>
    /// A clipped call is still a call. The screen asks for rows it knows will not fit and lets the clip
    /// decide, so reading the list alone answers what was drawn and not what anybody can see - which is
    /// the difference a scrolling list is entirely made of.
    /// </remarks>
    public IReadOnlyList<Drawn> Drawings
    {
        get
        {
            var drawings = new List<Drawn>();
            var clip = new RectangleF(0f, 0f, Width, Height);

            foreach (var command in _frame.Commands)
            {
                switch (command.Kind)
                {
                    case DrawKind.SetClip:
                        clip = new RectangleF(
                            command.Clip.X, command.Clip.Y, command.Clip.Width, command.Clip.Height);
                        continue;
                    case DrawKind.ClearClip:
                        clip = new RectangleF(0f, 0f, Width, Height);
                        continue;
                }

                drawings.Add(new Drawn(command, clip));
            }

            return drawings;
        }
    }

    /// <summary>The words the screen asked for, whether or not a clip let them through.</summary>
    public IReadOnlyList<string> Words =>
        Drawings.Where(d => d.IsText).Select(d => d.Text).ToList();

    /// <summary>The words somebody can actually read.</summary>
    public IReadOnlyList<string> Legible =>
        Drawings.Where(d => d.IsText && IsInsideItsClip(d)).Select(d => d.Text).ToList();

    /// <summary>Whether a drawing's line starts inside the clip it was drawn under.</summary>
    public bool IsInsideItsClip(Drawn drawing) => drawing.IsInsideItsClip(TopOf(drawing));

    public bool Shows(string text) => Legible.Contains(text);

    public Drawn Text(string text) => Drawings.First(d => d.IsText && d.Text == text);

    /// <summary>Every drawn mark of one kind that is on show, in the order they were asked for.</summary>
    /// <remarks>
    /// Judged by the middle of its line rather than its top. The symbol font's line is taller than the
    /// row a symbol is centred in, so the top of a symbol's line sits above the row's clip while the
    /// symbol itself is inside it, and a clip cuts pixels rather than lines.
    /// </remarks>
    public IReadOnlyList<Drawn> Marks(string symbol) =>
        Drawings.Where(d => d.IsSymbol && d.Text == symbol && OnShow(d)).ToList();

    bool OnShow(Drawn mark)
    {
        var middle = MiddleOf(mark);
        var clip = mark.Clip;

        return mark.At.X >= clip.X && mark.At.X <= clip.X + clip.Width
            && middle >= clip.Y && middle <= clip.Y + clip.Height;
    }

    public bool HasMark(string symbol) => Marks(symbol).Count > 0;

    /// <summary>
    /// The middle of the line a drawing sits on.
    /// </summary>
    /// <remarks>
    /// Two pieces of text at different sizes share a line by their middles, not by their tops - which
    /// is the whole of what lining a heading up with the mark beside it means.
    /// </remarks>
    public float MiddleOf(Drawn drawing) =>
        TopOf(drawing) + _frame.LineHeight(drawing.Font, drawing.Scale) / 2f;

    /// <summary>
    /// The top of the line a drawing sits on.
    /// </summary>
    /// <remarks>
    /// Below where the text was drawn by the game's own bearing, which the renderer adds to every glyph and
    /// the screen takes off before drawing - so the line sits where the layout put it, and the drawing is
    /// that bearing above it.
    /// </remarks>
    public float TopOf(Drawn drawing)
    {
        var units = _frame.LineUnits(drawing.Font);
        var line = _frame.LineHeight(drawing.Font, drawing.Scale);
        return units > 0 ? drawing.At.Y + line * ScreenSpaceGraphics.TopSideBearing / units : drawing.At.Y;
    }

    /// <summary>
    /// A point the frame reported, in the units a script presses in.
    /// </summary>
    /// <remarks>
    /// The frame is in device pixels and a script is in UI units, and the two differ by the letterbox
    /// the game keeps either side of a four-by-three screen. Anything read out of a frame and then
    /// pressed at has to come through here.
    /// </remarks>
    public static Vector2 Ui(float x, float y) => new Vector2(x - (Width - Height * 4f / 3f) / 2f, y);
}

/// <summary>One call the screen made, and what it was allowed to draw into.</summary>
readonly struct Drawn(DrawCommand command, RectangleF clip)
{
    public bool IsText => command.Kind == DrawKind.Text;

    /// <summary>Whether this is one of the drawn marks rather than words somebody reads.</summary>
    public bool IsSymbol => IsText && command.Font == Symbols.Font;

    public string Text => command.Text ?? "";

    public Vector2 At => command.Position;

    public Rectangle Where => command.Destination;

    public Color Ink => command.Color;

    public string Font => command.Font ?? "";

    public float Scale => command.Scale;

    public RectangleF Clip => clip;

    const float Rounding = 0.5f;

    /// <summary>Whether the drawing starts inside its clip, its line beginning at <paramref name="top" />.</summary>
    public bool IsInsideItsClip(float top) =>
        At.X >= clip.X - Rounding && At.X <= clip.X + clip.Width + Rounding
        && top >= clip.Y - Rounding && top <= clip.Y + clip.Height + Rounding;
}
