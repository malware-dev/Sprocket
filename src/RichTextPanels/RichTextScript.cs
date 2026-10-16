using System;
using System.Collections.Generic;
using System.Text;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRage.Game.GUI.TextPanel;
using VRage.ModAPI;
using VRageMath;
using IMyCubeBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace RichTextPanels
{
    /// <summary>The Rich Text app: the panel's own text, as written in its text editor, shown as markdown-lite.</summary>
    /// <remarks>
    ///     Its font size and padding are the panel's own settings from its text page, and its colors those of its script
    ///     page. Where the block's <see cref="Fitting" /> says so it grows or shrinks to fill the screen in place of the font
    ///     size, and it sits up or down the screen as the block's <see cref="Vertical" /> setting says. It reads from the
    ///     left; a <c>::: center</c> or <c>::: right</c> box places a part of it. The bold and
    ///     italic faces come from the Space Engineers font styles mod, which this one requires, and code is drawn as
    ///     <see cref="CodeFace" /> says.
    ///     An empty panel says how to give it text: at length where that fits, or in one line on a small screen.
    /// </remarks>
    [MyTextSurfaceScript(Id, "Rich Text")]
    public class RichTextScript : MyTSSCommon
    {
        /// <summary>The name the game knows the app by, which the panel stores as its chosen script.</summary>
        public const string Id = "MalwareRichText";

        const string Face = "White";

        readonly List<MySprite> _sprites = new List<MySprite>();
        readonly StringBuilder _read = new StringBuilder();
        readonly StringBuilder _shown = new StringBuilder();
        readonly SpriteGraphics _graphics;
        readonly RichText _text = new RichText
        {
            Font = Face,
            StrongFont = "SpaceEngineersBold",
            EmphasisFont = "SpaceEngineersItalic",
            StrongEmphasisFont = "SpaceEngineersBoldItalic"
        };

        readonly Viewbox _box;
        readonly Grid _page;

        bool _laid;
        bool _fitted;
        Align _align;
        float _fontSize;
        float _padding;
        Color _foreground;
        Color _background;
        Vector2 _size;

        /// <summary>The app on <paramref name="surface" /> of <paramref name="block" />.</summary>
        public RichTextScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            _graphics = new SpriteGraphics(surface, _sprites, new PanelPictures());
            _text.CodeFont = CodeFace.Of(surface);
            _box = new Viewbox { Content = _text };
            _page = new Grid
            {
                Columns = { Length.Flex(1f) },
                Rows = { Length.Flex(1f) },
                Cells = { [0, 0] = _box }
            };
            PanelControls.Refresh(block as Sandbox.ModAPI.IMyTerminalBlock);
        }

        /// <inheritdoc />
        /// <remarks>The app is made when it is chosen and done with when another is, which is when the terminal's settings change.</remarks>
        public override void Dispose()
        {
            base.Dispose();
            PanelControls.Refresh(Block as Sandbox.ModAPI.IMyTerminalBlock);
        }

        /// <inheritdoc />
        /// <remarks>
        ///     The game raises nothing a script can hear when a panel's text changes, so the text is looked at every ten
        ///     ticks, which answers whoever wrote it, a player, a program or a mod, within a sixth of a second, the same in
        ///     multiplayer. Looking costs a copy into a builder kept for it and a comparison, and allocates nothing; only a
        ///     change lays anything out.
        /// </remarks>
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

        /// <inheritdoc />
        /// <remarks>
        ///     The frame is handed over again every time, laid out or not: a panel can drop its picture, as it does when
        ///     nobody is near, and draws nothing until it is given one.
        /// </remarks>
        public override void Run()
        {
            base.Run();

            _read.Clear();
            Surface.ReadText(_read);

            if (!_laid || Changed())
                Lay();

            using (var frame = Surface.DrawFrame())
            {
                foreach (var sprite in _sprites)
                    frame.Add(sprite);
            }
        }

        /// <summary>Whether the text or any setting it is laid out by is different from when it was last laid out.</summary>
        bool Changed()
        {
            if (Vertical.Stored(Block as IMyEntity) != _align || Fitting.Stored(Block as IMyEntity) != _fitted
                || Surface.FontSize != _fontSize || Surface.TextPadding != _padding
                || Surface.ScriptForegroundColor != _foreground || Surface.ScriptBackgroundColor != _background
                || Surface.SurfaceSize != _size || _read.Length != _shown.Length)
                return true;

            for (var i = 0; i < _read.Length; i++)
            {
                if (_read[i] != _shown[i])
                    return true;
            }

            return false;
        }

        /// <summary>Lays the text out by the panel's settings as they are now, and writes it down as the frame to draw.</summary>
        void Lay()
        {
            _laid = true;
            var source = _read.ToString();
            _shown.Clear();
            _shown.Append(source);

            _align = Vertical.Stored(Block as IMyEntity);
            _fitted = Fitting.Stored(Block as IMyEntity);
            _fontSize = Surface.FontSize;
            _padding = Surface.TextPadding;
            _foreground = Surface.ScriptForegroundColor;
            _background = Surface.ScriptBackgroundColor;
            _size = Surface.SurfaceSize;

            _graphics.Begin();
            var room = _graphics.Size;
            var body = _graphics.LineHeight(Face) * _fontSize;

            _text.FontSize = body;
            _text.Heading1Size = body * 1.5f;
            _text.Heading2Size = body * 1.25f;
            _text.Heading3Size = body;
            _text.BlockSpacing = body * 0.5f;
            _text.Indent = body;
            _text.Color = _foreground;
            _text.StrongColor = _foreground;
            _text.EmphasisColor = _foreground;
            _text.HeadingColor = _foreground;
            _text.RuleColor = Color.Lerp(_background, _foreground, 0.5f);
            _box.Padding = new Thickness(_padding / 100f * Math.Min(room.X, room.Y));

            // The instructions are sentences that wrap, so they are never fitted: they are shown at the panel's own size, and
            // in one line where the whole of them does not fit.
            var blank = IsBlank(source);
            _box.Grows = _fitted && !blank;
            _box.Shrinks = _fitted && !blank;
            if (blank)
            {
                _text.Markdown = Instructions.Full;
                _box.Measure(_graphics, room);
                if (_box.DesiredSize.Y > room.Y)
                    _text.Markdown = Instructions.Brief;
            }
            else
                _text.Markdown = source;

            _box.Align = Align.Wide | _align;
            _page.Measure(_graphics, room);
            _page.Arrange(new RectangleF(Vector2.Zero, room));

            _graphics.Fill(new RectangleF(Vector2.Zero, room), _background);
            _page.Draw(_graphics);
        }

        static bool IsBlank(string text)
        {
            foreach (var character in text)
            {
                if (!char.IsWhiteSpace(character))
                    return false;
            }

            return true;
        }
    }
}
