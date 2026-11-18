using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>
    /// Version 2 alpha 1: a thing is written with its kind where alone it could mean more than one thing, and a choice is
    /// written with its type as a kind, <c>side(inside)</c> for <c>side.inside</c>. A program written before it gets each
    /// choice written so, and each name its system gives to more than one thing written with the kind it read as where it
    /// stands: a slot in a list of names, <c>slot(side)</c>; a sequence run or stopped, <c>sequence(cycle)</c>; a state set or
    /// compared, <c>state(where)</c>; and a choice, <c>side(inside)</c>.
    /// </summary>
    /// <remarks>
    /// A program written before it never gives a state a slot's or a type's name, and never has a setting, so the names that
    /// can be shared are a type's and a slot's, a sequence's and a choice's with any of them, and a type's with a type of block a slot expects, written <c>block_type(Door)</c>.
    /// </remarks>
    public sealed class KindNames : FormatChange
    {
        /// <inheritdoc />
        public override int Arrived => 1;

        /// <inheritdoc />
        public override void Statements(List<Statement> constructs)
        {
            foreach (var construct in constructs)
            {
                if (construct.Key == Keyword.System)
                    Upgraded(construct.Body, Names.Of(construct.Body));
            }
        }

        static void Upgraded(IList<Statement> statements, Names names)
        {
            foreach (var statement in statements)
            {
                if (statement.Key == Keyword.Let)
                    Valued(statement, 2, names.StartType(statement, 2), names);

                // set where to inside, on its own line or as what a trigger does.
                if (statement.Key == Keyword.Set)
                    Set(statement, 0, names);

                if (statement.Key == Keyword.Run || statement.Key == Keyword.Stop)
                    Run(statement, 0, names);

                // required slot hatch expects Door: in a program written before it, always a type of block.
                if ((statement.Key == Keyword.Required || statement.Key == Keyword.Optional) && statement.Says(2, Keyword.Expects)
                    && ScriptQuotes.Named(statement.ValueAt(3)) && names.Types.ContainsKey(statement.Word(3)))
                    statement.Kind(3, Kinds.BlockType);

                // whenever button then: a signal slot.
                if (statement.Key == Keyword.Whenever && ScriptQuotes.Named(statement.ValueAt(0)) && names.IsShared(statement.Word(0)))
                    statement.Kind(0, Keywords.Name(Keyword.Slot));

                for (var at = 0; at < statement.Count; at++)
                {
                    if (statement.Key != Keyword.Set && statement.Says(at, Keyword.Set))
                        Set(statement, at + 1, names);

                    if ((statement.Says(at, Keyword.Run) || statement.Says(at, Keyword.Stop)) && at > 0)
                        Run(statement, at + 1, names);

                    // where == home, after an if, an else if or a when.
                    var relation = statement.ValueAt(at + 1);
                    if (names.IsState(statement, at) && (relation.Says(Keyword.EqualTo) || relation.Says(Keyword.NotEqualTo)))
                    {
                        var state = statement.Word(at);
                        Valued(statement, at + 2, names.TypeOf(state), names);
                        Subject(statement, at, names);
                    }

                    Lists(statement, statement.ValueAt(at), names);
                }

                Upgraded(statement.Body, names);
            }
        }

        /// <summary><c>set where to inside</c> from <paramref name="at" />: the state set and what it is set to.</summary>
        static void Set(Statement statement, int at, Names names)
        {
            if (!names.IsState(statement, at) || !statement.Says(at + 1, Keyword.To))
                return;

            Valued(statement, at + 2, names.TypeOf(statement.Word(at)), names);
            Subject(statement, at, names);
        }

        /// <summary>The state at <paramref name="at" />, written <c>state(where)</c> where its name is shared.</summary>
        static void Subject(Statement statement, int at, Names names)
        {
            if (names.IsShared(statement.Word(at)))
                statement.Kind(at, Kinds.State);
        }

        /// <summary>The sequence run or stopped at <paramref name="at" />, written <c>sequence(cycle)</c> where its name is shared.</summary>
        static void Run(Statement statement, int at, Names names)
        {
            var named = statement.ValueAt(at);
            if (ScriptQuotes.Named(named) && names.IsShared(named.Word))
                statement.Kind(at, Keywords.Name(Keyword.Sequence));
        }

        /// <summary>
        /// The value at <paramref name="at" />, given to or compared with a state holding <paramref name="type" />: a choice
        /// written <c>side.inside</c> becomes <c>side(inside)</c>, and one written bare whose name is shared is written with its type.
        /// </summary>
        static void Valued(Statement statement, int at, string type, Names names)
        {
            var written = statement.ValueAt(at);
            if (written.Form == ValueForm.Qualified)
            {
                var parts = statement.PartsOf(written);
                if (parts.Count == 2 && names.Types.ContainsKey(parts[0].Word))
                    statement.Put(at, names.Choice(statement, parts[0].Word, parts[1]));

                return;
            }

            if (type == null || !ScriptQuotes.Named(written) || !names.IsShared(written.Word) || names.States.ContainsKey(written.Word))
                return;

            if (names.Types[type].Contains(written.Word))
                statement.Put(at, names.Choice(statement, type, written));
        }

        /// <summary>The slots of every list of names <paramref name="value" /> holds, a member's blocks and arguments, written <c>slot(side)</c> where shared.</summary>
        static void Lists(Statement statement, Value value, Names names)
        {
            if (value.Form != ValueForm.Reference)
                return;

            KindedSlots(statement, statement.NamesOf(value), names);
            var arguments = statement.Arguments(value);
            for (var i = 0; i < arguments.Count; i++)
            {
                if (arguments[i].Form == ValueForm.List)
                    KindedSlots(statement, arguments[i], names);
            }
        }

        /// <summary>Each slot in the list <paramref name="list" /> written <c>slot(side)</c> where its name is shared.</summary>
        static void KindedSlots(Statement statement, Value list, Names names)
        {
            var items = statement.Within(list);
            for (var i = 0; i < items.Count; i++)
            {
                if (ScriptQuotes.Named(items[i]) && names.Slots.Contains(items[i].Word) && names.IsShared(items[i].Word))
                    statement.KindWithin(list, i, Keywords.Name(Keyword.Slot));
            }
        }

        /// <summary>What a system's body names, by kind, as a program written before the change declares it.</summary>
        sealed class Names
        {
            readonly HashSet<string> _sequences = new HashSet<string>(StringComparer.Ordinal);

            /// <summary>The slots' names.</summary>
            public readonly HashSet<string> Slots = new HashSet<string>(StringComparer.Ordinal);

            /// <summary>The types, each with its choices.</summary>
            public readonly Dictionary<string, List<string>> Types = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            /// <summary>The states, each with its type, or null for a flag.</summary>
            public readonly Dictionary<string, string> States = new Dictionary<string, string>(StringComparer.Ordinal);

            public static Names Of(IList<Statement> body)
            {
                var names = new Names();
                foreach (var inner in body)
                {
                    if ((inner.Key == Keyword.Required || inner.Key == Keyword.Optional) && inner.Says(0, Keyword.Slot) && ScriptQuotes.Named(inner.ValueAt(1)))
                        names.Slots.Add(inner.Word(1));
                    else if (inner.Key == Keyword.Type && ScriptQuotes.Named(inner.ValueAt(0)))
                    {
                        var choices = new List<string>();
                        for (var at = 2; at < inner.Count; at += 2)
                        {
                            if (ScriptQuotes.Named(inner.ValueAt(at)))
                                choices.Add(inner.Word(at));
                        }

                        names.Types[inner.Word(0)] = choices;
                    }
                    else if (inner.Key == Keyword.Sequence && inner.Says(1, Keyword.As) && ScriptQuotes.Named(inner.ValueAt(2)))
                        names._sequences.Add(inner.Word(2));
                }

                // A state's type comes from its starting value, once every type is known.
                foreach (var inner in body)
                {
                    if (inner.Key == Keyword.Let && ScriptQuotes.Named(inner.ValueAt(0)))
                        names.States[inner.Word(0)] = names.StartType(inner, 2);
                }

                return names;
            }

            /// <summary>Whether the value at <paramref name="at" /> is one of the states, written bare.</summary>
            public bool IsState(Statement statement, int at)
            {
                var value = statement.ValueAt(at);
                return ScriptQuotes.Named(value) && States.ContainsKey(value.Word);
            }

            /// <summary>The type of the state <paramref name="state" />, or null for a flag.</summary>
            public string TypeOf(string state)
            {
                string type;
                return States.TryGetValue(state, out type) ? type : null;
            }

            /// <summary>The type of the starting value at <paramref name="at" />: the one type having a bare choice, or the type qualifying it; null for anything else.</summary>
            public string StartType(Statement statement, int at)
            {
                var value = statement.ValueAt(at);
                if (value.Form == ValueForm.Qualified)
                {
                    var parts = statement.PartsOf(value);
                    return parts.Count == 2 && Types.ContainsKey(parts[0].Word) ? parts[0].Word : null;
                }

                if (!ScriptQuotes.Named(value))
                    return null;

                string having = null;
                foreach (var type in Types)
                {
                    if (!type.Value.Contains(value.Word))
                        continue;

                    if (having != null)
                        return null;

                    having = type.Key;
                }

                return having;
            }

            /// <summary>Whether more than one thing is called <paramref name="name" />: a slot, a type, a state, a sequence or a choice.</summary>
            public bool IsShared(string name)
            {
                var count = (Slots.Contains(name) ? 1 : 0) + (Types.ContainsKey(name) ? 1 : 0) + (States.ContainsKey(name) ? 1 : 0)
                    + (_sequences.Contains(name) ? 1 : 0);
                foreach (var type in Types.Values)
                {
                    if (type.Contains(name))
                        count++;
                }

                return count > 1;
            }

            /// <summary><paramref name="choice" /> with its type <paramref name="type" /> as a kind, <c>type(side)(inside)</c> where the type's name is shared.</summary>
            public Value Choice(Statement statement, string type, Value choice)
            {
                var kind = new Value(ValueForm.Identifier, type);
                if (IsShared(type))
                    kind = statement.Kinded(new Value(ValueForm.Identifier, Keywords.Name(Keyword.Type)), kind);

                return statement.Kinded(kind, choice);
            }
        }
    }
}
