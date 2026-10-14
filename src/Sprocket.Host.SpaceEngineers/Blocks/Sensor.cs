using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

// What a sensor detects lives in the Ingame namespace, which cannot be imported whole: IMySensorBlock is declared in both,
// and the mod-side one is the one with the event on it.
using MyDetectedEntityInfo = Sandbox.ModAPI.Ingame.MyDetectedEntityInfo;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A sensor: the two signals its toolbar sends, whether it is detecting and how many things, and the members of its
    /// terminal said in Sprocket's words.
    /// </summary>
    /// <remarks>
    /// The game runs the first slot of every page when it starts detecting, and the second when it stops. It says when it
    /// starts and stops detecting; nothing tells of a change in how many things it detects.
    /// </remarks>
    public sealed class Sensor : Capability
    {
        static readonly Being Detecting = Being.Flag(Reading.Of<IMySensorBlock>(sensor => sensor.IsActive));

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

        // Filled and emptied on every look at the count, so a look allocates nothing once it has held the most a sensor
        // has seen. Holds nothing between looks: the game's main thread is the only one that reads a block.
        readonly List<MyDetectedEntityInfo> _detected = new List<MyDetectedEntityInfo>();

        readonly Being _count;

        /// <summary>A sensor's capability, with nothing read yet.</summary>
        public Sensor()
        {
            _count = Being.Whole<IMySensorBlock>(Count);
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
                .WithValue("things-detected", "things detected", _count)
                .Looked("A sensor says when it starts and stops detecting, and nothing of how many things it detects.")
                .Renaming("Audible Proximity Alert", true, "audible proximity alert");

            for (var i = 0; i < Switches.Length; i++)
            {
                kind.Renaming(Switches[i], true, Switches[i].ToLowerInvariant())
                    .Renaming(Switches[i], false, "toggle " + Switches[i].ToLowerInvariant());
            }

            for (var i = 0; i < Extents.Length; i++)
                kind.Renaming(Extents[i], true, Extents[i].ToLowerInvariant() + " extent");
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

        /// <summary>A sensor being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMySensorBlock _sensor;
            readonly Action _told;

            public Listening(IMySensorBlock sensor, Action told)
            {
                _sensor = sensor;
                _told = told;
                _sensor.StateChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _sensor.StateChanged -= Changed;
            }

            void Changed(bool detecting)
            {
                _told();
            }
        }
    }
}
