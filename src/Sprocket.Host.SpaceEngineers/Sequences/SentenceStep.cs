using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One numbered line of the sequence.
    /// </summary>
    /// <remarks>
    /// Flat and numbered. Nesting is what defeats novices, so there isn't any -
    /// every line sits at the same level and reads top to bottom.
    /// </remarks>
    public sealed class SentenceStep : Observable
    {
        public SentenceStep(int number, StepKind kind)
        {
            Number = number;
            Kind = kind;
            Slots = new List<SentenceSlot>();
        }

        public int Number
        {
            get { return _number; }
            set { Set(ref _number, value, nameof(Number)); }
        }

        int _number;

        /// <summary>Which sort of line this is, which decides how a target opens it.</summary>
        public StepKind Kind { get; private set; }

        public List<SentenceSlot> Slots { get; private set; }

        /// <summary>
        /// The sequence is sitting on this step right now. Seeing execution move down the
        /// list is what makes a sequence readable instead of imaginable.
        /// </summary>
        public bool IsCurrent
        {
            get { return _isCurrent; }
            set { Set(ref _isCurrent, value, nameof(IsCurrent)); }
        }

        bool _isCurrent;

        /// <summary>
        /// Where this step sits, counting from zero.
        /// </summary>
        /// <remarks>
        /// <see cref="Number"/> is what it is called on screen and starts at one. This is the
        /// position <see cref="MoveToCommand"/> speaks in, and the two differ on purpose:
        /// somebody reading "step 3" and a target computing an index should not have to agree
        /// about which one they mean.
        /// </remarks>
        public int Position
        {
            get { return _position; }
            set { Set(ref _position, value, nameof(Position)); }
        }

        int _position;

        /// <summary>
        /// Whether the reason the sequence cannot work is this step.
        /// </summary>
        /// <remarks>
        /// At most one line of a sentence carries this at a time - see <c>SequenceFault</c>, which
        /// reports one fault so that a screen says one thing.
        /// </remarks>
        public bool HasProblem
        {
            get { return _hasProblem; }
            set { Set(ref _hasProblem, value, nameof(HasProblem)); }
        }

        bool _hasProblem;

        /// <summary>
        /// Send a position here to move this step there.
        /// </summary>
        /// <remarks>
        /// One command rather than an up and a down, because up-and-down-by-one is a control
        /// and core does not get to choose controls. A target may draw two arrows and send
        /// <c>Position - 1</c> and <c>Position + 1</c>, or let somebody drag the line and send
        /// where they dropped it, or offer a number. All three are answers to the same
        /// statement, and none of them is core's business.
        ///
        /// **A target must offer some way to reorder.** Core cannot enforce that in code - see
        /// <see cref="Conformance"/>, which is where it is at least written down and where a
        /// host is made to acknowledge it.
        ///
        /// Out of range is ignored, so a drop past either end costs nothing.
        /// </remarks>
        public int MoveToCommand
        {
            get { return _moveToCommand; }
            set { Set(ref _moveToCommand, value, nameof(MoveToCommand)); }
        }

        int _moveToCommand;

        public int DeleteCommand
        {
            get { return _deleteCommand; }
            set { Set(ref _deleteCommand, value, nameof(DeleteCommand)); }
        }

        int _deleteCommand;
    }
}
