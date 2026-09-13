using System.Collections.Generic;

namespace Reactor.Storage
{
    /// <summary>
    /// A word, the values after it, and the block under it if it has one.
    /// </summary>
    /// <remarks>
    /// The whole syntax tree is this one type. A construct nobody recognises is a statement like any
    /// other and is dropped whole, with its block, which is the property the format exists for: a
    /// reader loses what it cannot understand and keeps everything else.
    /// <para>
    /// Reading a value that is not there answers empty rather than failing, because a save written by
    /// a later build may carry more values on a line this one knows.
    /// </para>
    /// </remarks>
    sealed class Statement
    {
        /// <summary>An array rather than a list, so adding to what a bodiless statement hands back throws.</summary>
        static readonly Statement[] Nothing = new Statement[0];

        readonly List<Value> _values = new List<Value>();
        readonly List<Statement> _body;

        public Statement(string head, bool quoted, List<Statement> body)
        {
            Head = head;
            Quoted = quoted;
            Annotation = "";
            _body = body;
        }

        /// <summary>The word the statement starts with.</summary>
        public string Head { get; private set; }

        /// <summary>
        /// What somebody wrote above it, or empty.
        /// </summary>
        /// <remarks>
        /// Data somebody typed and not a comment - it is part of the model and round-trips like a name,
        /// which is why the format has no comments at all: text that belonged to nobody would be a
        /// parser feature serving a workflow nobody is offered.
        /// </remarks>
        public string Annotation { get; set; }

        /// <summary>
        /// Whether the head was written as a string, which means it is data and not a keyword.
        /// </summary>
        /// <remarks>
        /// This is how the language has no reserved words. A step is <c>open [outer] wait</c>, so an
        /// action genuinely called <c>every</c> is written <c>"every" [outer] wait</c> and read as
        /// what it is.
        /// </remarks>
        public bool Quoted { get; private set; }

        public IList<Value> Values => _values;

        /// <summary>What is inside its braces, empty when it has none.</summary>
        public IList<Statement> Body => _body != null ? (IList<Statement>)_body : Nothing;

        public Value At(int index)
        {
            return index >= 0 && index < _values.Count ? _values[index] : Value.None;
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

        /// <summary>Whether the word at a position is the bare keyword given.</summary>
        public bool Says(int index, string keyword)
        {
            return At(index).Says(keyword);
        }

        /// <summary>Where the given keyword sits among the values, or -1.</summary>
        public int Find(string keyword)
        {
            for (var i = 0; i < _values.Count; i++)
                if (_values[i].Says(keyword))
                    return i;

            return -1;
        }

        /// <summary>The values inside a list at a position, or null where there is no list there.</summary>
        public List<Value> Items(int index)
        {
            var value = At(index);
            return value.Form == ValueForm.List ? value.Items : null;
        }

        public void Add(Value value)
        {
            _values.Add(value);
        }
    }
}
