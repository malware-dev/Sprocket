using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

// What a sensor detects lives in the Ingame namespace, which cannot be imported whole: IMySensorBlock is declared in both,
// and the mod-side one is the one with the event on it.
using MyDetectedEntityInfo = Sandbox.ModAPI.Ingame.MyDetectedEntityInfo;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A sensor, with the two signals its toolbar sends, read for whether it is detecting and how many things.</summary>
    /// <remarks>
    /// The game runs the first slot of every page as it starts detecting and the second as it stops; nothing tells of a
    /// change in the count.
    /// </remarks>
    public sealed partial class Sensor : Capability
    {
        static readonly OwnValue Detecting = OwnValue.Flag(Reading.Of<IMySensorBlock>(sensor => sensor.IsActive));

        // The game's own words for each switch, which name its value.
        static readonly string[] Switches =
        {
            "Detect Players",
            "Detect Small Ships",
            "Detect Large Ships",
            "Detect Stations",
            "Detect Floating Objects",
            "Detect Asteroids",
            "Detect Subgrids",
            "Detect Owner",
            "Detect Friendly",
            "Detect Neutral",
            "Detect Enemy"
        };

        static readonly string[] Extents = { "Left", "Right", "Top", "Bottom", "Front", "Back" };

        // Reused on every look at the count, so a look allocates nothing once the list has grown; only the game's main
        // thread reads a block.
        readonly List<MyDetectedEntityInfo> _detected = new List<MyDetectedEntityInfo>();

        readonly OwnValue _count;

        /// <summary>A sensor's capability, with nothing read yet.</summary>
        public Sensor()
        {
            _count = OwnValue.Whole<IMySensorBlock>(Count);
        }

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySensorBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithSignal(new ToolbarSlot(1, 0, 1), "starts detecting")
                .WithSignal(new ToolbarSlot(1, 0, 2), "stops detecting")
                .WithValue("detecting", "detecting", Detecting).Announced()
                .WithValue("detections", "detections", _count)
                .Looked("A sensor says when it starts and stops detecting, and nothing of how many things it detects.")
                .In(Category.Detection)
                .Renaming("Audible Proximity Alert", true, "audible proximity alert");

            foreach (var switchName in Switches)
            {
                kind.Renaming(switchName, true, switchName.ToLowerInvariant())
                    .Renaming(switchName, false, $"toggle {switchName.ToLowerInvariant()}");
            }

            foreach (var extent in Extents)
                kind.Renaming(extent, true, $"{extent.ToLowerInvariant()} extent").Measuring(extent, " m");
        }

        /// <summary>Told as the sensor starts or stops detecting, which it raises on the server and on every client.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var sensor = block as IMySensorBlock;
            return sensor == null ? null : new Listening(sensor, told);
        }

        /// <summary>How many things <paramref name="sensor" /> detects, of those its switches let it react to.</summary>
        long Count(IMySensorBlock sensor)
        {
            _detected.Clear();
            sensor.DetectedEntities(_detected);
            var count = _detected.Count;
            _detected.Clear();
            return count;
        }
    }
}
