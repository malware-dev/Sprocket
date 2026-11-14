using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A block that is several things at once answers as all of them: what stops it first, then the first that says anything.</summary>
public class CapabilityTests
{
    [Fact]
    public void WhatStopsTheBlockIsShownBeforeWhatItIsDoing()
    {
        var entity = Entity(new Saying(Status.Of(Condition.Open)), new Saying(Status.Of(BlockTrouble.Waiting(Cause.VentNoOxygen))));

        Assert.Equal(Cause.VentNoOxygen, entity.Status.Trouble.Cause);
    }

    [Fact]
    public void OtherwiseTheFirstThatSaysAnythingIsShown()
    {
        var entity = Entity(new Saying(Status.None), new Saying(Status.Of(Condition.Full)), new Saying(Status.Of(Condition.Open)));

        Assert.Equal(Condition.Full, entity.Status.Condition);
    }

    [Fact]
    public void ABlockIsBusyWhereAnythingItIsSaysSo()
    {
        Assert.False(Entity(new Saying(Status.None), new Saying(Status.None)).IsActive);
        Assert.True(Entity(new Saying(Status.None), new Saying(Status.None, true)).IsActive);
    }

    /// <summary>A lamp known as each of <paramref name="capabilities" />, in that order.</summary>
    static GridEntity Entity(params Capability[] capabilities)
    {
        var ship = new Rigged();
        var lamp = ship.Light(1, "Lamp");
        var kind = new EntityKind("lamp", "Lamp");
        foreach (var capability in capabilities)
            kind.Known(capability);

        return new GridEntity(lamp.Block, kind);
    }

    /// <summary>Something a block is, which says one thing whatever is asked.</summary>
    sealed class Saying : Capability
    {
        readonly Status _says;
        readonly bool _busy;

        public Saying(Status says, bool busy = false)
        {
            _says = says;
            _busy = busy;
        }

        public override bool Claims(IMyTerminalBlock block)
        {
            return true;
        }

        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
        }

        public override Status StatusOf(IMyTerminalBlock block)
        {
            return _says;
        }

        public override bool IsBusy(IMyTerminalBlock block)
        {
            return _busy;
        }
    }
}
