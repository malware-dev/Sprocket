using Sprocket.Host.SpaceEngineers.Features.Gravity;
using VRageMath;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A gravity generator's field fitted to a box of cells reaches every one of them, in the generator's own axes.</summary>
public class GravityFieldSizeTests
{
    const float Large = 2.5f;

    /// <summary>A generator at the grid's origin, turned as the grid is, reaches the farthest cell on each axis both ways.</summary>
    [Fact]
    public void TheFieldReachesTheFarthestCellBothWays()
    {
        var size = GravityFieldSize.Covering(MatrixD.Identity, Large, new Vector3I(-2, -1, 0), new Vector3I(3, 1, 4), MatrixD.Identity,
            Vector3.Zero);

        Near(new Vector3(17.5f, 7.5f, 22.5f), size);
    }

    /// <summary>A generator turned on its grid sizes the field along its own axes.</summary>
    [Fact]
    public void ATurnedGeneratorSizesAlongItsOwnAxes()
    {
        var turned = MatrixD.CreateWorld(Vector3D.Zero, Vector3D.Right, Vector3D.Up);

        var size = GravityFieldSize.Covering(MatrixD.Identity, Large, new Vector3I(-2, -1, 0), new Vector3I(3, 1, 4), turned, Vector3.Zero);

        Near(new Vector3(22.5f, 7.5f, 17.5f), size);
    }

    /// <summary>A generator at one end of the cells overhangs the near end, the field being centred on it.</summary>
    [Fact]
    public void AGeneratorAtOneEndOverhangsTheNearEnd()
    {
        var size = GravityFieldSize.Covering(MatrixD.Identity, Large, Vector3I.Zero, new Vector3I(3, 0, 0), MatrixD.Identity, Vector3.Zero);

        Near(new Vector3(17.5f, 2.5f, 2.5f), size);
    }

    /// <summary>Where the grid stands in the world makes no difference.</summary>
    [Fact]
    public void WhereTheGridStandsMakesNoDifference()
    {
        var grid = MatrixD.CreateWorld(new Vector3D(1000, -50, 7), Vector3D.Normalize(new Vector3D(1, 0, 1)), Vector3D.Up);
        var generator = MatrixD.CreateTranslation(new Vector3D(-2, 0, 0) * Large) * grid;

        var size = GravityFieldSize.Covering(grid, Large, new Vector3I(-2, -1, 0), new Vector3I(3, 1, 4), generator, Vector3.Zero);

        Near(new Vector3(27.5f, 7.5f, 22.5f), size);
    }

    static void Near(Vector3 expected, Vector3 actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
        Assert.Equal(expected.Z, actual.Z, 3);
    }
}
