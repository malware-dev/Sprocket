using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Reads tokens into statements.
    /// </summary>
    /// <remarks>
    /// <b>Trouble is recorded, not thrown.</b> The script is the only copy of somebody's program, so
    /// losing the line that is wrong and keeping the rest is worth far more than a clean failure - and
    /// whether the program is then safe to write back over is the caller's decision, which is why
    /// <see cref="Trouble" /> is an answer rather than an exception.
    /// <para>
    /// Recovery is to the end of the line. Braces are still counted through it, so one bad step does not
    /// tip every following statement out of its system.
    /// </para>
    /// </remarks>
    sealed class Parser
    {
        readonly Lexer _lexer;
        readonly Reading _reading = new Reading();
        Token _token;

        int _failures;

        public Parser(string text)
        {
            _lexer = new Lexer(text);
            Step();
        }

        /// <summary>What was wrong with the first thing that was wrong, or empty.</summary>
        public string Trouble { get; private set; } = "";

        public List<Statement> Read()
        {
            var into = new List<Statement>();
            Constructs(into, false);
            return into;
        }

        void Step()
        {
            _token = _lexer.Next();
            if (_token.Kind == TokenKind.Bad)
                Fail(_token.Text.ToString());
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
                Trouble = "line " + new TextPtr(_lexer.Script, _token.At).FindLineNo() + ": " + what;
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

            Fail("a statement ends at the end of its line");
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
                        Fail("a block is never closed");

                    Orphan(note);
                    return;
                }

                if (_token.Kind == TokenKind.BlockClose)
                {
                    if (!nested)
                    {
                        Fail("a '}' that closes nothing");
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

                var statement = One(note);
                note = "";
                if (statement != null)
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
                Fail("an annotation with nothing under it");
        }

        Statement One(string note)
        {
            var kind = _token.Kind;
            if (kind != TokenKind.Identifier && kind != TokenKind.Keyword)
            {
                Fail("a statement starts with a word");
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
                    Fail("a comma separates the items of a list, and nothing else");
                    break;
                }

                if (!Valuish())
                    break;

                values.Add(Next());
            }

            // A line that went wrong is dropped whole rather than half-read, because half a statement is a
            // step with no gate or a trigger with no sources, which reads as a change somebody made.
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
                    return _token.Kind == TokenKind.Dot ? Qualified(name) : name;

                case TokenKind.Keyword:
                    Step();
                    return new Value(ValueForm.Keyword, null, 0, 0, token.Keyword);

                case TokenKind.Equals:
                    Step();
                    return new Value(ValueForm.Keyword, null, 0, 0, Keyword.Equals);

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
                    return new Value(ValueForm.Duration, null, token.Whole);

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
        /// A list, its items written straight into the reading's shared run.
        /// </summary>
        /// <remarks>
        /// <b>A list may not hold a list.</b> Nothing in the model has a use for one and nothing writes one,
        /// and refusing it is what keeps a list's items unbroken in <see cref="Reading.Items" />: a list
        /// inside a list would have its items land in the middle of its parent's.
        /// </remarks>
        Value List()
        {
            var items = _reading.Items;
            var from = items.Count;
            Step();

            while (_token.Kind != TokenKind.ListClose)
            {
                if (_token.Kind == TokenKind.ListOpen)
                {
                    Fail("a list cannot hold a list");
                    return Value.None;
                }

                if (!Valuish())
                {
                    Fail("a list holds values and ends with a ']'");
                    return Value.None;
                }

                items.Add(Next());
                if (_token.Kind != TokenKind.Comma)
                    break;

                Step();
            }

            if (_token.Kind != TokenKind.ListClose)
            {
                Fail("a list ends with a ']'");
                return Value.None;
            }

            Step();
            return Value.Listed(from, items.Count - from);
        }

        /// <summary>
        /// A name in parts, <c>MalTek.Airlock</c>, having read its first part already.
        /// </summary>
        /// <remarks>
        /// The dot is an operator, so spaces around it are nothing and <c>'MalTek Inc.' . Airlock</c> is the
        /// same name as <c>'MalTek Inc.'.Airlock</c>. A part after a dot may be a keyword written bare - the
        /// writer never produces one, but a name is still a name - and its text is its part.
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
                else if (_token.Kind == TokenKind.Keyword)
                    parts.Add(new Value(ValueForm.Identifier, Keywords.Name(_token.Keyword)));
                else
                {
                    Fail("a '.' is followed by the next part of a name");
                    return Value.None;
                }

                Step();
            }

            return Value.Qualified(from, parts.Count - from);
        }

        Value Vector()
        {
            Step();

            for (var i = 0; i < 3; i++)
            {
                if (_token.Kind != TokenKind.Integer && _token.Kind != TokenKind.Real)
                {
                    Fail("a vector is three numbers in brackets");
                    return Value.None;
                }

                Step();
            }

            if (_token.Kind != TokenKind.VectorClose)
            {
                Fail("a vector is three numbers in brackets");
                return Value.None;
            }

            Step();
            return new Value(ValueForm.Vector);
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
