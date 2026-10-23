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

        const string Tips = "tipKeeping";

        const string Links = "tipLinks";

        /// <summary>A level saying nothing.</summary>
        public static readonly SettingsLayer Empty = new SettingsLayer(null, null);

        /// <summary>The fastest updates there are to choose: every tick, every ten and every hundred, the rota's own periods.</summary>
        public static readonly int[] Updates = { 1, 10, 100 };

        /// <summary>A level giving the values that are not null, and leaving the rest to the level below.</summary>
        public SettingsLayer(bool? developerMode, int? fastestUpdate, bool? debugLog = null, TipKeeping? tipKeeping = null, TipLinks? tipLinks = null)
        {
            DeveloperMode = developerMode;
            FastestUpdate = fastestUpdate.HasValue ? Nearest(fastestUpdate.Value) : (int?)null;
            DebugLog = debugLog;
            TipKeeping = tipKeeping;
            TipLinks = tipLinks;
        }

        /// <summary>Whether the developer tools are on, or null where this level leaves it.</summary>
        public bool? DeveloperMode { get; }

        /// <summary>The fewest ticks between two runs of anything on a repeat, one of 1, 10 and 100, or null where this level leaves it.</summary>
        public int? FastestUpdate { get; }

        /// <summary>Whether every run writes what it does to the game's log, or null where this level leaves it.</summary>
        public bool? DebugLog { get; }

        /// <summary>How a tip stays open, the trial's variant, or null where this level leaves it.</summary>
        public TipKeeping? TipKeeping { get; }

        /// <summary>Where a link inside a tip shows its page, the trial's variant, or null where this level leaves it.</summary>
        public TipLinks? TipLinks { get; }

        /// <summary>This level with developer mode given as <paramref name="on" />, or left where it is null.</summary>
        public SettingsLayer WithDeveloperMode(bool? on)
        {
            return new SettingsLayer(on, FastestUpdate, DebugLog, TipKeeping, TipLinks);
        }

        /// <summary>This level with the fastest update given as <paramref name="ticks" />, or left where it is null.</summary>
        public SettingsLayer WithFastestUpdate(int? ticks)
        {
            return new SettingsLayer(DeveloperMode, ticks, DebugLog, TipKeeping, TipLinks);
        }

        /// <summary>This level with the debug log given as <paramref name="on" />, or left where it is null.</summary>
        public SettingsLayer WithDebugLog(bool? on)
        {
            return new SettingsLayer(DeveloperMode, FastestUpdate, on, TipKeeping, TipLinks);
        }

        /// <summary>This level with how a tip stays open given as <paramref name="keeping" />, or left where it is null.</summary>
        public SettingsLayer WithTipKeeping(TipKeeping? keeping)
        {
            return new SettingsLayer(DeveloperMode, FastestUpdate, DebugLog, keeping, TipLinks);
        }

        /// <summary>This level with where a link inside a tip shows its page given as <paramref name="links" />, or left where it is null.</summary>
        public SettingsLayer WithTipLinks(TipLinks? links)
        {
            return new SettingsLayer(DeveloperMode, FastestUpdate, DebugLog, TipKeeping, links);
        }

        /// <summary>Whether this level says the same as <paramref name="other" /> about every setting.</summary>
        public bool Same(SettingsLayer other)
        {
            return other != null && DeveloperMode == other.DeveloperMode && FastestUpdate == other.FastestUpdate && DebugLog == other.DebugLog
                   && TipKeeping == other.TipKeeping && TipLinks == other.TipLinks;
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

            string tips;
            TipKeeping? tipKeeping = null;
            if (ini.Get(section, Tips).TryGetString(out tips))
            {
                if (tips == Keeping(World.TipKeeping.Stay))
                    tipKeeping = World.TipKeeping.Stay;
                else if (tips == Keeping(World.TipKeeping.Alt))
                    tipKeeping = World.TipKeeping.Alt;
                else
                    GameLog.Say($"{where} gives {Tips} as {tips}, which is not {Keeping(World.TipKeeping.Stay)} or {Keeping(World.TipKeeping.Alt)}; it is left to the level below");
            }

            return new SettingsLayer(developerMode, fastestUpdate, debugLog, tipKeeping, ReadLinks(ini, section, where));
        }

        /// <summary>
        /// Where a link inside a tip shows its page, as <paramref name="section" /> of <paramref name="ini" /> says, or null where it
        /// says nothing; a value that is not one of the choices is said in the log, naming <paramref name="where" />, and left.
        /// </summary>
        static TipLinks? ReadLinks(MyIni ini, string section, string where)
        {
            string links;
            if (!ini.Get(section, Links).TryGetString(out links))
                return null;

            if (links == Linking(World.TipLinks.Replace))
                return World.TipLinks.Replace;

            if (links == Linking(World.TipLinks.Beside))
                return World.TipLinks.Beside;

            GameLog.Say($"{where} gives {Links} as {links}, which is not {Linking(World.TipLinks.Replace)} or {Linking(World.TipLinks.Beside)}; it is left to the level below");
            return null;
        }

        /// <summary>How <paramref name="links" /> is written in a settings file.</summary>
        /// <remarks>Persisted.</remarks>
        static string Linking(TipLinks links)
        {
            return links == World.TipLinks.Beside ? "beside" : "replace";
        }

        /// <summary>How <paramref name="keeping" /> is written in a settings file.</summary>
        /// <remarks>Persisted.</remarks>
        static string Keeping(TipKeeping keeping)
        {
            return keeping == World.TipKeeping.Alt ? "alt" : "stay";
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

            if (TipKeeping.HasValue)
                ini.Set(section, Tips, Keeping(TipKeeping.Value));
            else
                ini.Delete(section, Tips);

            if (TipLinks.HasValue)
                ini.Set(section, Links, Linking(TipLinks.Value));
            else
                ini.Delete(section, Links);
        }
    }
}
