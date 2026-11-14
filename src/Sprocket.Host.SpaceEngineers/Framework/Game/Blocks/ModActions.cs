using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// What the mods in this world say about their own actions.
    /// </summary>
    /// <remarks>Read once, as a world comes up: it opens a file per mod that has one.</remarks>
    public static class ModActions
    {
        const string File = "Data/SprocketDefinitions.xml";

        /// <summary>Reads every loaded mod's descriptions file into <paramref name="into" />, in the order the game loads them.</summary>
        public static void Read(ParameterCatalog into)
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

                GameLog.Say(named, $"described {said.Actions.Count} actions");
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
                GameLog.Say(named, $"has a {File} that will not read - {trouble.Message}");
                return null;
            }
        }
    }
}
