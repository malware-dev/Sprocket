using System.Collections.Generic;
using System.Globalization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Gravity;
using VRage;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sprocket.GravityFit
{
    /// <summary>The two buttons on a box gravity generator that size its field: to every block of its grid, and to every pressurized room in it.</summary>
    public static class GravityFieldButtons
    {
        const string Depth = "Depth";

        const float Slack = 0.01f;

        const float Air = 0.01f;

        // Keys into the game's table of texts, which the session fills from Content/Data/Localization.
        static readonly MyStringId Extents = MyStringId.GetOrCompute("Sprocket.GravityFit.Extents");
        static readonly MyStringId ExtentsAbout = MyStringId.GetOrCompute("Sprocket.GravityFit.ExtentsAbout");
        static readonly MyStringId Interior = MyStringId.GetOrCompute("Sprocket.GravityFit.Interior");
        static readonly MyStringId InteriorAbout = MyStringId.GetOrCompute("Sprocket.GravityFit.InteriorAbout");
        static readonly MyStringId NoRooms = MyStringId.GetOrCompute("Sprocket.GravityFit.NoRooms");
        static readonly MyStringId Short = MyStringId.GetOrCompute("Sprocket.GravityFit.Short");

        // Static because every block of the type asks on its first frame and the controls are added once for the world;
        // the game clears every control as a world unloads, and the mod is compiled again for the next, so this starts
        // over with it.
        static bool _done;

        static IMyTerminalControlButton _extents;
        static IMyTerminalControlButton _interior;

        /// <summary>Makes the two buttons once, which the game keeps for the session.</summary>
        public static void DoOnce()
        {
            if (_done)
                return;

            _done = true;

            _extents = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyGravityGenerator>("Sprocket_FitExtents");

            _extents.Title = Extents;
            _extents.Tooltip = ExtentsAbout;
            _extents.SupportsMultipleBlocks = true;
            _extents.Visible = _ => true;
            _extents.Enabled = _ => true;
            _extents.Action = FitExtents;

            _interior = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyGravityGenerator>("Sprocket_FitInterior");

            _interior.Title = Interior;
            _interior.Tooltip = InteriorAbout;
            _interior.SupportsMultipleBlocks = true;
            _interior.Visible = _ => true;
            _interior.Enabled = HasRooms;
            _interior.Action = FitInterior;

            MyAPIGateway.TerminalControls.AddControl<IMyGravityGenerator>(_extents);
            MyAPIGateway.TerminalControls.AddControl<IMyGravityGenerator>(_interior);
        }

        /// <summary>Moves the two buttons to stand under the field's depth, in the list of controls a gravity generator is about to show.</summary>
        /// <remarks>Called as the terminal's <c>CustomControlGetter</c>.</remarks>
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
            if (generator == null)
                return;

            var grid = generator.CubeGrid;
            Fit(generator, grid.Min, grid.Max, Extents);
        }

        static void FitInterior(IMyTerminalBlock block)
        {
            var generator = block as IMyGravityGenerator;
            if (generator == null || !HasRooms(block))
                return;

            var rooms = new List<IMyOxygenRoom>();
            generator.CubeGrid.GasSystem.GetRooms(rooms);

            var min = new Vector3I(int.MaxValue);
            var max = new Vector3I(int.MinValue);
            var any = false;
            var size = generator.CubeGrid.GridSize;
            foreach (var room in rooms)
            {
                if (!Pressurized(room, size))
                    continue;

                foreach (var cell in room.Blocks)
                {
                    min = Vector3I.Min(min, cell);
                    max = Vector3I.Max(max, cell);
                    any = true;
                }
            }

            if (!any)
            {
                Say(Interior, MyTexts.GetString(NoRooms));
                return;
            }

            Fit(generator, min, max, Interior);
        }

        /// <summary>Whether <paramref name="room" />, on a grid of cells <paramref name="size" /> wide, holds more air than what surrounds the grid.</summary>
        /// <remarks>The game also counts every sealed pocket inside blocks, such as a run of conveyor tubes, as an airtight room.</remarks>
        static bool Pressurized(IMyOxygenRoom room, float size)
        {
            return room.IsAirtight && room.OxygenLevel(size) > room.EnvironmentOxygen + Air;
        }

        /// <summary>
        /// Sizes <paramref name="generator" />'s field to the cells from <paramref name="min" /> to <paramref name="max" />; where the
        /// generator cannot reach that far, says by how much and shows both in the world.
        /// </summary>
        /// <remarks>The game holds the field to the generator's own limits as it is set.</remarks>
        static void Fit(IMyGravityGenerator generator, Vector3I min, Vector3I max, MyStringId title)
        {
            var grid = generator.CubeGrid;
            var centre = generator.PositionComp.LocalVolume.Center;
            var wanted = GravityFieldSize.Covering(generator, min, max);

            generator.FieldSize = wanted;

            var kept = generator.FieldSize;
            if (kept.X >= wanted.X - Slack && kept.Y >= wanted.Y - Slack && kept.Z >= wanted.Z - Slack)
                return;

            var marks = GravityFitSession.Marks;
            if (marks != null && MyAPIGateway.Session != null)
            {
                var cells = new BoundingBoxD(((Vector3D)min - 0.5) * grid.GridSize, ((Vector3D)max + 0.5) * grid.GridSize);
                var field = new BoundingBoxD(centre - kept * 0.5f, centre + kept * 0.5f);
                marks.Show(generator, cells, field, MyAPIGateway.Session.ElapsedPlayTime);
            }

            Say(title, MyTexts.GetString(Short).Replace("{needed}", Metres(wanted)).Replace("{reached}", Metres(kept)));
        }

        /// <summary>A field size as a person reads it: width by height by depth, in meters.</summary>
        static string Metres(Vector3 size)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.#} × {1:0.#} × {2:0.#} m", size.X, size.Y, size.Z);
        }

        /// <summary>Tells the player <paramref name="what" />, under <paramref name="title" />.</summary>
        /// <remarks>The HUD does not draw while the terminal is open.</remarks>
        static void Say(MyStringId title, string what)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowMissionScreen(MyTexts.GetString(title), null, null, what);
        }
    }
}
