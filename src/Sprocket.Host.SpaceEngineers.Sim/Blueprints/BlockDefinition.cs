using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace Sprocket.Sim.Blueprints;

/// <summary>What the game's definition of a kind of block says about placing it.</summary>
public sealed class BlockDefinition
{
    /// <summary>The definition of the block <paramref name="type" /> and <paramref name="subtype" />.</summary>
    public BlockDefinition(string type, string subtype, Vector3I size, Vector3I center, IReadOnlyList<Mount> mounts, string stored)
    {
        Stored = stored;
        Type = type;
        Subtype = subtype;
        Size = size;
        Center = center;
        Mounts = mounts;
    }

    /// <summary>The block's type as a blueprint writes it after <c>MyObjectBuilder_</c>: <c>Door</c>, <c>ButtonPanel</c>.</summary>
    public string Type { get; private set; }

    /// <summary>The block's subtype, empty for the type's own block.</summary>
    public string Subtype { get; private set; }

    /// <summary>How many cells it takes along its own right, up and back.</summary>
    public Vector3I Size { get; private set; }

    /// <summary>The cell, counted from its own corner, that the game calls its position.</summary>
    public Vector3I Center { get; private set; }

    /// <summary>How much power it stores when full, as the definition writes it, or null.</summary>
    public string Stored { get; private set; }

    /// <summary>The faces it mounts by, as the definition lists them.</summary>
    public IReadOnlyList<Mount> Mounts { get; private set; }

    /// <summary>The face it is meant to stand on.</summary>
    /// <remarks>A block that lists no mount points mounts by every face, as an armor block does.</remarks>
    public Side Resting
    {
        get
        {
            if (Mounts.Count == 0)
                return Side.Bottom;

            foreach (var mount in Mounts)
            {
                if (mount.Default)
                    return mount.Side;
            }

            var sides = Mounts.Select(mount => mount.Side).Distinct().ToList();
            return sides.Contains(Side.Bottom) ? Side.Bottom : sides.Min();
        }
    }
}
