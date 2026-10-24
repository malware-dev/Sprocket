using System;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// The switches a construct's block holds in the earlier form, lines of words, read so that they carry over; nothing writes
    /// them.
    /// </summary>
    /// <remarks>
    /// A block of that form holds one line for each logic system or installation switched off, with why where Sprocket did it:
    /// <code>
    /// off "3f2a…" slot empty sequence 2 step 1 block 'Inner Door'
    /// off "3f2a…" instance 2
    /// </code>
    /// </remarks>
    public static class EarlierSwitches
    {
        /// <summary>Switches off in <paramref name="program" /> what <paramref name="saved" /> says is off; what is wrong with it, or empty.</summary>
        /// <remarks>Switches that will not read leave the program marked not to be saved over.</remarks>
        public static string Load(string saved, Automation program)
        {
            if (string.IsNullOrEmpty(saved))
                return "";

            var parser = new Parser(saved);
            var lines = parser.Read();
            var trouble = new ScriptTrouble(saved, parser.Trouble);

            foreach (var line in lines)
            {
                if (line.Key == Keyword.Off && !line.IsOff)
                    Switch(line, program, trouble);
                else
                    trouble.Say(line, "expected 'off'.");
            }

            if (trouble.What.Length > 0)
                program.Damaged(trouble.What);

            return trouble.What;
        }

        /// <summary>Switches off the logic system or installation <paramref name="line" /> names, where it is still there.</summary>
        static void Switch(Statement line, Automation program, ScriptTrouble trouble)
        {
            Guid identity;
            if (!Guid.TryParseExact(line.Text(0), "D", out identity))
            {
                trouble.Say(line, "expected the system's identity.");
                return;
            }

            var system = program.FindSystem(identity);
            if (system == null)
                return;

            if (line.Count == 1 || line.ValueAt(1).Form == ValueForm.Text)
            {
                system.Enabled = false;
                system.Halt = Why(line, 1);
                return;
            }

            var number = line.ValueAt(2);
            if (number.Form != ValueForm.Integer || number.Whole < 1 || number.Whole > int.MaxValue)
            {
                trouble.Say(line, "expected the instance's number.");
                return;
            }

            // A sequence has no switch of its own, so a line naming one switches nothing.
            if (line.Says(1, Keyword.Sequence))
                return;

            if (!line.Says(1, Keyword.Instance))
            {
                trouble.Say(line, "expected 'instance', a reason, or the end of the line.");
                return;
            }

            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i].Number != number.Whole)
                    continue;

                instances[i].Enabled = false;
                instances[i].Halt = Why(line, 3);
            }
        }

        /// <summary>Why a line says Sprocket switched something off, read from <paramref name="from" /> on, or null where it says nobody but a person did.</summary>
        static Halt Why(Statement line, int from)
        {
            if (from >= line.Count)
                return null;

            var problem = EarlierSpellings.ProblemOf(line.Text(from));
            var sequence = 0;
            var step = -1;
            string block = null;
            var cause = Cause.None;
            string about = null;
            for (var i = from + 1; i < line.Count; i++)
            {
                if (line.Says(i, Keyword.Sequence) && i + 1 < line.Count)
                {
                    sequence = Counted(line, ++i);
                }
                else if (line.Says(i, Keyword.Step) && i + 1 < line.Count)
                {
                    var number = line.ValueAt(++i);
                    if (number.Form == ValueForm.Integer && number.Whole >= 0 && number.Whole <= int.MaxValue)
                        step = (int)number.Whole;
                }
                else if (line.Says(i, Keyword.Block) && i + 1 < line.Count)
                {
                    block = line.Text(++i);
                }
                else if (cause == Cause.None)
                {
                    cause = EarlierSpellings.CauseOf(line.Text(i));
                }
                else
                {
                    about = line.Text(i);
                }
            }

            var because = cause == Cause.None ? BlockTrouble.None : BlockTrouble.Kept(cause, string.IsNullOrEmpty(about) ? null : about);
            return new Halt(problem, sequence, step, because,
                string.IsNullOrEmpty(block) ? null : block);
        }

        /// <summary>A whole number in the line, or zero where it is not one.</summary>
        static int Counted(Statement line, int index)
        {
            var value = line.ValueAt(index);
            return value.Form == ValueForm.Integer && value.Whole > 0 && value.Whole <= int.MaxValue
                ? (int)value.Whole
                : 0;
        }
    }
}
