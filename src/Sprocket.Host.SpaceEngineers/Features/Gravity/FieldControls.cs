using System.Collections.Generic;
using System.Globalization;
using Lumel.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Gravity
{
    /// <summary>The two buttons on a box gravity generator that size its field: to every block of its grid, and to every pressurized room in it.</summary>
    /// <remarks>
    /// Controls are global per block type rather than per block, so this runs once per mod load, driven off the first
    /// gravity generator to come alive; see <see cref="GravityBlock" /> for why no earlier.
    /// <para>
    /// Static because the controls are: one set per block type for the whole game, which the terminal hands back through
    /// a callback that carries no state of the mod's. They die with the mod's load.
    /// </para>
    /// </remarks>
    public static class FieldControls
    {
        const string Depth = "Depth";

        /// <summary>The most a fitted side may fall short of what was asked and still count as covering it, in metres.</summary>
        const float Slack = 0.01f;

        /// <summary>How much more air than its surroundings a room must hold to count as pressurized, as a share of a full room.</summary>
        const float Air = 0.01f;

        static bool _done;

        static IMyTerminalControlButton _extents;
        static IMyTerminalControlButton _interior;

        /// <summary>Makes the two buttons once, in <paramref name="texts" />, which the game keeps for the session.</summary>
        public static void DoOnce(Texts texts)
        {
            if (_done)
                return;

            _done = true;

            _extents = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyGravityGenerator>("Sprocket_FitExtents");

            _extents.Title = MyStringId.GetOrCompute(texts.TerminalFitExtents);
            _extents.Tooltip = MyStringId.GetOrCompute(texts.TerminalFitExtentsAbout);
            _extents.SupportsMultipleBlocks = true;
            _extents.Visible = _ => true;
            _extents.Enabled = _ => true;
            _extents.Action = FitExtents;

            _interior = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyGravityGenerator>("Sprocket_FitInterior");

            _interior.Title = MyStringId.GetOrCompute(texts.TerminalFitInterior);
            _interior.Tooltip = MyStringId.GetOrCompute(texts.TerminalFitInteriorAbout);
            _interior.SupportsMultipleBlocks = true;
            _interior.Visible = _ => true;
            _interior.Enabled = HasRooms;
            _interior.Action = FitInterior;

            MyAPIGateway.TerminalControls.AddControl<IMyGravityGenerator>(_extents);
            MyAPIGateway.TerminalControls.AddControl<IMyGravityGenerator>(_interior);
        }

        /// <summary>Moves the two buttons to stand under the field's depth, in the list of controls a gravity generator is about to show.</summary>
        /// <remarks>The terminal's <c>CustomControlGetter</c>, as for the automation button. Where the depth is missing, the two go last.</remarks>
        public static void Arrange(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (_extents == null || !(block is IMyGravityGenerator))
                return;

            controls.Remove(_extents);
            controls.Remove(_interior);

            var at = controls.Count;
            for (var i = 0; i < controls.Count; i++)
            {
                if (controls[i].Id == Depth)
                {
                    at = i + 1;
                    break;
                }
            }

            controls.Insert(at, _interior);
            controls.Insert(at, _extents);
        }

        /// <summary>Whether <paramref name="block" />'s grid has rooms at all, which it has only where the world has pressurization on.</summary>
        static bool HasRooms(IMyTerminalBlock block)
        {
            return block.CubeGrid != null && block.CubeGrid.GasSystem != null;
        }

        static void FitExtents(IMyTerminalBlock block)
        {
            var generator = block as IMyGravityGenerator;
            var texts = SprocketSession.Texts;
            if (generator == null || texts == null)
                return;

            var grid = generator.CubeGrid;
            Fit(generator, grid.Min, grid.Max, texts.TerminalFitExtents, texts);
        }

        static void FitInterior(IMyTerminalBlock block)
        {
            var generator = block as IMyGravityGenerator;
            var texts = SprocketSession.Texts;
            if (generator == null || texts == null || !HasRooms(block))
                return;

            var rooms = new List<IMyOxygenRoom>();
            generator.CubeGrid.GasSystem.GetRooms(rooms);

            var min = new Vector3I(int.MaxValue);
            var max = new Vector3I(int.MinValue);
            var any = false;
            var size = generator.CubeGrid.GridSize;
            for (var i = 0; i < rooms.Count; i++)
            {
                if (!Pressurized(rooms[i], size))
                    continue;

                foreach (var cell in rooms[i].Blocks)
                {
                    min = Vector3I.Min(min, cell);
                    max = Vector3I.Max(max, cell);
                    any = true;
                }
            }

            if (!any)
            {
                Say(texts.TerminalFitInterior, texts.TerminalFitNoRooms);
                return;
            }

            Fit(generator, min, max, texts.TerminalFitInterior, texts);
        }

        /// <summary>Whether <paramref name="room" />, on a grid of cells <paramref name="size" /> wide, holds more air than what surrounds the grid.</summary>
        /// <remarks>
        /// The game also counts as an airtight room every sealed pocket inside blocks, such as the inside of a run of conveyor
        /// tube ducts, and those can reach to the ends of a ship. They hold no more air than the outside, so this leaves them
        /// out, and leaves in a room built only of corridor blocks.
        /// </remarks>
        static bool Pressurized(IMyOxygenRoom room, float size)
        {
            return room.IsAirtight && room.OxygenLevel(size) > room.EnvironmentOxygen + Air;
        }

        /// <summary>
        /// Sizes <paramref name="generator" />'s field to the cells from <paramref name="min" /> to <paramref name="max" />; where the
        /// generator cannot reach that far, says by how much and shows both in the world.
        /// </summary>
        /// <remarks>The game holds the field to the generator's own limits as it is set, so what it keeps is read back to tell.</remarks>
        static void Fit(IMyGravityGenerator generator, Vector3I min, Vector3I max, string title, Texts texts)
        {
            var grid = generator.CubeGrid;
            var centre = generator.PositionComp.LocalVolume.Center;
            var wanted = FieldFit.Covering(grid.WorldMatrix, grid.GridSize, min, max, generator.WorldMatrix, centre);

            generator.FieldSize = wanted;

            var kept = generator.FieldSize;
            if (kept.X >= wanted.X - Slack && kept.Y >= wanted.Y - Slack && kept.Z >= wanted.Z - Slack)
                return;

            var marks = SprocketSession.Marks;
            if (marks != null && MyAPIGateway.Session != null)
            {
                var cells = new BoundingBoxD(((Vector3D)min - 0.5) * grid.GridSize, ((Vector3D)max + 0.5) * grid.GridSize);
                var field = new BoundingBoxD(centre - kept * 0.5f, centre + kept * 0.5f);
                marks.Show(generator, cells, field, MyAPIGateway.Session.ElapsedPlayTime);
            }

            Say(title, Template.Fill(Template.Fill(texts.TerminalFitShort, "needed", Metres(wanted)), "reached", Metres(kept)));
        }

        /// <summary>A field size as a person reads it: width by height by depth, in metres.</summary>
        static string Metres(Vector3 size)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.#} × {1:0.#} × {2:0.#} m", size.X, size.Y, size.Z);
        }

        /// <summary>Tells the player <paramref name="what" />, under <paramref name="title" />.</summary>
        /// <remarks>A window rather than the HUD's notifications: this answers a press in the terminal, and the HUD does not draw while the terminal is open.</remarks>
        static void Say(string title, string what)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowMissionScreen(title, null, null, what);
        }
    }
}
