using System.Text;
using VRage.Game;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// One run of text on one line.
    /// <para>
    /// It keeps a <c>StringBuilder</c> beside the string it was given, because measuring happens every layout
    /// pass and the only allocation-free way to measure is from one of those. Writing <see cref="Text" />
    /// syncs it, so the cost lands when somebody changes the words rather than sixty times a second.
    /// </para>
    /// </summary>
    public class Label : Element
    {
        private readonly StringBuilder _measured = new StringBuilder(64);
        private string _text = string.Empty;

        public Label()
        {
            Font = MyFontEnum.White;
            Scale = 1f;
            Color = Color.White;
        }

        public string Text
        {
            get { return _text; }
            set
            {
                string incoming = value ?? string.Empty;
                if (_text == incoming)
                    return;

                _text = incoming;
                _measured.Clear();
                _measured.Append(incoming);
            }
        }

        public string Font { get; set; }

        public float Scale { get; set; }

        public Color Color { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_text.Length == 0)
                return new Vector2(0f, g.LineHeight(Font, Scale));

            return g.MeasureText(Font, _measured, Scale);
        }

        protected override void DrawContent(IGraphics g)
        {
            if (_text.Length == 0)
                return;

            g.DrawText(Font, _text, new Vector2(Bounds.X, Bounds.Y), Scale, Color);
        }
    }
}
