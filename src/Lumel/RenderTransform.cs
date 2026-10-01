using System;
using VRageMath;

namespace Lumel
{
    /// <summary>How an element is drawn and pressed away from where layout put it: moved, and scaled about its top-left corner.</summary>
    /// <remarks>
    ///     The default is no transform at all. Equality is written out because the inherited
    ///     <c>ValueType.Equals</c> boxes, and for float fields compares them by reflection.
    /// </remarks>
    public struct RenderTransform : IEquatable<RenderTransform>
    {
        // The scale less one, so the default is a scale of one.
        readonly float _grown;

        /// <summary>Moved by <paramref name="translate" /> and scaled by <paramref name="scale" />.</summary>
        public RenderTransform(Vector2 translate, float scale)
        {
            Translate = translate;
            _grown = scale - 1f;
        }

        /// <summary>No transform.</summary>
        public static RenderTransform Identity => default(RenderTransform);

        /// <summary>How far the element is moved, in UI units.</summary>
        public Vector2 Translate { get; }

        /// <summary>How much the element is scaled, about its top-left corner.</summary>
        public float Scale => 1f + _grown;

        /// <summary>Whether this moves and scales nothing.</summary>
        public bool IsIdentity => _grown == 0f && Translate.X == 0f && Translate.Y == 0f;

        /// <summary>Moved by <paramref name="translate" />, and not scaled.</summary>
        public static RenderTransform Moved(Vector2 translate)
        {
            return new RenderTransform(translate, 1f);
        }

        /// <summary>Where a point laid out at <paramref name="point" /> is drawn, for an element whose top-left corner is <paramref name="origin" />.</summary>
        public Vector2 Apply(Vector2 point, Vector2 origin)
        {
            return (point - origin) * Scale + origin + Translate;
        }

        /// <summary>Where a point drawn at <paramref name="point" /> was laid out: the reverse of <see cref="Apply" />.</summary>
        public Vector2 Undo(Vector2 point, Vector2 origin)
        {
            var scale = Scale;
            return scale == 0f ? origin : (point - Translate - origin) / scale + origin;
        }

        /// <summary>Whether <paramref name="a" /> and <paramref name="b" /> move and scale by the same.</summary>
        public static bool operator ==(RenderTransform a, RenderTransform b)
        {
            return a.Equals(b);
        }

        /// <summary>Whether <paramref name="a" /> and <paramref name="b" /> differ.</summary>
        public static bool operator !=(RenderTransform a, RenderTransform b)
        {
            return !a.Equals(b);
        }

        /// <inheritdoc />
        public bool Equals(RenderTransform other)
        {
            return _grown == other._grown && Translate.X == other.Translate.X && Translate.Y == other.Translate.Y;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is RenderTransform && Equals((RenderTransform)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (_grown.GetHashCode() * 397) ^ Translate.X.GetHashCode() ^ (Translate.Y.GetHashCode() * 31);
        }
    }
}
