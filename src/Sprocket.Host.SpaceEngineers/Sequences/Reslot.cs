using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What a blueprint's slots become as the sets of blocks its steps and triggers name change, and how to take it back.</summary>
    public sealed class Reslot
    {
        readonly SystemInstance _shown;
        readonly List<Part> _made = new List<Part>();
        readonly List<List<string>> _madeFills = new List<List<string>>();
        readonly List<List<int>> _madeFacets = new List<List<int>>();
        readonly List<Part> _refilled = new List<Part>();
        readonly List<List<string>> _refilledWas = new List<List<string>>();
        readonly List<List<int>> _refilledFacetsWas = new List<List<int>>();
        readonly List<List<string>> _refilledNow = new List<List<string>>();
        readonly List<List<int>> _refilledFacetsNow = new List<List<int>>();
        readonly List<string> _expectedWas = new List<string>();
        readonly List<string> _expectedNow = new List<string>();
        readonly List<string> _holdsWas = new List<string>();
        readonly List<string> _holdsNow = new List<string>();
        readonly List<IList<string>> _renamed = new List<IList<string>>();
        readonly List<List<string>> _renamedWas = new List<List<string>>();
        readonly List<List<string>> _renamedNow = new List<List<string>>();
        readonly List<Part> _dropped = new List<Part>();
        readonly List<int> _droppedAt = new List<int>();
        readonly List<List<SlotAnswer>> _droppedAnswers = new List<List<SlotAnswer>>();

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

        /// <summary>The installation whose blocks the slots this makes and fills are filled with.</summary>
        public SystemInstance Shown => _shown;

        /// <summary>The slots this makes, in the order it makes them.</summary>
        public IReadOnlyList<Part> Made => _made;

        /// <summary>The blocks the installation shown fills the slot at <paramref name="made" /> of <see cref="Made" /> with, by their names in the program.</summary>
        public IList<string> MadeWith(int made) => _madeFills[made];

        /// <summary>How many lists of names, each a step's or a trigger's, this renames.</summary>
        public int Renames => _renamed.Count;

        /// <summary>What the list at <paramref name="rename" /> of those this renames names before it.</summary>
        public IList<string> RenamedFrom(int rename) => _renamedWas[rename];

        /// <summary>What the list at <paramref name="rename" /> of those this renames names after it.</summary>
        public IList<string> RenamedTo(int rename) => _renamedNow[rename];

        /// <summary>The slots this takes out, in the order it takes them out.</summary>
        public IReadOnlyList<Part> Dropped => _dropped;

        /// <summary>
        /// Makes the slot <paramref name="part" />, filled with <paramref name="blocks" /> in the installation shown, holding the
        /// facet <paramref name="facets" /> numbers of each where it holds a type the host provides.
        /// </summary>
        public void Make(Part part, IList<string> blocks, IList<int> facets = null)
        {
            _made.Add(part);
            _madeFills.Add(new List<string>(blocks));
            _madeFacets.Add(Copied(facets));
        }

        /// <summary>
        /// Fills the slot <paramref name="part" /> with <paramref name="blocks" /> in the installation shown, expecting
        /// <paramref name="expects" />, and holding the facet <paramref name="facets" /> numbers of each where it holds a type
        /// the host provides, and holding <paramref name="holds" /> where that is not null.
        /// </summary>
        public void Refill(Part part, IList<string> blocks, string expects, IList<int> facets = null, string holds = null)
        {
            var had = _shown.FilledWith(part.Id);
            _refilled.Add(part);
            _refilledWas.Add(had == null ? new List<string>() : new List<string>(had));
            _refilledFacetsWas.Add(Copied(_shown.Facets(part.Id)));
            _refilledNow.Add(new List<string>(blocks));
            _refilledFacetsNow.Add(Copied(facets));
            _expectedWas.Add(part.Expects);
            _expectedNow.Add(expects);
            _holdsWas.Add(part.Holds);
            _holdsNow.Add(holds ?? part.Holds);
        }

        /// <summary>
        /// Adds to <paramref name="screens" /> the slots this makes, fills and drops, the installation shown where it fills any,
        /// and the sequence or logic system a list it renames is on.
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

        /// <summary>Takes out the slot <paramref name="part" />, which nothing uses, with how every installation answers it.</summary>
        public void Drop(Part part)
        {
            _dropped.Add(part);
            _droppedAt.Add(-1);
            var answers = new List<SlotAnswer>();
            foreach (var instance in System.Instances)
            {
                var answer = SlotAnswer.Of(instance, part.Id);
                if (answer != null)
                    answers.Add(answer);
            }

            _droppedAnswers.Add(answers);
        }

        /// <summary>Makes the changes, in the order they were worked out.</summary>
        public void Apply()
        {
            for (var i = 0; i < _made.Count; i++)
            {
                System.PutPartBack(-1, _made[i]);
                Fill(_shown, _made[i].Id, _madeFills[i], _madeFacets[i]);
            }

            for (var i = 0; i < _refilled.Count; i++)
            {
                _refilled[i].Holds = _holdsNow[i];
                Fill(_shown, _refilled[i].Id, _refilledNow[i], _refilledFacetsNow[i]);
                _refilled[i].Expects = _expectedNow[i];
            }

            for (var i = 0; i < _renamed.Count; i++)
                Put(_renamed[i], _renamedNow[i]);

            for (var i = 0; i < _dropped.Count; i++)
            {
                _droppedAt[i] = System.ForgetPart(_dropped[i]);
                foreach (var answer in _droppedAnswers[i])
                    answer.Instance.Clear(_dropped[i].Id);
            }
        }

        /// <summary>Takes the changes back, last first, leaving the slots as they were before <see cref="Apply" />.</summary>
        public void Undo()
        {
            for (var i = _dropped.Count - 1; i >= 0; i--)
            {
                System.PutPartBack(_droppedAt[i], _dropped[i]);
                foreach (var answer in _droppedAnswers[i])
                    answer.Put(_dropped[i].Id);
            }

            for (var i = _renamed.Count - 1; i >= 0; i--)
                Put(_renamed[i], _renamedWas[i]);

            for (var i = _refilled.Count - 1; i >= 0; i--)
            {
                if (_refilledWas[i].Count > 0)
                    Fill(_shown, _refilled[i].Id, _refilledWas[i], _refilledFacetsWas[i]);
                else
                    _shown.Clear(_refilled[i].Id);

                _refilled[i].Expects = _expectedWas[i];
                _refilled[i].Holds = _holdsWas[i];
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

        /// <summary>Fills <paramref name="slot" /> of <paramref name="instance" /> with <paramref name="blocks" />, each holding its facet of <paramref name="facets" /> where there are any.</summary>
        static void Fill(SystemInstance instance, string slot, IList<string> blocks, IList<int> facets)
        {
            if (facets == null)
                instance.Fill(slot, blocks);
            else
                instance.Fill(slot, blocks, facets);
        }

        static List<int> Copied(IList<int> facets) => facets == null ? null : new List<int>(facets);

        static void Put(IList<string> target, IList<string> source)
        {
            target.Clear();
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }
    }
}
