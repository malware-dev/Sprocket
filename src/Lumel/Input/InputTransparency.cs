using System;

namespace Lumel.Input
{
    /// <summary>What of an element lets presses, the hover and the keyboard pass through to what is beneath it.</summary>
    /// <remarks>Tips are said whatever this is. <see cref="Children" /> does nothing on an element with nothing inside it.</remarks>
    [Flags]
    public enum InputTransparency
    {
        /// <summary>The element and everything inside it take input as usual.</summary>
        None = 0,

        /// <summary>The element itself passes input through; what is inside it still takes it.</summary>
        Self = 1,

        /// <summary>Everything inside the element passes input through; the element itself still takes it.</summary>
        Children = 2,

        /// <summary>The element and everything inside it pass input through.</summary>
        All = Self | Children
    }
}
