using System;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What one level of the settings says: each setting it gives a value of, and none for the ones it leaves to the level below.</summary>
    /// <remarks>Two levels: every world, kept in the global file, and this world, kept in the world's own file, which wins.</remarks>
    public sealed class SettingsLayer
    {
        const string Developer = "developerMode";

        const string Fastest = "fastestUpdate";

        const string Debug = "debugLog";

        /// <summary>A level saying nothing.</summary>
        public static readonly SettingsLayer Empty = new SettingsLayer(null, null);

        /// <summary>The fastest updates there are to choose: every tick, every ten and every hundred, the rota's own periods.</summary>
        public static readonly int[] Updates = { 1, 10, 100 };

        /// <summary>A level giving the values that are not null, and leaving the rest to the level below.</summary>
        public SettingsLayer(bool? developerMode, int? fastestUpdate, bool? debugLog = null)
        {
            DeveloperMode = developerMode;
            FastestUpdate = fastestUpdate.HasValue ? Nearest(fastestUpdate.Value) : (int?)null;
            DebugLog = debugLog;
        }

        /// <summary>Whether the developer tools are on, or null where this level leaves it.</summary>
        public bool? DeveloperMode { get; }

        /// <summary>The fewest ticks between two runs of anything on a repeat, one of 1, 10 and 100, or null where this level leaves it.</summary>
        public int? FastestUpdate { get; }

        /// <summary>Whether every run writes what it does to the game's log, or null where this level leaves it.</summary>
        public bool? DebugLog { get; }

        /// <summary>This level with developer mode given as <paramref name="on" />, or left where it is null.</summary>
        public SettingsLayer WithDeveloperMode(bool? on)
        {
            return new SettingsLayer(on, FastestUpdate, DebugLog);
        }

        /// <summary>This level with the fastest update given as <paramref name="ticks" />, or left where it is null.</summary>
        public SettingsLayer WithFastestUpdate(int? ticks)
        {
            return new SettingsLayer(DeveloperMode, ticks, DebugLog);
        }

        /// <summary>This level with the debug log given as <paramref name="on" />, or left where it is null.</summary>
        public SettingsLayer WithDebugLog(bool? on)
        {
            return new SettingsLayer(DeveloperMode, FastestUpdate, on);
        }

        /// <summary>Whether this level says the same as <paramref name="other" /> about every setting.</summary>
        public bool Same(SettingsLayer other)
        {
            return other != null && DeveloperMode == other.DeveloperMode && FastestUpdate == other.FastestUpdate && DebugLog == other.DebugLog;
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
        public static SettingsLayer Read(MyIni ini, string section, string where)
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
                    GameLog.Say($"{where} gives {Fastest} as {ticks}, which is not 1, 10 or 100; it is taken as {fastestUpdate}");
            }

            bool debug;
            bool? debugLog = ini.Get(section, Debug).TryGetBoolean(out debug) ? debug : (bool?)null;

            return new SettingsLayer(developerMode, fastestUpdate, debugLog);
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

            if (DebugLog.HasValue)
                ini.Set(section, Debug, DebugLog.Value);
            else
                ini.Delete(section, Debug);
        }
    }
}
