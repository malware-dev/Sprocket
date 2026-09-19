using System.Collections.Generic;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Drawing
{
    /// <summary>A stack of clip rectangles where every entry is already intersected with the one below it.</summary>
    /// <remarks>
    /// Neither target has this. A text panel has a single clip state - one sprite sets it, another clears
    /// it - with no stack and no intersection, and screen space has nothing at all. Both need the same two
    /// things on top: an inner container must never widen its parent's clip, and tearing an inner clip
    /// down must leave the parent's standing rather than clearing outright. Doing it here means a backend
    /// only ever has to honour one rectangle.
    /// </remarks>
    public sealed class ClipStack
    {
        private readonly List<RectangleF> _stack = new List<RectangleF>();

        /// <summary>Whether anything is clipping right now.</summary>
        public bool HasClip => _stack.Count > 0;

        /// <summary>
        /// The rectangle in force, already intersected all the way down. Only meaningful while
        /// <see cref="HasClip" /> is true.
        /// </summary>
        public RectangleF Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : new RectangleF(0f, 0f, 0f, 0f);

        /// <summary>Pushes <paramref name="bounds" />, narrowed by whatever is already in force.</summary>
        public void Push(RectangleF bounds)
        {
            _stack.Add(_stack.Count > 0 ? Intersect(bounds, _stack[_stack.Count - 1]) : bounds);
        }

        /// <summary>Drops the innermost rectangle. Popping an empty stack is not an error.</summary>
        public void Pop()
        {
            if (_stack.Count > 0)
                _stack.RemoveAt(_stack.Count - 1);
        }

        /// <summary>Drops everything, for the start of a frame.</summary>
        public void Clear()
        {
            _stack.Clear();
        }

        /// <summary>The overlap of two rectangles, or one of zero size where they do not meet.</summary>
        public static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            float left = a.X > b.X ? a.X : b.X;
            float top = a.Y > b.Y ? a.Y : b.Y;

            float aRight = a.X + a.Width;
            float bRight = b.X + b.Width;
            float aBottom = a.Y + a.Height;
            float bBottom = b.Y + b.Height;

            float right = aRight < bRight ? aRight : bRight;
            float bottom = aBottom < bBottom ? aBottom : bBottom;

            float width = right - left;
            float height = bottom - top;

            return new RectangleF(left, top, width > 0f ? width : 0f, height > 0f ? height : 0f);
        }

        /// <summary>Whether two rectangles meet at all.</summary>
        public static bool Overlaps(RectangleF a, RectangleF b)
        {
            return a.X < b.X + b.Width && b.X < a.X + a.Width
                && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
        }
    }
}
