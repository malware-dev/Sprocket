using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Tests that must not run beside any other, because they count what the whole process does.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class Alone
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Alone";
}
