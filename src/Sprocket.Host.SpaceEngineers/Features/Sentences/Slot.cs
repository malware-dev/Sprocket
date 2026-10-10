using System.Collections.Generic;
using Lumel;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRage.Game;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>A fillable part of a sentence, saying what it holds in pieces: each block it names with its icon, a member with its marks, and the words around them.</summary>
    /// <remarks>
    /// The only thing on screen that changes what a sequence means, so nothing else may borrow this look. The line
    /// under it is what distinguishes it from a row at a glance, and it is drawn rather than being a border because
    /// a rectangle of color is all the game can be asked for.
    /// </remarks>
    public class Slot : HStack, ITextual
    {
        const float UnderlineThickness = 2f;

        readonly List<Mention> _said = new List<Mention>();

        bool _empty;
        Texts _texts;
        string _font = MyFontEnum.White;
        float _fontSize = TextState.NeutralFontSize;
        Color _color = Color.White;

        /// <summary>An empty part, pressable where it is given a command, in the look every part of a sentence has.</summary>
        public Slot()
        {
            Style = Styles.Slot;
        }

        /// <summary>The color of the line along the bottom.</summary>
        public Color Underline { get; set; }

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
        public string Text
        {
            get
            {
                var said = "";
                for (var i = 0; i < _said.Count; i++)
                    said += _said[i].Text;

                return said;
            }
        }

        /// <inheritdoc />
        public string Font
        {
            get { return _font; }
            set
            {
                _font = value;
                Dress();
            }
        }

        /// <inheritdoc />
        public float FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                Dress();
            }
        }

        /// <inheritdoc />
        public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                Dress();
            }
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

        /// <summary>Shows <paramref name="part" /> as <paramref name="pieces" />, pressed by the part's own command.</summary>
        /// <remarks>A part standing for a color shows the color in place of its words.</remarks>
        public void Show(SentenceSlot part, IList<Mention> pieces)
        {
            _said.Clear();
            for (var i = 0; i < pieces.Count; i++)
                _said.Add(pieces[i]);

            Lay(part.Sample);
            IsEmpty = part.IsEmpty;
            Command = part.Opens;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            g.Fill(
                new RectangleF(Bounds.X, Bounds.Y + Bounds.Height - UnderlineThickness, Bounds.Width, UnderlineThickness),
                Underline);

            base.DrawContent(g);
        }

        void Lay(Color? sample)
        {
            // Each piece keeps the element it had where it is still the same kind, so saying new words over the
            // same blocks makes nothing new.
            for (var i = 0; i < _said.Count; i++)
            {
                var piece = _said[i];

                if (i < Children.Count && !Says(Children[i], piece))
                    Children.Remove(Children[i]);

                if (i >= Children.Count || !Says(Children[i], piece))
                    Children.Insert(i, piece.Icon != null ? new BlockName() : piece.IsMember ? new MemberName() : (Element)new Label());

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
                    continue;
                }

                var words = (Label)Children[i];
                words.Text = piece.Text;
                words.Sample = _said.Count == 1 ? sample : null;
                words.SampleEdge = Palette.Line;
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

        /// <summary>Whether <paramref name="child" /> is the element that says <paramref name="piece" />: a block's, a member's, or words.</summary>
        static bool Says(Element child, Mention piece)
        {
            return piece.Icon != null ? child is BlockName : piece.IsMember ? child is MemberName : child is Label;
        }

        void Dress()
        {
            for (var i = 0; i < Children.Count; i++)
            {
                var text = (ITextual)Children[i];
                text.Font = _font;
                text.FontSize = _fontSize;
                text.Color = _color;
            }
        }
    }
}
