using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What somebody has asked Sprocket to do differently.
    /// </summary>
    /// <remarks>
    /// Read from mod storage twice: the global store, which is the same for every world a player opens,
    /// and the world's own. A setting present in both takes the world's value, so the global store is
    /// where somebody puts what they always want and the world's is where they say "not in this one".
    /// <para>
    /// <b>Ini rather than the XML the definitions use.</b> Absent and off are different things here -
    /// a world saying nothing has to leave what the global store said standing - and a deserialised
    /// <c>bool</c> cannot tell them apart. <c>MyIniValue.TryGetBoolean</c> can.
    /// </para>
    /// <para>
    /// <b>Once, as a world comes up.</b> Two files opened, and nothing here is read again.
    /// </para>
    /// </remarks>
    public sealed class Settings
    {
        /// <summary>
        /// The file, in both stores, and the one section inside it.
        /// </summary>
        /// <remarks>
        /// Global storage is shared with every other mod, so the name has to be ours.
        /// </remarks>
        const string File = "Sprocket.ini";

        const string Section = "Sprocket";

        const string Developer = "developerMode";

        /// <summary>
        /// Said outright, for a host that has its own way of being told.
        /// </summary>
        /// <remarks>
        /// The harness has no mod storage to read and wants to drive both answers anyway, so it says
        /// what it wants rather than going through <see cref="Read" />.
        /// </remarks>
        public Settings(bool developerMode)
        {
            DeveloperMode = developerMode;
        }

        /// <summary>Tools that are not part of the product, off unless somebody asked for them.</summary>
        public bool DeveloperMode { get; }

        /// <summary>Both stores, merged, with nothing said anywhere meaning every default.</summary>
        public static Settings Read()
        {
            Settings asked = From(Text(true), Text(false));

            Offer();

            // Only when it is on. A line every world load saying everything is as it ships is the
            // noise that gets a log stopped being read, and on is the state worth being reminded of.
            if (asked.DeveloperMode)
                Told.Say("developer mode is on");

            return asked;
        }

        /// <summary>What the two stores say between them, whatever the two files happen to contain.</summary>
        internal static Settings From(string global, string world)
        {
            MyIni everywhere = Parse(global, "global");
            MyIni here = Parse(world, "world");

            return new Settings(Said(everywhere, here, Developer));
        }

        /// <summary>The world's answer where it has one, then the global one, then off.</summary>
        static bool Said(MyIni global, MyIni world, string name)
        {
            bool said;
            if (world != null && world.Get(Section, name).TryGetBoolean(out said))
                return said;

            if (global != null && global.Get(Section, name).TryGetBoolean(out said))
                return said;

            return false;
        }

        /// <summary>One store's file as it stands, or null where there is none or it will not open.</summary>
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
                Told.Say("the " + Where(everywhere) + " " + File + " will not open - " + trouble.Message);
                return null;
            }
        }

        /// <summary>
        /// One store's file read, or null where it is empty or malformed.
        /// </summary>
        /// <remarks>
        /// A file somebody typed is not to be trusted to be a file. One that will not parse is said out
        /// loud, with the line, and then ignored: losing what one store said is a poor outcome and
        /// refusing to load a world over a stray bracket is a worse one.
        /// </remarks>
        static MyIni Parse(string text, string which)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var read = new MyIni();

            MyIniParseResult trouble;
            if (read.TryParse(text, out trouble))
                return read;

            Told.Say("the " + which + " " + File + " has a problem on line " + trouble.LineNo + " - " + trouble.Error);
            return null;
        }

        /// <summary>
        /// Writes the global file where there is none, so there is something to edit.
        /// </summary>
        /// <remarks>
        /// A setting nobody can find is a setting nobody has. This creates and never overwrites, so
        /// whatever somebody has typed stands - including a file they emptied on purpose.
        /// </remarks>
        static void Offer()
        {
            IMyUtilities files = MyAPIGateway.Utilities;
            if (files == null || files.FileExistsInGlobalStorage(File))
                return;

            var write = new MyIni();
            write.AddSection(Section);
            write.SetSectionComment(Section, " Sprocket. The same for every world; each world may override it.");
            write.Set(Section, Developer, false);
            write.SetComment(Section, Developer, " Tools for working on Sprocket itself, rather than for playing with it.");

            try
            {
                using (TextWriter writer = files.WriteFileInGlobalStorage(File))
                    writer.Write(write.ToString());
            }
            catch (Exception trouble)
            {
                Told.Say("the global " + File + " will not write - " + trouble.Message);
            }
        }

        static string Where(bool everywhere)
        {
            return everywhere ? "global" : "world";
        }
    }
}
