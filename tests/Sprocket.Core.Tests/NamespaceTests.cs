using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A host's namespaces are the ones registered, each by one owner, of any number of parts, and a program reads whether or not they are.</summary>
public class NamespaceTests
{
    const string Hovering = "sprocket 2 alpha 2\n\n"
        + "let pad = block 'Door/LargeBlockDoor' [ 7, \"Pad\" ]\n\n"
        + "system \"4b6e1f20-8c3a-4d5e-9f71-2a3b4c5d6e7f\" as Malforge.Hover\n{\n"
        + "    whenever malforge.hover_repulsors.landed on [ pad ] then run 1\n\n"
        + "    sequence 1 as Lift {\n"
        + "        call malforge.hover_repulsors.lift on [ pad ]\n"
        + "        set malforge.hover_repulsors.ride_height on [ pad ] to 4\n"
        + "    }\n"
        + "}\n";

    /// <summary>A member, a signal and a value in a namespace of several parts are read with that namespace, and written back so.</summary>
    /// <remarks>Read whether or not a mod has registered the namespace: a mod may load after the world's programs are read.</remarks>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANamespaceOfSeveralPartsIsReadAndWrittenBack(bool registered)
    {
        try
        {
            if (registered)
                Assert.True(HostNamespaces.Register("malforge.hover_repulsors", "Hover Repulsors", "Hover Repulsors", "Malforge"));

            var program = new Automation();
            Assert.Equal("", ProgramReader.Load(Hovering, program, new Values()));

            var steps = program.Systems.Single().Sequences.Single().Steps.OfType<Act>().ToList();
            Assert.All(steps, act => Assert.Equal("malforge.hover_repulsors", act.Provider));
            Assert.Equal(new[] { "lift", "ride_height" }, steps.Select(act => act.ActionId));

            var written = ProgramWriter.Save(program, new Values());
            Assert.Contains("whenever malforge.hover_repulsors.landed on [ pad ] then run 1", written);
            Assert.Contains("call malforge.hover_repulsors.lift on [ pad ]", written);
            Assert.Contains("set malforge.hover_repulsors.ride_height on [ pad ] to 4", written);
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>A namespace is its first owner's, with the name and author it was registered with: another owner is refused, and only its owner takes it back.</summary>
    [Fact]
    public void ANamespaceHasOneOwner()
    {
        try
        {
            Assert.True(HostNamespaces.Register("malforge.hover_repulsors", "Hover Repulsors", "Hover Repulsors", "Malforge"));
            Assert.True(HostNamespaces.Register("malforge.hover_repulsors", "Hover Repulsors", "Hover Repulsors", "Malforge"));
            Assert.False(HostNamespaces.Register("malforge.hover_repulsors", "Someone Else", "Copy", "Someone"));
            Assert.False(HostNamespaces.Register("sprocket", "Someone Else", "Copy", "Someone"));
            Assert.False(HostNamespaces.Register("game", "Someone Else", "Copy", "Someone"));

            HostNamespaces.Unregister("malforge.hover_repulsors", "Someone Else");
            var kept = HostNamespaces.Find("malforge.hover_repulsors")!;
            Assert.Equal(("malforge.hover_repulsors", "Hover Repulsors", "Hover Repulsors", "Malforge"), (kept.Name, kept.Owner, kept.Title, kept.Author));

            HostNamespaces.Unregister("malforge.hover_repulsors", "Hover Repulsors");
            Assert.False(HostNamespaces.Has("malforge.hover_repulsors"));
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>Clearing as a session ends forgets every namespace but Sprocket's own and the game's.</summary>
    [Fact]
    public void ClearingKeepsOnlySprocketsOwnAndTheGames()
    {
        HostNamespaces.Register("malforge.hover_repulsors", "Hover Repulsors", "Hover Repulsors", "Malforge");

        HostNamespaces.Clear();

        Assert.False(HostNamespaces.Has("malforge.hover_repulsors"));
        Assert.True(HostNamespaces.Has("sprocket"));
        Assert.True(HostNamespaces.Has("game"));
        HostNamespaces.Unregister("game", HostNamespaces.Sprocket);
        Assert.Equal("Space Engineers", HostNamespaces.Find("game")!.Title);
    }
}
