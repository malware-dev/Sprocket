using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System.Collections.Generic;
using Lumel.Controls;
using Lumel.Styling;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>A fillable part of a sentence: a button holding what the part says, in pieces with their icons and marks.</summary>
    /// <remarks>Its looks dress the pieces' words, which the button takes as its own.</remarks>
    public class SlotChip : Button
    {
        readonly SlotPieces _pieces;

        /// <summary>An empty part, pressable where it is given a command, its pieces in a line across.</summary>
        public SlotChip() : this(true)
        {
        }

        /// <summary>An empty part, pressable where it is given a command, its pieces in a line across where <paramref name="piecesAcross" /> is set and one under the other otherwise.</summary>
        public SlotChip(bool piecesAcross)
        {
            _pieces = new SlotPieces(piecesAcross);
            Content = _pieces;
            Style = Styles.Slot;
        }

        /// <summary>How this looks.</summary>
        public new IStyle<SlotChip> Style
        {
            get { return (IStyle<SlotChip>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>What it says, in pieces: one element each for a block, a member and the words around them.</summary>
        public SlotPieces Pieces => _pieces;

        /// <summary>How far apart its pieces stand.</summary>
        public float Spacing
        {
            get { return _pieces.Spacing; }
            set { _pieces.Base.Spacing = value; }
        }

        /// <summary>The texts what the members it says have to say are in.</summary>
        public Texts Texts
        {
            get { return _pieces.Texts; }
            set { _pieces.Texts = value; }
        }

        /// <summary>The color of the outline around the marks of the members it says, transparent for none: tied to the wait or step under the pointer.</summary>
        public Color Glow
        {
            get { return _pieces.Glow; }
            set { _pieces.Glow = value; }
        }

        /// <summary>Whether the marks of the members it says are as quiet as the words of a paused step.</summary>
        public bool Paused
        {
            set { _pieces.Paused = value; }
        }

        /// <summary>Whether nothing fills the part yet, so it reads as an invitation.</summary>
        public bool IsEmpty
        {
            get { return Is(LookStates.Empty); }
            set { Set(LookStates.Empty, value); }
        }

        /// <summary>What it says, its pieces joined as they read.</summary>
        public new string Text => _pieces.Text;

        /// <summary>Says <paramref name="words" />, and nothing else.</summary>
        public void Say(string words)
        {
            _pieces.Say(words);
        }

        /// <summary>Says <paramref name="pieces" />, in order.</summary>
        public void Say(IList<Mention> pieces)
        {
            _pieces.Say(pieces, null);
        }

        /// <summary>Shows <paramref name="part" /> as <paramref name="pieces" />, pressed by the part's own command.</summary>
        /// <remarks>A part standing for a color shows the color in place of its words.</remarks>
        public void Show(SentenceSlot part, IList<Mention> pieces)
        {
            _pieces.Say(pieces, part.Sample);
            IsEmpty = part.IsEmpty;
            Command = part.Opens;
        }

        /// <inheritdoc />
        public override State States => Command == null || !IsEnabled ? base.States | LookStates.Still : base.States;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<SlotChip>;
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);
            _pieces.Dress(Font, FontSize, Color);
        }
    }
}
