using System.Runtime.CompilerServices;

// What the harness and the tests are allowed to see.
//
// A .debug.cs file is compiled into this assembly locally and left out of what is packed for the
// game, which is the only way this attribute can exist at all: the game's whitelist bans it, so a
// mod that shipped it would refuse to load. Both halves of that are configured - mdk.ini ignores the
// pattern when packing, and the analysers ignore it when checking.
[assembly: InternalsVisibleTo("Reactor.Host.SpaceEngineers.Sim")]
[assembly: InternalsVisibleTo("Reactor.Core.Tests")]
[assembly: InternalsVisibleTo("Reactor.Host.SpaceEngineers.Tests")]
[assembly: InternalsVisibleTo("Reactor.Host.SpaceEngineers.Presentation.Tests")]
