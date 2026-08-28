using System.Collections.Generic;
using Phosphor;
using VRage.Utils;
using Reactor.Ui;
using VRageMath;

namespace Reactor.Screens
{
    /// <summary>Something held to a width, whatever it would rather be.</summary>
    /// <remarks>
    /// Columns need this and nothing else does yet. A stack hands every child the width it asked for, and
    /// a scrolling column asks for everything on offer, so without a cap the first column would take the
    /// screen and the rest would have nowhere to go.
    /// </remarks>
    public class Sized : Element
    {
        readonly ElementCollection _children;

        Element _content;

        public Sized(float width)
        {
            _children = new ElementCollection(this);
            Width = width;
        }

        public float Width { get; set; }

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

    /// <summary>
    /// A picture of a thing, held to a square.
    /// </summary>
    /// <remarks>
    /// Nothing underneath it. A block in Space Engineers always has an icon - it is part of the block's
    /// definition and the game will not load one without it - so a target drawing something else in case
    /// there is no picture would be carrying a branch that can never be taken.
    /// </remarks>
    public class IconBox : Element
    {
        public IconBox(float size)
        {
            Size = size;
        }

        /// <summary>Square, and the same square on every row whatever is being listed.</summary>
        public float Size { get; set; }

        /// <summary>
        /// What core called this kind of thing.
        /// </summary>
        /// <remarks>
        /// A name, not a path. Core says "door" because that is all core knows; which picture a door has
        /// is the target's business, and in the game it is the block's own definition that answers.
        /// </remarks>
        public string Art { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Size, Size);
        }

        protected override void DrawContent(IGraphics g)
        {
            g.DrawSprite(Icons.MaterialFor(Art), Bounds, Color.White, 0f);
        }
    }

    /// <summary>
    /// Turns core's name for a kind of thing into the material that draws it.
    /// </summary>
    /// <remarks>
    /// One function, on purpose. Today it makes a name up and the backend behind it keeps a table of
    /// where Space Engineers happens to store that artwork - which is a stand-in for what the mod will do
    /// instead, which is ask the block for its own icon. When that arrives this is the only thing that
    /// changes.
    /// </remarks>
    public static class Icons
    {
        /// <summary>What every block icon's material name begins with.</summary>
        /// <remarks>
        /// So that a backend standing in for the game can tell which of its materials are block pictures
        /// and go and find them. The game itself needs no such thing - it has the block.
        /// </remarks>
        public const string Prefix = "Reactor_Block_";

        public static MyStringId MaterialFor(string name)
        {
            // A slash means it is the game's own block definition id, and the screen-space API resolves
            // one to that block's picture by itself. Anything without a slash is a word made up by a host
            // that has no blocks to ask, and gets the prefix a stand-in backend goes looking for.
            if (name != null && name.IndexOf('/') >= 0)
                return MyStringId.GetOrCompute(name);

            return MyStringId.GetOrCompute(Prefix + name);
        }
    }
}
