using System.Collections.Generic;

namespace Sprocket.GameTests
{
    /// <summary>
    /// A client test kept on one machine only, in the Local folder git leaves out, played when SPROCKET_CLIENTTEST_ONLY names
    /// its part.
    /// </summary>
    /// <remarks>Found by the plugin among the types of this assembly, so an implementation has a constructor taking nothing.</remarks>
    public interface ILocalClientTest
    {
        /// <summary>The name SPROCKET_CLIENTTEST_ONLY plays it by.</summary>
        string Part { get; }

        /// <summary>
        /// The test's steps on the blueprints in <paramref name="blueprints" />, pictured by <paramref name="tour" /> and answering
        /// into <paramref name="report" />, each yielding the plugin updates to wait before the next.
        /// </summary>
        IEnumerator<int> Run(string blueprints, EditorTour tour, GameTestReport report);
    }
}
