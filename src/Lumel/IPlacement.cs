using System;

namespace Lumel
{
    /// <summary>How an element asks whatever holds it to place it.</summary>
    /// <remarks>
    ///     One placement may be shared by many elements, so it is read-only to all but itself. A placement that
    ///     never changes implements <see cref="Changed" /> with empty accessors.
    /// </remarks>
    public interface IPlacement
    {
        /// <summary>Raised when anything this says has changed.</summary>
        event Action Changed;
    }
}
