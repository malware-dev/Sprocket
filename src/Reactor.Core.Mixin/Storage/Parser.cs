using System.Collections.Generic;

namespace Reactor.Storage
{
    /// <summary>
    /// Reads tokens into statements.
    /// </summary>
    /// <remarks>
    /// <b>Trouble is recorded, not thrown.</b> The script is the only copy of somebody's program, so
    /// losing the line that is wrong and keeping the rest is worth far more than a clean failure -
    /// and whether the program is then safe to write back over is the caller's decision, which is why
    /// <see cref="Trouble" /> is an answer rather than an exception.
    /// <para>
    /// Recovery is to the end of the line. Braces are still counted through it, so one bad step does
    /// not tip every following statement out of its system.
    /// </para>
    /// </remarks>
    sealed class Parser
    {
        readonly Lexer _lexer;
        Token _token;

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
                Fail(_token.Word);
        }

        void Fail(string what)
        {
            if (Trouble.Length == 0)
                Trouble = "line " + _token.Line + ": " + what;
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
                    note = note.Length == 0 ? _token.Word : note + "\n" + _token.Word;
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
        /// There is no state where somebody's words exist and nobody knows what they are about, so
        /// this is trouble rather than something to drop quietly.
        /// </remarks>
        void Orphan(string note)
        {
            if (note.Length > 0)
                Fail("an annotation with nothing under it");
        }

        Statement One(string note)
        {
            if (_token.Kind != TokenKind.Identifier && _token.Kind != TokenKind.Text)
            {
                Fail("a statement starts with a word");
                Recover();
                return null;
            }

            var head = _token.Word;
            var quoted = _token.Kind == TokenKind.Text;
            var values = new List<Value>();
            var wasWrong = Trouble.Length > 0;

            Step();
            while (Valuish())
                values.Add(Next());

            // A line that went wrong is dropped whole rather than half-read, because half a statement
            // is a step with no gate or a trigger with no sources, which reads as a change somebody made.
            if (!wasWrong && Trouble.Length > 0)
            {
                Recover();
                return null;
            }

            var body = _token.Kind == TokenKind.BlockOpen ? new List<Statement>() : null;
            var statement = new Statement(head, quoted, body);
            statement.Annotation = Trim(note);
            for (var i = 0; i < values.Count; i++)
                statement.Add(values[i]);

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
                case TokenKind.Text:
                case TokenKind.Integer:
                case TokenKind.Real:
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
                    return new Value(ValueForm.Identifier, token.Word);

                case TokenKind.Text:
                    Step();
                    return new Value(ValueForm.Text, token.Word);

                case TokenKind.Integer:
                    Step();
                    return new Value(ValueForm.Integer, null, token.Whole);

                case TokenKind.Real:
                    Step();
                    return new Value(ValueForm.Real, null, 0, token.Real);

                case TokenKind.Color:
                    Step();
                    return new Value(ValueForm.Color, null, token.Whole);

                case TokenKind.ListOpen:
                    return List();

                default:
                    return Vector();
            }
        }

        Value List()
        {
            var items = new List<Value>();
            Step();

            while (_token.Kind != TokenKind.ListClose)
            {
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
            return new Value(ValueForm.List, null, 0, 0, items);
        }

        Value Vector()
        {
            var items = new List<Value>();
            Step();

            while (items.Count < 3)
            {
                if (_token.Kind != TokenKind.Integer && _token.Kind != TokenKind.Real)
                {
                    Fail("a vector is three numbers in brackets");
                    return Value.None;
                }

                items.Add(Next());
            }

            if (_token.Kind != TokenKind.VectorClose)
            {
                Fail("a vector is three numbers in brackets");
                return Value.None;
            }

            Step();
            return new Value(ValueForm.Vector, null, 0, 0, items);
        }

        /// <summary>
        /// Annotation text with its empty first and last lines taken off, so the form round-trips.
        /// </summary>
        /// <remarks>
        /// A bare hash is an empty paragraph line, which is how spacing works inside a note without a
        /// blank line breaking the attachment. Leading and trailing ones carry nothing, and keeping
        /// them would mean a note growing a line every time it was written out and read back.
        /// </remarks>
        static string Trim(string note)
        {
            var from = 0;
            var to = note.Length;

            while (from < to && note[from] == '\n')
                from++;

            while (to > from && note[to - 1] == '\n')
                to--;

            return note.Substring(from, to - from);
        }
    }
}
