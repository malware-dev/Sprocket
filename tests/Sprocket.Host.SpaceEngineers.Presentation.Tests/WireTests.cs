using Sandbox.ModAPI;
using Mal.MdkModMixin.Network;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The harness's network carries the mod's own channel between a server and its clients.
/// </summary>
/// <remarks>
/// Every machine is one process here, so what is being held is that each side hears only what was
/// sent to it, as the machine it is, after the delay the test set - which is what lets the rest of
/// the multiplayer work be tested without a dedicated server.
/// </remarks>
public class WireTests : IDisposable
{
    readonly Wire _wire = new Wire();

    public WireTests()
    {
        _wire.Install();
    }

    public void Dispose()
    {
        _wire.Uninstall();
    }

    /// <summary>A message of the kind the mod's channel carries: a number and some words.</summary>
    struct Note
    {
        public int Number;
        public string Words;

        public static int SizeOf(ref Note note) => sizeof(int) + ByteData.SizeOfString(note.Words);

        public static int Write(ref Note note, byte[] data, ref int offset)
        {
            var start = offset;
            ByteData.Write(note.Number, data, ref offset);
            ByteData.WriteString(note.Words, data, ref offset);
            return offset - start;
        }

        public static int Read(byte[] data, ref int offset, out Note note)
        {
            var start = offset;
            note = new Note();
            ByteData.TryReadInt32(data, ref offset, out note.Number);
            ByteData.TryReadString(data, ref offset, out note.Words);
            return offset - start;
        }
    }

    sealed class Heard
    {
        public readonly List<string> Said = new List<string>();
        public readonly List<string> By = new List<string>();
        public readonly List<bool> FromServer = new List<bool>();
        public readonly List<bool> AsServer = new List<bool>();
    }

    /// <summary>The mod's channel on one machine, and what arrives on it there.</summary>
    NetworkChannel Channel(Peer peer, Heard heard, Dictionary<ulong, Peer> known)
    {
        NetworkChannel made = null!;
        _wire.As(peer, () =>
        {
            made = NetworkChannel.Create("Sprocket.WireTests")
                .UsingModel<Note>(Note.SizeOf, Note.Read, Note.Write)
                .Build();

            made.ListenFor<Note>((_, note, sender, fromServer) =>
            {
                heard.Said.Add(note.Number + " " + note.Words);
                heard.By.Add(known[sender].Name);
                heard.FromServer.Add(fromServer);
                heard.AsServer.Add(MyAPIGateway.Multiplayer.IsServer);
            });
        });

        return made;
    }

    [Fact]
    public void AClientsMessageReachesTheServerAfterTheDelay()
    {
        var server = _wire.Server();
        var client = _wire.Client("Anna");
        var known = new Dictionary<ulong, Peer> { { server.Id, server }, { client.Id, client } };

        var atServer = new Heard();
        var atClient = new Heard();
        Channel(server, atServer, known);
        var clients = Channel(client, atClient, known);

        _wire.Latency = 12;
        _wire.As(client, () => clients.SendToServer(new Note { Number = 1, Words = "open" }, true));

        for (var i = 0; i < 12; i++)
            _wire.Tick();

        Assert.Empty(atServer.Said);

        _wire.Tick();

        Assert.Equal(new[] { "1 open" }, atServer.Said);
        Assert.Equal(new[] { "Anna" }, atServer.By);
        Assert.Equal(new[] { false }, atServer.FromServer);
        Assert.Equal(new[] { true }, atServer.AsServer);
        Assert.Empty(atClient.Said);
    }

    [Fact]
    public void TheServerBroadcastsToEveryClientAndNotItself()
    {
        var server = _wire.Server();
        var anna = _wire.Client("Anna");
        var bert = _wire.Client("Bert");
        var known = new Dictionary<ulong, Peer> { { server.Id, server }, { anna.Id, anna }, { bert.Id, bert } };

        var atServer = new Heard();
        var atAnna = new Heard();
        var atBert = new Heard();
        var servers = Channel(server, atServer, known);
        Channel(anna, atAnna, known);
        Channel(bert, atBert, known);

        _wire.As(server, () => servers.Broadcast(new Note { Number = 2, Words = "running" }, true));
        _wire.Settle();

        Assert.Empty(atServer.Said);
        Assert.Equal(new[] { "2 running" }, atAnna.Said);
        Assert.Equal(new[] { "2 running" }, atBert.Said);
        Assert.Equal(new[] { true }, atAnna.FromServer);
        Assert.Equal(new[] { false }, atAnna.AsServer);
    }

    /// <summary>
    /// Two messages sent back to back arrive as they were sent, although the channel writes both into
    /// the same buffer.
    /// </summary>
    [Fact]
    public void MessagesInFlightKeepWhatTheySaid()
    {
        var server = _wire.Server();
        var client = _wire.Client("Anna");
        var known = new Dictionary<ulong, Peer> { { server.Id, server }, { client.Id, client } };

        var atServer = new Heard();
        Channel(server, atServer, known);
        var clients = Channel(client, new Heard(), known);

        _wire.Latency = 5;
        _wire.As(client, () =>
        {
            clients.SendToServer(new Note { Number = 1, Words = "first" }, true);
            clients.SendToServer(new Note { Number = 2, Words = "second" }, true);
        });
        _wire.Settle();

        Assert.Equal(new[] { "1 first", "2 second" }, atServer.Said);
    }
}
