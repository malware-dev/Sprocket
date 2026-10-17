using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>The keys Sprocket writes into the toolbar slots it wires, and what pressing a slot holding each one runs or stops.</summary>
    public sealed class Keys
    {
        // What a key names: a sequence, which one of its system, and what to do to it.
        struct Target
        {
            public Sequence Sequence;
            public SystemInstance Instance;
            public bool Stops;
        }

        readonly Dictionary<string, Target> _byKey =
            new Dictionary<string, Target>(StringComparer.Ordinal);

        readonly Automation _program;
        readonly AutomationRunner _runner;

        /// <summary>The keys of <paramref name="program" />, whose presses <paramref name="runner" /> carries out.</summary>
        public Keys(Automation program, AutomationRunner runner)
        {
            _program = program;
            _runner = runner;
        }

        /// <summary>
        /// What goes in a slot: which system, which one of it, which sequence.
        /// </summary>
        /// <remarks>
        /// <b>The system is named by its identity</b>, because a number cannot say which system it means once
        /// systems are exported, imported, or on constructs that join, and a name changes. So a button keeps
        /// working through a rename. Built once, at bind, and read back verbatim.
        /// </remarks>
        public static string KeyOf(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            return $"{system.Identity:D}/{(instance?.Number ?? 0)}/{sequence.Number}";
        }

        /// <summary>
        /// What goes in a slot for a button that stops the sequence rather than starting it, where <paramref name="stops" /> says so.
        /// </summary>
        /// <remarks>
        /// The verb is on the end rather than the front, so the key that runs a sequence reads the same as the one that stops
        /// it without its verb. A slot is never rewritten, so a key keeps the meaning it was written with.
        /// </remarks>
        public static string KeyOf(AutomationSystem system, SystemInstance instance, Sequence sequence,
            bool stops)
        {
            var key = KeyOf(system, instance, sequence);
            return stops ? $"{key}/stop" : key;
        }

        /// <summary>
        /// What goes in a slot for a trigger that acts itself: which system, which one of it, and the signal part, or the
        /// slot and blocks of a signal written in place.
        /// </summary>
        /// <remarks>
        /// By the part's name, because a trigger has no number and its part is what each instance answers. Renaming the part
        /// changes what is wanted, and the slot is written again with the rest of the wiring. Marked <c>part:</c>, because a
        /// part may be called anything, a sequence's number and <c>2/stop</c> among it, and <c>at:</c> for one written in
        /// place, which is the slot and the blocks it is sent from.
        /// </remarks>
        public static string KeyOf(AutomationSystem system, SystemInstance instance, SignalTrigger trigger)
        {
            var head = $"{system.Identity:D}/{(instance?.Number ?? 0)}";
            if (!trigger.IsWritten)
                return $"{head}/part:{trigger.Part}";

            return $"{head}/at:{(trigger.At == null ? "" : trigger.At.EventId)}:{string.Join(",", trigger.Sources)}";
        }

        /// <summary>Runs or stops what <paramref name="key" /> names, a block having fired one of the actions Sprocket put in it.</summary>
        public void Fire(string key)
        {
            // A press can come before the first tick after a load, as a timer block's can, and finds what it starts all the same.
            if (_byKey.Count == 0)
            {
                Index();
                _runner.Prepare();
            }

            // Said every time, because a press that does nothing is otherwise invisible: this says whether it arrived.
            Target target;
            if (!_byKey.TryGetValue(key, out target))
            {
                Told.Say($"a button fired {key}, which nothing here answers");
                return;
            }

            Told.Say($"a button fired {key}, {(target.Stops ? "stopping " : "running ")}{(!target.Sequence.IsAlone ? $"sequence {target.Sequence.Number}" : "what its trigger does")}{(target.Instance == null ? "" : $" on {target.Instance.Number}")}");

            if (target.Stops)
                _runner.Halt(target.Sequence, target.Instance, Stopper.Trigger, 0);
            else
                _runner.Call(target.Sequence, target.Instance);
        }

        /// <summary>
        /// Rebuilds what the keys name, from the program.
        /// </summary>
        /// <remarks>
        /// Cleared rather than merged, so a deleted sequence stops answering. Runs with the rest of
        /// the reconcile, on the same one-integer signal, because a sequence or one of a system
        /// appears and disappears only by an edit to the program.
        /// </remarks>
        public void Index()
        {
            _byKey.Clear();

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = system.Sequences;
                var instances = system.Uses;

                var triggers = system.Triggers;
                for (var t = 0; t < triggers.Count; t++)
                {
                    var needed = triggers[t] as SignalTrigger;
                    var alone = needed == null || needed.IsWatched ? null : _runner.Alone(system, needed);
                    for (var n = 0; alone != null && n < instances.Count; n++)
                    {
                        var target = new Target();
                        target.Sequence = alone;
                        target.Instance = instances[n];
                        _byKey[KeyOf(system, instances[n], needed)] = target;
                    }
                }

                for (var q = 0; q < sequences.Count; q++)
                {
                    for (var n = 0; n < instances.Count; n++)
                        Register(system, instances[n], sequences[q]);
                }
            }
        }

        /// <summary>
        /// Both names a sequence answers to: the one that runs it and the one that stops it.
        /// </summary>
        /// <remarks>
        /// Both always, rather than only the ones some trigger asks for, because a key is a name and
        /// not a permission - a slot naming one is a slot somebody made, whatever the program has
        /// since become.
        /// </remarks>
        void Register(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            var target = new Target();
            target.Sequence = sequence;
            target.Instance = instance;
            _byKey[KeyOf(system, instance, sequence)] = target;

            var halting = new Target();
            halting.Sequence = sequence;
            halting.Instance = instance;
            halting.Stops = true;
            _byKey[KeyOf(system, instance, sequence, true)] = halting;
        }

        /// <summary>Whether <paramref name="key" /> names something of the program.</summary>
        public bool Knows(string key)
        {
            return _byKey.ContainsKey(key);
        }
    }
}
