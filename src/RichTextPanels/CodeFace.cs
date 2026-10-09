using System.Collections.Generic;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace RichTextPanels
{
    /// <summary>Which face code is drawn in: Maltek Mono where the world has that font mod, and the game's own monospace face otherwise.</summary>
    public static class CodeFace
    {
        /// <summary>The Maltek Mono font mod's face, which the app prefers and does not require.</summary>
        public const string Maltek = "MaltekMono";

        /// <summary>The game's own monospace face, which every world has.</summary>
        public const string Game = "Monospace";

        /// <summary>The face code is drawn in on <paramref name="surface" />, as the fonts it offers say.</summary>
        /// <remarks>Asked once for each panel, as its app starts; the fonts a world offers stay the same while it runs.</remarks>
        public static string Of(IMyTextSurface surface)
        {
            if (surface == null)
                return Game;

            var fonts = new List<string>();
            surface.GetFonts(fonts);
            return fonts.Contains(Maltek) ? Maltek : Game;
        }
    }
}
