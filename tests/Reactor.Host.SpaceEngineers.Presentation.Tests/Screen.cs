using Reactor.Host.SpaceEngineers.Editing;
using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Sim.Simulation;
using VRageMath;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The editor, driven and then read.
/// </summary>
/// <remarks>
/// The same offscreen frame the reference dumps are made from, so a test and a dump are looking at
/// one thing. Scripts are written the way they are written in tests/Reference/suite.txt: coordinates
/// in UI units, and the words move, drag, scroll, tick, type, enter, escape and clear.
/// </remarks>
sealed class Screen
{
    /// <summary>The size a letter actually lands at for a player. Layout does not depend on it.</summary>
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

    /// <summary>Does more to the screen this one is already showing.</summary>
    public Screen Do(params string[] script)
    {
        Script.Parse(script, 0).Play(_frame.Scene, _frame.Draw);
        return this;
    }

    public EditorScene Scene => _frame.Scene;

    /// <summary>How much text this screen has measured since it was made.</summary>
    public int Measured => _frame.Measured;

    /// <summary>The editor, for tests about what it does rather than where this target draws it.</summary>
    public ReactorEditor Editor => _frame.Scene.Editor;

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
        Drawings.Where(d => d.IsText && d.IsInsideItsClip).Select(d => d.Text).ToList();

    public bool Shows(string text) => Legible.Contains(text);

    public Drawn Text(string text) => Drawings.First(d => d.IsText && d.Text == text);

    /// <summary>Every drawn mark of one kind, in the order they were asked for.</summary>
    public IReadOnlyList<Drawn> Marks(string symbol) =>
        Drawings.Where(d => d.IsSymbol && d.Text == symbol && d.IsInsideItsClip).ToList();

    public bool HasMark(string symbol) => Marks(symbol).Count > 0;

    /// <summary>
    /// The middle of the line a drawing sits on.
    /// </summary>
    /// <remarks>
    /// Two pieces of text at different sizes share a line by their middles, not by their tops - which
    /// is the whole of what lining a heading up with the mark beside it means.
    /// </remarks>
    public float MiddleOf(Drawn drawing) =>
        drawing.At.Y + _frame.LineHeight(drawing.Font, drawing.Scale) / 2f;

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

    public bool IsInsideItsClip =>
        At.X >= clip.X && At.X <= clip.X + clip.Width
        && At.Y >= clip.Y && At.Y <= clip.Y + clip.Height;
}
