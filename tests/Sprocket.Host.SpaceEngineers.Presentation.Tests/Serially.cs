using Xunit;

// One screen at a time.
//
// The game reaches drawing through statics - MyScreenSpace and the gateway - so the harness that
// stands in for it does too, and there is one device behind them for the process. Tests that drive a
// screen therefore cannot run beside each other: two of them sharing one scissor stack pop each
// other's clips, which shows up as an index out of range somewhere innocent.
//
// This was hidden while the tests ran on a UI toolkit's test host, which marshalled every one of them
// onto a single thread as a side effect of having a dispatcher.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
