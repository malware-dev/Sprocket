using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>How many blocks the kinds selector's rows stand for: all of them, each kind's and each family's.</summary>
    public interface IKindCounts
    {
        /// <summary>How many blocks there are in all.</summary>
        int All { get; }

        /// <summary>How many blocks are of the kind <paramref name="kindId" />.</summary>
        int CountOfKind(string kindId);

        /// <summary>How many blocks are of <paramref name="family" />.</summary>
        int CountOfFamily(Family family);
    }
}
