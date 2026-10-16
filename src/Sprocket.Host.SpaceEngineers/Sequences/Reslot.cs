using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What a blueprint's slots become as the sets of blocks its steps and triggers name change, and how to take it back.</summary>
    /// <remarks>
    /// Worked out by <see cref="Slotting" /> and carried by the edit that brought it about, so taking the edit back puts every
    /// slot, every installation's blocks in it and every name a step uses back as it was. Each part records what it replaced,
    /// so undoing never has to work anything out again.
    /// </remarks>
    public sealed class Reslot
    {
        readonly SystemInstance _shown;
        readonly List<Part> _made = new List<Part>();
        readonly List<List<string>> _madeFills = new List<List<string>>();
        readonly List<Part> _refilled = new List<Part>();
        readonly List<List<string>> _refilledWas = new List<List<string>>();
        readonly List<List<string>> _refilledNow = new List<List<string>>();
        readonly List<string> _expectedWas = new List<string>();
        readonly List<string> _expectedNow = new List<string>();
        readonly List<IList<string>> _renamed = new List<IList<string>>();
        readonly List<List<string>> _renamedWas = new List<List<string>>();
        readonly List<List<string>> _renamedNow = new List<List<string>>();
        readonly List<Part> _dropped = new List<Part>();
        readonly List<int> _droppedAt = new List<int>();
        readonly List<Dictionary<SystemInstance, List<string>>> _droppedFills = new List<Dictionary<SystemInstance, List<string>>>();

        /// <summary>Changes to the slots of <paramref name="system" />, filled as <paramref name="shown" /> fills them.</summary>
        public Reslot(AutomationSystem system, SystemInstance shown)
        {
            System = system;
            _shown = shown;
        }

        /// <summary>The blueprint whose slots these are.</summary>
        public AutomationSystem System { get; private set; }

        /// <summary>Whether this changes nothing.</summary>
        public bool IsEmpty => _made.Count == 0 && _refilled.Count == 0 && _renamed.Count == 0 && _dropped.Count == 0;

        /// <summary>Makes the slot <paramref name="part" />, filled with <paramref name="blocks" /> in the installation shown.</summary>
        public void Make(Part part, IList<string> blocks)
        {
            _made.Add(part);
            _madeFills.Add(new List<string>(blocks));
        }

        /// <summary>Gives the slot <paramref name="part" /> the blocks <paramref name="blocks" /> in the installation shown, expecting <paramref name="expects" />.</summary>
        public void Refill(Part part, IList<string> blocks, string expects)
        {
            var had = _shown.FilledWith(part.Id);
            _refilled.Add(part);
            _refilledWas.Add(had == null ? new List<string>() : new List<string>(had));
            _refilledNow.Add(new List<string>(blocks));
            _expectedWas.Add(part.Expects);
            _expectedNow.Add(expects);
        }

        /// <summary>
        /// Adds to <paramref name="screens" /> the slots this makes, fills and drops, the installation shown where it fills any,
        /// and the sequence or logic system a list it renames is on. The logic system's screen lists the slots, and a slot coming
        /// or going there is the slot's change, not one made on that screen.
        /// </summary>
        public void Touches(IList<object> screens)
        {
            foreach (var part in _made)
                Touch(screens, part);

            foreach (var part in _refilled)
                Touch(screens, part);

            foreach (var part in _dropped)
                Touch(screens, part);

            if (_made.Count + _refilled.Count + _dropped.Count > 0)
                Touch(screens, _shown);

            foreach (var names in _renamed)
                Touch(screens, Owner(names));
        }

        /// <summary>The sequence whose step holds <paramref name="names" />, or the logic system where a trigger does.</summary>
        object Owner(IList<string> names)
        {
            var sequences = System.Sequences;
            for (var s = 0; s < sequences.Count; s++)
            {
                var steps = sequences[s].Steps;
                for (var i = 0; i < steps.Count; i++)
                {
                    if (ReferenceEquals(steps[i].Targets, names))
                        return sequences[s];
                }
            }

            return System;
        }

        static void Touch(IList<object> screens, object screen)
        {
            if (!screens.Contains(screen))
                screens.Add(screen);
        }

        /// <summary>Has <paramref name="names" />, a step's or a trigger's list, name <paramref name="now" /> in place of what it names.</summary>
        public void Rename(IList<string> names, IList<string> now)
        {
            _renamed.Add(names);
            _renamedWas.Add(new List<string>(names));
            _renamedNow.Add(new List<string>(now));
        }

        /// <summary>Takes out the slot <paramref name="part" />, which nothing uses and no other installation holds blocks for.</summary>
        public void Drop(Part part)
        {
            _dropped.Add(part);
            _droppedAt.Add(-1);
            var fills = new Dictionary<SystemInstance, List<string>>();
            var instances = System.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                var had = instances[i].FilledWith(part.Id);
                if (had != null)
                    fills[instances[i]] = new List<string>(had);
            }

            _droppedFills.Add(fills);
        }

        /// <summary>Makes the changes, in the order they were worked out.</summary>
        public void Apply()
        {
            for (var i = 0; i < _made.Count; i++)
            {
                System.PutPartBack(-1, _made[i]);
                _shown.Fill(_made[i].Id, _madeFills[i]);
            }

            for (var i = 0; i < _refilled.Count; i++)
            {
                _shown.Fill(_refilled[i].Id, _refilledNow[i]);
                _refilled[i].Expects = _expectedNow[i];
            }

            for (var i = 0; i < _renamed.Count; i++)
                Put(_renamed[i], _renamedNow[i]);

            for (var i = 0; i < _dropped.Count; i++)
            {
                _droppedAt[i] = System.ForgetPart(_dropped[i]);
                foreach (var instance in _droppedFills[i].Keys)
                    instance.Clear(_dropped[i].Id);
            }
        }

        /// <summary>Takes the changes back, last first, leaving the slots as they were before <see cref="Apply" />.</summary>
        public void Undo()
        {
            for (var i = _dropped.Count - 1; i >= 0; i--)
            {
                System.PutPartBack(_droppedAt[i], _dropped[i]);
                foreach (var fill in _droppedFills[i])
                    fill.Key.Fill(_dropped[i].Id, fill.Value);
            }

            for (var i = _renamed.Count - 1; i >= 0; i--)
                Put(_renamed[i], _renamedWas[i]);

            for (var i = _refilled.Count - 1; i >= 0; i--)
            {
                if (_refilledWas[i].Count > 0)
                    _shown.Fill(_refilled[i].Id, _refilledWas[i]);
                else
                    _shown.Clear(_refilled[i].Id);

                _refilled[i].Expects = _expectedWas[i];
            }

            for (var i = _made.Count - 1; i >= 0; i--)
            {
                _shown.Clear(_made[i].Id);
                System.ForgetPart(_made[i]);
            }
        }

        /// <summary>
        /// Whether the slots stand as this left them, where <paramref name="made" />, or as they were before it: its slots made
        /// or not, the lists it renamed saying what it left, and its slots dropped or not.
        /// </summary>
        /// <remarks>What decides whether the step carrying it can still be walked; a walk of what it changed, and no more.</remarks>
        public bool Holds(bool made)
        {
            foreach (var part in _made)
            {
                if ((System.FindPart(part.Id) == part) != made)
                    return false;
            }

            for (var i = 0; i < _renamed.Count; i++)
            {
                if (!Same(_renamed[i], made ? _renamedNow[i] : _renamedWas[i]))
                    return false;
            }

            foreach (var part in _dropped)
            {
                if ((System.FindPart(part.Id) == part) == made)
                    return false;
            }

            return true;
        }

        static bool Same(IList<string> left, IList<string> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        static void Put(IList<string> target, IList<string> source)
        {
            target.Clear();
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }
    }
}
