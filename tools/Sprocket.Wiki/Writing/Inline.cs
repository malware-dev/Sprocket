using System.Globalization;
using System.Text;
using Lumel.Text;
using Sprocket.Wiki.Pictures;
using Sprocket.Wiki.Reading;

namespace Sprocket.Wiki.Writing;

/// <summary>Writes the words of one block as GitHub reads them, so they show what the codex shows.</summary>
/// <remarks>
/// Strong words and emphasis are written as <c>&lt;strong&gt;</c> and <c>&lt;em&gt;</c>, never as stars: whether a star opens or closes
/// depends on the characters either side of it, which change as the words around it are escaped or become tags and pictures, where
/// a tag means the same wherever it stands. Every character GitHub would read as markup is escaped.
/// </remarks>
public sealed class Inline
{
    /// <summary>How tall a mark is drawn, in pixels.</summary>
    /// <remarks>
    /// Three quarters of GitHub's line, as the game draws a mark at three quarters of its own: GitHub sets its text at 16 pixels
    /// with a line of 1.5 times that.
    /// </remarks>
    const int MarkHeight = 18;

    readonly Codex _codex;
    readonly Catalog _catalog;

    /// <summary>Words whose links lead into <paramref name="codex" /> and whose pictures are found in <paramref name="catalog" />.</summary>
    public Inline(Codex codex, Catalog catalog)
    {
        _codex = codex;
        _catalog = catalog;
    }

    /// <summary><paramref name="text" /> as words GitHub shows exactly as written.</summary>
    /// <remarks>A character that would begin a heading, a list or a quote is escaped only where it starts the text.</remarks>
    public static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length + 8);
        Escape(text, escaped, false);
        return escaped.ToString();
    }

    /// <summary>The GitHub markdown for <paramref name="runs" />, each line break written as <paramref name="lineBreak" />; in a table's cell where <paramref name="cell" /> says so.</summary>
    /// <exception cref="InvalidDataException">A link leads to no term, or a picture is none of Sprocket's own.</exception>
    public string Write(IList<Run> runs, string lineBreak, bool cell)
    {
        var written = new StringBuilder();
        var strong = false;
        var emphasis = false;
        Link link = null;

        foreach (var run in runs)
        {
            var linked = (run as LinkRun)?.Link;
            if (linked != link)
            {
                Style(written, ref strong, ref emphasis, false, false);
                if (link != null)
                    Close(written, link, cell);

                if (linked != null)
                    written.Append('[');

                link = linked;
            }

            if (run.IsBreak)
            {
                written.Append(lineBreak);
                continue;
            }

            Style(written, ref strong, ref emphasis, (run.Kind & RunKind.Strong) != 0, (run.Kind & RunKind.Emphasis) != 0);

            if (run is ImageRun image)
                Picture(written, image);
            else if ((run.Kind & RunKind.Code) != 0)
                Code(written, run.Text, cell);
            else
                Escape(run.Text, written, cell);
        }

        Style(written, ref strong, ref emphasis, false, false);
        if (link != null)
            Close(written, link, cell);

        return written.ToString();
    }

    /// <summary>Closes and opens <c>&lt;strong&gt;</c> and <c>&lt;em&gt;</c> so the words written next are as strong and as emphasized as asked, emphasis always inside strong.</summary>
    static void Style(StringBuilder written, ref bool strong, ref bool emphasis, bool strongNext, bool emphasisNext)
    {
        if (emphasis && (!emphasisNext || strong != strongNext))
        {
            written.Append("</em>");
            emphasis = false;
        }

        if (strong && !strongNext)
        {
            written.Append("</strong>");
            strong = false;
        }

        if (!strong && strongNext)
        {
            written.Append("<strong>");
            strong = true;
        }

        if (!emphasis && emphasisNext)
        {
            written.Append("<em>");
            emphasis = true;
        }
    }

    /// <summary>Ends the words of <paramref name="link" />, with the plain words of its term's tooltip as its title, which GitHub shows where the link is rested on.</summary>
    void Close(StringBuilder written, Link link, bool cell)
    {
        var term = _codex.Find(link.Target) ?? throw new InvalidDataException("a link leads to " + link.Target + ", which is no term of the codex");
        var title = Plain.Of(term.Tooltip).Replace("\\", "\\\\").Replace("\"", "\\\"");
        if (cell)
            title = title.Replace("|", "\\|");

        written.Append("](").Append(link.Target).Append(" \"").Append(title).Append("\")");
    }

    /// <summary>
    /// Writes <paramref name="image" />'s picture: a mark as tall as three quarters of the line, in the ink of the theme it is shown in,
    /// a picture given a size at that size, and any other at its own.
    /// </summary>
    void Picture(StringBuilder written, ImageRun image)
    {
        var picture = image.Picture;
        if (picture.Rotation != 0f)
            throw new InvalidDataException("the picture " + picture.Locator + " is turned, which the wiki cannot show");

        var found = _catalog.Find(picture.Locator);
        if (!found.Mark && float.IsNaN(picture.Width) && float.IsNaN(picture.Height))
        {
            written.Append("![");
            Escape(image.Text, written, false);
            written.Append("](").Append(found.Path).Append(')');
            return;
        }

        if (found.Mark)
            written.Append("<picture><source media=\"(prefers-color-scheme: dark)\" srcset=\"").Append(found.Dark).Append("\">");

        written.Append("<img src=\"").Append(found.Path).Append("\" alt=\"").Append(Attribute(image.Text)).Append('"');
        if (found.Mark)
            written.Append(" height=\"").Append(MarkHeight).Append("\"></picture>");
        else
        {
            Side(written, "width", picture.Width);
            Side(written, "height", picture.Height);
            written.Append('>');
        }
    }

    static void Side(StringBuilder written, string name, float size)
    {
        if (!float.IsNaN(size))
            written.Append(' ').Append(name).Append("=\"").Append(Math.Round(size).ToString(CultureInfo.InvariantCulture)).Append('"');
    }

    /// <summary>Writes <paramref name="code" /> as a code span, fenced by one backtick more than the longest run inside it.</summary>
    static void Code(StringBuilder written, string code, bool cell)
    {
        var longest = 0;
        for (var i = 0; i < code.Length;)
        {
            var run = 0;
            while (i + run < code.Length && code[i + run] == '`')
                run++;

            longest = Math.Max(longest, run);
            i += Math.Max(run, 1);
        }

        var fence = new string('`', longest + 1);
        var padded = code.StartsWith('`') || code.EndsWith('`') || code.Length > 1 && code[0] == ' ' && code[^1] == ' ';
        var pad = padded ? " " : "";
        written.Append(fence).Append(pad).Append(cell ? code.Replace("|", "\\|") : code).Append(pad).Append(fence);
    }

    /// <summary>Writes <paramref name="text" /> so GitHub reads every character of it as itself.</summary>
    /// <remarks>
    /// GitHub reads a line by what it starts with, so a character that would begin a heading, a list, a quote or a heading's underline
    /// is escaped where it starts a line of what has been written; a table's cell escapes its pipes as well.
    /// </remarks>
    static void Escape(string text, StringBuilder written, bool cell)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var starts = written.Length == 0 || written[^1] == '\n';

            switch (c)
            {
                case '\\' or '`' or '*' or '_' or '[' or ']' or '~':
                    written.Append('\\').Append(c);
                    continue;
                case '|':
                    written.Append(cell ? "\\|" : "&#124;");
                    continue;
                case '&':
                    written.Append("&amp;");
                    continue;
                case '<':
                    written.Append("&lt;");
                    continue;
                case '>':
                    written.Append("&gt;");
                    continue;
                case ' ':
                    written.Append("&nbsp;");
                    continue;
            }

            // A run's last character can stand before a link's bracket, where a ! would make GitHub read the link as a picture.
            if (starts && (c == '#' || c == '-' || c == '+' || c == '=') || c == '!' && i == text.Length - 1)
            {
                written.Append('\\').Append(c);
                continue;
            }

            if (starts && char.IsAsciiDigit(c))
            {
                var end = i;
                while (end < text.Length && char.IsAsciiDigit(text[end]))
                    end++;

                written.Append(text, i, end - i);
                if (end < text.Length && (text[end] == '.' || text[end] == ')'))
                {
                    written.Append('\\').Append(text[end]);
                    end++;
                }

                i = end - 1;
                continue;
            }

            written.Append(c);
        }
    }

    /// <summary><paramref name="text" /> as the value of an HTML attribute.</summary>
    static string Attribute(string text)
    {
        return text.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
