using System.Collections.Generic;
using Lumel.Input;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// What one of a logic system's states can be set to or compared with, as words to choose between: its type's choices, or
    /// on and off for a flag, then the other states that hold the same.
    /// </summary>
    public static class StateValueQuestion
    {
        /// <summary>
        /// The fields a step setting <paramref name="state" /> asks for where <paramref name="tests" /> is false: the value it
        /// is set to, which may be left empty where another state's value is taken. A test of it asks first how it compares.
        /// <paramref name="made" /> is a type being made for it, not yet the system's.
        /// </summary>
        public static List<Field> Fields(Texts texts, AutomationSystem system, State state, bool tests, DeclaredType made = null)
        {
            var fields = tests ? ComparisonQuestion.StateFields() : new List<Field>();
            var words = Words(system, state, made);
            var value = Field.Among(texts.PickStateFixedValue, words, words);
            value.About = texts.PickStateFixedValueAbout;
            value.Entry = FieldInput.Name;
            value.Default = "";
            fields.Add(value);
            return fields;
        }

        /// <summary>
        /// The values <paramref name="state" /> can be given or compared with, in the order they are written; <paramref name="made" />
        /// is a type being made for it, not yet the system's.
        /// </summary>
        public static List<string> Words(AutomationSystem system, State state, DeclaredType made = null)
        {
            return Choices(system, state, made);
        }

        /// <summary>The other states of <paramref name="system" /> holding what <paramref name="state" /> holds, in the order they are written.</summary>
        public static List<string> Others(AutomationSystem system, State state)
        {
            var others = new List<string>();
            foreach (var other in system.States)
            {
                if (other != state && other.Type == state.Type)
                    others.Add(other.Name);
            }

            return others;
        }

        /// <summary>
        /// The value <paramref name="word" />, one of the words of <paramref name="state" />, gives; false where it is none of
        /// them. <paramref name="made" /> is a type being made for it, not yet the system's.
        /// </summary>
        public static bool Read(AutomationSystem system, State state, string word, out Literal value, DeclaredType made = null)
        {
            value = default(Literal);
            var at = Choices(system, state, made).IndexOf(word);
            if (at < 0)
                return false;

            value = state.IsFlag ? Literal.Of(at == 0) : Literal.Name(word);
            return true;
        }

        /// <summary>
        /// The part of a sentence saying the value the state <paramref name="named" /> of <paramref name="system" /> is given or
        /// compared with, pressed to <paramref name="opens" />: <paramref name="value" />, or the state <paramref name="other" />
        /// where it is not empty.
        /// </summary>
        public static SentenceSlot Part(AutomationSystem system, string named, Literal value, string other, ICommand opens, ArgumentRole role)
        {
            if (!string.IsNullOrEmpty(other))
                return new SystemStateSlot(other, opens);

            var state = system?.FindState(named);
            var words = state == null ? new List<string>() : Words(system, state);
            return new ArgumentsSlot(new[] { value }, new[] { Field.Among(named, words, words) }, opens, role);
        }

        /// <summary>A flag starting off, which a state made new starts as.</summary>
        public static StateStart Off => new StateStart(Literal.Of(false).Said, Literal.Of(false), "");

        /// <summary>
        /// Every value a state of <paramref name="system" /> can start at: on and off, then each type's choices, then those of
        /// <paramref name="made" />, a type being made and not yet the system's; only the values of the type
        /// <paramref name="only" /> where it is not null, empty for on and off alone.
        /// </summary>
        /// <remarks>Where every type's values are offered, a choice is said with its type where another value is said the same.</remarks>
        public static List<StateStart> Starts(Texts texts, AutomationSystem system, string only, DeclaredType made = null)
        {
            var types = new List<DeclaredType>(system.Types);
            if (made != null)
                types.Add(made);

            var starts = new List<StateStart>();
            if (string.IsNullOrEmpty(only))
            {
                starts.Add(new StateStart(Literal.Of(true).Said, Literal.Of(true), ""));
                starts.Add(Off);
                if (only != null)
                    return starts;
            }

            foreach (var type in types)
            {
                if (only != null && type.Name != only)
                    continue;

                foreach (var choice in type.Choices)
                {
                    var word = only == null && Shared(types, type, choice) ? Template.Fill(Template.Fill(texts.PickChoiceOfType, "choice", choice), "type", type.Name) : choice;
                    starts.Add(new StateStart(word, Literal.Name(choice), type.Name));
                }
            }

            return starts;
        }

        /// <summary>Whether <paramref name="choice" /> of <paramref name="type" /> is said the same as a value of another of <paramref name="types" />, or as a flag.</summary>
        static bool Shared(IList<DeclaredType> types, DeclaredType type, string choice)
        {
            if (choice == Literal.Of(true).Said || choice == Literal.Of(false).Said)
                return true;

            foreach (var other in types)
            {
                if (other != type && other.Has(choice))
                    return true;
            }

            return false;
        }

        /// <summary>The values <paramref name="state" /> holds: on and off for a flag, its type's choices otherwise, the type <paramref name="made" /> where it is that one.</summary>
        static List<string> Choices(AutomationSystem system, State state, DeclaredType made = null)
        {
            if (state.IsFlag)
                return new List<string> { Literal.Of(true).Said, Literal.Of(false).Said };

            var type = made != null && made.Name == state.Type ? made : system.FindType(state.Type);
            return type == null ? new List<string>() : new List<string>(type.Choices);
        }
    }
}
