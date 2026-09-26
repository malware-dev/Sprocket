using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Words;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Controls
{
    /// <summary>A fillable part of a sentence, saying what it holds in pieces: each block it names with its icon, and the words around them.</summary>
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
                var block = piece.Icon != null;

                if (i < Children.Count && (Children[i] is BlockName) != block)
                    Children.Remove(Children[i]);

                if (i >= Children.Count || (Children[i] is BlockName) != block)
                    Children.Insert(i, block ? (Element)new BlockName() : new Label());

                if (block)
                {
                    var named = (BlockName)Children[i];
                    named.Name = piece.Text;
                    named.Icon = piece.Icon;
                    continue;
                }

                var words = (Label)Children[i];
                words.Text = piece.Text;
                words.Sample = _said.Count == 1 ? sample : null;
            }

            while (Children.Count > _said.Count)
                Children.Remove(Children[Children.Count - 1]);

            Dress();
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
