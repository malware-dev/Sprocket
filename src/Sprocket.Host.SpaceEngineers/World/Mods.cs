using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What the mods in this world say about their own actions.
    /// </summary>
    /// <remarks>
    /// A sweep rather than a handshake: every loaded mod is asked whether it ships the file, and
    /// the ones that do are read. Nothing registers, nothing has to be present before anything
    /// else, and a mod that ships the file works in a world without Sprocket - a file nobody reads
    /// is inert.
    /// <para>
    /// In the order the game loads them, so the last word wins, which is the rule the mods
    /// themselves follow. Sprocket's own goes in first and is the one anybody may override.
    /// </para>
    /// <para>
    /// <b>Once, as a world comes up.</b> It opens a file per mod that has one, which is not
    /// something to do twice, let alone on a tick.
    /// </para>
    /// </remarks>
    public static class Mods
    {
        const string File = "Data/SprocketDefinitions.xml";

        public static void Read(Catalogue into)
        {
            if (into == null || MyAPIGateway.Session == null || MyAPIGateway.Utilities == null)
                return;

            List<MyObjectBuilder_Checkpoint.ModItem> mods = MyAPIGateway.Session.Mods;
            if (mods == null)
                return;

            foreach (var mod in mods)
            {
                var named = Named(mod);
                Descriptions said = Said(mod, named);
                if (said == null)
                    continue;

                Told.Say(named, $"described {said.Actions.Count} actions");
                into.Add(said, named);
            }
        }

        /// <summary>What to call a mod in the log, which is whatever it calls itself.</summary>
        static string Named(MyObjectBuilder_Checkpoint.ModItem mod)
        {
            return string.IsNullOrEmpty(mod.FriendlyName) ? mod.Name ?? "" : mod.FriendlyName;
        }

        /// <summary>
        /// One mod's file, or null where it has none or it will not read.
        /// </summary>
        /// <remarks>
        /// A file written by somebody else is not to be trusted to be a file: one that will not
        /// deserialise is skipped and the rest of the world goes on. Losing what one mod says about
        /// its actions is a poor outcome; refusing to load a world over it is a worse one.
        /// </remarks>
        static Descriptions Said(MyObjectBuilder_Checkpoint.ModItem mod, string named)
        {
            if (!MyAPIGateway.Utilities.FileExistsInModLocation(File, mod))
                return null;

            try
            {
                using (TextReader reader = MyAPIGateway.Utilities.ReadFileInModLocation(File, mod))
                    return MyAPIGateway.Utilities.SerializeFromXML<Descriptions>(reader.ReadToEnd());
            }
            catch (System.Exception trouble)
            {
                Told.Say(named, $"has a {File} that will not read - {trouble.Message}");
                return null;
            }
        }
    }
}
