using System.Collections.Generic;

namespace Reactor.Sim.Content
{
    /// <summary>
    /// Which file in the game stands behind each of Phosphor's symbol materials.
    /// </summary>
    /// <remarks>
    /// The same table the mod keeps in <c>TransparentMaterials.sbc</c>, written twice because the two
    /// targets reach a texture by different means and neither can read the other's list. <b>They have to
    /// agree</b>: a symbol that resolves here and not in the game is a symbol nobody sees until the mod
    /// runs, which is the one place there is no way to look.
    /// <para>
    /// Every entry is white or near enough, and that is a requirement rather than a coincidence. A tint is
    /// a multiply on both targets, so it can darken a picture and never brighten one - which rules out the
    /// game's plain artwork, all of which is already the steel blue it is drawn in. The <c>_highlight</c>
    /// variants are the white ones.
    /// </para>
    /// </remarks>
    public static class SymbolArt
    {
        const string Controls = "Textures/GUI/Controls/";
        const string Sprites = "Textures/Sprites/";

        static readonly Dictionary<string, string> Art = new Dictionary<string, string>
        {
            { "Phosphor_Chevron", Controls + "button_arrow_menu_left_highlight.dds" },
            { "Phosphor_Cross", Controls + "button_close_symbol_highlight.dds" },
            { "Phosphor_Disc", Sprites + "Circle.dds" },
            { "Phosphor_Ring", Sprites + "Circle_Hollow.dds" }
        };

        /// <summary>Every symbol, so the extractor can ask for them all.</summary>
        public static IEnumerable<KeyValuePair<string, string>> All
        {
            get { return Art; }
        }

        /// <summary>
        /// Where <paramref name="material" /> lives in the game's content, or null if it is not a symbol.
        /// </summary>
        public static string PathFor(string material)
        {
            if (string.IsNullOrEmpty(material))
                return null;

            string path;
            return Art.TryGetValue(material, out path) ? path : null;
        }
    }
}
