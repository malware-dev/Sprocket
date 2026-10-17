using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using IngameBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>
    /// The construct's automation on a panel: one line per system, and whether anything wants looking at.
    /// </summary>
    /// <remarks>
    /// For a glance from across the bridge rather than for working in. The editor is where somebody
    /// changes a rule; this says whether the ship is doing what they built, and it is deliberately
    /// nothing a person can press.
    /// <para>
    /// <b>Cheap on purpose.</b> A surface script is asked to draw far more often than its news changes,
    /// so the facts are worked out on <c>Update100</c> - a second and a half - and everything below is
    /// one sprite per row. Working out a fault can send the host off to read a toolbar, which is the
    /// same reason the editor's screens do it on a cadence.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// The words are what a player picks from the list of apps a screen can show, and they are the
    /// only part of this a person ever reads. They name what it shows rather than what wrote it: a
    /// screen showing the state of the ship's automation is "Automation", and whose mod it is has
    /// nothing to do with the choice being made.
    /// </remarks>
    [MyTextSurfaceScript(Id, "Automation")]
    public class StatusApp : MyTextSurfaceScriptBase
    {
        /// <summary>
        /// What the game calls this script, in a slot and in the list a player picks from.
        /// </summary>
        /// <remarks>
        /// <b>Never change it.</b> A surface stores the script it was given by this name, so a new one
        /// reads as a surface asking for a script that does not exist.
        /// </remarks>
        public const string Id = "Sprocket_Status";

        const float DesignHeight = 320f;

        const byte Faint = 110;

        readonly Overview _overview = new Overview();
        readonly List<SprocketUnit> _found = new List<SprocketUnit>();

        Color _ink;
        Color _ground;
        Color _quiet;

        /// <summary>The status panel drawn on <paramref name="surface" /> of <paramref name="block" />, <paramref name="size" /> big.</summary>
        public StatusApp(IMyTextSurface surface, IngameBlock block, Vector2 size)
            : base(surface, block, size)
        {
        }

        /// <summary>
        /// A second and a half, which is slower than anything on here can change and be missed.
        /// </summary>
        /// <remarks>
        /// A sequence firing is visible for longer than that in every case a glance is for: a door
        /// takes seconds to move, and a rule that fired is still "running" while it does.
        /// </remarks>
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

        /// <summary>The block, mod-side, which is what the session and the grid ask for.</summary>
        /// <remarks>
        /// The base hands out the Ingame face of it. The same object implements both, and every
        /// other MalTek surface script narrows it here for the same reason: nothing below wants to
        /// think about which of the two it is holding.
        /// </remarks>
        public new IMyCubeBlock Block => (IMyCubeBlock)base.Block;

        /// <inheritdoc />
        public override void Run()
        {
            base.Run();

            // A surface script that throws takes the session with it, and a status panel is the last
            // thing that should be able to end somebody's world. So it says what went wrong on the
            // panel, where whoever is standing in front of it can read it out to me - and it says it
            // here rather than on the next pass, which might throw in the same place and never arrive.
            try
            {
                Draw();
            }
            catch (Exception wrong)
            {
                Shades();
                Trouble(wrong.Message);
            }
        }

        /// <summary>
        /// Takes the panel's own two colours, and fades one of them.
        /// </summary>
        /// <remarks>
        /// <b>No settings, and nothing in Custom Data.</b> Every scripted surface already has a script
        /// foreground and background the player sets in the terminal, so those <em>are</em> the colour
        /// choice - asking again in Custom Data would be a second place to set one thing and a first
        /// place for the two to disagree. Read on every draw, so moving a slider shows up without
        /// anything having to notice that it moved.
        /// <para>
        /// The faded one is the foreground at low alpha rather than a blend towards the background, so
        /// it stays right whatever ends up behind it.
        /// </para>
        /// <para>
        /// <b>Running, problem and warning do not track the player.</b> Green means go, amber means look
        /// and red means stop; a panel that hue-shifted those to match somebody's bridge would be saying
        /// nothing at all. They are the editor's own three, so a glance and a look agree.
        /// </para>
        /// </remarks>
        void Shades()
        {
            _ink = Surface.ScriptForegroundColor;
            _ground = Surface.ScriptBackgroundColor;
            _quiet = new Color(_ink.R, _ink.G, _ink.B, Faint);
        }

        void Draw()
        {
            Shades();

            var block = Block as IMyTerminalBlock;
            var functional = Block as IMyFunctionalBlock;

            // An unpowered or switched-off panel shows nothing, the way every other screen in the game
            // does. Drawing a stale ship on a dead panel is the one reading nobody can correct.
            if (functional != null && !functional.Enabled)
            {
                Blank();
                return;
            }

            SprocketSession.OnConstruct(Block, _found);

            using (MySpriteDrawFrame frame = Surface.DrawFrame())
            {
                var size = Surface.SurfaceSize;
                var scale = size.Y / DesignHeight;
                var inset = new Vector2(
                    (float)Math.Round(size.X * Surface.TextPadding / 100f) + (8f * scale),
                    (float)Math.Round(size.Y * Surface.TextPadding / 100f) + (6f * scale));

                var origin = (Surface.TextureSize - size) / 2f;
                var left = origin.X + inset.X;
                var right = origin.X + size.X - inset.X;
                var y = origin.Y + inset.Y;

                Ground(frame);

                y = Heading(frame, block, left, right, y, scale);

                if (_found.Count == 0)
                {
                    Line(frame, SprocketSession.Runs
                        ? "No Sprocket on this construct."
                        : "Sprocket runs on the server.", left, y, scale * 0.8f, _quiet);
                    return;
                }

                Systems(frame, left, right, y, scale, origin.Y + size.Y - (46f * scale));
                Totals(frame, left, right, origin, size, scale);
            }
        }

        float Heading(MySpriteDrawFrame frame, IMyTerminalBlock block, float left, float right, float y,
            float scale)
        {
            Line(frame, "SPROCKET", left, y, scale * 0.9f, _ink);

            IMyCubeGrid grid = block?.CubeGrid;
            if (grid != null)
                Line(frame, grid.CustomName, right, y, scale * 0.9f, _quiet, TextAlignment.RIGHT);

            y += 30f * scale;
            frame.Add(Fill(new Vector2((left + right) / 2f, y), new Vector2(right - left, scale),
                _quiet));

            return y + (10f * scale);
        }

        /// <summary>
        /// One line per system, and a line saying how many did not fit.
        /// </summary>
        /// <remarks>
        /// Cut off rather than shrunk to fit. A panel that reduces its type until forty systems are
        /// legible is a panel nobody can read from where they are standing, which is the only place
        /// this is ever read from.
        /// </remarks>
        void Systems(MySpriteDrawFrame frame, float left, float right, float y, float scale,
            float bottom)
        {
            // Every Sprocket on the construct into one picture, before any of it is drawn, so the line
            // that says how many did not fit can count them.
            _overview.Clear();
            foreach (SprocketUnit unit in _found)
            {
                _overview.Include(unit.World, unit.Editor.Program, unit.Runner);
            }

            var rows = _overview.Rows;
            var texts = _found[0].Editor.Texts;
            if (rows.Count == 0)
            {
                Line(frame, "Nothing built yet.", left, y, scale * 0.8f, _quiet);
                return;
            }

            var height = 26f * scale;
            for (var i = 0; i < rows.Count; i++)
            {
                if (y + height > bottom)
                {
                    Line(frame, $"and {(rows.Count - i)} more", left, y, scale * 0.7f,
                        _quiet);
                    return;
                }

                Row(frame, rows[i], texts, left, right, y, scale);
                y += height;
            }
        }

        void Row(MySpriteDrawFrame frame, Overview.Row row, Texts texts, float left, float right, float y, float scale)
        {
            var lamp = 10f * scale;
            frame.Add(Fill(new Vector2(left + (lamp / 2f), y + (9f * scale)),
                new Vector2(lamp, lamp), Lamp(row)));

            var name = string.IsNullOrEmpty(row.Name) ? "Unnamed" : row.Name;
            if (row.Instances > 1)
                name = $"{name}  x{row.Instances}";

            Line(frame, name, left + (lamp * 2f), y, scale * 0.8f, _ink);
            Line(frame, Says(row, texts), right, y, scale * 0.8f, Lamp(row), TextAlignment.RIGHT);
        }

        /// <summary>
        /// What the row says on its right: the news, or what it is doing when there is none.
        /// </summary>
        /// <remarks>
        /// Trouble outranks state, because a system that is broken and waiting is worth reading as
        /// broken. The words are the editor's own - see <see cref="SequenceText" /> - so the two
        /// screens cannot come to disagree about what a state is called.
        /// </remarks>
        string Says(Overview.Row row, Texts texts)
        {
            if (row.Problems > 0)
                return row.Problems == 1 ? "1 problem" : $"{row.Problems} problems";

            if (row.Parked)
                return SequenceText.Status(texts, SequenceStatus.Off);

            if (row.Warnings > 0)
                return row.Warnings == 1 ? "1 warning" : $"{row.Warnings} warnings";

            return SequenceText.Status(texts, row.Status);
        }

        Color Lamp(Overview.Row row)
        {
            if (row.Problems > 0)
                return Palette.Problem;

            if (row.Parked)
                return _quiet;

            if (row.Warnings > 0)
                return Palette.Notice;

            if (row.Status == SequenceStatus.Running || row.Status == SequenceStatus.RunningWaiting)
                return Palette.Live;

            return row.Status == SequenceStatus.Off ? _quiet : _ink;
        }

        /// <summary>The one line somebody actually reads from across the room.</summary>
        void Totals(MySpriteDrawFrame frame, float left, float right, Vector2 origin, Vector2 size,
            float scale)
        {
            var y = origin.Y + size.Y - (34f * scale);

            frame.Add(Fill(new Vector2((left + right) / 2f, y), new Vector2(right - left, scale),
                _quiet));

            y += 8f * scale;

            Color colour;
            string news;
            if (_overview.Problems > 0)
            {
                news = _overview.Problems == 1 ? "1 problem" : $"{_overview.Problems} problems";
                colour = Palette.Problem;
            }
            else if (_overview.Warnings > 0)
            {
                news = _overview.Warnings == 1 ? "1 warning" : $"{_overview.Warnings} warnings";
                colour = Palette.Notice;
            }
            else
            {
                news = "all clear";
                colour = _quiet;
            }

            Line(frame, news, left, y, scale * 0.8f, colour);
            Line(frame, _overview.Running == 0 ? "nothing running" : $"{_overview.Running} running",
                right, y, scale * 0.8f, _ink, TextAlignment.RIGHT);
        }

        void Line(MySpriteDrawFrame frame, string text, float x, float y, float scale, Color colour,
            TextAlignment alignment = TextAlignment.LEFT)
        {
            MySprite sprite = MySprite.CreateText(text ?? "", "White", colour, scale, alignment);
            sprite.Position = new Vector2(x, y);
            frame.Add(sprite);
        }

        static MySprite Fill(Vector2 at, Vector2 size, Color colour)
        {
            MySprite sprite = MySprite.CreateSprite("SquareSimple", at, size);
            sprite.Color = colour;
            return sprite;
        }

        void Blank()
        {
            using (MySpriteDrawFrame frame = Surface.DrawFrame())
                Ground(frame);
        }

        void Trouble(string said)
        {
            using (MySpriteDrawFrame frame = Surface.DrawFrame())
            {
                Vector2 size = Ground(frame);
                var scale = size.Y / DesignHeight;
                var origin = (Surface.TextureSize - size) / 2f;

                Line(frame, "Sprocket could not draw this:", origin.X + (10f * scale),
                    origin.Y + (10f * scale), scale * 0.8f, Palette.Problem);
                Line(frame, said ?? "", origin.X + (10f * scale), origin.Y + (36f * scale),
                    scale * 0.6f, _quiet);
            }
        }

        /// <summary>Paints the panel, and answers the size everything else is laid out against.</summary>
        Vector2 Ground(MySpriteDrawFrame frame)
        {
            var size = Surface.SurfaceSize;
            var origin = (Surface.TextureSize - size) / 2f;

            frame.Add(Fill(new Vector2(origin.X + (size.X / 2f), origin.Y + (size.Y / 2f)),
                size, _ground));

            return size;
        }
    }
}
