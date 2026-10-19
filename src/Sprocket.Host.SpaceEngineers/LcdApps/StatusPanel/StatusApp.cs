using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Text;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using IngameBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>The construct's automation on a screen: the installations that want looking at, one at a time, or that all is normal.</summary>
    /// <remarks>
    /// The game runs it only while somebody can see the screen. It looks at the automation every hundred ticks, a second
    /// and a half, works it out again only when the automation has changed, and lays the screen out again only when what it
    /// shows, the screen's size or its colors have changed. Every other run hands the game the frame it already has. On a
    /// client the automation runs on the server, so the panel watches what the server says of it instead.
    /// </remarks>
    [MyTextSurfaceScript(Id, "Automation")]
    public class StatusApp : MyTextSurfaceScriptBase, ILiveSurface
    {
        /// <summary>What the game calls this script, in a slot and in the list a player picks from.</summary>
        /// <remarks>Never change it: a surface stores the script it was given by this name.</remarks>
        public const string Id = "Sprocket_Status";

        readonly StatusSource _source = new StatusSource();
        readonly StatusPicture _picture = new StatusPicture();
        readonly List<SprocketUnit> _found = new List<SprocketUnit>();
        readonly List<MySprite> _sprites = new List<MySprite>();
        readonly SpriteGraphics _graphics;
        readonly Action _heard;

        const int Turn = 3;

        /// <summary>How long a page turn takes, in ticks: the cards fade out over half of it and the next page in over the rest.</summary>
        const int Fading = 20;

        /// <summary>How long a newly arrived installation's mark pulses, in ticks, and how long one pulse takes.</summary>
        const int Pulsing = 180;

        const int Pulse = 45;

        /// <summary>How many of its own long sides away a screen may be seen from for its motion to be worth drawing.</summary>
        const float Seen = 10f;

        readonly List<StatusItem> _seen = new List<StatusItem>();
        bool _seenHeard;
        bool _moving;
        int _from;
        int _fadeAt = -1;
        int _pulseAt = -1;

        ConstructStatus _status;
        ConstructStatus _drawnOf;
        long _watching;
        int _ran;
        int _turn;
        int _drawn = -1;
        Vector2 _size;
        Color _ink;
        Color _ground;
        float _padding;
        bool _blank;

        /// <summary>The status panel drawn on <paramref name="surface" /> of <paramref name="block" />, <paramref name="size" /> big.</summary>
        public StatusApp(IMyTextSurface surface, IngameBlock block, Vector2 size)
            : base(surface, block, size)
        {
            _graphics = new SpriteGraphics(surface, _sprites, new PanelPictures());
            _status = _source.Status;
            _heard = Heard;
        }

        /// <summary>Every hundred ticks, a second and a half.</summary>
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

        /// <summary>The block, mod-side, which is what the session and the grid ask for.</summary>
        public new IMyCubeBlock Block => (IMyCubeBlock)base.Block;

        /// <inheritdoc />
        IngameBlock ILiveSurface.Block => base.Block;

        /// <inheritdoc />
        public override void Run()
        {
            base.Run();
            _ran++;
            Safely(true);
        }

        /// <inheritdoc />
        /// <remarks>The game makes an app when a screen is given it and is done with it when another is, which ends its watch.</remarks>
        public override void Dispose()
        {
            base.Dispose();
            SprocketSession.Still(this);
            if (_watching != 0)
                SprocketSession.Unwatch(_watching, _heard);
        }

        /// <summary>The status this panel watches arriving from the server, which it draws at once rather than at its next turn.</summary>
        void Heard()
        {
            Safely(false);
        }

        /// <summary>Draws the panel, looking at the automation again where <paramref name="look" />.</summary>
        /// <remarks>A surface script that throws takes the session with it, so the error is drawn on the panel instead.</remarks>
        void Safely(bool look)
        {
            try
            {
                Draw(look);
            }
            catch (Exception wrong)
            {
                _drawn = -1;
                Trouble(wrong.Message);
            }
        }

        void Draw(bool look)
        {
            // An unpowered or switched-off panel shows nothing, the way every other screen in the game does.
            var functional = Block as IMyFunctionalBlock;
            var texts = SprocketSession.Texts;
            if ((functional != null && !functional.Enabled) || texts == null)
            {
                Blank();
                return;
            }

            if (look)
                Look();

            // A page at a time where there are several, each for three runs, five seconds.
            var turn = _picture.Pages > 1 ? _ran / Turn : 0;
            var ink = Surface.ScriptForegroundColor;
            var ground = Surface.ScriptBackgroundColor;
            if (_blank || _status != _drawnOf || _status.Revision != _drawn || turn != _turn || Surface.SurfaceSize != _size || ink != _ink || ground != _ground
                || Surface.TextPadding != _padding)
            {
                var turned = !_blank && _status == _drawnOf && turn != _turn;
                var fresh = Freshen();
                _blank = false;
                _drawnOf = _status;
                _drawn = _status.Revision;
                _from = _turn;
                _turn = turn;
                _size = Surface.SurfaceSize;
                _ink = ink;
                _ground = ground;
                _padding = Surface.TextPadding;

                // Where somebody is near enough to see it move, the loop draws the turn and the pulse, frame by frame.
                if ((turned || fresh) && Near())
                {
                    var now = SprocketSession.Tick;
                    if (turned)
                        _fadeAt = now;
                    if (fresh)
                        _pulseAt = now;

                    _moving = true;
                    SprocketSession.Animate(this);
                }

                if (!_moving)
                    Lay(turn);
            }

            if (!_moving)
                Hand();
        }

        /// <inheritdoc />
        public bool Frame(List<MySprite> output, int tick)
        {
            var look = _picture.Look;
            var shown = _turn;
            look.Fade = 1f;
            if (_fadeAt >= 0 && tick - _fadeAt < Fading)
            {
                var half = Fading / 2f;
                var into = tick - _fadeAt;
                shown = into < half ? _from : _turn;
                look.Fade = into < half ? 1f - into / half : (into - half) / half;
            }
            else
                _fadeAt = -1;

            look.Pulse = 0f;
            if (_pulseAt >= 0 && tick - _pulseAt < Pulsing)
                look.Pulse = 0.5f - 0.5f * (float)Math.Cos(2.0 * Math.PI * (tick - _pulseAt) / Pulse);
            else
            {
                _pulseAt = -1;
                look.Fresh.Clear();
            }

            _moving = _fadeAt >= 0 || _pulseAt >= 0;
            Lay(shown);
            output.AddRange(_sprites);

            // The game draws the panel's own frame again whenever its texture is made afresh, so it is handed the last one.
            if (!_moving)
                Hand();

            return _moving;
        }

        /// <summary>Lays the status out showing the page <paramref name="turn" /> places in.</summary>
        void Lay(int turn)
        {
            _graphics.Begin();
            _picture.Draw(_graphics, _status, SprocketSession.Texts, _ink, _ground, _padding, Metres(), turn);
        }

        /// <summary>Hands the game the frame last laid out.</summary>
        void Hand()
        {
            using (MySpriteDrawFrame frame = Surface.DrawFrame())
            {
                foreach (MySprite sprite in _sprites)
                    frame.Add(sprite);
            }
        }

        /// <summary>Notes which installations newly want looking at, for their marks to pulse, and answers whether any do.</summary>
        bool Freshen()
        {
            var fresh = _picture.Look.Fresh;
            var any = false;
            if (_status.Heard && _seenHeard)
            {
                foreach (var item in _status.Items)
                {
                    var known = false;
                    foreach (var seen in _seen)
                        known |= seen.Same(item);

                    if (!known)
                    {
                        fresh.Add(item);
                        any = true;
                    }
                }
            }

            _seen.Clear();
            foreach (var item in _status.Items)
                _seen.Add(item);

            _seenHeard = _status.Heard;
            return any;
        }

        /// <summary>Whether the camera is near enough the screen for its motion to be seen: within <see cref="Seen" /> of its long sides.</summary>
        bool Near()
        {
            var camera = MyAPIGateway.Session?.Camera;
            if (camera == null)
                return false;

            var reach = Seen * Metres();
            return Vector3D.DistanceSquared(camera.Position, Block.GetPosition()) <= reach * reach;
        }

        /// <summary>How long the screen's long side is, in metres: the block's longest side, as near as the panel can know it.</summary>
        float Metres()
        {
            var grid = Block.CubeGrid;
            if (grid == null)
                return 0f;

            var cells = Block.Max - Block.Min + Vector3I.One;
            return grid.GridSize * Math.Max(cells.X, Math.Max(cells.Y, cells.Z));
        }

        /// <summary>Works out how the construct's automation stands where it runs here, and watches what the server says of it on a client.</summary>
        void Look()
        {
            if (SprocketSession.Runs)
            {
                SprocketSession.OnConstruct(Block, _found);
                _source.Look(_found, SprocketSession.ConstructName(Block), _ran);
                _status = _source.Status;
                return;
            }

            var construct = SprocketSession.Construct(Block);
            if (construct != _watching && _watching != 0)
                SprocketSession.Unwatch(_watching, _heard);

            _watching = construct;
            _status = construct == 0 ? _source.Status : SprocketSession.Watch(construct, _heard);
        }

        void Blank()
        {
            _blank = true;
            using (MySpriteDrawFrame frame = Surface.DrawFrame())
                frame.Add(Fill(Surface.SurfaceSize, Surface.ScriptBackgroundColor));
        }

        void Trouble(string said)
        {
            using (MySpriteDrawFrame frame = Surface.DrawFrame())
            {
                var size = Surface.SurfaceSize;
                var origin = (Surface.TextureSize - size) / 2f;
                var scale = Math.Max(0.5f, size.Y / 640f);
                var texts = SprocketSession.Texts;

                frame.Add(Fill(size, Surface.ScriptBackgroundColor));
                var line = MySprite.CreateText(texts == null ? said ?? "" : Template.Fill(texts.PanelFailed, "reason", said ?? ""),
                    MyFontEnum.White, Palette.Problem, scale, TextAlignment.LEFT);
                line.Position = origin + new Vector2(10f, 10f);
                frame.Add(line);
            }
        }

        /// <summary>A square of <paramref name="color" /> over the whole face.</summary>
        MySprite Fill(Vector2 size, Color color)
        {
            var origin = (Surface.TextureSize - size) / 2f;
            var sprite = MySprite.CreateSprite("SquareSimple", origin + size / 2f, size);
            sprite.Color = color;
            return sprite;
        }
    }
}
