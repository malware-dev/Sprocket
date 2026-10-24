using System;
using System.Globalization;
using System.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>What the runs of one construct's automation do, a line for each thing that happens, for finding out why one went wrong.</summary>
    /// <remarks>
    /// Exists only while the world's debug log is switched on; a runner whose log is null records nothing and pays one null
    /// check at each place a line would be written. Everything the line says is worked out here, from what the runner hands
    /// over as it is, so all the cost of the words falls on a log somebody asked for.
    /// </remarks>
    public sealed class RunLog
    {
        readonly Action<string> _write;
        readonly Func<string> _host;
        readonly StringBuilder _line = new StringBuilder();

        /// <summary>A log handing each line to <paramref name="write" />, naming the programmable block running the automation as <paramref name="host" /> calls it when the line is written.</summary>
        public RunLog(Action<string> write, Func<string> host)
        {
            _write = write;
            _host = host;
        }

        /// <summary>A trigger of <paramref name="system" /> fired on <paramref name="instance" />.</summary>
        public void Fired(AutomationSystem system, Trigger trigger, SystemInstance instance, int tick)
        {
            Begin(tick).Append(", ");
            System(system, instance);
            _line.Append(": a trigger fired, to ").Append(Script(trigger.Does));
            End();
        }

        /// <summary><paramref name="run" /> started: by its trigger, by hand, or called by a sequence or a button.</summary>
        public void Started(SequenceRunner run, int tick, RunStart how)
        {
            Of(run, tick).Append("started");
            switch (how)
            {
                case RunStart.Trigger:
                    _line.Append(" by its trigger");
                    break;
                case RunStart.Hand:
                    _line.Append(" by hand");
                    break;
                default:
                    _line.Append(" by a call");
                    break;
            }

            End();
        }

        /// <summary><paramref name="run" /> was asked to start and did not, for the reason <paramref name="why" /> gives.</summary>
        public void Refused(SequenceRunner run, int tick, RunRefusal why)
        {
            Of(run, tick).Append("did not start: ");
            switch (why)
            {
                case RunRefusal.Busy:
                    _line.Append("it is already running");
                    break;
                case RunRefusal.Unpowered:
                    _line.Append("the block it runs on is unpowered or switched off");
                    break;
                case RunRefusal.Off:
                    _line.Append("it is switched off");
                    break;
                default:
                    _line.Append("a slot it needs is empty");
                    break;
            }

            End();
        }

        /// <summary><paramref name="run" /> reached <paramref name="step" />.</summary>
        public void Entered(SequenceRunner run, int tick, int step)
        {
            Numbered(Of(run, tick), step).Append(": ").Append(Script(run.Sequence.Steps[step]));
            End();
        }

        /// <summary><paramref name="run" /> did the act at <paramref name="step" />, on each of its blocks.</summary>
        public void Acted(SequenceRunner run, int tick, int step, CompiledAct compiled)
        {
            Numbered(Of(run, tick), step).Append(" asked ");
            if (compiled.Acts.Count == 0)
                _line.Append("no block");

            for (var i = 0; i < compiled.Acts.Count; i++)
            {
                var acted = compiled.Acts[i];
                if (i > 0)
                    _line.Append(", ");

                _line.Append('"').Append(acted.Entity.Name).Append('"');
                if (acted.Action != null)
                    _line.Append(" to ").Append(acted.Action.Id);

                if (acted.Arguments != null && acted.Arguments.Count > 0)
                {
                    _line.Append('(');
                    for (var a = 0; a < acted.Arguments.Count; a++)
                        _line.Append(a > 0 ? " " : "").Append(acted.Arguments[a].Said);

                    _line.Append(')');
                }
            }

            End();
        }

        /// <summary><paramref name="run" /> tested the if or otherwise-if at <paramref name="step" />: what each block read, and whether it held.</summary>
        public void Tested(SequenceRunner run, int tick, int step, CompiledTest tested, bool held)
        {
            Numbered(Of(run, tick), step).Append(held ? " holds:" : " does not hold:");
            if (tested.Reads.Count == 0)
                _line.Append(" no block to read");

            foreach (var read in tested.Reads)
            {
                _line.Append(" \"").Append(read.Entity.Name).Append("\" reads ");
                if (read.Gauge != null)
                    _line.Append(read.Gauge.Said());
                else
                    _line.Append(read.Holds() ? "yes" : "no");
            }

            End();
        }

        /// <summary><paramref name="run" /> waits at <paramref name="step" /> until <paramref name="until" />.</summary>
        public void Waiting(SequenceRunner run, int tick, int step, int until)
        {
            Numbered(Of(run, tick), step).Append(" waits until tick ").Append(until.ToString(CultureInfo.InvariantCulture));
            End();
        }

        /// <summary><paramref name="run" /> is held at the await at <paramref name="step" />, for the steps and the sequences it waits on.</summary>
        public void Holding(SequenceRunner run, int tick, int step, RunMemory memory)
        {
            Numbered(Of(run, tick), step).Append(" waits for");
            if (memory.Outstanding.Count == 0 && memory.Started.Count == 0)
                _line.Append(" nothing");

            foreach (var waited in memory.Outstanding)
                _line.Append(" step ").Append((waited + 1).ToString(CultureInfo.InvariantCulture));

            foreach (var started in memory.Started)
                _line.Append(" sequence ").Append(started.ToString(CultureInfo.InvariantCulture));

            End();
        }

        /// <summary><paramref name="run" /> was held at the await at <paramref name="step" /> since <paramref name="since" />, and goes on.</summary>
        public void Held(SequenceRunner run, int tick, int step, int since)
        {
            Numbered(Of(run, tick), step).Append(" done waiting, after ").Append((tick - since).ToString(CultureInfo.InvariantCulture))
                .Append(" ticks");
            End();
        }

        /// <summary><paramref name="run" /> ended at a step it could not do, or was stopped by a block sent the other way, as <paramref name="fault" /> says.</summary>
        public void Faulted(SequenceRunner run, int tick, SequenceFault fault)
        {
            Of(run, tick);
            if (fault.Step >= 0)
                Numbered(_line, fault.Step).Append(' ');

            _line.Append("failed: ").Append(fault.Problem.ToString());
            if (!string.IsNullOrEmpty(fault.Which))
                _line.Append(", \"").Append(fault.Which).Append('"');

            if (fault.Because.Exists)
            {
                _line.Append(", ").Append(fault.Because.Cause.ToString());
                if (!string.IsNullOrEmpty(fault.Because.About))
                    _line.Append(" (").Append(fault.Because.About).Append(')');
            }

            End();
        }

        /// <summary><paramref name="run" /> did its last step.</summary>
        public void Finished(SequenceRunner run, int tick)
        {
            Of(run, tick).Append("finished");
            End();
        }

        /// <summary><paramref name="run" /> was stopped at <paramref name="step" /> by <paramref name="by" />, sequence <paramref name="from" /> where a sequence did.</summary>
        public void Stopped(SequenceRunner run, int tick, int step, StopCause by, int from)
        {
            Numbered(Of(run, tick).Append("stopped at "), step).Append(" by ");
            switch (by)
            {
                case StopCause.Sequence:
                    _line.Append("sequence ").Append(from.ToString(CultureInfo.InvariantCulture));
                    break;
                case StopCause.Trigger:
                    _line.Append("a trigger or a button");
                    break;
                case StopCause.Hand:
                    _line.Append("hand");
                    break;
                default:
                    _line.Append("its logic system or installation switching off");
                    break;
            }

            End();
        }

        /// <summary>The start of a line: the tick and the programmable block running the automation.</summary>
        StringBuilder Begin(int tick)
        {
            _line.Clear().Append("tick ").Append(tick.ToString(CultureInfo.InvariantCulture)).Append(", \"").Append(_host()).Append('"');
            return _line;
        }

        /// <summary>The start of a line about <paramref name="run" />: the tick, the programmable block running the automation, the logic system and its installation, and the sequence.</summary>
        StringBuilder Of(SequenceRunner run, int tick)
        {
            Begin(tick).Append(", ");
            var sequence = run.Sequence;
            System(sequence.System, run.Instance);
            _line.Append(", sequence ").Append(sequence.Number.ToString(CultureInfo.InvariantCulture));
            if (sequence.HasName)
                _line.Append(" \"").Append(sequence.Name).Append('"');

            return _line.Append(": ");
        }

        /// <summary>The logic system and the installation it runs on, the installation counted from 1 as the screens count them, with its name where it has one.</summary>
        void System(AutomationSystem system, SystemInstance instance)
        {
            if (system == null)
                _line.Append("no logic system");
            else if (system.HasName)
                _line.Append('"').Append(system.Name).Append('"');
            else
                _line.Append("a logic system with no name");

            if (instance == null || system == null)
                return;

            _line.Append(" on installation ").Append(SystemSummary.Position(system, instance).ToString(CultureInfo.InvariantCulture));
            if (instance.HasName)
                _line.Append(" \"").Append(instance.Name).Append('"');
        }

        /// <summary>A step as the screens count them, from 1.</summary>
        static StringBuilder Numbered(StringBuilder line, int step)
        {
            return line.Append("step ").Append((step + 1).ToString(CultureInfo.InvariantCulture));
        }

        static string Script(Step step)
        {
            return step == null ? "nothing" : ProgramWriter.Line(step);
        }

        void End()
        {
            _write(_line.ToString());
        }
    }
}
