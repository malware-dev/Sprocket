using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Wiring
{
    /// <summary>The keys Sprocket writes into the toolbar slots it wires, and what pressing a slot holding each one runs or stops.</summary>
    public sealed class SlotKeys
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
        public SlotKeys(Automation program, AutomationRunner runner)
        {
            _program = program;
            _runner = runner;
        }

        /// <summary>
        /// What goes in a slot: which system, which one of it, which sequence.
        /// </summary>
        /// <remarks>
        /// Persisted in the slot and read back verbatim. The system is named by its identity, so a button keeps working through
        /// a rename.
        /// </remarks>
        public static string KeyOf(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            return $"{system.Identity:D}/{(instance?.Number ?? 0)}/{sequence.Number}";
        }

        /// <summary>
        /// What goes in a slot for a button that stops the sequence rather than starting it, where <paramref name="stops" /> says so.
        /// </summary>
        /// <remarks>Persisted in the slot: a key keeps the meaning it was written with.</remarks>
        public static string KeyOf(AutomationSystem system, SystemInstance instance, Sequence sequence,
            bool stops)
        {
            var key = KeyOf(system, instance, sequence);
            return stops ? $"{key}/stop" : key;
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

            // Said on every press, so a press that does nothing still shows it arrived.
            Target target;
            if (!_byKey.TryGetValue(key, out target))
            {
                GameLog.Say($"a button fired {key}, which nothing here answers");
                return;
            }

            GameLog.Say($"a button fired {key}, {(target.Stops ? "stopping " : "running ")}sequence {target.Sequence.Number}{(target.Instance == null ? "" : $" on {target.Instance.Number}")}");

            if (target.Stops)
                _runner.Halt(target.Sequence, target.Instance, StopCause.Trigger, 0);
            else
                _runner.Call(target.Sequence, target.Instance);
        }

        /// <summary>
        /// Rebuilds what the keys name, from the program.
        /// </summary>
        /// <remarks>Called with the rest of the reconcile, when the program's wiring changes.</remarks>
        public void Index()
        {
            _byKey.Clear();

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = system.Sequences;
                var instances = system.Uses;

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
