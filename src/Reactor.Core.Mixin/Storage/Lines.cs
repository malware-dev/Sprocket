using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Storage
{
    /// <summary>
    /// Reads a <c>reactor 1</c> save, which was one construct per bar-separated line.
    /// </summary>
    /// <remarks>
    /// Kept only so that a world saved before the script language still opens. Nothing writes this
    /// format any more, and the whole file can go once no save carries it.
    /// <para>
    /// Position carried the nesting: an instance or a sequence belonged to the last system named, a
    /// step to the last sequence, a gate to the last step. It is the shape the model has, which is
    /// why it worked - and it is also why it could not hold what a construct nobody recognised was
    /// part of, which is what braces bought.
    /// </para>
    /// </remarks>
    static class Lines
    {
        public static void Load(string saved, Automation program, Values values)
        {
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

                    case "block":
                        var declared = new EntityRef(Unescape(At(parts, 1)), Unescape(At(parts, 2)));
                        ReadKeys(values, At(parts, 3), declared.Keys);
                        program.Restore(declared);
                        break;

                    case "role":
                        if (system == null)
                            break;
                        var role = system.RoleFor(Unescape(At(parts, 1)));
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
                        var filled = new List<string>();
                        ReadIds(At(parts, 2), filled);
                        instance.Fill(Unescape(At(parts, 1)), filled);
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

        static void ReadKeys(Values values, string field, IList<EntityKey> into)
        {
            if (string.IsNullOrEmpty(field))
                return;

            var parts = field.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Length < 2 || part[1] != ':')
                    continue;

                var body = part.Substring(2);
                switch (part[0])
                {
                    case 't':
                        into.Add(new EntityKey(ValueKind.Text, values.Text(Unescape(body))));
                        break;
                    case 'i':
                        into.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(ReadLong(body))));
                        break;
                    case 'f':
                        into.Add(new EntityKey(ValueKind.Flag, EntityValue.Of(body == "1")));
                        break;
                    case 'c':
                        var bits = body.Split(' ');
                        if (bits.Length == 4)
                            into.Add(new EntityKey(ValueKind.Color, EntityValue.Of(
                                (byte)ReadLong(bits[0]), (byte)ReadLong(bits[1]),
                                (byte)ReadLong(bits[2]), (byte)ReadLong(bits[3]))));
                        break;
                    case 'r':
                        double real;
                        if (double.TryParse(body, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out real))
                            into.Add(new EntityKey(ValueKind.Real, EntityValue.Of(real)));
                        break;
                }
            }
        }

        static long ReadLong(string text)
        {
            long value;
            return long.TryParse(text, out value) ? value : 0;
        }

        static void ReadIds(string field, IList<string> into)
        {
            into.Clear();
            if (string.IsNullOrEmpty(field))
                return;

            var parts = field.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                var name = Unescape(parts[i]);
                if (name.Length > 0)
                    into.Add(name);
            }
        }

        static string At(string[] parts, int index)
        {
            return index < parts.Length ? parts[index] : "";
        }

        static int Int(string[] parts, int index)
        {
            int value;
            int.TryParse(At(parts, index), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);
            return value;
        }

        /// <summary>
        /// Puts back what the old writer escaped.
        /// </summary>
        /// <remarks>
        /// Two characters rather than quoting, and a backslash that meant something - which is the
        /// rule the script language deliberately does not have, because a writer that forgets to
        /// escape one produces a file that still reads, just wrong.
        /// </remarks>
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
