using System.Xml.Linq;
using SkiaSharp;

namespace Sprocket.Wiki.Pictures;

/// <summary>The Maltek Symbols font's glyph page, read as the game reads it, from which each glyph can be cut as a picture of its own.</summary>
/// <remarks>
/// The page is what <c>art/symbols/build_font.py</c> draws, beside the XNA bitmap-font description that says where each glyph
/// stands on it. Only the page's alpha is read: the glyphs are drawn in one ink, and the wiki's is given here.
/// </remarks>
public sealed class Glyphs
{
    static readonly XNamespace Font = "http://xna.microsoft.com/bitmapfont";

    readonly SKBitmap _page;
    readonly Dictionary<char, SKRectI> _cells = new();

    /// <summary>The glyphs <paramref name="description" /> places on the page <paramref name="page" />.</summary>
    public Glyphs(string description, string page)
    {
        foreach (var glyph in XDocument.Load(description).Descendants(Font + "glyph"))
        {
            var origin = ((string)glyph.Attribute("origin")).Split(',');
            var size = ((string)glyph.Attribute("size")).Split('x');
            var x = int.Parse(origin[0]);
            var y = int.Parse(origin[1]);
            _cells[((string)glyph.Attribute("ch"))[0]] = new SKRectI(x, y, x + int.Parse(size[0]), y + int.Parse(size[1]));
        }

        _page = SKBitmap.Decode(page) ?? throw new InvalidDataException(page + " is no picture");
    }

    /// <summary>The glyph for <paramref name="symbol" />, its whole cell, in <paramref name="ink" />, as a PNG.</summary>
    public byte[] Cut(char symbol, SKColor ink)
    {
        if (!_cells.TryGetValue(symbol, out var cell))
            throw new InvalidDataException("the symbol font has no glyph for '" + symbol + "'");

        using var glyph = new SKBitmap(new SKImageInfo(cell.Width, cell.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        for (var y = 0; y < cell.Height; y++)
        {
            for (var x = 0; x < cell.Width; x++)
            {
                var alpha = _page.GetPixel(cell.Left + x, cell.Top + y).Alpha;
                glyph.SetPixel(x, y, ink.WithAlpha((byte)(alpha * ink.Alpha / 255)));
            }
        }

        using var image = SKImage.FromBitmap(glyph);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }
}
