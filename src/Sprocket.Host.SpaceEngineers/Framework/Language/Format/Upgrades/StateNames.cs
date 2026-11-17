using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>
    /// Version 2 alpha 1: a bare name that could be a state or a choice is no longer read as the state, and a state may be
    /// written <c>states.home</c>. A program written before it gets every state it sets a state to or compares one with
    /// written so, as it read them, <c>set where to home</c> becoming <c>set where to states.home</c>.
    /// </summary>
    public sealed class StateNames : FormatChange
    {
        /// <inheritdoc />
        public override int Arrived => 1;

        /// <inheritdoc />
        public override void Statements(List<Statement> constructs)
        {
            foreach (var construct in constructs)
            {
                if (construct.Key != Keyword.System)
                    continue;

                var states = new HashSet<string>();
                foreach (var inner in construct.Body)
                {
                    if (inner.Key == Keyword.Let && ScriptQuotes.Named(inner.ValueAt(0)))
                        states.Add(inner.Word(0));
                }

                if (states.Count > 0)
                    Qualified(construct.Body, states);
            }
        }

        /// <summary>Writes qualified every state of <paramref name="states" /> that <paramref name="statements" />, and the statements in their blocks, give a state or compare one with.</summary>
        static void Qualified(IList<Statement> statements, ICollection<string> states)
        {
            foreach (var statement in statements)
            {
                // set where to home, on its own line or as what a trigger does.
                if (statement.Key == Keyword.Set)
                    Given(statement, 0, states);

                for (var at = 0; at < statement.Count; at++)
                {
                    if (statement.Says(at, Keyword.Set))
                        Given(statement, at + 1, states);

                    // where == home, after an if, an else if or a when.
                    var relation = statement.ValueAt(at + 1);
                    if (State(statement, at, states) && (relation.Says(Keyword.EqualTo) || relation.Says(Keyword.NotEqualTo)) && State(statement, at + 2, states))
                        statement.Qualify(at + 2, Namespaces.States);
                }

                Qualified(statement.Body, states);
            }
        }

        /// <summary>Writes qualified the state that the state at <paramref name="at" /> is set to, after <c>to</c>.</summary>
        static void Given(Statement statement, int at, ICollection<string> states)
        {
            if (State(statement, at, states) && statement.Says(at + 1, Keyword.To) && State(statement, at + 2, states))
                statement.Qualify(at + 2, Namespaces.States);
        }

        /// <summary>Whether the value at <paramref name="at" /> is one of <paramref name="states" />, written bare.</summary>
        static bool State(Statement statement, int at, ICollection<string> states)
        {
            var value = statement.ValueAt(at);
            return ScriptQuotes.Named(value) && states.Contains(value.Word);
        }
    }
}
