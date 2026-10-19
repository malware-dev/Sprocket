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
    public class StatusApp : MyTextSurfaceScriptBase
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
                _blank = false;
                _drawnOf = _status;
                _drawn = _status.Revision;
                _turn = turn;
                _size = Surface.SurfaceSize;
                _ink = ink;
                _ground = ground;
                _padding = Surface.TextPadding;

                _graphics.Begin();
                _picture.Draw(_graphics, _status, texts, ink, ground, _padding, Metres(), turn);
            }

            using (MySpriteDrawFrame frame = Surface.DrawFrame())
            {
                foreach (MySprite sprite in _sprites)
                    frame.Add(sprite);
            }
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
