using Xunit;

// One construct at a time.
//
// These tests run against the mod's own world layer, which reaches the game through statics - the
// gateway above all. A rig installs itself behind those while it runs, so two tests building two
// constructs at once would each see the other's blocks.
//
// The cost is real and worth stating: this suite used to run in parallel because the world it tested
// against was a hand-written one with no statics in it. That world was also a second answer to every
// question these tests ask, which is why it is gone.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
