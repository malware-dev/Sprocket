using System;
using System.Collections.Generic;
using System.IO;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Sim.Content
{
    /// <summary>The faces the mod draws in that come from its own font mods rather than the game, and where they are copied beside the harness.</summary>
    public static class ModFonts
    {
        /// <summary>Every face the mod's styles name that a font mod declares.</summary>
        public static readonly IReadOnlyList<string> All = new[]
        {
            Symbols.Font, TextSizes.Mono,
            TextSizes.Bold, TextSizes.Italic, TextSizes.BoldItalic
        };

        /// <summary>The folder the font mods' pages and tables are copied to.</summary>
        public static string Folder => Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Mod");
    }
}
