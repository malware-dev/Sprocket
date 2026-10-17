using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>When each logic system of a program last changed, by comparing what it says with what it said.</summary>
    public sealed class Dates
    {
        readonly Automation _program;
        readonly Dictionary<Guid, string> _said = new Dictionary<Guid, string>();
        readonly List<Guid> _gone = new List<Guid>();
        int _saidEdits;
        bool _reshaped;

        /// <summary>Dates the systems of <paramref name="program" /> from here on.</summary>
        public Dates(Automation program)
        {
            _program = program;

            // What each system says as the editor finds it, so only what changes from here on is dated.
            var systems = _program.Systems;
            for (var i = 0; i < systems.Count; i++)
                _said[systems[i].Identity] = Scribe.Said(systems[i]);

            _saidEdits = _program.Edits;
            _program.Reshaped += () => _reshaped = true;
        }

        /// <summary>
        /// Dates each system whose words have changed since they were last read with the time now; a few comparisons where
        /// nothing has, unless <paramref name="surely" /> asks for every system to be read.
        /// </summary>
        /// <remarks>
        /// Asked on every tick, where the program saying it was edited, reshaped or given another system is what makes it
        /// read anything; and surely at the end of every command and before the program or a blueprint is written out, so a
        /// change made by a way that says nothing is dated by then. A system's words are read without its date, so
        /// switching it, which is not something it says, dates nothing. Known by identity, so a program read again over this
        /// one is compared with what it was. A system met for the first time is dated only where it has no date: one just
        /// made.
        /// </remarks>
        public void Stamp(bool surely = false)
        {
            if (!surely && !_reshaped && _program.Edits == _saidEdits && _program.Systems.Count == _said.Count)
                return;

            _reshaped = false;
            _saidEdits = _program.Edits;

            var now = DateTime.UtcNow;
            var systems = _program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                var said = Scribe.Said(systems[i]);
                string was;
                if (_said.TryGetValue(systems[i].Identity, out was) ? was != said : !systems[i].Changed.HasValue)
                    systems[i].Changed = now;

                _said[systems[i].Identity] = said;
            }

            if (_said.Count == systems.Count)
                return;

            _gone.Clear();
            foreach (var identity in _said.Keys)
            {
                if (_program.FindSystem(identity) == null)
                    _gone.Add(identity);
            }

            foreach (var gone in _gone)
                _said.Remove(gone);
        }

        /// <summary>Takes what <paramref name="system" /> says now as what it said, so nothing reads it as changed since.</summary>
        public void Know(AutomationSystem system)
        {
            _said[system.Identity] = Scribe.Said(system);
        }
    }
}
