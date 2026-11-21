using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A trigger reads the way the program writes it.
/// </summary>
/// <remarks>
/// "when any closed on [ inner ] == on then run 2", and "every 600t run 3": a moment is followed by "then", a
/// clock by the verb alone. A row that said "runs" read as the block running the sequence.
/// </remarks>
public class TriggerWordsTests
{
    [Theory]
    [InlineData(TriggerKind.When, false, "then run")]
    [InlineData(TriggerKind.When, true, "then stop")]
    [InlineData(TriggerKind.Every, false, "run")]
    [InlineData(TriggerKind.Every, true, "stop")]
    public void WhatStartsItIsJoinedToWhatItDoesAsTheProgramWritesIt(TriggerKind kind, bool stops, string said)
    {
        Assert.Equal(said, SentenceText.Then(kind, stops));
    }
}
