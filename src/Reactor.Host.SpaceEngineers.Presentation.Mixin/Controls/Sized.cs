using System.Collections.Generic;
using VRage.Utils;
using Reactor.Ui;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Host.SpaceEngineers.Presentation.Controls
{
    /// <summary>Something held to a width, whatever it would rather be.</summary>
    /// <remarks>
    /// Columns need this and nothing else does yet. A stack hands every child the width it asked for, and
    /// a scrolling column asks for everything on offer, so without a cap the first column would take the
    /// screen and the rest would have nowhere to go.
    /// </remarks>
    public class Sized : Element
    {
        private float _width;

        readonly ElementCollection _children;

        Element _content;

        public Sized(float width)
        {
            _children = new ElementCollection(this);
            Width = width;
        }

        public float Width
        {
            get { return _width; }
            set
            {
                if (_width == value)
                    return;

                _width = value;
                InvalidateMeasure();
            }
        }

        public Element Content
        {
            get { return _content; }
            set
            {
                if (_content == value)
                    return;

                if (_content != null)
                    _children.Remove(_content);

                if (value != null)
                    _children.Add(value);

                _content = value;
            }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_content == null)
                return new Vector2(Width, 0f);

            _content.Measure(g, new Vector2(Width, available.Y));
            return new Vector2(Width, _content.DesiredSize.Y);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_content != null)
                _content.Arrange(new RectangleF(bounds.X, bounds.Y, Width, bounds.Height));
        }

        protected override void DrawContent(IGraphics g)
        {
            if (_content != null)
                _content.Draw(g);
        }
    }
}
