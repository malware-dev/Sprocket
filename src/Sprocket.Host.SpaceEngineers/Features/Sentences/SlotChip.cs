using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>A fillable part of a sentence: a button holding what the part says, in pieces with their icons and marks.</summary>
    /// <remarks>Its style dresses the pieces' words, which the button takes as its own.</remarks>
    public class SlotChip : Button, ITextual
    {
        readonly SlotPieces _pieces;
        bool _empty;

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

        /// <summary>What it says, in pieces: one element each for a block, a member and the words around them.</summary>
        public SlotPieces Pieces => _pieces;

        /// <summary>How far apart its pieces stand.</summary>
        public float Spacing
        {
            get { return _pieces.Spacing; }
            set { _pieces.Spacing = value; }
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
            get { return _empty; }
            set
            {
                if (_empty == value)
                    return;

                _empty = value;
                StateChanged();
            }
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

        // Lumel's Button lays its default style from its own constructor, before this one makes the pieces; the style set
        // here at the end of construction gives the pieces their words' look again.

        string ITextual.Font
        {
            get { return _pieces?.Font; }
            set
            {
                if (_pieces != null)
                    _pieces.Font = value;
            }
        }

        float ITextual.FontSize
        {
            get { return _pieces?.FontSize ?? 0f; }
            set
            {
                if (_pieces != null)
                    _pieces.FontSize = value;
            }
        }

        Color ITextual.Color
        {
            get { return _pieces?.Color ?? Color.White; }
            set
            {
                if (_pieces != null)
                    _pieces.Color = value;
            }
        }
    }
}
