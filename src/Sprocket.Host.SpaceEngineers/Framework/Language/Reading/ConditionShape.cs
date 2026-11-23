namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// How the words an <c>if</c> or a <c>when</c> tests are read: as one expression, or as a test written as the statement's
    /// parts, which every condition of a format before expressions is.
    /// </summary>
    public static class ConditionShape
    {
        /// <summary>Whether <paramref name="condition" />, the words an <c>if</c> or a <c>when</c> tests, is read as one expression, as <see cref="Whole" /> says.</summary>
        /// <remarks>A condition that is not reads as a test written as the statement's parts, so a writer puts it in parentheses.</remarks>
        public static bool ReadsWhole(string condition)
        {
            var lexer = new Lexer(condition + " then");
            var first = lexer.Next();
            return Whole(first, lexer.Next(), lexer);
        }

        /// <summary>
        /// Whether the condition starting <paramref name="token" />, then <paramref name="next" />, read on by <paramref name="ahead" />,
        /// is one expression, which a test written as the statement's parts is not: it joins tests with <c>and</c>, <c>or</c> or
        /// <c>not</c>, opens with a parenthesis or a fixed value, compares other than once, works out a value before its
        /// comparison, reads a member with no <c>any</c>, <c>all</c> or combining word before it, or, where it starts with a
        /// state or a setting, compares it with more than one value.
        /// </summary>
        /// <remarks>Reads <paramref name="ahead" /> on to the end of the condition, so it is handed a lexer of its own.</remarks>
        public static bool Whole(Token token, Token next, Lexer ahead)
        {
            var leads = Leading(token, next);
            var opens = token.Kind == TokenKind.VectorOpen || token.Kind == TokenKind.Minus || token.Kind == TokenKind.Integer
                        || token.Kind == TokenKind.Real || token.Kind == TokenKind.Text || token.Kind == TokenKind.Duration
                        || token.Kind == TokenKind.Color;
            var depth = 0;
            var comparisons = 0;
            var worked = false;
            var member = false;
            var joined = false;

            // The parts after the comparison, a name and the name after each dot counting as one, and what is in parentheses
            // belonging to the part before it.
            var after = 0;
            var dotted = false;
            while (token.Kind != TokenKind.End && token.Kind != TokenKind.Newline && token.Kind != TokenKind.BlockOpen)
            {
                if (token.Kind == TokenKind.VectorOpen || token.Kind == TokenKind.ListOpen)
                    depth++;
                else if (token.Kind == TokenKind.VectorClose || token.Kind == TokenKind.ListClose)
                    depth--;
                else if (depth == 0)
                {
                    if (token.Keyword == Keyword.Then || token.Keyword == Keyword.Every || token.Keyword == Keyword.Is)
                        break;

                    if (token.Kind == TokenKind.Comparison)
                        comparisons++;
                    else if (Joins(token) || token.Keyword == Keyword.Not)
                        joined = true;
                    else if (comparisons == 0 && (token.Kind == TokenKind.Plus || token.Kind == TokenKind.Minus || token.Kind == TokenKind.Star
                                 || token.Kind == TokenKind.Slash))
                        worked = true;
                    else if (comparisons == 0 && token.Keyword == Keyword.On && next.Kind == TokenKind.ListOpen)
                        member = true;
                    else if (comparisons == 1 && token.Kind != TokenKind.Dot && !dotted)
                        after++;

                    dotted = token.Kind == TokenKind.Dot;
                }

                token = next;
                next = ahead.Next();
            }

            if (token.Keyword == Keyword.Is)
                return false;

            return joined || opens || comparisons != 1 || worked || member && !leads || after > 1 && !leads;
        }

        /// <summary>Whether <paramref name="token" /> is <c>and</c> or <c>or</c>, which join two flags.</summary>
        public static bool Joins(Token token)
        {
            if (token.Kind != TokenKind.Identifier || token.Keyword != Keyword.None)
                return false;

            var word = token.Text.ToString();
            return word == ExpressionWords.And || word == ExpressionWords.Or;
        }

        /// <summary>Whether a condition starting <paramref name="token" />, then <paramref name="next" />, starts with any, all or a combining word before a member.</summary>
        static bool Leading(Token token, Token next)
        {
            switch (token.Keyword)
            {
                case Keyword.Any:
                case Keyword.All:
                case Keyword.Sum:
                case Keyword.Average:
                    return true;
                case Keyword.Min:
                case Keyword.Max:
                    return next.Kind == TokenKind.Identifier;
            }

            return false;
        }
    }
}
