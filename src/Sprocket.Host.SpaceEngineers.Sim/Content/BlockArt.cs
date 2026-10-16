using System.Collections.Generic;

namespace Sprocket.Sim.Content
{
    /// <summary>
    /// Which of the game's block pictures stands in for each kind of block on the rig.
    /// </summary>
    /// <remarks>
    /// A stand-in for something the mod will not need. A real block's definition already says which icon
    /// it has, so in the game the picture is looked up on the block itself; here the rig makes its blocks
    /// up and something has to decide that a door looks like a door.
    /// <para>
    /// Keyed by type id, which is what the mod names a kind by - <c>MyObjectBuilder_Door</c> - and not by
    /// subtype: every door shares a picture here, which is close enough for a harness and wrong in the
    /// game, where a hangar door has its own.
    /// </para>
    /// <para>
    /// The paths were taken from the block definitions rather than guessed from the file listing, which
    /// matters more than it sounds: the folder holds a dozen plausible doors, and only one of them is
    /// what the game actually draws for the block called Door.
    /// </para>
    /// </remarks>
    public static class BlockArt
    {
        const string Cubes = "Textures/GUI/Icons/Cubes/";

        static readonly Dictionary<string, string> Art = new Dictionary<string, string>
        {
            { "MyObjectBuilder_Door", Cubes + "door.dds" },
            { "MyObjectBuilder_AirtightHangarDoor", Cubes + "HangarDoor.dds" },
            { "MyObjectBuilder_InteriorLight", Cubes + "interior_light.dds" },
            { "MyObjectBuilder_Beacon", Cubes + "beacon.dds" },
            { "MyObjectBuilder_SensorBlock", Cubes + "Sensor.dds" },
            { "MyObjectBuilder_ButtonPanel", Cubes + "ButtonPanel.dds" },
            { "MyObjectBuilder_Cockpit", Cubes + "CockpitLarge.dds" },
            { "MyObjectBuilder_OxygenTank", Cubes + "OxygenTank.dds" },
            { "MyObjectBuilder_AirVent", Cubes + "AirVent.dds" },
            { "MyObjectBuilder_MyProgrammableBlock", Cubes + "ProgrammableBlock.dds" }
        };

        /// <summary>Every picture this knows about, so the extractor can ask for them all.</summary>
        public static IEnumerable<KeyValuePair<string, string>> All => Art;

        /// <summary>
        /// Where the picture for a kind lives in the game's content, or null for one with no picture.
        /// </summary>
        /// <remarks>
        /// A kind is <c>Type/Subtype</c>, as the mod builds it from the block's definition, and the
        /// subtype is dropped here - see the note on the class.
        /// </remarks>
        public static string PathFor(string kind)
        {
            if (string.IsNullOrEmpty(kind))
                return null;

            int slash = kind.IndexOf('/');
            string type = slash < 0 ? kind : kind.Substring(0, slash);

            string path;
            return Art.TryGetValue(type, out path) ? path : null;
        }
    }
}
