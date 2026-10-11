using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>An edit to a blueprint, together with what it does to the blueprint's slots, taken back as one.</summary>
    /// <remarks>
    /// The slots the picks for it bring about are worked out with the picks and handed in, in the order they were made; any
    /// other edit has them worked out the first time it is made, from the lists as it leaves them. Either way they are kept, so
    /// making the edit again after taking it back makes the same slots.
    /// </remarks>
    public sealed class Reslotted<T> : Edit<T>
    {
        readonly Edit<T> _edit;
        readonly Func<Reslot> _settle;
        readonly List<Reslot> _reslots = new List<Reslot>();
        bool _made;

        /// <summary>
        /// <paramref name="edit" />, bringing about <paramref name="picked" /> where picks worked them out, and what
        /// <paramref name="settle" /> works out after it where none did.
        /// </summary>
        public Reslotted(Edit<T> edit, IList<Reslot> picked, Func<Reslot> settle)
        {
            _edit = edit;
            _settle = settle;
            for (var i = 0; i < picked.Count; i++)
                _reslots.Add(picked[i]);
        }

        /// <inheritdoc />
        public override Change Change => _edit.Change;

        /// <inheritdoc />
        public override bool Changes => _edit.Changes || Reslots;

        /// <inheritdoc />
        public override bool ChangesShape => _edit.ChangesShape || Reslots;

        /// <summary>Whether it changes any slot.</summary>
        bool Reslots
        {
            get
            {
                for (var i = 0; i < _reslots.Count; i++)
                {
                    if (!_reslots[i].IsEmpty)
                        return true;
                }

                return false;
            }
        }

        /// <inheritdoc />
        public override void Apply(T subject)
        {
            _edit.Apply(subject);
            if (!_made && _reslots.Count == 0)
                _reslots.Add(_settle());

            _made = true;
            for (var i = 0; i < _reslots.Count; i++)
                _reslots[i].Apply();
        }

        /// <inheritdoc />
        public override void Undo(T subject)
        {
            for (var i = _reslots.Count - 1; i >= 0; i--)
                _reslots[i].Undo();

            _edit.Undo(subject);
        }

        /// <inheritdoc />
        public override bool Undoable(T subject)
        {
            return _edit.Undoable(subject);
        }

        /// <inheritdoc />
        public override bool Redoable(T subject)
        {
            return _edit.Redoable(subject);
        }
    }
}
