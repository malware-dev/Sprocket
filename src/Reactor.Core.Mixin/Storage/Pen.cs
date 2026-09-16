using System.Globalization;
using System.Text;

namespace Reactor.Storage
{
    /// <summary>
    /// Writes script text: statements, blocks, lists and annotations.
    /// </summary>
    /// <remarks>
    /// It holds the indent and the separators so that nothing writing a program has to think about
    /// either. Quoting is mechanical here too - see <see cref="Keywords.Bare" /> - because a writer
    /// that decides per call is a writer that gets one call wrong.
    /// </remarks>
    sealed class Pen
    {
        const string Indent = "    ";

        readonly StringBuilder _text = new StringBuilder();
        int _depth;
        bool _inList;
        bool _first;
        bool _fresh = true;

        /// <summary>
        /// A blank line, unless one would be the first thing inside a block or in the file.
        /// </summary>
        /// <remarks>
        /// Spacing is for somebody reading the file, so it is a rule here rather than a decision at
        /// every call site. A leading blank line inside a block reads as a gap where nothing is
        /// missing, and one after an annotation would break the annotation off what it belongs to -
        /// which is a parse error, so the writer must not be able to produce it.
        /// </remarks>
        public void Blank()
        {
            if (!_fresh)
                _text.Append('\n');
        }

        /// <summary>
        /// Text somebody typed, above the statement it belongs to. Nothing when it is empty.
        /// </summary>
        /// <remarks>
        /// One hash line per line of it, and a bare hash for an empty one, which is how a paragraph
        /// break survives without a blank line breaking the attachment. Empty first and last lines are
        /// dropped, the same trim the reader does, so that writing what was read gives the same text.
        /// </remarks>
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

                // A line kept its carriage return when the text was split on newlines alone, and it
                // would come back one character shorter, so it never gets written.
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
        public Pen Start(Keyword keyword)
        {
            Pad();
            _text.Append(Keywords.Name(keyword));
            _fresh = false;
            return this;
        }

        /// <summary>
        /// Starts a statement whose first word is an id rather than a keyword.
        /// </summary>
        /// <remarks>
        /// A step reads as what it does - <c>open [outer] wait</c> - so the action id is the head, and
        /// an action named after one of the format's words is quoted like any other collision.
        /// </remarks>
        public Pen Head(string id)
        {
            Pad();
            Write(id);
            _fresh = false;
            return this;
        }

        /// <summary>An id: bare where it can be, quoted where it cannot.</summary>
        public Pen Word(string id)
        {
            Space();
            Write(id);
            return this;
        }

        /// <summary>
        /// One of the format's own words, written bare.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Word" />, which would quote it for being a keyword. That is the
        /// whole distinction the format rests on - bare is the language speaking, quoted is data - so
        /// it is two methods rather than one that has to guess which it was handed.
        /// </remarks>
        public Pen Says(Keyword keyword)
        {
            Space();
            _text.Append(Keywords.Name(keyword));
            return this;
        }

        /// <summary>Text, always quoted, because it is whatever somebody typed.</summary>
        public Pen Text(string value)
        {
            Space();
            Quote(_text, value, '"');
            return this;
        }

        /// <summary>A name in its parts, <c>MalTek.Airlock</c>, or the name alone when it has no namespace.</summary>
        public Pen Qualified(string space, string name)
        {
            Space();
            Spell(_text, space, name);
            return this;
        }

        public Pen Whole(long value)
        {
            Space();
            _text.Append(value.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        /// <summary>A length of time, written with its unit against it: <c>600t</c>.</summary>
        public Pen Ticks(long ticks)
        {
            Space();
            _text.Append(ticks.ToString(CultureInfo.InvariantCulture));
            _text.Append('t');
            return this;
        }

        /// <summary>
        /// A real, written so it reads back as the same double.
        /// </summary>
        /// <remarks>
        /// The round-trip format rather than a fixed number of decimals, which is the one thing this
        /// format exists not to lose. It is why the reader has to accept <c>1E-05</c>.
        /// </remarks>
        public Pen Real(double value)
        {
            Space();
            _text.Append(value.ToString("R", CultureInfo.InvariantCulture));
            return this;
        }

        public Pen Colour(byte r, byte g, byte b, byte a)
        {
            Space();
            _text.Append('#');
            Hex(r);
            Hex(g);
            Hex(b);
            Hex(a);
            return this;
        }

        public Pen OpenList()
        {
            Space();
            _text.Append('[');
            _inList = true;
            _first = true;
            return this;
        }

        public Pen CloseList()
        {
            _inList = false;
            _text.Append(_first ? "]" : " ]");
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
            if (!_inList)
            {
                _text.Append(' ');
                return;
            }

            _text.Append(_first ? " " : ", ");
            _first = false;
        }

        /// <summary>How a name in parts is spelt, exactly as the writer spells it.</summary>
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
            if (!string.IsNullOrEmpty(space))
            {
                Spell(into, space);
                into.Append('.');
            }

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
        /// <remarks>
        /// A line break folds to a space, because a statement ends at the end of its line and a value holding
        /// one would write a file that cannot be read back. <see cref="Sequences.Naming" /> is where that rule
        /// belongs and it keeps names to one line, so this is the guard rather than the rule: the writer must
        /// not be able to produce a broken save whatever reaches it.
        /// </remarks>
        static void Quote(StringBuilder into, string value, char quote)
        {
            into.Append(quote);
            if (!string.IsNullOrEmpty(value))
            {
                for (var i = 0; i < value.Length; i++)
                {
                    var c = value[i];
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
