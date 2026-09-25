using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>
    ///     The game's own on/off control: two halves side by side, both words always readable, the half that
    ///     is in force filled and the other left dark.
    /// </summary>
    /// <remarks>Transcribed from the game's <c>MyGuiControlOnOffSwitch</c>.</remarks>
    public class Switch : Element
    {
        readonly Label _off = Half("OFF");
        readonly Label _on = Half("ON");

        Color _color;
        float _half;
        bool _isOn;
        bool _isSet;
        Color _lit;
        Color _quiet;
        Color _unlit;

        /// <summary>A switch with neither half in force, in the neutral look.</summary>
        public Switch()
        {
            var children = new ElementCollection(this);
            children.Add(_on);
            children.Add(_off);

            EdgeThickness = 1f;
        }

        /// <summary>Which half is in force, once <see cref="IsSet" /> says either is.</summary>
        public bool IsOn
        {
            get { return _isOn; }
            set
            {
                if (_isOn == value)
                    return;

                _isOn = value;
                Recolor();
            }
        }

        /// <summary>Whether either half is in force, which it is not until somebody chooses.</summary>
        public bool IsSet
        {
            get { return _isSet; }
            set
            {
                if (_isSet == value)
                    return;

                _isSet = value;
                Recolor();
            }
        }

        /// <summary>Which face the words are drawn in.</summary>
        public string Font
        {
            get { return _on.Font; }
            set
            {
                _on.Font = value;
                _off.Font = value;
            }
        }

        /// <summary>How tall a line of the words is, in UI units.</summary>
        public float FontSize
        {
            get { return _on.FontSize; }
            set
            {
                _on.FontSize = value;
                _off.FontSize = value;
            }
        }

        /// <summary>The room held clear round each half's word.</summary>
        public Thickness WordPadding
        {
            get { return _on.Padding; }
            set
            {
                _on.Padding = value;
                _off.Padding = value;
            }
        }

        /// <summary>The color of the word in force.</summary>
        public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                Recolor();
            }
        }

        /// <summary>The color of a word not in force, and of both while neither is.</summary>
        public Color QuietColor
        {
            get { return _quiet; }
            set
            {
                _quiet = value;
                Recolor();
            }
        }

        /// <summary>What the half in force is filled with.</summary>
        public Color LitColor
        {
            get { return _lit; }
            set { _lit = value; }
        }

        /// <summary>What the half not in force is filled with, and both while neither is.</summary>
        public Color UnlitColor
        {
            get { return _unlit; }
            set { _unlit = value; }
        }

        /// <summary>Renames the two halves.</summary>
        public void Says(string on, string off)
        {
            _on.Text = on;
            _off.Text = off;
        }

        /// <summary>Lights this where the pointer is resting on it.</summary>
        /// <remarks>Only for a switch that takes its own presses, as on a form; elsewhere the screen lights it.</remarks>
        public void Lit(Vector2 point)
        {
            IsHot = !Collapsed && Holds(point);
        }

        /// <summary>Which half a point is on, and whether it is on the control at all.</summary>
        public bool Chose(Vector2 point, out bool isOn)
        {
            isOn = false;
            if (Collapsed || !Holds(point))
                return false;

            isOn = point.X < Bounds.X + Bounds.Width * 0.5f;
            return true;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _on.Measure(g, available);
            _off.Measure(g, available);

            // Both halves take the longer word's width, so the switch keeps its shape when switched.
            _half = _on.DesiredSize.X > _off.DesiredSize.X ? _on.DesiredSize.X : _off.DesiredSize.X;

            var height = _on.DesiredSize.Y > _off.DesiredSize.Y ? _on.DesiredSize.Y : _off.DesiredSize.Y;
            return new Vector2(_half * 2f, height);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _on.Arrange(new RectangleF(bounds.X, bounds.Y, _half, bounds.Height));
            _off.Arrange(new RectangleF(bounds.X + _half, bounds.Y, _half, bounds.Height));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            // Inside the border, so the fills do not cover it.
            var inset = EdgeThickness;
            var inner = new RectangleF(
                Bounds.X + inset, Bounds.Y + inset,
                Bounds.Width - inset * 2f, Bounds.Height - inset * 2f);

            // The unlit fill under the whole, then the lit half on top, rather than two halves side by
            // side: each rectangle is rounded on its own, so two halves on a fractional width overlap or
            // leave a gap.
            g.Fill(inner, _unlit);

            if (!_isSet)
            {
                _on.Draw(g);
                _off.Draw(g);
                return;
            }

            var half = inner.Width * 0.5f;
            g.Fill(_isOn
                    ? new RectangleF(inner.X, inner.Y, half, inner.Height)
                    : new RectangleF(inner.X + half, inner.Y, inner.Width - half, inner.Height),
                _lit);

            _on.Draw(g);
            _off.Draw(g);
        }

        void Recolor()
        {
            _on.Color = _isSet && _isOn ? _color : _quiet;
            _off.Color = _isSet && !_isOn ? _color : _quiet;
        }

        static Label Half(string text)
        {
            var half = new Label();
            half.Text = text;
            half.Centres = true;
            half.Wraps = false;
            return half;
        }
    }
}
