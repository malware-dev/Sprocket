using System.Collections.Generic;
using System.Text;
using Reactor.Sequences;

namespace Reactor.Storage
{
    /// <summary>
    /// Writes a program out as text, and reads one back.
    /// </summary>
    /// <remarks>
    /// Core never touches a file - the banned list says so, because Space Engineers does not
    /// whitelist IO and a mod stores its data in the save. So this turns a program into a
    /// string and back, and whoever is hosting decides where the string lives: a file beside
    /// the prototype, a section of the world save, a datapad.
    ///
    /// Text rather than anything binary, and hand-rolled rather than a serializer. Reflection
    /// is banned too, and a format somebody can open and read is worth a great deal while the
    /// model is still moving - a save that breaks can be looked at and fixed by hand instead
    /// of being lost.
    ///
    /// Unknown lines are skipped rather than refused. A save written by a later version should
    /// lose the parts this build does not understand, not everything.
    /// </remarks>
    public static class ProgramText
    {
        const string Version = "reactor 1";

        public static string Save(Automation program)
        {
            var text = new StringBuilder();
            text.Append(Version).Append('\n');

            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                Line(text, "system", Num(system.Id), OnOff(system.Enabled),
                    Escape(system.Name), Escape(system.Annotation));

                // What the author wrote about the parts, before the ones that fill them in, so
                // a reader meets the vocabulary before the answers.
                var roles = system.Roles;
                for (var r = 0; r < roles.Count; r++)
                {
                    Line(text, "role", Long(roles[r].Id), OnOff(roles[r].Optional),
                        Escape(roles[r].Name), Escape(roles[r].Instructions));
                }

                var instances = system.Instances;
                for (var n = 0; n < instances.Count; n++)
                {
                    Line(text, "one", Num(instances[n].Number), Escape(instances[n].Name));
                    WriteFills(text, instances[n]);
                }

                var sequences = system.Sequences;
                for (var q = 0; q < sequences.Count; q++)
                    WriteSequence(text, sequences[q]);
            }

            return text.ToString();
        }

        /// <summary>
        /// Which of this one's own things stand in for the parts the system was built with.
        /// </summary>
        /// <remarks>
        /// Sorted, so a saved file does not churn between runs for no reason: a dictionary
        /// hands its keys back in whatever order it likes, and a save that differs every time
        /// is one that gets written every time and reads as a change in a diff.
        /// </remarks>
        static void WriteFills(StringBuilder text, SystemInstance instance)
        {
            var parts = new List<long>();
            foreach (var role in instance.Filled)
                parts.Add(role);

            parts.Sort();

            for (var i = 0; i < parts.Count; i++)
                Line(text, "fill", Long(parts[i]), Ids(instance.FilledWith(parts[i])));
        }

        static void WriteSequence(StringBuilder text, Sequence sequence)
        {
            Line(text, "sequence", Num(sequence.Id), OnOff(sequence.Enabled),
                Escape(sequence.Name), Escape(sequence.Annotation));

            var when = sequence.Trigger as WhenTrigger;
            if (when != null)
            {
                Line(text, "when", when.RequiresAll ? "all" : "any", Escape(when.EventId),
                    Ids(when.Sources));
            }

            var every = sequence.Trigger as EveryTrigger;
            if (every != null)
                Line(text, "every", Num(every.IntervalTicks));

            if (sequence.Trigger is CalledTrigger)
                Line(text, "called");

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];
                Line(text, "step", Escape(step.ActionId),
                    step.WaitsForCompletion ? "wait" : "now", Ids(step.Targets),
                    Escape(step.Annotation));

                var after = step.Gate as AfterGate;
                if (after != null)
                    Line(text, "after", Num(after.Ticks));

                var once = step.Gate as OnceGate;
                if (once != null)
                    WriteCondition(text, "once", once.Condition);

                var onlyIf = step.Gate as OnlyIfGate;
                if (onlyIf != null)
                    WriteCondition(text, "onlyif", onlyIf.Condition);
            }
        }

        static void WriteCondition(StringBuilder text, string keyword, Condition condition)
        {
            if (condition == null)
                return;

            Line(text, keyword, Escape(condition.StateId), Ids(condition.Entities));
        }

        /// <summary>
        /// Reads a program back into an empty one.
        /// </summary>
        /// <remarks>
        /// Position carries the nesting rather than any brackets: an instance or a sequence
        /// belongs to the last system named, a step to the last sequence, a gate to the last
        /// step. It keeps the format flat enough to read at a glance, and it is the same shape
        /// the thing being described has.
        /// </remarks>
        public static void Load(string saved, Automation program)
        {
            if (string.IsNullOrEmpty(saved))
                return;

            AutomationSystem system = null;
            SystemInstance instance = null;
            Sequence sequence = null;
            Step step = null;

            var lines = saved.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split('|');
                switch (parts[0])
                {
                    case "system":
                        system = program.Restore(Int(parts, 1));
                        system.Enabled = At(parts, 2) != "off";
                        system.Name = Unescape(At(parts, 3));
                        system.Annotation = Unescape(At(parts, 4));
                        instance = null;
                        sequence = null;
                        step = null;
                        break;

                    case "role":
                        if (system == null)
                            break;
                        var role = system.RoleFor(Long(parts, 1));
                        role.Optional = At(parts, 2) == "on";
                        role.Name = Unescape(At(parts, 3));
                        role.Instructions = Unescape(At(parts, 4));
                        break;

                    case "one":
                        if (system != null)
                            instance = system.Restore(Int(parts, 1), Unescape(At(parts, 2)));
                        break;

                    case "fill":
                        if (instance == null)
                            break;
                        var filled = new List<long>();
                        ReadIds(At(parts, 2), filled);
                        instance.Fill(Long(parts, 1), filled);
                        break;

                    case "sequence":
                        if (system == null)
                            break;
                        sequence = program.Restore(system, Int(parts, 1), new CalledTrigger());
                        sequence.Enabled = At(parts, 2) != "off";
                        sequence.Name = Unescape(At(parts, 3));
                        sequence.Annotation = Unescape(At(parts, 4));
                        step = null;
                        break;

                    case "when":
                        if (sequence == null)
                            break;
                        var when = new WhenTrigger(Unescape(At(parts, 2)));
                        when.RequiresAll = At(parts, 1) == "all";
                        ReadIds(At(parts, 3), when.Sources);
                        sequence.Trigger = when;
                        break;

                    case "every":
                        if (sequence != null)
                            sequence.Trigger = new EveryTrigger(Int(parts, 1));
                        break;

                    case "called":
                        if (sequence != null)
                            sequence.Trigger = new CalledTrigger();
                        break;

                    case "step":
                        if (sequence == null)
                            break;
                        step = new Step(Unescape(At(parts, 1)));
                        step.WaitsForCompletion = At(parts, 2) != "now";
                        ReadIds(At(parts, 3), step.Targets);
                        step.Annotation = Unescape(At(parts, 4));
                        sequence.Steps.Add(step);
                        break;

                    case "after":
                        if (step != null)
                            step.Gate = new AfterGate(Int(parts, 1));
                        break;

                    case "once":
                        if (step != null)
                            step.Gate = new OnceGate(ReadCondition(parts));
                        break;

                    case "onlyif":
                        if (step != null)
                            step.Gate = new OnlyIfGate(ReadCondition(parts));
                        break;
                }
            }
        }

        static Condition ReadCondition(string[] parts)
        {
            var condition = new Condition(Unescape(At(parts, 1)));
            ReadIds(At(parts, 2), condition.Entities);
            return condition;
        }

        static void Line(StringBuilder text, string keyword, params string[] fields)
        {
            text.Append(keyword);
            for (var i = 0; i < fields.Length; i++)
                text.Append('|').Append(fields[i]);
            text.Append('\n');
        }

        static string Long(long value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        static long Long(string[] parts, int index)
        {
            long value;
            long.TryParse(At(parts, index), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out value);
            return value;
        }

        static string Num(int value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        static string OnOff(bool on)
        {
            return on ? "on" : "off";
        }

        static string Ids(IList<long> ids)
        {
            var text = new StringBuilder();
            for (var i = 0; i < ids.Count; i++)
            {
                if (i > 0)
                    text.Append(',');
                text.Append(ids[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        static void ReadIds(string field, IList<long> into)
        {
            into.Clear();
            if (string.IsNullOrEmpty(field))
                return;

            var parts = field.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                long id;
                if (long.TryParse(parts[i], System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out id))
                    into.Add(id);
            }
        }

        static string At(string[] parts, int index)
        {
            return index < parts.Length ? parts[index] : "";
        }

        static int Int(string[] parts, int index)
        {
            int value;
            int.TryParse(At(parts, index), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out value);
            return value;
        }

        /// <summary>
        /// Makes a value safe to sit between separators.
        /// </summary>
        /// <remarks>
        /// Names are whatever somebody typed, which includes bars and newlines. Escaping is
        /// two characters rather than quoting, because quoting needs its own escape anyway and
        /// this way a line still reads at a glance in a text editor.
        /// </remarks>
        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var text = new StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '\\')
                    text.Append("\\\\");
                else if (c == '|')
                    text.Append("\\p");
                else if (c == '\n')
                    text.Append("\\n");
                else if (c == '\r')
                    text.Append("\\r");
                else
                    text.Append(c);
            }
            return text.ToString();
        }

        static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var text = new StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != '\\' || i + 1 >= value.Length)
                {
                    text.Append(value[i]);
                    continue;
                }

                i++;
                if (value[i] == 'p')
                    text.Append('|');
                else if (value[i] == 'n')
                    text.Append('\n');
                else if (value[i] == 'r')
                    text.Append('\r');
                else
                    text.Append(value[i]);
            }
            return text.ToString();
        }
    }
}
