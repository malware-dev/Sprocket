using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// Reads tokens into statements.
    /// </summary>
    /// <remarks>What is wrong with the text is recorded in <see cref="ScriptTrouble" />, never thrown.</remarks>
    public sealed class Parser
    {
        /// <summary>The word a duration in seconds carries, where one in ticks carries none.</summary>
        public const string Seconds = "s";

        readonly Lexer _lexer;
        readonly ScriptValues _reading = new ScriptValues();

        readonly List<List<Value>> _held = new List<List<Value>>();

        int _depth;

        Token _token;

        Token _ahead;

        bool _looked;

        int _failures;

        /// <summary>A parser at the start of <paramref name="text" />.</summary>
        public Parser(string text)
        {
            _lexer = new Lexer(text);
            Step();
        }

        /// <summary>What was wrong with the first thing that was wrong, or empty.</summary>
        public string Trouble { get; private set; } = "";

        /// <summary>Every statement of the text, nested in the blocks they are written in.</summary>
        public List<Statement> Read()
        {
            var into = new List<Statement>();
            Constructs(into, false);
            return into;
        }

        void Step()
        {
            if (_looked)
            {
                _token = _ahead;
                _looked = false;
            }
            else
            {
                _token = _lexer.Next();
            }

            if (_token.Kind == TokenKind.Bad)
                Fail(_token.Text.ToString());
        }

        /// <summary>The token after the current one, read without moving on to it.</summary>
        Token Peek()
        {
            if (!_looked)
            {
                _ahead = _lexer.Next();
                _looked = true;
            }

            return _ahead;
        }

        /// <summary>
        /// Whether a member's arguments or blocks follow the name just read: <c>(</c>, or <c>on</c> and a list.
        /// </summary>
        /// <remarks>A name followed by the flag <c>on</c> is written the same way: <c>door1 on</c>.</remarks>
        bool Referencing()
        {
            if (_token.Kind == TokenKind.VectorOpen)
                return true;

            return _token.Kind == TokenKind.Keyword && _token.Keyword == Keyword.On && Peek().Kind == TokenKind.ListOpen;
        }

        /// <summary>
        /// Counts something that went wrong, and says where the first one was.
        /// </summary>
        void Fail(string what)
        {
            _failures++;
            if (Trouble.Length == 0)
                Trouble = $"Line {new TextPtr(_lexer.Script, _token.At).FindLineNo()}: {what}";
        }

        /// <summary>Past the end of this line, so reading can pick up at the next construct.</summary>
        void Recover()
        {
            while (_token.Kind != TokenKind.End && _token.Kind != TokenKind.Newline)
                Step();

            if (_token.Kind == TokenKind.Newline)
                Step();
        }

        void EndOfLine()
        {
            if (_token.Kind == TokenKind.Newline)
            {
                Step();
                return;
            }

            if (_token.Kind == TokenKind.End)
                return;

            Fail("expected the end of the line.");
            Recover();
        }

        void Constructs(List<Statement> into, bool nested)
        {
            var note = "";
            List<ScriptAttribute> attributes = null;
            while (true)
            {
                if (_token.Kind == TokenKind.End)
                {
                    if (nested)
                        Fail("missing '}'.");

                    Orphan(note, attributes);
                    return;
                }

                if (_token.Kind == TokenKind.BlockClose)
                {
                    if (!nested)
                    {
                        Fail("unexpected '}'.");
                        Recover();
                        continue;
                    }

                    Orphan(note, attributes);
                    Step();
                    EndOfLine();
                    return;
                }

                if (_token.Kind == TokenKind.Newline)
                {
                    // A blank line separates constructs, and is what makes an annotation an orphan.
                    Orphan(note, attributes);
                    note = "";
                    attributes = null;
                    Step();
                    continue;
                }

                if (_token.Kind == TokenKind.Annotation)
                {
                    if (attributes != null)
                    {
                        Fail("unexpected annotation line after an attribute; the description comes before the attributes.");
                        Recover();
                        continue;
                    }

                    var line = _token.Text.ToString();
                    note = note.Length == 0 ? line : $"{note}\n{line}";
                    Step();
                    EndOfLine();
                    continue;
                }

                if (_token.Kind == TokenKind.Attribute)
                {
                    if (attributes == null)
                        attributes = new List<ScriptAttribute>();

                    Attribute(attributes);
                    continue;
                }

                if (_token.Kind == TokenKind.Bad)
                {
                    Recover();
                    note = "";
                    attributes = null;
                    continue;
                }

                // A line starting //: the statement after it is read as any other, and marked off.
                var off = _token.Kind == TokenKind.Off;
                if (off)
                    Step();

                var statement = One(note);
                if (statement != null && attributes != null)
                    statement.Attributes = attributes;

                note = "";
                attributes = null;
                if (statement == null)
                    continue;

                statement.IsOff = off;
                into.Add(statement);
            }
        }

        /// <summary>
        /// Annotation text, or attributes, with nothing to belong to.
        /// </summary>
        void Orphan(string note, List<ScriptAttribute> attributes)
        {
            if (note.Length > 0 || attributes != null)
                Fail("expected a statement under the annotation.");
        }

        /// <summary>Reads one attribute line, <c>#@ name: value</c>, into <paramref name="into" />, and the end of its line.</summary>
        void Attribute(List<ScriptAttribute> into)
        {
            Step();
            if (_token.Kind == TokenKind.Keyword)
            {
                Fail($"unexpected '{Keywords.Name(_token.Keyword)}' as an attribute's name; a name spelled like a keyword is quoted.");
                Recover();
                return;
            }

            if (_token.Kind != TokenKind.Identifier)
            {
                Fail("expected an attribute's name after '#@ '.");
                Recover();
                return;
            }

            var name = _token.Text.ToString();
            Step();
            if (_token.Kind != TokenKind.Colon)
            {
                Fail($"expected ':' after the attribute {name}.");
                Recover();
                return;
            }

            Step();
            if (!Valuish())
            {
                Fail($"expected a value for the attribute {name}.");
                Recover();
                return;
            }

            var failures = _failures;
            var start = _token.At;
            var value = Next();
            if (_failures > failures)
            {
                Recover();
                return;
            }

            var written = _lexer.Script.Substring(start, _token.At - start).TrimEnd();

            foreach (var had in into)
            {
                if (had.Name == name)
                {
                    Fail($"the attribute {name} is given twice.");
                    Recover();
                    return;
                }
            }

            into.Add(new ScriptAttribute(name, value, written));
            EndOfLine();
        }

        Statement One(string note)
        {
            var kind = _token.Kind;
            if (kind != TokenKind.Identifier && kind != TokenKind.Keyword)
            {
                Fail("expected a statement.");
                Recover();
                return null;
            }

            var at = _token.At;
            var key = _token.Keyword;
            var head = kind == TokenKind.Keyword ? "" : _token.Text.ToString();
            var failures = _failures;
            var values = _reading.Values;
            var from = values.Count;

            Step();
            Values(values);

            // A system's header goes on with a line for each of its clauses.
            List<int> lines = null;
            while (key == Keyword.System && _failures == failures && _token.Kind == TokenKind.Newline && Clause(Peek()))
            {
                Step();
                if (lines == null)
                    lines = new List<int>();

                lines.Add(values.Count - from);
                Values(values);
            }

            // A line that went wrong makes no statement, so it is judged once, by its trouble.
            if (_failures > failures)
            {
                Recover();
                return null;
            }

            // A '{' may stand on a line of its own under the statement it opens.
            if (_token.Kind == TokenKind.Newline && Peek().Kind == TokenKind.BlockOpen)
                Step();

            var body = _token.Kind == TokenKind.BlockOpen ? new List<Statement>() : null;
            var count = values.Count - from;
            var statement = new Statement(_reading, at, key, head, from, count, body);
            if (lines != null)
                statement.Lines = lines;

            statement.Annotation = Trim(note);

            if (body == null)
            {
                EndOfLine();
                return statement;
            }

            // Only a system, a sequence, an instance, an if, an else, a catch and a finally hold statements. The body is still read,
            // so the braces stay counted.
            if (key != Keyword.System && key != Keyword.Sequence && key != Keyword.Instance && key != Keyword.If
                && key != Keyword.Else && head != SequenceWords.Catch && head != SequenceWords.Finally)
                Fail("unexpected '{'.");

            Step();
            EndOfLine();
            Constructs(body, true);
            return statement;
        }

        /// <summary>Reads the values of one line into <paramref name="values" />, up to what is no value.</summary>
        void Values(ValueChunks values)
        {
            while (true)
            {
                if (_token.Kind == TokenKind.Comma)
                {
                    Fail("unexpected ','.");
                    return;
                }

                if (_token.Kind == TokenKind.Off)
                {
                    Fail("unexpected '//'; it only starts a line.");
                    return;
                }

                if (!Valuish())
                    return;

                values.Add(Next());
            }
        }

        /// <summary>Whether <paramref name="token" /> starts a line of a system's header: <c>changed</c> or <c>from</c>.</summary>
        static bool Clause(Token token)
        {
            return token.Kind == TokenKind.Keyword && (token.Keyword == Keyword.Changed || token.Keyword == Keyword.From);
        }

        bool Valuish()
        {
            switch (_token.Kind)
            {
                case TokenKind.Identifier:
                case TokenKind.Keyword:
                case TokenKind.Equals:
                case TokenKind.Comparison:
                case TokenKind.Bar:
                case TokenKind.Text:
                case TokenKind.Integer:
                case TokenKind.Real:
                case TokenKind.Duration:
                case TokenKind.Color:
                case TokenKind.ListOpen:
                case TokenKind.VectorOpen:
                    return true;
            }

            return false;
        }

        Value Next()
        {
            var token = _token;
            switch (token.Kind)
            {
                case TokenKind.Identifier:
                    Step();
                    var name = new Value(ValueForm.Identifier, token.Text.ToString());
                    if (_token.Kind == TokenKind.Dot)
                    {
                        var failures = _failures;
                        name = Qualified(name);
                        if (_failures > failures)
                            return Value.None;
                    }

                    return Referencing() ? Referenced(name) : name;

                case TokenKind.Keyword:
                    Step();

                    // A name spelled like a keyword is quoted, beside a dot as anywhere: ext.'on'.
                    if (_token.Kind == TokenKind.Dot)
                    {
                        Fail($"unexpected '{Keywords.Name(token.Keyword)}' before '.'; a name spelled like a keyword is quoted.");
                        return Value.None;
                    }

                    return new Value(ValueForm.Keyword, null, 0, 0, token.Keyword);

                case TokenKind.Equals:
                    Step();
                    return new Value(ValueForm.Keyword, null, 0, 0, Keyword.Equals);

                case TokenKind.Comparison:
                    Step();
                    return new Value(ValueForm.Keyword, null, 0, 0, token.Keyword);

                case TokenKind.Bar:
                    Step();
                    return new Value(ValueForm.Keyword, null, 0, 0, Keyword.Bar);

                case TokenKind.Text:
                    Step();
                    return new Value(ValueForm.Text, token.Text.ToString());

                case TokenKind.Integer:
                    Step();
                    return new Value(ValueForm.Integer, null, token.Whole);

                case TokenKind.Real:
                    Step();
                    return new Value(ValueForm.Real, null, 0, token.Real);

                case TokenKind.Duration:
                    Step();
                    return new Value(ValueForm.Duration, token.Text[0] == 's' ? Seconds : null, token.Whole, token.Real);

                case TokenKind.Color:
                    Step();
                    return new Value(ValueForm.Color, null, token.Whole);

                case TokenKind.ListOpen:
                    return List();

                default:
                    return Vector();
            }
        }

        /// <summary>
        /// A list, its items written into the reading's shared run once it closes.
        /// </summary>
        Value List()
        {
            return Run(TokenKind.ListClose, "']'");
        }

        /// <summary>A member's arguments in parentheses, at least one, kept as a list is.</summary>
        Value Arguments()
        {
            return Run(TokenKind.VectorClose, "')'");
        }

        /// <summary>Values separated by commas up to <paramref name="close" />, written as one run once it closes.</summary>
        Value Run(TokenKind close, string closing)
        {
            // One buffer per open list, held until it closes because a list inside writes its own run first. Kept for the
            // life of the reading, since loading lands inside a world load.
            if (_depth == _held.Count)
                _held.Add(new List<Value>());

            List<Value> held = _held[_depth];
            held.Clear();
            _depth++;
            Step();

            var value = Listed(held, close, closing);
            _depth--;
            return value;
        }

        /// <summary>Reads items into <paramref name="held" /> up to <paramref name="close" />, then writes them as one run.</summary>
        Value Listed(List<Value> held, TokenKind close, string closing)
        {
            if (close == TokenKind.VectorClose && _token.Kind == close)
            {
                Fail("expected an argument before ')'.");
                return Value.None;
            }

            while (_token.Kind != close)
            {
                if (!Valuish())
                {
                    Fail($"expected a value or {closing}.");
                    return Value.None;
                }

                held.Add(Next());
                if (_token.Kind != TokenKind.Comma)
                    break;

                Step();
                if (close == TokenKind.VectorClose && _token.Kind == close)
                {
                    Fail("expected an argument after ','.");
                    return Value.None;
                }
            }

            if (_token.Kind != close)
            {
                Fail($"expected {closing}.");
                return Value.None;
            }

            Step();

            var items = _reading.Items;
            var from = items.Count;
            foreach (var value in held)
                items.Add(value);

            return Value.Listed(from, held.Count);
        }

        /// <summary>
        /// A name in parts, <c>MalTek.Airlock</c>, having read its first part already.
        /// </summary>
        /// <remarks>Spaces around the dot are nothing: <c>'MalTek Inc.' . Airlock</c> is <c>'MalTek Inc.'.Airlock</c>.</remarks>
        Value Qualified(Value first)
        {
            var parts = _reading.Parts;
            var from = parts.Count;
            parts.Add(first);

            while (_token.Kind == TokenKind.Dot)
            {
                Step();
                if (_token.Kind == TokenKind.Identifier)
                    parts.Add(new Value(ValueForm.Identifier, _token.Text.ToString()));
                else
                {
                    Fail("expected a name after '.'.");
                    return Value.None;
                }

                Step();
            }

            return Value.Qualified(from, parts.Count - from);
        }

        /// <summary>
        /// A member and the blocks it is read on or done to, <c>ext.amount("Ingot/Iron") on [ cargo ]</c>, having read the
        /// member's name already.
        /// </summary>
        /// <remarks>
        /// The run holds the name's parts, then the arguments as a list, empty where there are none, then the names.
        /// </remarks>
        Value Referenced(Value member)
        {
            var failures = _failures;
            var arguments = Value.Listed(0, 0);
            if (_token.Kind == TokenKind.VectorOpen)
            {
                arguments = Arguments();
                if (_failures > failures)
                    return Value.None;
            }

            if (_token.Kind != TokenKind.Keyword || _token.Keyword != Keyword.On)
            {
                Fail("expected 'on' and a list of names after the arguments.");
                return Value.None;
            }

            Step();
            if (_token.Kind != TokenKind.ListOpen)
            {
                Fail("expected a list of names after 'on'.");
                return Value.None;
            }

            var names = List();
            if (_failures > failures)
                return Value.None;

            // A qualified name's parts are already the last of the run unless an argument wrote parts of its own after them,
            // so they are copied only then.
            var parts = _reading.Parts;
            var from = parts.Count;
            if (member.Form != ValueForm.Qualified)
                parts.Add(member);
            else if (member.From + member.Count == parts.Count)
                from = member.From;
            else
            {
                for (var i = 0; i < member.Count; i++)
                    parts.Add(parts[member.From + i]);
            }

            parts.Add(arguments);
            parts.Add(names);
            return Value.Referenced(from, parts.Count - from);
        }

        /// <summary>A vector, its three numbers written into the reading's shared run.</summary>
        Value Vector()
        {
            Step();

            var items = _reading.Items;
            var from = items.Count;
            for (var i = 0; i < 3; i++)
            {
                if (_token.Kind == TokenKind.Integer)
                    items.Add(new Value(ValueForm.Integer, null, _token.Whole));
                else if (_token.Kind == TokenKind.Real)
                    items.Add(new Value(ValueForm.Real, null, 0, _token.Real));
                else
                {
                    Fail("expected three numbers in the vector.");
                    return Value.None;
                }

                Step();
            }

            if (_token.Kind != TokenKind.VectorClose)
            {
                Fail("expected three numbers in the vector.");
                return Value.None;
            }

            Step();
            return Value.Vectored(from);
        }

        /// <summary>
        /// Annotation text with its empty first and last lines taken off, so the form round-trips.
        /// </summary>
        static string Trim(string note)
        {
            var from = 0;
            var to = note.Length;

            while (from < to && note[from] == '\n')
                from++;

            while (to > from && note[to - 1] == '\n')
                to--;

            return from == 0 && to == note.Length ? note : note.Substring(from, to - from);
        }
    }
}
