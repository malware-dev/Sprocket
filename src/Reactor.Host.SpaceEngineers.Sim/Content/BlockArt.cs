
namespace Reactor.Sim.Content
{
    /// <summary>
    /// Which of the game's block pictures stands in for each kind of thing on the made-up ship.
    /// </summary>
    /// <remarks>
    /// A stand-in for something the mod will not need. In the game a picker lists real blocks, and a real
    /// block's definition already says which icon it has - so the mod looks the picture up on the thing
    /// itself and no table like this exists. Here the ship is invented, its kinds are words core made up,
    /// and something has to decide that a "door" looks like a door.
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
            { "door", Cubes + "door.dds" },
            { "light", Cubes + "interior_light.dds" },
            { "sensor", Cubes + "Sensor.dds" },
            { "button", Cubes + "ButtonPanel.dds" },
            { "tank", Cubes + "OxygenTank.dds" },
            { "vent", Cubes + "AirVent.dds" }
        };

        /// <summary>Every kind this knows a picture for, so the extractor can ask for them all.</summary>
        public static IEnumerable<KeyValuePair<string, string>> All => Art;

        /// <summary>
        /// Where <paramref name="name" /> lives in the game's content, or null for a kind with no picture.
        /// </summary>
        public static string PathFor(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            string path;
            return Art.TryGetValue(name, out path) ? path : null;
        }
    }
}
