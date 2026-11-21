using Sprocket.Host.SpaceEngineers.Framework.Network;
using Sandbox.ModAPI;
using Sprocket.Sim.Machine;
using Xunit;
using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A press of a toolbar slot holding Sprocket's action fires on the server: at once where it is pressed there, and carried there
/// from a client, whose ship toolbars run what they hold on the client's own machine.
/// </summary>
public class SlotPressTests : IDisposable
{
    readonly Wire _wire = new Wire();
    readonly Peer _server;
    readonly SlotPresses _serving;
    readonly IMyTerminalBlock _block;
    readonly List<string> _fired = new List<string>();
    readonly List<ulong> _refused = new List<ulong>();

    public SlotPressTests()
    {
        var sim = Simulation.Fresh();
        _block = sim.Machine.Sprocket.Block;
        _wire.Install();
        _server = _wire.Server();
        _serving = Presses(_server, "server");
    }

    public void Dispose()
    {
        _wire.Uninstall();
    }

    /// <summary>The presses of <paramref name="peer" />, which record what they fire under <paramref name="where" /> and refuse every player in <see cref="_refused" />.</summary>
    SlotPresses Presses(Peer peer, string where)
    {
        SlotPresses made = null!;
        _wire.As(peer, () =>
        {
            made = new SlotPresses(id => id == _block.EntityId ? _block : null, (_, sender) => !_refused.Contains(sender),
                (block, key) => _fired.Add($"{where}: {key} on {block.EntityId}"));
            made.Listen(SlotPresses.Carry(NetworkChannel.Create("Sprocket.SlotPressTests")).Build());
        });

        return made;
    }

    /// <summary>A slot pressed on the server, as a button panel's is, fires there at once and sends nothing.</summary>
    [Fact]
    public void APressOnTheServerFiresThere()
    {
        _wire.As(_server, () => _serving.Pressed(_block, "key-1"));

        Assert.Equal(new[] { $"server: key-1 on {_block.EntityId}" }, _fired);
    }

    /// <summary>A slot pressed on a client, as a ship's toolbar runs it, fires on the server and nowhere else.</summary>
    [Fact]
    public void APressOnAClientFiresOnTheServer()
    {
        var anna = _wire.Client("Anna");
        var annas = Presses(anna, "client");
        _wire.As(anna, () => annas.Pressed(_block, "key-1"));
        Assert.Empty(_fired);

        _wire.Settle();
        Assert.Equal(new[] { $"server: key-1 on {_block.EntityId}" }, _fired);
    }

    /// <summary>A press from a player the block does not let use it is not fired, as the game refuses a button panel's.</summary>
    [Fact]
    public void APressFromAPlayerWithoutAccessIsIgnored()
    {
        var bert = _wire.Client("Bert");
        var berts = Presses(bert, "client");
        _refused.Add(bert.Id);
        _wire.As(bert, () => berts.Pressed(_block, "key-1"));
        _wire.Settle();

        Assert.Empty(_fired);
    }
}
