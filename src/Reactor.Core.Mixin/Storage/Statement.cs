using System.Collections.Generic;

namespace Reactor.Storage
{
    /// <summary>
    /// A word, the values after it, and the block under it if it has one.
    /// </summary>
    /// <remarks>
    /// The whole syntax tree is this one type. A construct nobody recognises is a statement like any
    /// other and is dropped whole, with its block, which is the property the format exists for: a reader
    /// loses what it cannot understand and keeps everything else.
    /// <para>
    /// It owns no values. They are a run of <see cref="Reading.Values" />, which is what keeps a statement
    /// to one object - see <see cref="Reading" /> for the measurement behind that. Reading a value that is
    /// not there answers empty rather than failing, because a save written by a later build may carry more
    /// values on a line this one knows.
    /// </para>
    /// </remarks>
    sealed class Statement
    {
        /// <summary>An array rather than a list, so adding to what a bodiless statement hands back throws.</summary>
        static readonly Statement[] Nothing = new Statement[0];

        readonly Reading _reading;
        readonly int _from;
        readonly List<Statement> _body;

        public Statement(Reading reading, Keyword key, string head, int from, int count, List<Statement> body)
        {
            _reading = reading;
            _from = from;
            _body = body;
            Key = key;
            Head = head ?? "";
            Count = count;
            Annotation = "";
        }

        /// <summary>
        /// The format's word the statement starts with, or <see cref="Keyword.None" /> when it starts with an id.
        /// </summary>
        /// <remarks>
        /// A quoted head never has one, and that is how the language has no reserved words. A step is
        /// <c>open [outer] wait</c>, so an action genuinely called <c>every</c> is written
        /// <c>"every" [outer] wait</c> and read as what it is.
        /// </remarks>
        public Keyword Key { get; private set; }

        /// <summary>The id the statement starts with, when it does not start with a keyword. A step's action.</summary>
        public string Head { get; private set; }

        /// <summary>
        /// What somebody wrote above it, or empty.
        /// </summary>
        /// <remarks>
        /// Data somebody typed and not a comment - it is part of the model and round-trips like a name,
        /// which is why the format has no comments at all: text that belonged to nobody would be a parser
        /// feature serving a workflow nobody is offered.
        /// </remarks>
        public string Annotation { get; set; }

        public int Count { get; private set; }

        /// <summary>What is inside its braces, empty when it has none.</summary>
        public IList<Statement> Body => _body != null ? (IList<Statement>)_body : Nothing;

        public Value At(int index)
        {
            return index >= 0 && index < Count ? _reading.Values[_from + index] : Value.None;
        }

        /// <summary>An identifier or string at a position, empty when there is none.</summary>
        public string Word(int index)
        {
            var value = At(index);
            return value.IsWord ? value.Word : "";
        }

        public int Whole(int index)
        {
            return (int)At(index).Number();
        }

        /// <summary>Whether the value at a position is the bare keyword given.</summary>
        public bool Says(int index, Keyword keyword)
        {
            return At(index).Says(keyword);
        }

        /// <summary>Where the given keyword sits among the values, or -1.</summary>
        public int Find(Keyword keyword)
        {
            var values = _reading.Values;
            for (var i = 0; i < Count; i++)
                if (values[_from + i].Says(keyword))
                    return i;

            return -1;
        }

        /// <summary>The items of a list at a position, or none where there is no list there.</summary>
        public ValueRange Items(int index)
        {
            var value = At(index);
            return value.Form == ValueForm.List
                ? new ValueRange(_reading.Items, value.From, value.Count)
                : new ValueRange();
        }
    }
}
