using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Words;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A trigger reads the way the program writes it.
/// </summary>
/// <remarks>
/// "when any closed [ inner ] then run 2", and "every 600t run 3": a moment is followed by "then", a
/// clock by the verb alone. A row that said "runs" read as the block running the sequence.
/// </remarks>
public class TriggerWordsTests
{
    [Theory]
    [InlineData(TriggerKind.When, false, true, false, "then run")]
    [InlineData(TriggerKind.When, true, true, false, "then stop")]
    [InlineData(TriggerKind.When, false, false, false, "then")]
    [InlineData(TriggerKind.When, false, false, true, "then set")]
    [InlineData(TriggerKind.Every, false, true, false, "run")]
    [InlineData(TriggerKind.Every, true, true, false, "stop")]
    [InlineData(TriggerKind.Every, false, false, false, "")]
    [InlineData(TriggerKind.Every, false, false, true, "set")]
    public void WhatStartsItIsJoinedToWhatItDoesAsTheProgramWritesIt(TriggerKind kind, bool stops,
        bool sequence, bool sets, string said)
    {
        Assert.Equal(said, SentenceText.Then(kind, stops, sequence, sets));
    }
}
