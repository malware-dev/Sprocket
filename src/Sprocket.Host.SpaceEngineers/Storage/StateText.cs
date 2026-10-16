using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
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
        /// <summary>What ends the line of a run started by hand, which may run where its sequence is switched off.</summary>
        /// <remarks>Persisted, and a phrase rather than a keyword, so it reserves no word in a script.</remarks>
        const string ByHand = "by hand";

        /// <summary>What heads the line naming the big owners the automation was last confirmed for.</summary>
        /// <remarks>Persisted. A name rather than a keyword, so it reserves no word in a script.</remarks>
        const string Owned = "owned";

        /// <summary>What heads the line naming who the automation belonged to when it paused, present only while it is paused.</summary>
        /// <remarks>Persisted. A name rather than a keyword, so it reserves no word in a script.</remarks>
        const string Paused = "paused";

        /// <summary>
        /// What is switched off and what is running, as a document, and who the automation belongs to where
        /// <paramref name="players" /> says.
        /// </summary>
        /// <remarks>
        /// A null runner writes the switches alone, which is what a caller with no runtime has. Who it belongs to goes only
        /// to a copy being edited: what a block keeps travels with every copy of the construct, which starts as nobody's.
        /// </remarks>
        public static string Save(Automation program, AutomationRunner runner, bool players = true)
        {
            var pen = new Pen();
            if (players)
            {
                Players(pen, Owned, program.Owners);
                Players(pen, Paused, program.PausedFrom);
            }

            var systems = program.Systems;

            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                var identity = system.Identity.ToString("D");

                if (!system.Enabled)
                {
                    pen.Start(Keyword.Off).Text(identity);
                    Why(pen, system.Halt);
                    pen.End();
                }

                var instances = system.Instances;
                for (var n = 0; n < instances.Count; n++)
                {
                    if (instances[n].Enabled)
                        continue;

                    pen.Start(Keyword.Off).Text(identity).Says(Keyword.Instance).Whole(instances[n].Number);
                    Why(pen, instances[n].Halt);
                    pen.End();
                }

                if (runner != null)
                {
                    Runs(pen, runner, system, identity);
                    Watches(pen, runner, system, identity);
                }
            }

            return pen.ToString();
        }

        /// <summary>Writes a line headed <paramref name="head" /> naming <paramref name="players" />, where there are any.</summary>
        static void Players(Pen pen, string head, List<long> players)
        {
            if (players.Count == 0)
                return;

            pen.Head(head);
            foreach (var player in players)
                pen.Whole(player);

            pen.End();
        }

        /// <summary>
        /// What is running, and nothing about what is switched off.
        /// </summary>
        /// <remarks>
        /// For carrying the runs across when the program is rebuilt from somebody else's switches: a
        /// client editing a Sprocket says what is off, and the server alone knows what is running.
        /// </remarks>
        public static string Running(Automation program, AutomationRunner runner)
        {
            var pen = new Pen();
            var systems = program.Systems;

            for (var i = 0; i < systems.Count; i++)
            {
                Runs(pen, runner, systems[i], systems[i].Identity.ToString("D"));
                Watches(pen, runner, systems[i], systems[i].Identity.ToString("D"));
            }

            return pen.ToString();
        }

        /// <summary>
        /// What each <c>when</c> and <c>every</c> remembers, one line per trigger on each one of its system.
        /// </summary>
        /// <remarks>
        /// A <c>when</c> fires on a change, which needs what it read at its last look; without it, a door that opened while
        /// the world was being written would be read as open all along. Written as what each block read, by the name the
        /// program gives it, or as whether all of them were there:
        /// <code>
        /// when "3f2a…" trigger 1 'Inner Door' on 'Outer Door' off
        /// when "3f2a…" instance 2 trigger 3 all on
        /// every "3f2a…" trigger 2 wait 120t
        /// </code>
        /// An <c>every</c> is written as how long since it last fell due, so it keeps its beat rather than starting a whole
        /// interval again. A trigger is counted by its place in its system, which reading the program keeps.
        /// </remarks>
        static void Watches(Pen pen, AutomationRunner runner, AutomationSystem system, string identity)
        {
            var uses = system.Uses;
            var triggers = system.Triggers;

            for (var t = 0; t < triggers.Count; t++)
            {
                var when = triggers[t] as WhenTrigger;
                var every = triggers[t] as EveryTrigger;
                var signal = triggers[t] as SignalTrigger;
                var watched = signal != null && signal.IsWatched;
                if (when == null && every == null && !watched)
                    continue;

                // A signal Sprocket watches for is a change on each block, kept block by block as a when on any of them is.
                var each = watched || (when != null && when.Quantifier == Quantifier.Any);

                for (var n = 0; n < uses.Count; n++)
                {
                    var poller = runner.Poller(triggers[t], uses[n]);
                    if (poller == null)
                        continue;

                    if (every != null && poller.Since < 0)
                        continue;

                    if (when != null && !each && poller.All < 0)
                        continue;

                    if (each && !Any(poller.Signals))
                        continue;

                    pen.Start(every != null ? Keyword.Every : Keyword.When).Text(identity);
                    if (system.IsBlueprint)
                        pen.Says(Keyword.Instance).Whole(uses[n].Number);

                    pen.Says(Keyword.Trigger).Whole(t + 1);

                    if (every != null)
                    {
                        pen.Says(Keyword.Wait).Ticks(poller.Since);
                    }
                    else if (!each)
                    {
                        pen.Says(Keyword.All).Says(poller.All == 1 ? Keyword.On : Keyword.Off);
                    }
                    else
                    {
                        foreach (var read in poller.Signals)
                            pen.Word(read.Key).Says(read.Value ? Keyword.On : Keyword.Off);
                    }

                    pen.End();
                }
            }
        }

        static bool Any(IEnumerable<KeyValuePair<string, bool>> signals)
        {
            using (var each = signals.GetEnumerator())
                return each.MoveNext();
        }

        /// <summary>
        /// Gives one <c>when</c> or <c>every</c> back what it remembered when the save caught it.
        /// </summary>
        /// <remarks>
        /// Dropped rather than reported where what it names has gone, or is no longer that kind of trigger, the same as a run:
        /// a program edited between saving and loading leaves those behind. A malformed line is reported.
        /// </remarks>
        static void Watch(Statement line, Automation program, AutomationRunner runner, Trouble trouble)
        {
            Guid identity;
            if (!Guid.TryParseExact(line.Text(0), "D", out identity))
            {
                trouble.Say(line, "expected the system's identity.");
                return;
            }

            var instanced = line.Says(1, Keyword.Instance);
            var at = instanced ? 3 : 1;
            var instance = instanced ? Counted(line, 2) : 0;
            var number = line.Says(at, Keyword.Trigger) ? Counted(line, at + 1) : 0;
            if ((instanced && instance < 1) || number < 1)
            {
                trouble.Say(line, "a trigger's memory is written as its system, then its instance where it has one, and trigger with a number counted from 1.");
                return;
            }

            Action<Poller> recall;
            var rest = at + 2;
            if (line.Key == Keyword.Every)
            {
                var ticks = line.ValueAt(rest + 1);
                if (!line.Says(rest, Keyword.Wait) || ticks.Form != ValueForm.Duration || ticks.Whole < 0 || ticks.Whole > int.MaxValue)
                {
                    trouble.Say(line, "expected wait and a duration, like wait 120t.");
                    return;
                }

                var since = (int)ticks.Whole;
                recall = poller => poller.RecallSince(since);
            }
            else if (line.Says(rest, Keyword.All))
            {
                if (!line.Says(rest + 1, Keyword.On) && !line.Says(rest + 1, Keyword.Off))
                {
                    trouble.Say(line, "expected on or off after all.");
                    return;
                }

                var all = line.Says(rest + 1, Keyword.On);
                recall = poller => poller.RecallAll(all);
            }
            else
            {
                var blocks = new List<string>();
                var read = new List<bool>();
                for (var i = rest; i < line.Count; i += 2)
                {
                    var block = line.ValueAt(i);
                    if (block.Form != ValueForm.Identifier || block.Word.Length == 0 || (!line.Says(i + 1, Keyword.On) && !line.Says(i + 1, Keyword.Off)))
                    {
                        trouble.Say(line, "expected each block's name followed by on or off.");
                        return;
                    }

                    blocks.Add(block.Word);
                    read.Add(line.Says(i + 1, Keyword.On));
                }

                recall = poller =>
                {
                    for (var i = 0; i < blocks.Count; i++)
                        poller.Recall(blocks[i], read[i]);
                };
            }

            if (runner == null)
                return;

            var system = program.FindSystem(identity);
            if (system == null || number > system.Triggers.Count)
                return;

            var trigger = system.Triggers[number - 1];
            var signal = trigger as SignalTrigger;
            var kind = line.Key == Keyword.Every
                ? trigger is EveryTrigger
                : trigger is WhenTrigger || (signal != null && signal.IsWatched);
            var one = !instanced ? (system.IsBlueprint ? null : system.First) : system.FindInstance(instance);
            if (!kind || one == null)
                return;

            runner.Recall(trigger, one, recall);
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
            var uses = system.Uses;
            var sequences = system.Sequences;

            for (var n = 0; n < uses.Count; n++)
            {
                for (var q = 0; q < sequences.Count; q++)
                {
                    var state = runner.Get(sequences[q], uses[n]);
                    if (state == null || !state.IsRunning || state.CurrentStep < 0)
                        continue;

                    pen.Start(Keyword.Running).Text(identity);
                    if (system.IsBlueprint)
                        pen.Says(Keyword.Instance).Whole(uses[n].Number);

                    pen.Says(Keyword.Sequence).Whole(sequences[q].Number)
                        .Says(Keyword.Step).Whole(state.CurrentStep);

                    if (state.WaitTicks > 0)
                        pen.Says(Keyword.Wait).Ticks(state.WaitTicks);
                    else if (state.IsWaiting)
                        pen.Says(Keyword.Await);

                    if (state.ByHand)
                        pen.Text(ByHand);

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

            foreach (var line in lines)
            {
                if (line.IsOff)
                {
                    trouble.Say(line, "unexpected '//'; only a step in a sequence can be switched off.");
                    continue;
                }

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

                if (line.Key == Keyword.When || line.Key == Keyword.Every)
                {
                    Watch(line, program, runner, trouble);
                    continue;
                }

                if (line.Key == Keyword.None && (line.Head == Owned || line.Head == Paused))
                {
                    Players(line, line.Head == Owned ? program.Owners : program.PausedFrom, trouble);
                    continue;
                }

                trouble.Say(line, "expected 'off', 'running', 'when', 'every', 'owned' or 'paused'.");
            }

            if (trouble.What.Length > 0)
                program.Damaged(trouble.What);

            return trouble.What;
        }

        /// <summary>Reads the players a line names into <paramref name="into" />, in place of what it held.</summary>
        static void Players(Statement line, List<long> into, Trouble trouble)
        {
            into.Clear();
            for (var i = 0; i < line.Count; i++)
            {
                var player = line.ValueAt(i);
                if (player.Form != ValueForm.Integer)
                {
                    trouble.Say(line, "expected players, each as a whole number.");
                    into.Clear();
                    return;
                }

                into.Add(player.Whole);
            }
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
                trouble.Say(line, "expected the system's identity.");
                return;
            }

            // A blueprint's run names its instance; a system's has none to name.
            var instanced = line.Says(1, Keyword.Instance);
            var at = instanced ? 3 : 1;
            if (!line.Says(at, Keyword.Sequence) || !line.Says(at + 2, Keyword.Step))
            {
                trouble.Say(line,
                    "a run is written as its system, then its instance where it has one, sequence and step, each with a number");
                return;
            }

            var instance = instanced ? Counted(line, 2) : 0;
            var sequence = Counted(line, at + 1);
            var step = line.ValueAt(at + 3);

            if ((instanced && instance < 1) || sequence < 1 || step.Form != ValueForm.Integer
                || step.Whole < 0 || step.Whole > int.MaxValue)
            {
                trouble.Say(line, "expected an instance and a sequence counted from 1, and a step from 0.");
                return;
            }

            var waiting = line.Says(at + 4, Keyword.Await);
            var held = 0;
            var after = waiting ? at + 5 : at + 4;

            if (line.Says(at + 4, Keyword.Wait))
            {
                var ticks = line.ValueAt(at + 5);
                if (ticks.Form != ValueForm.Duration || ticks.Whole < 0 || ticks.Whole > int.MaxValue)
                {
                    trouble.Say(line, "expected a duration after 'wait', like 60t.");
                    return;
                }

                held = (int)ticks.Whole;
                waiting = true;
                after = at + 6;
            }

            var byHand = after < line.Count && line.Text(after) == ByHand;

            if (runner == null)
                return;

            var system = program.FindSystem(identity);
            if (system == null)
                return;

            var one = !instanced ? (system.IsBlueprint ? null : system.First) : system.FindInstance(instance);
            var which = system.FindSequence(sequence);
            if (one == null || which == null)
                return;

            runner.Resume(which, one, (int)step.Whole, waiting, held, byHand);
        }

        /// <summary>A whole number in the line, or zero where it is not one.</summary>
        static int Counted(Statement line, int index)
        {
            var value = line.ValueAt(index);
            return value.Form == ValueForm.Integer && value.Whole > 0 && value.Whole <= int.MaxValue
                ? (int)value.Whole
                : 0;
        }

        /// <summary>What the reason Sprocket switched something off is written as.</summary>
        /// <remarks>Persisted: a spelling here cannot change without every saved reason being forgotten.</remarks>
        static readonly string[] Reasons =
        {
            "unstartable", "source gone", "block gone", "gate gone", "unoffered", "unfilled", "mistyped", "adrift",
            "slot empty", "circular", "unable"
        };

        static readonly SequenceProblem[] Problems =
        {
            SequenceProblem.NeverTriggered, SequenceProblem.MissingSource, SequenceProblem.MissingTarget,
            SequenceProblem.MissingGate, SequenceProblem.Unoffered, SequenceProblem.Unfilled, SequenceProblem.Mistyped,
            SequenceProblem.Adrift, SequenceProblem.EmptySlot, SequenceProblem.Circular,
            SequenceProblem.Unable
        };

        /// <summary>What a block's trouble is written as, beside <see cref="Troubles" />.</summary>
        /// <remarks>Persisted: a spelling here cannot change without every saved trouble being forgotten.</remarks>
        static readonly string[] Spelled =
        {
            "vent nowhere", "vent leaking", "vent no oxygen", "vent told to fill", "vent told to empty",
            "battery told to discharge", "battery told to auto", "battery off", "battery damaged", "battery no power",
            "piston no power", "piston no head", "piston stopped", "piston reversed extending", "piston reversed retracting",
            "rotor no power", "rotor no head", "rotor locked", "rotor no torque", "rotor stopped", "rotor held",
            "tank stockpile off", "tank no power", "tank no gas",
            "jump drive recharge off",
            "script not run",
            "door told to open", "door told to close"
        };

        static readonly Cause[] Troubles =
        {
            Cause.VentNowhere, Cause.VentNotAirtight, Cause.VentNoOxygen, Cause.VentToldToFill, Cause.VentToldToEmpty,
            Cause.BatteryToldToDischarge, Cause.BatteryToldToAuto, Cause.BatteryOff, Cause.BatteryDamaged, Cause.BatteryNoPower,
            Cause.PistonNoPower, Cause.PistonNoHead, Cause.PistonStopped, Cause.PistonReversedExtending,
            Cause.PistonReversedRetracting,
            Cause.RotorNoPower, Cause.RotorNoHead, Cause.RotorLocked, Cause.RotorNoTorque, Cause.RotorStopped, Cause.RotorHeld,
            Cause.TankStockpileOff, Cause.TankNoPower, Cause.TankNoGas,
            Cause.JumpDriveRechargeOff,
            Cause.ScriptNotRun,
            Cause.DoorToldToOpen, Cause.DoorToldToClose
        };

        /// <summary>Writes why Sprocket switched something off: what was wrong, in which sequence, at which step, with which block, and what the block said.</summary>
        static void Why(Pen pen, Halt halt)
        {
            if (halt == null)
                return;

            var at = Array.IndexOf(Problems, halt.Problem);
            pen.Text(at < 0 ? "unknown" : Reasons[at]);

            if (halt.Sequence > 0)
                pen.Says(Keyword.Sequence).Whole(halt.Sequence);

            if (halt.Step >= 0)
                pen.Says(Keyword.Step).Whole(halt.Step);

            if (!string.IsNullOrEmpty(halt.Block))
                pen.Says(Keyword.Block).Text(halt.Block);

            var cause = Array.IndexOf(Troubles, halt.Because.Cause);
            if (cause < 0)
                return;

            pen.Text(Spelled[cause]);
            if (!string.IsNullOrEmpty(halt.Because.About))
                pen.Text(halt.Because.About);
        }

        /// <summary>Why a line says Sprocket switched something off, read from <paramref name="from" /> on, or null where it says nobody but a person did.</summary>
        static Halt Why(Statement line, int from)
        {
            if (from >= line.Count)
                return null;

            var at = Array.IndexOf(Reasons, line.Text(from));
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
                    var spelled = Array.IndexOf(Spelled, line.Text(i));
                    cause = spelled < 0 ? Cause.None : Troubles[spelled];
                }
                else
                {
                    about = line.Text(i);
                }
            }

            var because = cause == Cause.None ? World.Trouble.None : World.Trouble.Kept(cause, string.IsNullOrEmpty(about) ? null : about);
            return new Halt(at < 0 ? SequenceProblem.None : Problems[at], sequence, step, because,
                string.IsNullOrEmpty(block) ? null : block);
        }

        static void Switch(Statement line, Automation program, Trouble trouble)
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
    }
}
