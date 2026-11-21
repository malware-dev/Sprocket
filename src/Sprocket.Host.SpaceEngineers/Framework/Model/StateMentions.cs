using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>Where a logic system's steps, tests and triggers name its states and their choices, and those names changed everywhere.</summary>
    public static class StateMentions
    {
        /// <summary>Whether anything in <paramref name="system" /> names the state called <paramref name="state" />, its own declaration aside.</summary>
        public static bool Mentions(AutomationSystem system, string state)
        {
            foreach (var test in Tests(system))
            {
                if (test.State == state || test.Other == state)
                    return true;
            }

            foreach (var sets in Sets(system))
            {
                if (sets.State == state || sets.From == state)
                    return true;
            }

            return false;
        }

        /// <summary>Whether a state of the type <paramref name="type" /> starts at, is set to or is compared with its choice <paramref name="choice" />.</summary>
        public static bool Mentions(AutomationSystem system, string type, string choice)
        {
            var named = Literal.Name(choice);
            foreach (var state in system.States)
            {
                if (state.Type == type && Same(state.Start, named))
                    return true;
            }

            foreach (var test in Tests(system))
            {
                if (test.Other.Length == 0 && Tests(system, test, type) && Same(test.Value, named))
                    return true;
            }

            foreach (var part in system.Parts)
            {
                if (part.Setting == type && (part.HasDefault && Same(part.Default, named) || Answers(system, part.Id, named)))
                    return true;
            }

            foreach (var sets in Sets(system))
            {
                if (sets.From.Length == 0 && OfType(system, sets.State, type) && Same(sets.Value, named))
                    return true;
            }

            return false;
        }

        /// <summary>Whether any state or setting of <paramref name="system" /> holds the type called <paramref name="type" />.</summary>
        public static bool Holds(AutomationSystem system, string type)
        {
            foreach (var state in system.States)
            {
                if (state.Type == type)
                    return true;
            }

            foreach (var part in system.Parts)
            {
                if (part.Setting == type)
                    return true;
            }

            return false;
        }

        /// <summary>Changes every mention of the state <paramref name="was" /> to <paramref name="now" />, its declaration aside.</summary>
        public static void RenameState(AutomationSystem system, string was, string now)
        {
            foreach (var test in Tests(system))
            {
                if (test.State == was)
                    test.State = now;

                if (test.Other == was)
                    test.Other = now;
            }

            foreach (var sets in Sets(system))
            {
                if (sets.State == was)
                    sets.State = now;

                if (sets.From == was)
                    sets.From = now;
            }
        }

        /// <summary>
        /// Changes every mention of the choice <paramref name="was" /> of <paramref name="type" /> to <paramref name="now" />:
        /// the starting values, the values set, the values compared with, and the settings' defaults and answers.
        /// </summary>
        /// <remarks>Run after the type's own choice is renamed, as it reads each state's type off the system.</remarks>
        public static void RenameChoice(AutomationSystem system, string type, string was, string now)
        {
            var old = Literal.Name(was);
            var renamed = Literal.Name(now);
            foreach (var state in system.States)
            {
                if (state.Type == type && Same(state.Start, old))
                    state.Start = renamed;
            }

            foreach (var test in Tests(system))
            {
                if (test.Other.Length == 0 && Tests(system, test, type) && Same(test.Value, old))
                    test.Value = renamed;
            }

            foreach (var part in system.Parts)
            {
                if (part.Setting != type)
                    continue;

                if (part.HasDefault && Same(part.Default, old))
                    part.Default = renamed;

                foreach (var instance in system.Instances)
                {
                    Literal answer;
                    if (instance.AnswerTo(part.Id, out answer) && Same(answer, old))
                        instance.Answer(part.Id, renamed);
                }
            }

            foreach (var sets in Sets(system))
            {
                if (sets.From.Length == 0 && OfType(system, sets.State, type) && Same(sets.Value, old))
                    sets.Value = renamed;
            }
        }

        /// <summary>Changes the type every state and setting of the type <paramref name="was" /> holds to <paramref name="now" />.</summary>
        public static void RenameType(AutomationSystem system, string was, string now)
        {
            foreach (var state in system.States)
            {
                if (state.Type == was)
                    state.Type = now;
            }

            foreach (var part in system.Parts)
            {
                if (part.Setting == was)
                    part.Setting = now;
            }
        }

        /// <summary>Whether two values a state or a setting holds are the same: the same flag, choice, text, number, color, length of time, or setting standing in for one.</summary>
        /// <remarks>A length of time is the same however it is written: <c>1s</c> is <c>60t</c>.</remarks>
        public static bool Same(Literal one, Literal other)
        {
            if (one.Kind != other.Kind)
                return false;

            switch (one.Kind)
            {
                case ValueKind.Flag:
                    return one.Flag == other.Flag;
                case ValueKind.Text:
                case ValueKind.Name:
                case ValueKind.Setting:
                    return one.Text == other.Text;
                case ValueKind.Duration:
                    return one.Length.Ticks == other.Length.Ticks;
            }

            return one.Value.Bits == other.Value.Bits;
        }

        /// <summary>Whether an installation of <paramref name="system" /> answers the setting <paramref name="setting" /> with <paramref name="value" />.</summary>
        static bool Answers(AutomationSystem system, string setting, Literal value)
        {
            foreach (var instance in system.Instances)
            {
                Literal answer;
                if (instance.AnswerTo(setting, out answer) && Same(answer, value))
                    return true;
            }

            return false;
        }

        /// <summary>Whether what <paramref name="test" /> tests, a state or a setting, holds the type <paramref name="type" />.</summary>
        static bool Tests(AutomationSystem system, StateTest test, string type)
        {
            if (test.Setting.Length == 0)
                return OfType(system, test.State, type);

            var part = system.FindPart(test.Setting);
            return part != null && part.Setting == type;
        }

        /// <summary>Whether the state called <paramref name="state" /> holds the type <paramref name="type" />.</summary>
        static bool OfType(AutomationSystem system, string state, string type)
        {
            var found = system.FindState(state);
            return found != null && found.Type == type;
        }

        /// <summary>Every test of a state in <paramref name="system" />: in its ifs and in what starts its sequences.</summary>
        static List<StateTest> Tests(AutomationSystem system)
        {
            var tests = new List<StateTest>();
            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    var test = (step as Branch)?.Test as StateTest;
                    if (test != null)
                        tests.Add(test);
                }
            }

            foreach (var trigger in system.Triggers)
            {
                var watched = trigger as StateTrigger;
                if (watched != null)
                    tests.Add(watched.Test);
            }

            return tests;
        }

        /// <summary>Every step of <paramref name="system" /> that sets a state: in its sequences.</summary>
        static List<SetState> Sets(AutomationSystem system)
        {
            var sets = new List<SetState>();
            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    var set = step as SetState;
                    if (set != null)
                        sets.Add(set);
                }
            }


            return sets;
        }
    }
}
