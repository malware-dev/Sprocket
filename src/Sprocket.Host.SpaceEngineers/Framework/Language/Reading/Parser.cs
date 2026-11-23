using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
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

        // How loosely binding an operator the value being read takes in: every operator, or only arithmetic.
        const int Every = 1;

        const int Arithmetic = 4;

        const int Comparisons = 3;

        // The head and first value of the statement being read, which say where a value may combine readings.
        Keyword _key;

        int _from;

        int _floor = Arithmetic;

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

            return _token.Keyword == Keyword.On && Peek().Kind == TokenKind.ListOpen;
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
                if (!nested && into.Count == 1)
                    Versioned(statement);
            }
        }

        /// <summary>
        /// Has the rest of the script read as the format <paramref name="first" /> names, where it is the version line: one
        /// written for a format earlier than the first with contextual keywords reads as that format did.
        /// </summary>
        /// <remarks>
        /// Only the next line's first token is read already, and it starts a statement or is none, which reads the same either way.
        /// </remarks>
        void Versioned(Statement first)
        {
            if (first.Key != Keyword.None || first.Head != ScriptFormat.Header || first.ValueAt(0).Form != ValueForm.Integer)
                return;

            var version = first.Integer(0);
            var draft = first.Says(1, Keyword.Alpha) ? first.Integer(2) : int.MaxValue;
            _lexer.Earlier = version < Keywords.ContextualVersion || (version == Keywords.ContextualVersion && draft < Keywords.ContextualDraft);
            _reading.Earlier = _lexer.Earlier;
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
                Fail($"unexpected '{Keywords.Name(_token.Keyword)}' as an attribute's name; a name spelled like a reserved keyword is quoted.");
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

            // A head spelled like a contextual keyword is a name: no statement starts with one.
            var at = _token.At;
            var key = kind == TokenKind.Keyword ? _token.Keyword : Keyword.None;
            var head = kind == TokenKind.Keyword ? "" : _token.Text.ToString();
            var failures = _failures;
            var values = _reading.Values;
            var from = values.Count;
            _key = key;
            _from = from;

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

                // A value a statement takes, and a condition written as an expression, take every operator; any other value
                // only arithmetic, as a comparison there is a part of the statement.
                var condition = Conditions(values) && ReadsAsExpression();
                var combine = condition || Combines(values);
                values.Add(Floored(combine ? Every : Arithmetic, () => Next(combine)));
            }
        }

        /// <summary><paramref name="read" />, done with the loosest operator taken in at <paramref name="floor" />, which is set back after.</summary>
        Value Floored(int floor, System.Func<Value> read)
        {
            var was = _floor;
            _floor = floor;
            var value = read();
            _floor = was;
            return value;
        }

        /// <summary>Whether the value to be read next, after the ones in <paramref name="values" />, starts what an <c>if</c>, an <c>else if</c> or a <c>when</c> tests.</summary>
        bool Conditions(ValueChunks values)
        {
            if (_key == Keyword.If || _key == Keyword.When)
                return values.Count == _from;

            return _key == Keyword.Else && values.Count == _from + 1 && values[_from].Keyword == Keyword.If;
        }

        /// <summary>Whether the condition starting at the token now standing is read as one expression, as <see cref="ConditionShape.Whole" /> says.</summary>
        /// <remarks>Reads ahead to the end of the condition on a copy of the lexer, which moves nothing the parser reads.</remarks>
        bool ReadsAsExpression()
        {
            var ahead = _lexer.Ahead();
            return ConditionShape.Whole(_token, _looked ? _ahead : ahead.Next(), ahead);
        }

        /// <summary>Whether <paramref name="token" /> starts a line of a system's header: <c>changed</c> or <c>from</c>.</summary>
        static bool Clause(Token token)
        {
            return token.Keyword == Keyword.Changed || token.Keyword == Keyword.From;
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
                case TokenKind.Minus:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether the value to be read next in the statement, after the ones in <paramref name="values" />, takes a value of a step
        /// or a test: after <c>to</c> or a comparison, as the length of a <c>wait</c> or after <c>every</c>, or as an abort's message.
        /// </summary>
        bool Combines(ValueChunks values)
        {
            if (values.Count == _from)
                return _key == Keyword.Wait || _key == Keyword.Every;

            var previous = values[values.Count - 1];
            if (previous.Form != ValueForm.Keyword && previous.Form != ValueForm.Identifier)
                return false;

            if (previous.Form == ValueForm.Identifier && previous.Keyword == Keyword.None && previous.Word == AbortWords.Saying)
                return true;

            switch (previous.Keyword)
            {
                case Keyword.To:
                case Keyword.EqualTo:
                case Keyword.NotEqualTo:
                case Keyword.Above:
                case Keyword.AtLeast:
                case Keyword.Below:
                case Keyword.AtMost:
                case Keyword.Every:
                    return true;
            }

            return false;
        }

        /// <summary>One value, or the expression it starts where an operator follows it.</summary>
        /// <remarks>
        /// <paramref name="combine" /> says whether <c>sum</c>, <c>average</c>, <c>min</c> and <c>max</c> before a member combine its readings, which they do only
        /// where a statement takes a value, and are names anywhere else.
        /// </remarks>
        Value Next(bool combine = true)
        {
            var failures = _failures;
            var first = Operand(combine);
            if (_failures > failures || !Operates(first))
                return first;

            var terms = new List<Value> { first };
            Climb(terms, _floor);
            return Expressed(terms);
        }

        /// <summary>Whether <paramref name="token" /> is an operator the value being read takes in.</summary>
        bool IsOperator(Token token)
        {
            var precedence = Precedence(token);
            return precedence > 0 && precedence >= _floor;
        }

        /// <summary>
        /// How tightly the operator <paramref name="token" /> binds: 5 for <c>*</c> and <c>/</c>, 4 for <c>+</c> and <c>-</c>, 3 for
        /// a comparison, 2 for <c>and</c>, 1 for <c>or</c>, and 0 where it is none.
        /// </summary>
        static int Precedence(Token token)
        {
            switch (token.Kind)
            {
                case TokenKind.Star:
                case TokenKind.Slash:
                    return 5;
                case TokenKind.Plus:
                case TokenKind.Minus:
                    return 4;
                case TokenKind.Comparison:
                    return Comparisons;
            }

            if (!ConditionShape.Joins(token))
                return 0;

            return token.Text.ToString() == ExpressionWords.And ? 2 : 1;
        }

        static string Spelling(Token token)
        {
            switch (token.Kind)
            {
                case TokenKind.Star: return ExpressionWords.Multiply;
                case TokenKind.Slash: return ExpressionWords.Divide;
                case TokenKind.Plus: return ExpressionWords.Add;
                case TokenKind.Minus: return ExpressionWords.Subtract;
                case TokenKind.Comparison: return Keywords.Name(token.Keyword);
            }

            return token.Text.ToString();
        }

        /// <summary>
        /// Whether the operator now standing joins <paramref name="first" /> to another operand: it is a value that can be one,
        /// and no word that only separates the parts of a statement, such as <c>to</c>; a name spelled like a combining word is one.
        /// </summary>
        bool Operates(Value first)
        {
            if (!IsOperator(_token))
                return false;

            switch (first.Form)
            {
                case ValueForm.Identifier:
                    return first.Keyword == Keyword.None || first.Keyword == Keyword.Sum || first.Keyword == Keyword.Average
                        || first.Keyword == Keyword.Min || first.Keyword == Keyword.Max;
                case ValueForm.Qualified:
                case ValueForm.Reference:
                case ValueForm.Kinded:
                case ValueForm.Integer:
                case ValueForm.Real:
                case ValueForm.Duration:
                case ValueForm.Text:
                case ValueForm.Color:
                case ValueForm.Expression:
                    return true;
            }

            return false;
        }

        /// <summary>Reads operators and the operands after them into <paramref name="terms" />, postfix, those binding at least as tightly as <paramref name="least" />.</summary>
        void Climb(List<Value> terms, int least)
        {
            while (true)
            {
                var precedence = Precedence(_token);
                if (precedence == 0 || precedence < least || precedence < _floor)
                    return;

                var op = _token;
                Step();
                var failures = _failures;
                if (!Valuish())
                {
                    Fail("expected a value after the operator.");
                    return;
                }

                terms.Add(Operand(true));
                if (_failures > failures)
                    return;

                // What binds tighter than this operator belongs to the operand just read.
                Climb(terms, precedence + 1);
                if (_failures > failures)
                    return;

                terms.Add(Value.Operating(Spelling(op), 2));
                if (precedence == Comparisons && _token.Kind == TokenKind.Comparison)
                {
                    Fail("comparisons do not chain; join two of them with and, like a < b and b < c.");
                    return;
                }
            }
        }

        /// <summary>The terms <paramref name="terms" />, written into the reading's shared run, as one expression.</summary>
        Value Expressed(List<Value> terms)
        {
            var all = _reading.Terms;
            var from = all.Count;
            foreach (var term in terms)
                all.Add(term);

            return Value.Expressed(from, terms.Count);
        }

        /// <summary>The negative of the operand after a <c>-</c>: a number written apart from its dash is the number negated.</summary>
        Value Negated()
        {
            Step();
            var failures = _failures;
            if (!Valuish())
            {
                Fail("expected a value after '-'.");
                return Value.None;
            }

            var operand = Operand(true);
            if (_failures > failures)
                return Value.None;

            if (operand.Form == ValueForm.Integer)
                return new Value(ValueForm.Integer, null, -operand.Integer);

            if (operand.Form == ValueForm.Real)
                return new Value(ValueForm.Real, null, 0, -operand.Real);

            return Expressed(new List<Value> { operand, Value.Operating(ExpressionWords.Negate, 1) });
        }

        /// <summary>The other value of the flag after a <c>not</c>.</summary>
        Value Inverted()
        {
            Step();
            var failures = _failures;
            if (!Valuish())
            {
                Fail("expected a value after 'not'.");
                return Value.None;
            }

            var operand = Operand(true);
            return _failures > failures ? Value.None : Expressed(new List<Value> { operand, Value.Operating(ExpressionWords.Not, 1) });
        }

        /// <summary>Whether the word <paramref name="token" /> checks a comparison on the blocks of the reading after it: <c>any</c> or <c>all</c> before a member.</summary>
        bool Checking(Token token)
        {
            if (token.Keyword != Keyword.Any && token.Keyword != Keyword.All)
                return false;

            var after = Peek();
            return after.Kind == TokenKind.Identifier && after.Keyword != Keyword.On;
        }

        /// <summary>
        /// <c>any gearbox.status on [ doors ] == open</c>, from its word: the reading, the value compared with, then one term
        /// saying the word and the comparison.
        /// </summary>
        /// <remarks>The value compared with takes arithmetic only, so an <c>and</c> after it joins the whole check.</remarks>
        Value Checked(Token token)
        {
            var word = Keywords.Name(token.Keyword);
            Step();
            var failures = _failures;
            var member = Operand(true);
            if (_failures > failures)
                return Value.None;

            if (member.Form != ValueForm.Reference)
            {
                Fail($"expected a member on a list of names after '{word}', like {word} gearbox.status on [ doors ] == open.");
                return Value.None;
            }

            if (_token.Kind != TokenKind.Comparison)
            {
                Fail($"expected a comparison and a value after the names, like {word} gearbox.status on [ doors ] == open.");
                return Value.None;
            }

            var relation = _token.Keyword;
            Step();
            if (!Valuish())
            {
                Fail("expected a value after the comparison.");
                return Value.None;
            }

            var compared = Floored(Arithmetic, () => Next());
            if (_failures > failures)
                return Value.None;

            return Expressed(new List<Value> { member, compared, Value.Operating(ExpressionWords.Checking(word, Keywords.Name(relation)), 2) });
        }

        /// <summary>Whether the word <paramref name="token" /> combines the readings that follow it: <c>sum</c>, <c>average</c>, <c>min</c> or <c>max</c> before a member.</summary>
        bool Combining(Token token)
        {
            if (token.Keyword != Keyword.Sum && token.Keyword != Keyword.Average && token.Keyword != Keyword.Min && token.Keyword != Keyword.Max)
                return false;

            var after = Peek();
            return after.Kind == TokenKind.Identifier && after.Keyword != Keyword.On;
        }

        /// <summary><c>sum gearbox.fill on [ tanks ]</c>, from its combining word: the reading, then the word.</summary>
        Value Combined(Token token)
        {
            var word = Keywords.Name(token.Keyword);
            Step();
            var failures = _failures;
            var member = Operand(true);
            if (_failures > failures)
                return Value.None;

            if (member.Form != ValueForm.Reference)
            {
                Fail($"expected a member on a list of names after '{word}', like {word} gearbox.fill on [ tanks ].");
                return Value.None;
            }

            return Expressed(new List<Value> { member, Value.Operating(word) });
        }

        /// <summary>One operand: a value of any form, without the operators that may follow it.</summary>
        Value Operand(bool combine)
        {
            var token = _token;
            switch (token.Kind)
            {
                case TokenKind.Minus:
                    return Negated();

                case TokenKind.Identifier:
                    if (_floor <= Comparisons && Checking(token))
                        return Checked(token);

                    if (combine && Combining(token))
                        return Combined(token);

                    Step();
                    var name = Named(token);
                    if (_token.Kind == TokenKind.Dot)
                    {
                        var failures = _failures;
                        name = Qualified(name);
                        if (_failures > failures)
                            return Value.None;
                    }

                    // A <c>(</c> after <c>to</c> opens a group: <c>to</c> is no function.
                    if (_token.Kind == TokenKind.VectorOpen && token.Keyword != Keyword.To)
                        return Called(name);

                    return token.Keyword != Keyword.To && Referencing() ? Referenced(name, Value.Listed(0, 0)) : name;

                case TokenKind.Keyword:
                    if (_floor <= Comparisons && token.Keyword == Keyword.Not)
                        return Inverted();

                    Step();

                    // A name spelled like a reserved keyword is quoted, beside a dot as anywhere: sprocket.'every'.
                    if (_token.Kind == TokenKind.Dot)
                    {
                        Fail($"unexpected '{Keywords.Name(token.Keyword)}' before '.'; a name spelled like a reserved keyword is quoted.");
                        return Value.None;
                    }

                    var keyword = new Value(ValueForm.Keyword, null, 0, 0, token.Keyword);
                    if (_token.Kind == TokenKind.VectorOpen && Kinds.IsKeyword(token.Keyword))
                    {
                        var failures = _failures;
                        var arguments = Arguments();
                        return _failures > failures ? Value.None : Kinded(keyword, arguments);
                    }

                    return keyword;

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
                    return new Value(ValueForm.Integer, null, token.Integer);

                case TokenKind.Real:
                    Step();
                    return new Value(ValueForm.Real, null, 0, token.Real);

                case TokenKind.Duration:
                    Step();
                    return new Value(ValueForm.Duration, token.Text[0] == 's' ? Seconds : null, token.Integer, token.Real);

                case TokenKind.Color:
                    Step();
                    return new Value(ValueForm.Color, null, token.Integer);

                case TokenKind.ListOpen:
                    return List();

                default:
                    return Group();
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

                held.Add(Floored(Every, () => Next()));
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
                    parts.Add(Named(_token));
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
        /// A member and the blocks it is read on or done to, <c>sprocket.amount("Ingot/Iron") on [ cargo ]</c>, having read the
        /// member's name already.
        /// </summary>
        /// <remarks>
        /// The run holds the name's parts, then the arguments as a list, empty where there are none, then the names.
        /// </remarks>
        Value Referenced(Value member, Value arguments)
        {
            var failures = _failures;
            if (_token.Keyword != Keyword.On)
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

        /// <summary>
        /// <c>name(...)</c>, having read the name: a member and its arguments where <c>on</c> and its names follow, or else a
        /// thing with the name as its kind, <c>side(inside)</c>.
        /// </summary>
        Value Called(Value name)
        {
            var failures = _failures;
            var arguments = Arguments();
            if (_failures > failures)
                return Value.None;

            if (_token.Keyword == Keyword.On)
                return Referenced(name, arguments);

            var term = name.Form == ValueForm.Identifier ? ExpressionWords.FunctionTerm(name.Word) : null;
            return term != null ? Function(name, term, arguments) : Kinded(name, arguments);
        }

        /// <summary><c>min(a, b)</c>, <c>max(a, b)</c>, <c>round(a)</c> or <c>abs(a)</c>, having read the arguments: they, then the function.</summary>
        Value Function(Value name, string term, Value arguments)
        {
            var most = term == ExpressionWords.RoundFunction || term == ExpressionWords.AbsFunction;
            if (most ? arguments.Count != 1 : arguments.Count < 2)
            {
                Fail(most ? $"expected one argument for {name.Word}." : $"expected two or more arguments for {name.Word}.");
                return Value.None;
            }

            var terms = new List<Value>(arguments.Count + 1);
            for (var i = 0; i < arguments.Count; i++)
                terms.Add(_reading.Items[arguments.From + i]);

            terms.Add(Value.Operating(term, arguments.Count));
            return Expressed(terms);
        }

        /// <summary>The name <paramref name="token" />, an identifier, spells, remembering the contextual keyword a bare one is spelled as.</summary>
        /// <remarks>A name spelled like a keyword takes the keyword's own spelling, so it makes no string.</remarks>
        static Value Named(Token token)
        {
            return token.Keyword != Keyword.None
                ? new Value(ValueForm.Identifier, null, 0, 0, token.Keyword)
                : new Value(ValueForm.Identifier, token.Text.ToString());
        }

        /// <summary>
        /// A thing with its kind, <paramref name="kind" />, the thing the one value of <paramref name="arguments" />, and any
        /// kinds written after it: <c>type(side)(inside)</c>.
        /// </summary>
        Value Kinded(Value kind, Value arguments)
        {
            while (true)
            {
                if (arguments.Count != 1)
                {
                    Fail("expected one value in a kind's parentheses, like real(4), or 'on' and a list of names after a member's arguments.");
                    return Value.None;
                }

                var parts = _reading.Parts;
                var from = parts.Count;
                parts.Add(kind);
                parts.Add(_reading.Items[arguments.From]);
                kind = Value.Kinded(from);
                if (_token.Kind != TokenKind.VectorOpen)
                    return kind;

                var failures = _failures;
                arguments = Arguments();
                if (_failures > failures)
                    return Value.None;
            }
        }

        /// <summary>
        /// Past a <c>(</c>: a vector, where it holds numbers with no operator between the first two, <c>(3 0 -2)</c>, and else
        /// the expression it groups.
        /// </summary>
        Value Group()
        {
            Step();
            if ((_token.Kind == TokenKind.Integer || _token.Kind == TokenKind.Real)
                && (Peek().Kind == TokenKind.Integer || Peek().Kind == TokenKind.Real))
                return Vector();

            var failures = _failures;
            if (!Valuish())
            {
                Fail("expected three numbers in the vector, or a value to group.");
                return Value.None;
            }

            var inner = Floored(Every, () => Next());
            if (_failures > failures)
                return Value.None;

            if (_token.Kind != TokenKind.VectorClose)
            {
                Fail("expected ')'.");
                return Value.None;
            }

            Step();
            return inner;
        }

        /// <summary>A vector, its three numbers written into the reading's shared run, having read its <c>(</c>.</summary>
        Value Vector()
        {
            var items = _reading.Items;
            var from = items.Count;
            for (var i = 0; i < 3; i++)
            {
                if (_token.Kind == TokenKind.Integer)
                    items.Add(new Value(ValueForm.Integer, null, _token.Integer));
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
