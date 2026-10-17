using System;

namespace Lumel
{
    /// <summary>Where an element sits in the room its holder gives it, on each axis.</summary>
    /// <remarks>
    /// Each axis is read alone: one named edge pins the element to it, both stretch it, and none centers it where
    /// <see cref="Center" /> is set. A holder ignores the axis it stacks along.
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
