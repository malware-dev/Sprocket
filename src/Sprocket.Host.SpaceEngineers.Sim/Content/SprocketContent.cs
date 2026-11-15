using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Lumel.Sim;
using Point = System.Windows.Point;

namespace Sprocket.Sim.Content
{
    /// <summary>What Sprocket's screens draw from in the harness: the game's content, Sprocket's own, and the font mods it requires.</summary>
    public static class SprocketContent
    {
        /// <summary>
        ///     The content every frame in the process draws from, read once: it is the same files for every frame, and reading
        ///     the block definitions again for each screen would cost seconds a test.
        /// </summary>
        public static readonly SimContent Instance = Make();

        /// <summary>The folder Sprocket's own content is copied to beside the harness.</summary>
        public static string Mods => Path.Combine(AppContext.BaseDirectory, "Content", "Mods");

        /// <summary>The folder the font mod <paramref name="font" /> declares its pages and tables in.</summary>
        public static string FontFolder(string font)
        {
            foreach (var declared in Instance.Fonts)
            {
                if (declared.Name == font)
                    return declared.Folder;
            }

            return null;
        }

        static SimContent Make()
        {
            return new SimContent()
                .Mod(Path.Combine(Mods, "Sprocket"))
                .Mod(WorkshopMods.Required(WorkshopMods.Symbols, "Maltek Symbols"))
                .Mod(WorkshopMods.Required(WorkshopMods.Mono, "Maltek Mono"))
                .Mod(WorkshopMods.Required(WorkshopMods.FontStyles, "Space Engineers Font Styles"))
                .Draws("Sprocket_Cursor", (context, box, color) => Cursor(context, box, new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))))
                .Draws("Sprocket_CursorHand", (context, box, _) => Cursor(context, box, Brushes.Gold))
                .Pictures(Picture)
                .Images(images => new MarkImages(images));
        }

        /// <summary>
        ///     Where the picture of a block named by a host with no blocks to ask is, <see cref="Icons.Prefix" /> and its
        ///     kind, or of a made-up block the game's definitions do not have; null for any other material.
        /// </summary>
        static string Picture(string material)
        {
            if (material.StartsWith(Icons.Prefix, StringComparison.Ordinal))
                return BlockArt.PathFor(material.Substring(Icons.Prefix.Length));

            return material.IndexOf('/') >= 0 ? BlockArt.PathFor(material) : null;
        }

        /// <summary>
        ///     The pointer, drawn as geometry rather than as the game's texture, which keeps the shape and the hotspot right:
        ///     the real one lives in the game's content and is named by no definition.
        /// </summary>
        static void Cursor(DrawingContext context, Rect destination, Brush body)
        {
            double size = Math.Min(destination.Width, destination.Height);
            double x = destination.X;
            double y = destination.Y;

            var geometry = new StreamGeometry();
            using (StreamGeometryContext figure = geometry.Open())
            {
                figure.BeginFigure(new Point(x, y), true, true);
                figure.LineTo(new Point(x, y + size * 0.78), true, false);
                figure.LineTo(new Point(x + size * 0.22, y + size * 0.58), true, false);
                figure.LineTo(new Point(x + size * 0.38, y + size * 0.92), true, false);
                figure.LineTo(new Point(x + size * 0.52, y + size * 0.85), true, false);
                figure.LineTo(new Point(x + size * 0.36, y + size * 0.52), true, false);
                figure.LineTo(new Point(x + size * 0.62, y + size * 0.50), true, false);
            }

            context.DrawGeometry(body, new Pen(Brushes.Black, 1d), geometry);
        }
    }
}
