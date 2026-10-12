using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Reads tokens into statements.
    /// </summary>
    /// <remarks>
    /// <b>Trouble is recorded, not thrown.</b> The script is the only copy of somebody's program, so
    /// what is wrong with it is an answer to show them, with the line it is on, rather than an exception -
    /// see <see cref="Trouble" />.
    /// <para>
    /// Recovery is to the end of the line. Braces are still counted through it, so the rest of the script
    /// is read in its right place and the first trouble reported is the one that matters.
    /// </para>
    /// </remarks>
    public sealed class Parser
    {
        /// <summary>The word a duration in seconds carries, where one in ticks carries none.</summary>
        public const string Seconds = "s";

        readonly Lexer _lexer;
        readonly Reading _reading = new Reading();

        /// <summary>
        /// A list's items as they are read, one buffer for each list open at once.
        /// </summary>
        /// <remarks>
        /// Kept aside until the list closes, because a list or a vector inside it writes its own run
        /// into <see cref="Reading.Items" /> first: held back, the outer list's items still land in one
        /// unbroken run of their own. Kept for the life of the reading rather than made per list, since
        /// a program is mostly lists and loading one lands inside a world load.
        /// </remarks>
        readonly List<List<Value>> _held = new List<List<Value>>();

        /// <summary>How many lists are open, which is the buffer the next one reads into.</summary>
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
        /// <remarks>
        /// <c>on</c> alone is not enough, since a name followed by a flag is written that way too: the reading a run left
        /// behind says <c>door1 on</c>.
        /// </remarks>
        bool Referencing()
        {
            if (_token.Kind == TokenKind.VectorOpen)
                return true;

            return _token.Kind == TokenKind.Keyword && _token.Keyword == Keyword.On && Peek().Kind == TokenKind.ListOpen;
        }

        /// <summary>
        /// Counts something that went wrong, and says where the first one was.
        /// </summary>
        /// <remarks>
        /// The line is worked out here, by counting from the start, rather than kept up to date as the text
        /// is read. Only the first trouble is ever reported, so a program that reads cleanly - which is nearly
        /// all of them - never pays for knowing which line anything was on.
        /// </remarks>
        void Fail(string what)
        {
            _failures++;
            if (Trouble.Length == 0)
                Trouble = "Line " + new TextPtr(_lexer.Script, _token.At).FindLineNo() + ": " + what;
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
            while (true)
            {
                if (_token.Kind == TokenKind.End)
                {
                    if (nested)
                        Fail("missing '}'.");

                    Orphan(note);
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

                    Orphan(note);
                    Step();
                    EndOfLine();
                    return;
                }

                if (_token.Kind == TokenKind.Newline)
                {
                    // A blank line separates constructs, and is what makes an annotation an orphan.
                    Orphan(note);
                    note = "";
                    Step();
                    continue;
                }

                if (_token.Kind == TokenKind.Annotation)
                {
                    var line = _token.Text.ToString();
                    note = note.Length == 0 ? line : note + "\n" + line;
                    Step();
                    EndOfLine();
                    continue;
                }

                if (_token.Kind == TokenKind.Bad)
                {
                    Recover();
                    note = "";
                    continue;
                }

                // A line starting //: the statement after it is read as any other, and marked off.
                var off = _token.Kind == TokenKind.Off;
                if (off)
                    Step();

                var statement = One(note);
                note = "";
                if (statement == null)
                    continue;

                statement.IsOff = off;
                into.Add(statement);
            }
        }

        /// <summary>
        /// Annotation text with nothing to belong to.
        /// </summary>
        /// <remarks>
        /// There is no state where somebody's words exist and nobody knows what they are about, so this is
        /// trouble rather than something to drop quietly.
        /// </remarks>
        void Orphan(string note)
        {
            if (note.Length > 0)
                Fail("expected a statement under the annotation.");
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

            while (true)
            {
                if (_token.Kind == TokenKind.Comma)
                {
                    Fail("unexpected ','.");
                    break;
                }

                if (_token.Kind == TokenKind.Off)
                {
                    Fail("unexpected '//'; it only starts a line.");
                    break;
                }

                if (!Valuish())
                    break;

                values.Add(Next());
            }

            // A line that went wrong is not made a statement at all, since the trouble already says what is wrong
            // with it and half a statement would only be judged again for what is missing.
            if (_failures > failures)
            {
                Recover();
                return null;
            }

            var body = _token.Kind == TokenKind.BlockOpen ? new List<Statement>() : null;
            var count = values.Count - from;
            var statement = new Statement(_reading, at, key, head, from, count, body);

            statement.Annotation = Trim(note);

            if (body == null)
            {
                EndOfLine();
                return statement;
            }

            // Only a system, a sequence, an instance, an if and an else hold statements. The body is still read, so the braces
            // stay counted.
            if (key != Keyword.System && key != Keyword.Sequence && key != Keyword.Instance && key != Keyword.If
                && key != Keyword.Else)
                Fail("unexpected '{'.");

            Step();
            EndOfLine();
            Constructs(body, true);
            return statement;
        }

        bool Valuish()
        {
            switch (_token.Kind)
            {
                case TokenKind.Identifier:
                case TokenKind.Keyword:
                case TokenKind.Equals:
                case TokenKind.Comparison:
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
                        Fail("unexpected '" + Keywords.Name(token.Keyword) + "' before '.'; a name spelled like a keyword is quoted.");
                        return Value.None;
                    }

                    return new Value(ValueForm.Keyword, null, 0, 0, token.Keyword);

                case TokenKind.Equals:
                    Step();
                    return new Value(ValueForm.Keyword, null, 0, 0, Keyword.Equals);

                case TokenKind.Comparison:
                    Step();
                    return new Value(ValueForm.Keyword, null, 0, 0, token.Keyword);

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
        /// <remarks>
        /// A list may hold lists and vectors, each of which is a run of its own: see <see cref="_held" />.
        /// </remarks>
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
                    Fail("expected a value or " + closing + ".");
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
                Fail("expected " + closing + ".");
                return Value.None;
            }

            Step();

            var items = _reading.Items;
            var from = items.Count;
            for (var i = 0; i < held.Count; i++)
                items.Add(held[i]);

            return Value.Listed(from, held.Count);
        }

        /// <summary>
        /// A name in parts, <c>MalTek.Airlock</c>, having read its first part already.
        /// </summary>
        /// <remarks>
        /// The dot is an operator, so spaces around it are nothing and <c>'MalTek Inc.' . Airlock</c> is the
        /// same name as <c>'MalTek Inc.'.Airlock</c>. Any part may be a keyword written bare, since only a name can stand
        /// beside a dot, and its text is its part.
        /// </remarks>
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
        /// One value, because a member is never written apart from its blocks. Which members there are, and what each takes,
        /// is the host's, so the arguments are read as whatever they are and judged by whoever reads the statement. The run
        /// is written once everything is read, because an argument may write a run of its own first: the name's parts, then
        /// the arguments as a list, empty where there are none, then the names.
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
        /// <remarks>
        /// A bare hash is an empty paragraph line, which is how spacing works inside a note without a blank
        /// line breaking the attachment. Leading and trailing ones carry nothing, and keeping them would mean
        /// a note growing a line every time it was written out and read back.
        /// </remarks>
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
