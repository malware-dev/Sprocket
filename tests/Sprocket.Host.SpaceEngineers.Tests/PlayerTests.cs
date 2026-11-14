using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Tests;

/// <summary>
/// A display name, without what the game drew in front of it.
/// </summary>
/// <remarks>
/// Expected rather than exceptional: Space Engineers keeps its own marks in the private use area and
/// puts one before a player's name, and that name becomes the namespace a system is written under.
/// U+E030 is the one it hands over here.
/// </remarks>
public class PlayerTests
{
    [Theory]
    [InlineData("Malware", "Malware")]
    [InlineData("Malware", "Malware")]
    [InlineData("Malware", "Malware")]
    [InlineData("", "")]
    public void TheGamesOwnMarkIsNotPartOfTheName(string shown, string wanted)
    {
        Assert.Equal(wanted, Player.Plain(shown));
    }

    /// <summary>
    /// Everything after the mark is left exactly as it came.
    /// </summary>
    /// <remarks>
    /// People put all sorts into a name and it is theirs to put there. Only what the game added is
    /// the game's to take away, so a mark somebody typed themselves stays where they typed it.
    /// </remarks>
    [Theory]
    [InlineData("Malware")]
    [InlineData("Malware")]
    [InlineData("🚀 Malware")]
    [InlineData("  Malware  ")]
    public void WhatThePlayerPutThereIsLeftAlone(string shown)
    {
        Assert.Equal(shown, Player.Plain(shown));
    }
}
