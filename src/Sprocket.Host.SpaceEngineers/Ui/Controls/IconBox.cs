using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
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
        private float _size;

        public IconBox(float size)
        {
            Size = size;
        }

        /// <summary>Square, and the same square on every row whatever is being listed.</summary>
        public float Size
        {
            get { return _size; }
            set
            {
                if (_size == value)
                    return;

                _size = value;
                InvalidateMeasure();
            }
        }

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
}
