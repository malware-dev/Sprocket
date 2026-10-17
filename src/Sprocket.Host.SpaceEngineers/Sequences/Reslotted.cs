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

        /// <summary>The edit the slot changes go with.</summary>
        public Edit<T> Edit => _edit;

        /// <inheritdoc />
        public override ChangeKind Change => _edit.Change;

        /// <inheritdoc />
        public override bool Changes => _edit.Changes || Reslots;

        /// <inheritdoc />
        public override bool ChangesShape => _edit.ChangesShape || Reslots;

        /// <inheritdoc />
        public override void Touches(T subject, IList<object> screens)
        {
            _edit.Touches(subject, screens);
            foreach (var reslot in _reslots)
                reslot.Touches(screens);
        }

        /// <inheritdoc />
        public override void Reassert(T subject)
        {
            _edit.Reassert(subject);
        }

        /// <summary>Whether it changes any slot.</summary>
        bool Reslots
        {
            get
            {
                foreach (var reslot in _reslots)
                {
                    if (!reslot.IsEmpty)
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
            foreach (var reslot in _reslots)
                reslot.Apply();
        }

        /// <inheritdoc />
        public override void Undo(T subject)
        {
            for (var i = _reslots.Count - 1; i >= 0; i--)
                _reslots[i].Undo();

            _edit.Undo(subject);
        }

        /// <summary>While the edit can be taken back, and the slots stand as it left them.</summary>
        public override bool Undoable(T subject)
        {
            return _edit.Undoable(subject) && Holds(true);
        }

        /// <summary>While the edit can be made again, and the slots stand as they were before it.</summary>
        public override bool Redoable(T subject)
        {
            return _edit.Redoable(subject) && Holds(false);
        }

        /// <summary>Whether every slot change it carries stands as it was made, where <paramref name="made" />, or as before it.</summary>
        bool Holds(bool made)
        {
            foreach (var reslot in _reslots)
            {
                if (!reslot.Holds(made))
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Held(T subject, bool back)
        {
            return _edit.Held(subject, back);
        }
    }
}
