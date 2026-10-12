using System;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What one level of the settings says: each setting it gives a value of, and none for the ones it leaves to the level below.</summary>
    /// <remarks>Two levels: every world, kept in the global file, and this world, kept in the world's own file, which wins.</remarks>
    public sealed class Layer
    {
        const string Developer = "developerMode";

        const string Fastest = "fastestUpdate";

        const string Waits = "waits";

        /// <summary>A level saying nothing.</summary>
        public static readonly Layer Empty = new Layer(null, null, null);

        /// <summary>The fastest updates there are to choose: every tick, every ten and every hundred, the rota's own periods.</summary>
        public static readonly int[] Updates = { 1, 10, 100 };

        /// <summary>A level giving the values that are not null, and leaving the rest to the level below.</summary>
        public Layer(bool? developerMode, int? fastestUpdate, Waiting? waiting)
        {
            DeveloperMode = developerMode;
            FastestUpdate = fastestUpdate.HasValue ? Nearest(fastestUpdate.Value) : (int?)null;
            Waiting = waiting;
        }

        /// <summary>Whether the developer tools are on, or null where this level leaves it.</summary>
        public bool? DeveloperMode { get; }

        /// <summary>The fewest ticks between two runs of anything on a repeat, one of 1, 10 and 100, or null where this level leaves it.</summary>
        public int? FastestUpdate { get; }

        /// <summary>How the editor shows and edits a sequence's waits, or null where this level leaves it.</summary>
        public Waiting? Waiting { get; }

        /// <summary>This level with developer mode given as <paramref name="on" />, or left where it is null.</summary>
        public Layer WithDeveloperMode(bool? on)
        {
            return new Layer(on, FastestUpdate, Waiting);
        }

        /// <summary>This level with the fastest update given as <paramref name="ticks" />, or left where it is null.</summary>
        public Layer WithFastestUpdate(int? ticks)
        {
            return new Layer(DeveloperMode, ticks, Waiting);
        }

        /// <summary>This level with waits shown as <paramref name="waiting" /> says, or left where it is null.</summary>
        public Layer WithWaiting(Waiting? waiting)
        {
            return new Layer(DeveloperMode, FastestUpdate, waiting);
        }

        /// <summary>Whether this level says the same as <paramref name="other" /> about every setting.</summary>
        public bool Same(Layer other)
        {
            return other != null && DeveloperMode == other.DeveloperMode && FastestUpdate == other.FastestUpdate && Waiting == other.Waiting;
        }

        /// <summary>The one of 1, 10 and 100 nearest <paramref name="ticks" />, the smaller where two are as near.</summary>
        public static int Nearest(int ticks)
        {
            var best = Updates[0];
            for (var i = 1; i < Updates.Length; i++)
            {
                if (Math.Abs(Updates[i] - ticks) < Math.Abs(best - ticks))
                    best = Updates[i];
            }

            return best;
        }

        /// <summary>
        /// What <paramref name="section" /> of <paramref name="ini" /> says, or nothing where there is none; a value that is not one of
        /// the choices is said in the log, naming <paramref name="where" />, and taken as the nearest one or left.
        /// </summary>
        public static Layer Read(MyIni ini, string section, string where)
        {
            if (ini == null)
                return Empty;

            bool developer;
            bool? developerMode = ini.Get(section, Developer).TryGetBoolean(out developer) ? developer : (bool?)null;

            int ticks;
            int? fastestUpdate = null;
            if (ini.Get(section, Fastest).TryGetInt32(out ticks))
            {
                fastestUpdate = Nearest(ticks);
                if (fastestUpdate != ticks)
                    Told.Say(where + " gives " + Fastest + " as " + ticks + ", which is not 1, 10 or 100; it is taken as " + fastestUpdate);
            }

            string word;
            Waiting? waiting = null;
            if (ini.Get(section, Waits).TryGetString(out word))
            {
                if (word == Spelled(World.Waiting.Switch))
                    waiting = World.Waiting.Switch;
                else if (word == Spelled(World.Waiting.Step))
                    waiting = World.Waiting.Step;
                else
                    Told.Say(where + " gives " + Waits + " as " + word + ", which is not switch or step; it is left to the level below");
            }

            return new Layer(developerMode, fastestUpdate, waiting);
        }

        /// <summary>Writes this level into <paramref name="section" /> of <paramref name="ini" />: each value it gives, and none for each it leaves.</summary>
        public void Write(MyIni ini, string section)
        {
            if (DeveloperMode.HasValue)
                ini.Set(section, Developer, DeveloperMode.Value);
            else
                ini.Delete(section, Developer);

            if (FastestUpdate.HasValue)
                ini.Set(section, Fastest, FastestUpdate.Value);
            else
                ini.Delete(section, Fastest);

            if (Waiting.HasValue)
                ini.Set(section, Waits, Spelled(Waiting.Value));
            else
                ini.Delete(section, Waits);
        }

        /// <summary>How <paramref name="waiting" /> is spelled in a file.</summary>
        static string Spelled(Waiting waiting)
        {
            return waiting == World.Waiting.Step ? "step" : "switch";
        }
    }
}
