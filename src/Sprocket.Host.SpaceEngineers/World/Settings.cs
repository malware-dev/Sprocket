using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What Sprocket is asked to do differently, at two levels: every world, and this world, which wins.
    /// </summary>
    /// <remarks>
    /// Read once, as a world comes up, on the machine that runs it. A client in multiplayer reads no file of its own, and takes
    /// the server's settings.
    /// </remarks>
    public sealed class Settings
    {
        const string File = "sprocket.ini";

        // Persisted: the section each file keeps its settings in.
        const string Section = "Sprocket";

        // The sections the text a client is sent keeps each level in.
        const string Everywhere = "Every world";

        const string Here = "This world";

        /// <summary>The settings at the two levels.</summary>
        public Settings(SettingsLayer global, SettingsLayer world)
        {
            Global = global ?? SettingsLayer.Empty;
            World = world ?? SettingsLayer.Empty;
        }

        /// <summary>
        /// Said outright, as every world's, for a host that has its own way of being told.
        /// </summary>
        /// <remarks>For the harness, which has no mod storage to read.</remarks>
        public Settings(bool developerMode, int fastestUpdate = 1)
            : this(new SettingsLayer(developerMode, fastestUpdate), SettingsLayer.Empty)
        {
        }

        /// <summary>What every world says.</summary>
        public SettingsLayer Global { get; }

        /// <summary>What this world says, which wins.</summary>
        public SettingsLayer World { get; }

        /// <summary>Tools that are not part of the product, off unless somebody asked for them.</summary>
        public bool DeveloperMode => World.DeveloperMode ?? DefaultDeveloperMode;

        /// <summary>The fewest ticks between two runs of anything on a repeat: a timer, or a look at a block for a change nothing announces.</summary>
        /// <remarks>One of 1, 10 and 100. What a server that cannot afford something done on every tick sets, and every program on it is held to.</remarks>
        public int FastestUpdate => World.FastestUpdate ?? DefaultFastestUpdate;

        /// <summary>Whether every run writes what it does to the game's log, for finding out why one went wrong; off unless somebody asked for it.</summary>
        public bool DebugLog => World.DebugLog ?? DefaultDebugLog;

        /// <summary>Whether developer mode is on in a world that leaves it: what every world says, or off.</summary>
        public bool DefaultDeveloperMode => Global.DeveloperMode ?? false;

        /// <summary>Whether the debug log is on in a world that leaves it: what every world says, or off.</summary>
        public bool DefaultDebugLog => Global.DebugLog ?? false;

        /// <summary>The fastest update of a world that leaves it: what every world says, or every tick.</summary>
        public int DefaultFastestUpdate => Global.FastestUpdate ?? 1;

        /// <summary>These settings with every world saying what <paramref name="global" /> says.</summary>
        public Settings WithGlobal(SettingsLayer global)
        {
            return new Settings(global, World);
        }

        /// <summary>These settings with this world saying what <paramref name="world" /> says.</summary>
        public Settings WithWorld(SettingsLayer world)
        {
            return new Settings(Global, world);
        }

        /// <summary>Both files, merged, with nothing said anywhere meaning every default.</summary>
        public static Settings Read()
        {
            var asked = From(Text(true), Text(false));

            if (asked.DeveloperMode)
                GameLog.Say("developer mode is on");

            if (asked.DebugLog)
                GameLog.Say("the debug log is on");

            return asked;
        }

        /// <summary>What the two files say, whatever they happen to contain.</summary>
        public static Settings From(string global, string world)
        {
            return new Settings(
                SettingsLayer.Read(Parse(global, "global"), Section, $"the global {File}"),
                SettingsLayer.Read(Parse(world, "world"), Section, $"the world {File}"));
        }

        /// <summary>The settings a server sent, as <see cref="Written" /> wrote them; every default where they will not read.</summary>
        public static Settings Of(string written)
        {
            var read = Parse(written, "sent");
            return new Settings(SettingsLayer.Read(read, Everywhere, "the server's settings"), SettingsLayer.Read(read, Here, "the server's settings"));
        }

        /// <summary>Both levels as one text, the way the server hands them to a client.</summary>
        public string Written()
        {
            var write = new MyIni();
            write.AddSection(Everywhere);
            Global.Write(write, Everywhere);
            write.AddSection(Here);
            World.Write(write, Here);
            return write.ToString();
        }

        /// <summary>Keeps each level that differs from <paramref name="was" /> in its own file, leaving the rest of the file as it was.</summary>
        /// <remarks>On the machine that runs the world, as an admin changes them.</remarks>
        public void Keep(Settings was)
        {
            if (was == null || !Global.Same(was.Global))
                Keep(Global, true);

            if (was == null || !World.Same(was.World))
                Keep(World, false);
        }

        /// <summary>Writes <paramref name="layer" /> into the global file where <paramref name="everywhere" /> says, and the world's otherwise.</summary>
        static void Keep(SettingsLayer layer, bool everywhere)
        {
            IMyUtilities files = MyAPIGateway.Utilities;
            if (files == null)
                return;

            var write = Parse(Text(everywhere), Where(everywhere)) ?? new MyIni();
            layer.Write(write, Section);
            Write(write, everywhere);
        }

        /// <summary>The global file as it stands, read, or null where there is none or it will not read.</summary>
        /// <remarks>On the machine it is asked on, a client's own included: the file holds the player's own section as well as every world's settings.</remarks>
        public static MyIni GlobalFile()
        {
            return Parse(Text(true), Where(true));
        }

        /// <summary>Writes <paramref name="ini" /> as the global file, on the machine it is asked on.</summary>
        public static void KeepGlobal(MyIni ini)
        {
            Write(ini, true);
        }

        /// <summary>Writes <paramref name="ini" /> into the global file where <paramref name="everywhere" /> says, and the world's otherwise.</summary>
        static void Write(MyIni ini, bool everywhere)
        {
            IMyUtilities files = MyAPIGateway.Utilities;
            if (files == null)
                return;

            try
            {
                using (TextWriter writer = everywhere
                           ? files.WriteFileInGlobalStorage(File)
                           : files.WriteFileInWorldStorage(File, typeof(Settings)))
                    writer.Write(ini.ToString());
            }
            catch (Exception trouble)
            {
                GameLog.Say($"the {Where(everywhere)} {File} will not write - {trouble.Message}");
            }
        }

        /// <summary>One file as it stands, or null where there is none or it will not open.</summary>
        static string Text(bool everywhere)
        {
            IMyUtilities files = MyAPIGateway.Utilities;
            if (files == null)
                return null;

            try
            {
                if (everywhere)
                {
                    if (!files.FileExistsInGlobalStorage(File))
                        return null;

                    using (TextReader reader = files.ReadFileInGlobalStorage(File))
                        return reader.ReadToEnd();
                }

                if (!files.FileExistsInWorldStorage(File, typeof(Settings)))
                    return null;

                using (TextReader reader = files.ReadFileInWorldStorage(File, typeof(Settings)))
                    return reader.ReadToEnd();
            }
            catch (Exception trouble)
            {
                GameLog.Say($"the {Where(everywhere)} {File} will not open - {trouble.Message}");
                return null;
            }
        }

        /// <summary>
        /// One text read, or null where it is empty or malformed.
        /// </summary>
        static MyIni Parse(string text, string which)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var read = new MyIni();

            MyIniParseResult trouble;
            if (read.TryParse(text, out trouble))
                return read;

            GameLog.Say($"the {which} {File} has a problem on line {trouble.LineNo} - {trouble.Error}");
            return null;
        }

        static string Where(bool everywhere)
        {
            return everywhere ? "global" : "world";
        }
    }
}
