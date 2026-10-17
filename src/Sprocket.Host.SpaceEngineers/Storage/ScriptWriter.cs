using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Writes script text: statements, blocks, lists and annotations.
    /// </summary>
    public sealed class ScriptWriter
    {
        const string Indent = "    ";

        readonly StringBuilder _text = new StringBuilder();
        int _depth;

        int _lists;

        bool _first;
        bool _fresh = true;

        bool _arguments;

        bool _firstArgument;

        /// <summary>
        /// A blank line, unless one would be the first thing inside a block or in the file.
        /// </summary>
        public void Blank()
        {
            if (!_fresh)
                _text.Append('\n');
        }

        /// <summary>
        /// Text somebody typed, above the statement it belongs to. Nothing when it is empty.
        /// </summary>
        /// <remarks>Persisted: one hash line per line, a bare hash for an empty one; empty first and last lines are dropped.</remarks>
        public void Note(string annotation)
        {
            if (string.IsNullOrEmpty(annotation))
                return;

            var from = 0;
            var end = annotation.Length;

            while (from < end && (annotation[from] == '\n' || annotation[from] == '\r'))
                from++;

            while (end > from && (annotation[end - 1] == '\n' || annotation[end - 1] == '\r'))
                end--;

            while (from <= end)
            {
                var to = annotation.IndexOf('\n', from);
                if (to < 0 || to > end)
                    to = end;

                // A carriage return left by splitting on newlines alone is never written, since it would not read back.
                var stop = to > from && annotation[to - 1] == '\r' ? to - 1 : to;

                Pad();
                _text.Append('#');
                if (stop > from)
                    _text.Append(' ').Append(annotation, from, stop - from);

                _text.Append('\n');
                from = to + 1;
            }

            _fresh = false;
        }

        /// <summary>Starts a statement with one of the format's own words.</summary>
        public ScriptWriter Start(Keyword keyword)
        {
            return Start(keyword, false);
        }

        /// <summary>Starts a statement with <paramref name="keyword" />, after <c>//</c> where <paramref name="off" /> says it is switched off.</summary>
        public ScriptWriter Start(Keyword keyword, bool off)
        {
            Pad();
            if (off)
                _text.Append("// ");

            _text.Append(Keywords.Name(keyword));
            _fresh = false;
            return this;
        }

        /// <summary>
        /// Starts a statement whose first word is an id rather than a keyword.
        /// </summary>
        public ScriptWriter Head(string id)
        {
            Pad();
            Write(id);
            _fresh = false;
            return this;
        }

        /// <summary>An id: bare where it can be, quoted where it cannot.</summary>
        public ScriptWriter Word(string id)
        {
            Space();
            Write(id);
            return this;
        }

        /// <summary>
        /// One of the format's own words, written bare.
        /// </summary>
        public ScriptWriter Says(Keyword keyword)
        {
            Space();
            _text.Append(Keywords.Name(keyword));
            return this;
        }

        /// <summary>Text, always quoted, because it is whatever somebody typed.</summary>
        public ScriptWriter Text(string value)
        {
            Space();
            Quote(_text, value, '"');
            return this;
        }

        /// <summary>A thing of the type <paramref name="type" /> on some blocks, by number from 1: <c>ext.screen(1) on [ board ]</c>.</summary>
        /// <remarks>Written as one value, so it can stand as one item of a list.</remarks>
        public ScriptWriter Facet(HostType type, IList<string> blocks, int number)
        {
            Space();
            var dot = type.Id.IndexOf('.');
            Spell(_text, type.Id.Substring(0, dot), type.Function);
            _text.Append('(').Append(number.ToString(CultureInfo.InvariantCulture)).Append(") on [");
            for (var i = 0; i < blocks.Count; i++)
            {
                _text.Append(i == 0 ? " " : ", ");
                Write(blocks[i]);
            }

            _text.Append(" ]");
            return this;
        }

        /// <summary>A signal sent from a toolbar slot on some blocks: <c>ext.toolbar(1, 1, 4) on [ panel1 ]</c>.</summary>
        /// <remarks>Written as one value, so it can stand as one item of a list.</remarks>
        public ScriptWriter Signal(IList<string> blocks, ToolbarSlot at)
        {
            Space();
            Spell(_text, BlockKinds.Own, ScriptFormat.Toolbar);
            _text.Append('(')
                .Append(at.Toolbar.ToString(CultureInfo.InvariantCulture)).Append(", ")
                .Append(at.Page.ToString(CultureInfo.InvariantCulture)).Append(", ")
                .Append(at.Slot.ToString(CultureInfo.InvariantCulture)).Append(") on [");

            for (var i = 0; i < blocks.Count; i++)
            {
                _text.Append(i == 0 ? " " : ", ");
                Write(blocks[i]);
            }

            _text.Append(" ]");
            return this;
        }

        /// <summary>Opens a member's arguments, against the name just written.</summary>
        public ScriptWriter Open()
        {
            _text.Append('(');
            _arguments = true;
            _firstArgument = true;
            return this;
        }

        /// <summary>Closes a member's arguments.</summary>
        public ScriptWriter Close()
        {
            _text.Append(')');
            _arguments = false;
            return this;
        }

        /// <summary>A name in its parts, <c>MalTek.Airlock</c>, or the name alone when it has no namespace.</summary>
        public ScriptWriter Qualified(string space, string name)
        {
            Space();
            Spell(_text, space, name);
            return this;
        }

        /// <summary>A whole number.</summary>
        public ScriptWriter Whole(long value)
        {
            Space();
            _text.Append(value.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        /// <summary>A length of time, written with its unit against it: <c>600t</c>.</summary>
        public ScriptWriter Ticks(long ticks)
        {
            Space();
            _text.Append(ticks.ToString(CultureInfo.InvariantCulture));
            _text.Append('t');
            return this;
        }

        /// <summary>A number of seconds, written as a real reads back, with its unit: <c>1.5s</c>.</summary>
        public ScriptWriter Seconds(double seconds)
        {
            Space();
            _text.Append(seconds.ToString("R", CultureInfo.InvariantCulture));
            _text.Append('s');
            return this;
        }

        /// <summary>
        /// A real, written so it reads back as the same double.
        /// </summary>
        public ScriptWriter Real(double value)
        {
            Space();
            _text.Append(value.ToString("R", CultureInfo.InvariantCulture));
            return this;
        }

        /// <summary>A color, written <c>#RRGGBBAA</c>.</summary>
        public ScriptWriter Colour(byte r, byte g, byte b, byte a)
        {
            Space();
            _text.Append('#');
            Hex(r);
            Hex(g);
            Hex(b);
            Hex(a);
            return this;
        }

        /// <summary>Opens a list, whose items are separated by commas.</summary>
        public ScriptWriter OpenList()
        {
            Space();
            _text.Append('[');
            _lists++;
            _first = true;
            return this;
        }

        /// <summary>Closes the list opened last.</summary>
        public ScriptWriter CloseList()
        {
            if (_lists > 0)
                _lists--;

            _text.Append(_first ? "]" : " ]");

            // The list just closed is an item of whatever list is still open, so what follows it is
            // the next item there.
            _first = false;
            return this;
        }

        /// <summary>Three whole numbers in brackets: <c>(3 0 -2)</c>.</summary>
        public ScriptWriter Vector(int x, int y, int z)
        {
            Space();
            _text.Append('(')
                .Append(x.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(y.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(z.ToString(CultureInfo.InvariantCulture)).Append(')');
            return this;
        }

        /// <summary>Ends a statement.</summary>
        public void End()
        {
            _text.Append('\n');
        }

        /// <summary>Ends a statement and opens its block.</summary>
        public void Body()
        {
            _text.Append(" {\n");
            _depth++;
            _fresh = true;
        }

        /// <summary>Closes a block.</summary>
        public void Shut()
        {
            if (_depth > 0)
                _depth--;

            Pad();
            _text.Append("}\n");
            _fresh = false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return _text.ToString();
        }

        void Pad()
        {
            for (var i = 0; i < _depth; i++)
                _text.Append(Indent);
        }

        void Space()
        {
            if (_arguments)
            {
                if (!_firstArgument)
                    _text.Append(", ");

                _firstArgument = false;
                return;
            }

            if (_lists == 0)
            {
                _text.Append(' ');
                return;
            }

            _text.Append(_first ? " " : ", ");
            _first = false;
        }

        /// <summary>How a name in parts is spelled, exactly as the writer spells it.</summary>
        public static string Spelling(string space, string name)
        {
            var spelt = new StringBuilder();
            Spell(spelt, space, name);
            return spelt.ToString();
        }

        void Write(string id)
        {
            Spell(_text, id);
        }

        static void Spell(StringBuilder into, string space, string name)
        {
            if (string.IsNullOrEmpty(space))
            {
                Spell(into, name);
                return;
            }

            Spell(into, space);
            into.Append('.');
            Spell(into, name);
        }

        /// <summary>A name: bare where it can be, in single quotes where it cannot.</summary>
        static void Spell(StringBuilder into, string id)
        {
            if (Keywords.Bare(id))
                into.Append(id);
            else
                Quote(into, id, '\'');
        }

        /// <summary>
        /// Something in quotes, escaping only its own quote, by doubling it.
        /// </summary>
        /// <remarks>Persisted: a line break is written as <c>&amp;br;</c> in text and as a space in a name.</remarks>
        static void Quote(StringBuilder into, string value, char quote)
        {
            into.Append(quote);
            if (!string.IsNullOrEmpty(value))
            {
                for (var i = 0; i < value.Length; i++)
                {
                    var c = value[i];
                    if (quote == '"')
                    {
                        if (c == quote)
                            into.Append(quote);

                        TextEntities.Write(into, value, i);
                        continue;
                    }

                    if (c == '\n' || c == '\r')
                    {
                        into.Append(' ');
                        continue;
                    }

                    if (c == quote)
                        into.Append(quote);

                    into.Append(c);
                }
            }

            into.Append(quote);
        }

        void Hex(byte value)
        {
            _text.Append(Digit(value >> 4)).Append(Digit(value & 0xF));
        }

        static char Digit(int value)
        {
            return (char)(value < 10 ? '0' + value : 'A' + (value - 10));
        }
    }
}
