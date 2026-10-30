using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>When each logic system of a program last changed, by comparing what it says with what it said.</summary>
    public sealed class ChangeDates
    {
        readonly Automation _program;
        readonly Dictionary<Guid, string> _said = new Dictionary<Guid, string>();
        readonly List<Guid> _gone = new List<Guid>();
        int _saidEdits;
        bool _reshaped;

        /// <summary>Dates the systems of <paramref name="program" /> from here on.</summary>
        public ChangeDates(Automation program)
        {
            _program = program;

            var systems = _program.Systems;
            for (var i = 0; i < systems.Count; i++)
                _said[systems[i].Identity] = ProgramWriter.Said(systems[i]);

            _saidEdits = _program.Edits;
            _program.Reshaped += () => _reshaped = true;
        }

        /// <summary><paramref name="now" />, or a second after <paramref name="origin" /> where that is later, so a change is dated after the version it changes.</summary>
        static DateTime After(DateTime now, DateTime? origin)
        {
            // A date is written to the second (ScriptFormat.Time), so a change within the second a blueprint was kept would read
            // back as that blueprint.
            return origin.HasValue && now < origin.Value.AddSeconds(1) ? origin.Value.AddSeconds(1) : now;
        }

        /// <summary>Dates with the time now each system whose words have changed, reading every system where <paramref name="surely" />.</summary>
        /// <remarks>
        /// Called every tick, costing a few comparisons where nothing changed; called surely at the end of every command and
        /// before the program or a blueprint is written out.
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
                var said = ProgramWriter.Said(systems[i]);
                string was;
                if (_said.TryGetValue(systems[i].Identity, out was) ? was != said : !systems[i].Changed.HasValue)
                    systems[i].Changed = After(now, systems[i].OriginChanged);

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
            _said[system.Identity] = ProgramWriter.Said(system);
        }
    }
}
