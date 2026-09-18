using System;
using Reactor.Host.SpaceEngineers.Runtime;
using Reactor.Host.SpaceEngineers.Sequences;

namespace Reactor.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Writes down what is switched off and what is part way through running, and reads it back.
    /// </summary>
    /// <remarks>
    /// A program says what a construct automates. What is switched off right now is not that, and
    /// neither is a cycle half finished: both are state, they change while nobody is editing
    /// anything, and neither must travel when somebody copies a system out of one file into another.
    /// <para>
    /// So it is a document of its own, written beside the program in the same language. What is off
    /// is listed, and everything not named is on, which makes an empty document the ordinary case
    /// and a lost one harmless - a construct that loses this one comes back with everything switched
    /// on and nothing running.
    /// </para>
    /// </remarks>
    public static class StateText
    {
        /// <summary>
        /// What is switched off and what is running, as a document.
        /// </summary>
        /// <remarks>
        /// A null runner writes the switches alone, which is what a caller with no runtime has.
        /// </remarks>
        public static string Save(Automation program, AutomationRunner runner)
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

                if (runner != null)
                    Runs(pen, runner, system, identity);
            }

            return pen.ToString();
        }

        /// <summary>
        /// What is part way through, one line per run.
        /// </summary>
        /// <remarks>
        /// Per one of the system and per sequence, because that is what runs: the port airlock being
        /// half way through cycling says nothing about the front one.
        /// <para>
        /// The step, and what is holding it there: <c>await</c> for a step waiting on the doors it
        /// started, <c>wait</c> and a count for one spending time. Nothing else is written down. What
        /// was invoked before the save has either arrived by the time the world loads or has not, and
        /// asking the doors is the only answer worth having.
        /// </para>
        /// </remarks>
        static void Runs(Pen pen, AutomationRunner runner, AutomationSystem system, string identity)
        {
            var instances = system.Instances;
            var sequences = system.Sequences;

            for (var n = 0; n < instances.Count; n++)
            {
                for (var q = 0; q < sequences.Count; q++)
                {
                    var state = runner.Get(sequences[q], instances[n]);
                    if (state == null || !state.IsRunning || state.CurrentStep < 0)
                        continue;

                    pen.Start(Keyword.Running).Text(identity)
                        .Says(Keyword.Instance).Whole(instances[n].Number)
                        .Says(Keyword.Sequence).Whole(sequences[q].Number)
                        .Says(Keyword.Step).Whole(state.CurrentStep);

                    if (state.WaitTicks > 0)
                        pen.Says(Keyword.Wait).Ticks(state.WaitTicks);
                    else if (state.IsWaiting)
                        pen.Says(Keyword.Await);

                    pen.End();
                }
            }
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
        public static string Load(string saved, Automation program, AutomationRunner runner)
        {
            if (string.IsNullOrEmpty(saved))
                return "";

            var parser = new Parser(saved);
            var lines = parser.Read();
            var trouble = new Trouble(saved, parser.Trouble);

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line.Key == Keyword.Off)
                {
                    Switch(line, program, trouble);
                    continue;
                }

                if (line.Key == Keyword.Running)
                {
                    Resume(line, program, runner, trouble);
                    continue;
                }

                trouble.Say(line, "this says what is switched off and what is running, and nothing else");
            }

            if (trouble.What.Length > 0)
                program.Damaged(trouble.What);

            return trouble.What;
        }

        /// <summary>
        /// Puts one run back where the save caught it.
        /// </summary>
        /// <remarks>
        /// Dropped rather than reported where what it names has gone, the same as a switch: a system
        /// deleted between saving and loading is the case this has to survive. A malformed line is
        /// reported, because that is a file nobody should be written over.
        /// <para>
        /// The runner is asked to resume rather than told to run. It refuses a step that is no longer
        /// there and a sequence that may not run, which is what a program edited in the meantime
        /// leaves behind.
        /// </para>
        /// </remarks>
        static void Resume(Statement line, Automation program, AutomationRunner runner, Trouble trouble)
        {
            Guid identity;
            if (!Guid.TryParseExact(line.Text(0), "D", out identity))
            {
                trouble.Say(line, "what is running is named by its system's identity");
                return;
            }

            if (!line.Says(1, Keyword.Instance) || !line.Says(3, Keyword.Sequence)
                || !line.Says(5, Keyword.Step))
            {
                trouble.Say(line,
                    "a run is written as its system, then instance, sequence and step, each with a number");
                return;
            }

            var instance = Counted(line, 2);
            var sequence = Counted(line, 4);
            var step = line.ValueAt(6);

            if (instance < 1 || sequence < 1 || step.Form != ValueForm.Integer
                || step.Whole < 0 || step.Whole > int.MaxValue)
            {
                trouble.Say(line, "an instance and a sequence are numbered from one, and a step from zero");
                return;
            }

            var waiting = line.Says(7, Keyword.Await);
            var held = 0;

            if (line.Says(7, Keyword.Wait))
            {
                var ticks = line.ValueAt(8);
                if (ticks.Form != ValueForm.Duration || ticks.Whole < 0 || ticks.Whole > int.MaxValue)
                {
                    trouble.Say(line, "what is left of a wait is written with its unit, like wait 60t");
                    return;
                }

                held = (int)ticks.Whole;
                waiting = true;
            }

            if (runner == null)
                return;

            var system = program.FindSystem(identity);
            if (system == null)
                return;

            var one = system.FindInstance(instance);
            var which = system.FindSequence(sequence);
            if (one == null || which == null)
                return;

            runner.Resume(which, one, (int)step.Whole, waiting, held);
        }

        /// <summary>A whole number in the line, or zero where it is not one.</summary>
        static int Counted(Statement line, int index)
        {
            var value = line.ValueAt(index);
            return value.Form == ValueForm.Integer && value.Whole > 0 && value.Whole <= int.MaxValue
                ? (int)value.Whole
                : 0;
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
