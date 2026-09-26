using System;

namespace Sprocket.Host.SpaceEngineers.Ui
{
    /// <summary>How an element asks whatever holds it to place it.</summary>
    /// <remarks>
    ///     Read-only to everything but itself, since one placement may be shared by many elements. A
    ///     placement that never changes implements <see cref="Changed" /> with empty accessors, so sharing it
    ///     holds no handlers.
    /// </remarks>
    public interface IPlacement
    {
        /// <summary>Raised when anything this says has changed.</summary>
        event Action Changed;
    }
}
