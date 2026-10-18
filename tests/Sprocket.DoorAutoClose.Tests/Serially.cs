using Xunit;

// One rig at a time: a rig installs itself behind the game's gateway, a static, and the network proxy is a static too.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
