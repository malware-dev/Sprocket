using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// A word, the values after it, and the block under it if it has one.
    /// </summary>
    /// <remarks>Reading a value that is not there answers empty rather than failing.</remarks>
    public sealed class Statement
    {
        static readonly Statement[] Nothing = new Statement[0];

        static readonly ScriptAttribute[] NoAttributes = new ScriptAttribute[0];

        static readonly int[] NoLines = new int[0];

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
        /// <remarks>A quoted head never has one.</remarks>
        public Keyword Key { get; private set; }

        /// <summary>The id the statement starts with, when it does not start with a keyword.</summary>
        public string Head { get; private set; }

        /// <summary>
        /// What somebody wrote above it, or empty.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>The attributes written above it, in the order they were written; none where it has none.</summary>
        public IList<ScriptAttribute> Attributes { get; set; } = NoAttributes;

        /// <summary>Where each line it goes on with starts, as the position of that line's first value; none for a statement on one line.</summary>
        /// <remarks>Only a system's header goes on, with a line for each of its clauses.</remarks>
        public IList<int> Lines { get; set; } = NoLines;

        /// <summary>Whether its line starts with <c>//</c>, which switches a step off.</summary>
        public bool IsOff { get; set; }

        /// <summary>How many values follow the word it starts with.</summary>
        public int Count { get; private set; }

        /// <summary>What is inside its braces, empty when it has none.</summary>
        public IList<Statement> Body => _body != null ? (IList<Statement>)_body : Nothing;

        /// <summary>Whether it was read from a script written for a format earlier than <see cref="Keywords.ContextualDraft" />, where a quoted name is never a flag.</summary>
        public bool Earlier => _reading.Earlier;

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
        /// <remarks>Only double-quoted text; a name where text belongs reads as empty.</remarks>
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

        /// <summary>The list of names the member <paramref name="value" /> is on, or none where it is no member.</summary>
        public Value NamesOf(Value value)
        {
            return value.Form == ValueForm.Reference ? _reading.Parts[value.From + value.Count - 1] : Value.None;
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
        public Statement Rest(int at)
        {
            var value = ValueAt(at);
            var key = value.Form == ValueForm.Keyword ? value.Keyword : Keyword.None;
            var head = key == Keyword.None && value.IsWord ? value.Word : "";
            var from = at < 0 ? Count : at + 1 > Count ? Count : at + 1;
            return new Statement(_reading, At, key, head, _from + from, Count - from, null);
        }

        /// <summary>
        /// This statement written with <paramref name="values" /> in place of its own, going on at <paramref name="lines" />,
        /// with <paramref name="attributes" /> above it: its head, annotation and block kept.
        /// </summary>
        /// <remarks>For an upgrade carrying a statement written in an earlier form into the current one, before it is read.</remarks>
        public Statement With(IList<Value> values, IList<int> lines, IList<ScriptAttribute> attributes)
        {
            var all = _reading.Values;
            var from = all.Count;
            foreach (var value in values)
                all.Add(value);

            return new Statement(_reading, At, Key, Head, from, values.Count, _body)
            {
                Annotation = Annotation,
                IsOff = IsOff,
                Lines = lines,
                Attributes = attributes
            };
        }

        /// <summary>Has the name in parts or the member <paramref name="value" />, of this statement's reading, start with the name <paramref name="first" /> in place of its first part.</summary>
        /// <remarks>For an upgrade carrying a statement written in an earlier form into the current one, before it is read.</remarks>
        public void Respell(Value value, string first)
        {
            if (value.Form == ValueForm.Qualified || value.Form == ValueForm.Reference)
                _reading.Parts.Set(value.From, new Value(ValueForm.Identifier, first));
        }

        /// <summary>Has the name of the member <paramref name="value" /> end with <paramref name="last" /> in place of its last part: <c>sprocket.get_screen</c> for <c>sprocket.screen</c>.</summary>
        /// <remarks>For an upgrade carrying a statement written in an earlier form into the current one, before it is read.</remarks>
        public void RespellLast(Value value, string last)
        {
            if (value.Form == ValueForm.Reference)
                _reading.Parts.Set(value.From + value.Count - 3, new Value(ValueForm.Identifier, last));
        }

        /// <summary>Has the value at <paramref name="index" /> written with the kind <paramref name="kind" /> in its place: <c>state(home)</c> for <c>home</c>.</summary>
        /// <remarks>For an upgrade carrying a statement written in an earlier form into the current one, before it is read.</remarks>
        public void Kind(int index, string kind)
        {
            _reading.Values.Set(_from + index, Kinded(new Value(ValueForm.Identifier, kind), ValueAt(index)));
        }

        /// <summary>Has the item at <paramref name="index" /> of the list <paramref name="list" /> written with the kind <paramref name="kind" /> in its place: <c>slot(side)</c> for <c>side</c>.</summary>
        /// <remarks>For an upgrade carrying a statement written in an earlier form into the current one, before it is read.</remarks>
        public void KindWithin(Value list, int index, string kind)
        {
            var items = _reading.Items;
            items.Set(list.From + index, Kinded(new Value(ValueForm.Identifier, kind), items[list.From + index]));
        }

        /// <summary>Has the value at <paramref name="index" /> written as <paramref name="value" /> in its place.</summary>
        /// <remarks>For an upgrade carrying a statement written in an earlier form into the current one, before it is read.</remarks>
        public void Put(int index, Value value)
        {
            _reading.Values.Set(_from + index, value);
        }

        /// <summary><paramref name="thing" /> with the kind <paramref name="kind" />, written into this statement's reading.</summary>
        /// <remarks>For an upgrade carrying a statement written in an earlier form into the current one, before it is read.</remarks>
        public Value Kinded(Value kind, Value thing)
        {
            var parts = _reading.Parts;
            var from = parts.Count;
            parts.Add(kind);
            parts.Add(thing);
            return Value.Kinded(from);
        }

        /// <summary>The parts of the name in parts <paramref name="value" />, <c>settings.'Open angle'</c>, or none where it is no such name.</summary>
        public ValueRange PartsOf(Value value)
        {
            return value.Form == ValueForm.Qualified ? new ValueRange(_reading.Parts, value.From, value.Count) : new ValueRange();
        }

        /// <summary>The kind of the thing written with its kind <paramref name="value" />, <c>side</c> in <c>side(inside)</c>, or none where it is no such thing.</summary>
        public Value KindOf(Value value)
        {
            return value.Form == ValueForm.Kinded ? _reading.Parts[value.From] : Value.None;
        }

        /// <summary>The thing written with its kind <paramref name="value" />, <c>inside</c> in <c>side(inside)</c>, or none where it is no such thing.</summary>
        public Value ThingOf(Value value)
        {
            return value.Form == ValueForm.Kinded ? _reading.Parts[value.From + 1] : Value.None;
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
        /// <remarks>None where there is no name there.</remarks>
        public ValueRange Parts(int index)
        {
            // Bounded first: a name written alone is answered from this statement's own run, and an index past its end
            // would reach the statement below.
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
