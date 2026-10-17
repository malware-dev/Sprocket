using System;
using System.Collections.Generic;
using VRageMath;

namespace Sprocket.Sim.Blueprints;

/// <summary>Where a block stands on its grid, and which way it faces.</summary>
/// <remarks>The game's directions: <c>Forward</c> is minus Z, <c>Up</c> plus Y, <c>Right</c> plus X.</remarks>
public sealed class Placement
{
    /// <summary>A block at <paramref name="min" />, facing <paramref name="forward" /> with its top toward <paramref name="up" />.</summary>
    public Placement(Vector3I min, string forward, string up)
    {
        Min = min;
        Forward = forward;
        Up = up;
    }

    /// <summary>Its lowest corner cell.</summary>
    public Vector3I Min { get; private set; }

    /// <summary>The grid's direction its front faces.</summary>
    public string Forward { get; private set; }

    /// <summary>The grid's direction its top faces.</summary>
    public string Up { get; private set; }

    /// <summary>The turn that stands a block on <paramref name="face" />, at <paramref name="min" />.</summary>
    public static Placement Resting(Side face, Vector3I min)
    {
        switch (face)
        {
            case Side.Top: return new Placement(min, "Forward", "Down");
            case Side.Back: return new Placement(min, "Up", "Forward");
            case Side.Front: return new Placement(min, "Down", "Backward");
            case Side.Left: return new Placement(min, "Forward", "Left");
            case Side.Right: return new Placement(min, "Forward", "Right");
            default: return new Placement(min, "Forward", "Up");
        }
    }

    /// <summary>How many cells a block of <paramref name="definition" /> takes along each axis.</summary>
    public Vector3I Extent(BlockDefinition definition)
    {
        return Vector3I.Abs(Turned(definition.Size - Vector3I.One)) + Vector3I.One;
    }

    /// <summary>Every cell a block of <paramref name="definition" /> takes.</summary>
    public IEnumerable<Vector3I> Cells(BlockDefinition definition)
    {
        var extent = Extent(definition);
        for (var x = 0; x < extent.X; x++)
        for (var y = 0; y < extent.Y; y++)
        for (var z = 0; z < extent.Z; z++)
            yield return Min + new Vector3I(x, y, z);
    }

    /// <summary>The cell the game calls the block's position.</summary>
    /// <remarks>The game's rule: the turned center from the lowest corner, shifted along each axis the turn reverses.</remarks>
    public Vector3I Position(BlockDefinition definition)
    {
        var length = Turned(definition.Size - Vector3I.One);
        var position = Min + Turned(definition.Center);
        if (length.X < 0)
            position.X -= length.X;
        if (length.Y < 0)
            position.Y -= length.Y;
        if (length.Z < 0)
            position.Z -= length.Z;

        return position;
    }

    Vector3I Turned(Vector3I local)
    {
        var up = Direction(Up);
        var back = -Direction(Forward);
        var right = new Vector3I(up.Y * back.Z - up.Z * back.Y, up.Z * back.X - up.X * back.Z, up.X * back.Y - up.Y * back.X);
        return right * local.X + up * local.Y + back * local.Z;
    }

    static Vector3I Direction(string name)
    {
        switch (name)
        {
            case "Forward": return new Vector3I(0, 0, -1);
            case "Backward": return new Vector3I(0, 0, 1);
            case "Left": return new Vector3I(-1, 0, 0);
            case "Right": return new Vector3I(1, 0, 0);
            case "Up": return new Vector3I(0, 1, 0);
            case "Down": return new Vector3I(0, -1, 0);
            default: throw new ArgumentException($"'{name}' is no direction a blueprint writes.");
        }
    }
}
