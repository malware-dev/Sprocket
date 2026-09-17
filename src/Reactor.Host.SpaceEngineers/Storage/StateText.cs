using System;
using Reactor.Host.SpaceEngineers.Sequences;

namespace Reactor.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Writes down what is switched off, and reads it back.
    /// </summary>
    /// <remarks>
    /// A program says what a construct automates. What is switched off right now is not that: it is
    /// state, it changes at the press of a button, and it must not travel when somebody copies a
    /// system out of one file and into another.
    /// <para>
    /// So it is a document of its own, written beside the program in the same language, and it lists
    /// only what is off. Everything is on unless it is named here, which makes an empty document the
    /// ordinary case and a lost one harmless.
    /// </para>
    /// </remarks>
    public static class StateText
    {
        public static string Save(Automation program)
        {
            var pen = new Pen();
            var systems = program.Systems;

            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                var identity = system.Identity.ToString("D");

                if (!system.Enabled)
                {
                    pen.Start(Keyword.Off).Text(identity);
                    pen.End();
                }

                var sequences = system.Sequences;
                for (var s = 0; s < sequences.Count; s++)
                {
                    if (sequences[s].Enabled)
                        continue;

                    pen.Start(Keyword.Off).Text(identity).Says(Keyword.Sequence).Whole(sequences[s].Number);
                    pen.End();
                }

                var instances = system.Instances;
                for (var n = 0; n < instances.Count; n++)
                {
                    if (instances[n].Enabled)
                        continue;

                    pen.Start(Keyword.Off).Text(identity).Says(Keyword.Instance).Whole(instances[n].Number);
                    pen.End();
                }
            }

            return pen.ToString();
        }

        /// <summary>
        /// Switches off whatever the document names, and answers what could not be read.
        /// </summary>
        /// <remarks>
        /// A line naming something that is no longer there is dropped rather than reported: a system
        /// somebody deleted is exactly the case this has to survive, and being noisy about it would
        /// mark a program damaged for having been edited.
        /// <para>
        /// Anything that genuinely would not read marks the program damaged, which is what stops the
        /// next save writing over it. The switches moved out of the program; the promise that a
        /// half-read load is never written back moved with them.
        /// </para>
        /// </remarks>
        public static string Load(string saved, Automation program)
        {
            if (string.IsNullOrEmpty(saved))
                return "";

            var parser = new Parser(saved);
            var lines = parser.Read();
            var trouble = new Trouble(saved, parser.Trouble);

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Key != Keyword.Off)
                {
                    trouble.Say(line, "this says what is switched off, and nothing else");
                    continue;
                }

                Switch(line, program, trouble);
            }

            if (trouble.What.Length > 0)
                program.Damaged(trouble.What);

            return trouble.What;
        }

        static void Switch(Statement line, Automation program, Trouble trouble)
        {
            Guid identity;
            if (!Guid.TryParseExact(line.Text(0), "D", out identity))
            {
                trouble.Say(line, "what is switched off is named by its system's identity");
                return;
            }

            var system = program.FindSystem(identity);
            if (system == null)
                return;

            if (line.Count == 1)
            {
                system.Enabled = false;
                return;
            }

            var number = line.ValueAt(2);
            if (number.Form != ValueForm.Integer || number.Whole < 1 || number.Whole > int.MaxValue)
            {
                trouble.Say(line, "a sequence or one of a system is switched off by its number");
                return;
            }

            if (line.Says(1, Keyword.Sequence))
            {
                var sequence = system.FindSequence((int)number.Whole);
                if (sequence != null)
                    sequence.Enabled = false;

                return;
            }

            if (!line.Says(1, Keyword.Instance))
            {
                trouble.Say(line, "a system's identity is followed by 'sequence' or 'instance', or nothing");
                return;
            }

            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i].Number == number.Whole)
                    instances[i].Enabled = false;
            }
        }
    }
}
