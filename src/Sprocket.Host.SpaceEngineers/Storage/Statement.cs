using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// A word, the values after it, and the block under it if it has one.
    /// </summary>
    /// <remarks>
    /// The whole syntax tree is this one type.
    /// <para>
    /// It owns no values. They are a run of <see cref="ScriptValues.Values" />, which is what keeps a statement
    /// to one object - see <see cref="ScriptValues" /> for the measurement behind that. Reading a value that is
    /// not there answers empty rather than failing, so a reader asks for what a shape needs and says what
    /// is missing itself.
    /// </para>
    /// </remarks>
    public sealed class Statement
    {
        static readonly Statement[] Nothing = new Statement[0];

        readonly ScriptValues _reading;
        readonly int _from;
        readonly List<Statement> _body;

        /// <summary>
        /// A statement at <paramref name="at" /> in the script, headed by <paramref name="key" /> or <paramref name="head" />,
        /// whose values are the <paramref name="count" /> of <paramref name="reading" />'s from <paramref name="from" />.
        /// </summary>
        public Statement(ScriptValues reading, int at, Keyword key, string head, int from, int count,
            List<Statement> body)
        {
            _reading = reading;
            _from = from;
            _body = body;
            At = at;
            Key = key;
            Head = head ?? "";
            Count = count;
            Annotation = "";
        }

        /// <summary>Where in the script it starts, so trouble about it can say which line.</summary>
        public int At { get; private set; }

        /// <summary>
        /// The format's word the statement starts with, or <see cref="Keyword.None" /> when it starts with an id.
        /// </summary>
        /// <remarks>
        /// A quoted head never has one: a name spelled like a keyword is written quoted, and read as the
        /// name it is.
        /// </remarks>
        public Keyword Key { get; private set; }

        /// <summary>The id the statement starts with, when it does not start with a keyword.</summary>
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

        /// <summary>Whether its line starts with <c>//</c>, which switches a step off.</summary>
        public bool IsOff { get; set; }

        /// <summary>How many values follow the word it starts with.</summary>
        public int Count { get; private set; }

        /// <summary>What is inside its braces, empty when it has none.</summary>
        public IList<Statement> Body => _body != null ? (IList<Statement>)_body : Nothing;

        /// <summary>Whether it was written with a block, empty or not.</summary>
        public bool HasBlock => _body != null;

        /// <summary>The value at a position, or none where there is none.</summary>
        public Value ValueAt(int index)
        {
            return index >= 0 && index < Count ? _reading.Values[_from + index] : Value.None;
        }

        /// <summary>A name at a position, bare or quoted, empty when there is none.</summary>
        public string Word(int index)
        {
            var value = ValueAt(index);
            return value.IsWord ? value.Word : "";
        }

        /// <summary>
        /// Text at a position, empty when there is none.
        /// </summary>
        /// <remarks>
        /// Only double-quoted text. A name where text belongs reads as empty, the same as nothing at all:
        /// the two are kept apart so that neither can ever stand in for the other.
        /// </remarks>
        public string Text(int index)
        {
            var value = ValueAt(index);
            return value.Form == ValueForm.Text ? value.Word : "";
        }

        /// <summary>The number at a position as a whole number, 0 where there is none.</summary>
        public int Whole(int index)
        {
            return (int)ValueAt(index).Number();
        }

        /// <summary>Whether the value at a position is the bare keyword given.</summary>
        public bool Says(int index, Keyword keyword)
        {
            return ValueAt(index).Says(keyword);
        }

        /// <summary>Where the given keyword sits among the values, or -1.</summary>
        public int Find(Keyword keyword)
        {
            return Find(keyword, 0);
        }

        /// <summary>Where the given keyword sits among the values from a position on, or -1.</summary>
        public int Find(Keyword keyword, int from)
        {
            var values = _reading.Values;
            for (var i = from; i < Count; i++)
                if (values[_from + i].Says(keyword))
                    return i;

            return -1;
        }

        /// <summary>The arguments of the member at a position, none where it was written without them or is no member.</summary>
        public ValueRange Arguments(int index)
        {
            return Arguments(ValueAt(index));
        }

        /// <summary>The arguments of the member <paramref name="value" />, none where it was written without them or is no member.</summary>
        public ValueRange Arguments(Value value)
        {
            return value.Form == ValueForm.Reference ? Within(_reading.Parts[value.From + value.Count - 2]) : new ValueRange();
        }

        /// <summary>The names the member at a position is on, none where there is no member there.</summary>
        public ValueRange On(int index)
        {
            return On(ValueAt(index));
        }

        /// <summary>The names the member <paramref name="value" /> is on, none where it is no member.</summary>
        public ValueRange On(Value value)
        {
            return value.Form == ValueForm.Reference ? Within(_reading.Parts[value.From + value.Count - 1]) : new ValueRange();
        }

        /// <summary>The parts of the name of the member <paramref name="value" />, none where it is no member.</summary>
        public ValueRange Member(Value value)
        {
            return value.Form == ValueForm.Reference ? new ValueRange(_reading.Parts, value.From, value.Count - 2) : new ValueRange();
        }

        /// <summary>Whether the value at a position is a member written on blocks.</summary>
        public bool IsMember(int index)
        {
            return ValueAt(index).Form == ValueForm.Reference;
        }

        /// <summary>A written number as a count, 0 where it is none a count can be.</summary>
        public static int Counted(Value value)
        {
            return value.Form == ValueForm.Integer ? Number(value.Whole) : 0;
        }

        /// <summary>The statement that starts at the value <paramref name="at" />: its word, and the values after it.</summary>
        /// <remarks>How a trigger reads the step it does, which is written as a sequence writes it, after <c>then</c> or a clock's duration.</remarks>
        public Statement Rest(int at)
        {
            var value = ValueAt(at);
            var key = value.Form == ValueForm.Keyword ? value.Keyword : Keyword.None;
            var head = key == Keyword.None && value.IsWord ? value.Word : "";
            var from = at < 0 ? Count : at + 1 > Count ? Count : at + 1;
            return new Statement(_reading, At, key, head, _from + from, Count - from, null);
        }

        /// <summary>The items of a list at a position, or none where there is no list there.</summary>
        public ValueRange Items(int index)
        {
            return Within(ValueAt(index));
        }

        /// <summary>
        /// What <paramref name="value" /> holds: a list's items or a vector's three numbers, or none where
        /// it is neither.
        /// </summary>
        public ValueRange Within(Value value)
        {
            return value.Form == ValueForm.List || value.Form == ValueForm.Vector
                ? new ValueRange(_reading.Items, value.From, value.Count)
                : new ValueRange();
        }

        /// <summary>
        /// The parts of a name at a position: two for <c>MalTek.Airlock</c>, one for a name written alone, and the member's
        /// for a member written on blocks.
        /// </summary>
        /// <remarks>
        /// A name written alone is answered as a run of one, so a reader asks one question whichever way a
        /// name was written. None where there is no name there.
        /// </remarks>
        public ValueRange Parts(int index)
        {
            // Bounded first. A name written alone is answered out of this statement's own run, so an
            // index past the end would otherwise hand back a value belonging to the statement below -
            // and a statement that stops early is exactly when a reader asks.
            if (index < 0 || index >= Count)
                return new ValueRange();

            var value = ValueAt(index);
            if (value.Form == ValueForm.Qualified)
                return new ValueRange(_reading.Parts, value.From, value.Count);

            if (value.Form == ValueForm.Reference)
                return new ValueRange(_reading.Parts, value.From, value.Count - 2);

            return value.Form == ValueForm.Identifier
                ? new ValueRange(_reading.Values, _from + index, 1)
                : new ValueRange();
        }

        /// <summary>A written number as a count, 0 where it is none a count can be.</summary>
        static int Number(long written)
        {
            return written < 1 || written > int.MaxValue ? 0 : (int)written;
        }
    }
}
