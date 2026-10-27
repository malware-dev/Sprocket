using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRage.Game;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>What a part of a sentence says, in pieces: each block it names with its icon, a member with its marks, and the words around them.</summary>
    public class SlotPieces : Stacking, ITextual
    {
        /// <summary>How wide a color shown in place of the words is: a short word's width.</summary>
        const float SampleWide = 44f;

        readonly List<Mention> _said = new List<Mention>();
        readonly bool _across;

        Color _glow;
        Texts _texts;
        string _font = MyFontEnum.White;
        float _fontSize = TextLook.NeutralFontSize;
        Color _color = Color.White;

        /// <summary>Nothing said yet, its pieces in a line across where <paramref name="piecesAcross" /> is set and one under the other otherwise, words standing at the right.</summary>
        public SlotPieces(bool piecesAcross) : base(piecesAcross)
        {
            _across = piecesAcross;
        }

        /// <summary>The texts what the members it says have to say are in.</summary>
        public Texts Texts
        {
            get { return _texts; }
            set
            {
                if (_texts == value)
                    return;

                _texts = value;
                for (var i = 0; i < Children.Count && i < _said.Count; i++)
                {
                    var member = Children[i] as MemberName;
                    if (member != null)
                        member.About = Noted(_said[i].Mark);
                }
            }
        }

        /// <summary>The color of the outline around the marks of the members it says, transparent for none: tied to the wait or step under the pointer.</summary>
        public Color Glow
        {
            get { return _glow; }
            set
            {
                if (_glow == value)
                    return;

                _glow = value;
                foreach (var child in Children)
                {
                    var member = child as MemberName;
                    if (member != null)
                        member.Glow = value;
                }
            }
        }

        /// <summary>Whether the marks of the members it says are as quiet as the words of a paused step.</summary>
        public bool Paused
        {
            set
            {
                foreach (var child in Children)
                {
                    var member = child as MemberName;
                    if (member != null)
                        member.Paused = value;
                }
            }
        }

        /// <summary>What it says, its pieces joined as they read.</summary>
        public string Text
        {
            get
            {
                var said = "";
                foreach (var mention in _said)
                    said += mention.Text;

                return said;
            }
        }

        /// <inheritdoc />
        public string Font => _font;

        /// <inheritdoc />
        public float FontSize => _fontSize;

        /// <inheritdoc />
        public Color Color => _color;

        /// <summary>Draws every piece's words in <paramref name="font" />, <paramref name="fontSize" /> and <paramref name="color" />.</summary>
        public void Dress(string font, float fontSize, Color color)
        {
            if (_font == font && _fontSize == fontSize && _color == color)
                return;

            _font = font;
            _fontSize = fontSize;
            _color = color;
            Dress();
        }

        /// <summary>Says <paramref name="words" />, and nothing else.</summary>
        public void Say(string words)
        {
            _said.Clear();
            _said.Add(new Mention(words, null));
            Lay(null);
        }

        /// <summary>Says <paramref name="pieces" />, in order.</summary>
        public void Say(IList<Mention> pieces)
        {
            _said.Clear();
            for (var i = 0; i < pieces.Count; i++)
                _said.Add(pieces[i]);

            Lay(null);
        }

        /// <summary>Says <paramref name="pieces" />, in order, showing <paramref name="sample" /> in place of the words where it is a color and they are one piece.</summary>
        public void Say(IList<Mention> pieces, Color? sample)
        {
            _said.Clear();
            for (var i = 0; i < pieces.Count; i++)
                _said.Add(pieces[i]);

            Lay(sample);
        }

        void Lay(Color? sample)
        {
            // A piece keeps its element where the kind is unchanged, so new words over the same blocks allocate
            // nothing.
            for (var i = 0; i < _said.Count; i++)
            {
                var piece = _said[i];
                var sampled = _said.Count == 1 && sample.HasValue && piece.Icon == null && !piece.IsMember;

                if (i < Children.Count && !Says(Children[i], piece, sampled))
                    Children.Remove(Children[i]);

                if (i >= Children.Count || !Says(Children[i], piece, sampled))
                    Children.Insert(i, piece.Icon != null ? new BlockName() : piece.IsMember ? new MemberName() : sampled ? new Swatch { Style = Styles.ColorSample } : (Element)new Label());

                if (sampled)
                {
                    var swatch = (Swatch)Children[i];
                    swatch.Color = sample.Value;
                    continue;
                }

                if (piece.Icon != null)
                {
                    var named = (BlockName)Children[i];
                    named.Name = piece.Text;
                    named.Icon = piece.Icon;
                    continue;
                }

                if (piece.IsMember)
                {
                    var member = (MemberName)Children[i];
                    member.Name = piece.Text;
                    member.Mark = piece.Mark;
                    member.About = Noted(piece.Mark);
                    member.Glow = _glow;
                    continue;
                }

                var words = (Label)Children[i];
                words.Text = piece.Text;
                words.Align = _across ? Align.Default : Align.Right;
            }

            while (Children.Count > _said.Count)
                Children.Remove(Children[Children.Count - 1]);

            Dress();
        }

        /// <summary>What a member under <paramref name="mark" /> says, in the texts given, or null before they are.</summary>
        string Noted(string mark)
        {
            return _texts == null ? null : MarkText.About(_texts, mark);
        }

        /// <summary>Whether <paramref name="child" /> is the element that says <paramref name="piece" />: a block's, a member's, a color where <paramref name="sampled" />, or words.</summary>
        static bool Says(Element child, Mention piece, bool sampled)
        {
            return piece.Icon != null ? child is BlockName : piece.IsMember ? child is MemberName : sampled ? child is Swatch : child is Label;
        }

        void Dress()
        {
            foreach (var child in Children)
            {
                // A color stands as tall as a line of the words would.
                var swatch = child as Swatch;
                if (swatch != null)
                    swatch.Placement = FixedSize.Size(SampleWide, _fontSize);

                var named = child as BlockName;
                if (named != null)
                    named.Dress(_font, _fontSize, _color);

                var member = child as MemberName;
                if (member != null)
                    member.Base.Wear(_font, _fontSize, _color);

                var words = child as Label;
                if (words != null)
                    words.Base.Wear(_font, _fontSize, _color);
            }
        }
    }
}
