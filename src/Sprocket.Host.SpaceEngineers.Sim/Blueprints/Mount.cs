namespace Sprocket.Sim.Blueprints;

/// <summary>One mount point of a block's definition.</summary>
public sealed class Mount
{
    /// <summary>A mount on <paramref name="side" />.</summary>
    public Mount(Side side, bool isDefault)
    {
        Side = side;
        Default = isDefault;
    }

    /// <summary>The face it is on.</summary>
    public Side Side { get; private set; }

    /// <summary>Whether it is the face the block is placed by.</summary>
    public bool Default { get; private set; }
}
