using System;

namespace Lumel
{
    /// <summary>Where an element sits in the room its holder gives it, on each axis.</summary>
    /// <remarks>
    /// Read one axis at a time. An axis whose edges are named goes by them: one edge pins the element to it, both stretch
    /// it between them. An axis with no edge named is centered where <see cref="Center" /> is set, and otherwise placed as
    /// the holder places a child that says nothing. So <c>Left | Center</c> is left and centered down, and
    /// <c>Center | Top | Bottom</c> is centered across and stretched down. A holder ignores the axis it stacks along.
    /// </remarks>
    [Flags]
    public enum Align
    {
        /// <summary>Placed as the holder places a child that says nothing, on every axis.</summary>
        Default = 0,

        /// <summary>Centered on every axis whose edges are not named.</summary>
        Center = 1,

        /// <summary>Pinned to the left edge; with <see cref="Right" />, stretched across.</summary>
        Left = 2,

        /// <summary>Pinned to the right edge; with <see cref="Left" />, stretched across.</summary>
        Right = 4,

        /// <summary>Pinned to the top edge; with <see cref="Bottom" />, stretched down.</summary>
        Top = 8,

        /// <summary>Pinned to the bottom edge; with <see cref="Top" />, stretched down.</summary>
        Bottom = 16,

        /// <summary>Stretched across, from the left edge to the right.</summary>
        Wide = Left | Right,

        /// <summary>Stretched down, from the top edge to the bottom.</summary>
        Tall = Top | Bottom,

        /// <summary>Stretched both ways, filling the room.</summary>
        Fill = Wide | Tall
    }
}
