using System;
using System.Collections.Generic;
using Sprocket.Sim.Machine;
using VRageMath;

namespace Sprocket.Sim.Blueprints;

/// <summary>A generated build on one grid: blocks standing in rows on armor floors.</summary>
/// <remarks>Laid out before the program is written, since a pasted copy's program finds its blocks by their cells.</remarks>
public sealed class Deck
{
    /// <summary>The type of a floor's armor.</summary>
    public const string ArmorType = "CubeBlock";

    /// <summary>The subtype of a floor's armor.</summary>
    public const string ArmorSubtype = "LargeBlockArmorBlock";

    /// <summary>The type of a conveyor junction, which joins the conveyor ports on all six of its faces.</summary>
    public const string ConveyorType = "Conveyor";

    /// <summary>The subtype of a large grid's conveyor junction.</summary>
    public const string ConveyorSubtype = "LargeBlockConveyor";

    readonly BlockDefinitions _definitions;
    readonly Dictionary<Fitting, Placement> _placed = new Dictionary<Fitting, Placement>();
    readonly List<Fitting> _order = new List<Fitting>();
    readonly List<Vector3I> _armor = new List<Vector3I>();
    readonly List<Vector3I> _conveyors = new List<Vector3I>();
    readonly Dictionary<Vector3I, string> _taken = new Dictionary<Vector3I, string>();
    readonly Dictionary<Fitting, string> _texts = new Dictionary<Fitting, string>();
    readonly Dictionary<Vector3I, Fitting> _at = new Dictionary<Vector3I, Fitting>();

    /// <summary>An empty deck, its blocks read from <paramref name="definitions" />.</summary>
    public Deck(BlockDefinitions definitions)
    {
        _definitions = definitions;
    }

    /// <summary>The blocks laid.</summary>
    public IReadOnlyList<Fitting> Blocks => _order;

    /// <summary>The armor cells.</summary>
    public IReadOnlyList<Vector3I> Armor => _armor;

    /// <summary>The definitions the deck was laid from.</summary>
    public BlockDefinitions Definitions => _definitions;

    /// <summary>Where <paramref name="fitting" /> stands.</summary>
    public Placement this[Fitting fitting] => _placed[fitting];

    /// <summary>The text a sign shows, or null for any other block.</summary>
    public string Text(Fitting fitting)
    {
        string text;
        return _texts.TryGetValue(fitting, out text) ? text : null;
    }

    /// <summary>Stands <paramref name="panel" />, a wide LCD, upright against a short wall off the floor's edge at <paramref name="edge" />, showing <paramref name="text" />.</summary>
    public void Sign(Fitting panel, Vector3I edge, string text)
    {
        Floor(edge + new Vector3I(0, 0, -1));
        Floor(edge + new Vector3I(0, 0, -2));
        Floor(edge + new Vector3I(0, 1, -2));
        Floor(edge + new Vector3I(0, 2, -2));
        Place(panel, Defined(panel), new Placement(edge + new Vector3I(0, 1, -1), "Forward", "Left"));
        _texts[panel] = text;
    }

    /// <summary>Stands <paramref name="fittings" /> in rows on a floor from <paramref name="corner" />; answers the floor's far corner.</summary>
    public Vector3I Lay(IEnumerable<Fitting> fittings, Vector3I corner, int width)
    {
        var x = 0;
        var z = 0;
        var depth = 0;
        var widest = 0;
        foreach (var fitting in fittings)
        {
            var definition = Defined(fitting);
            var resting = Placement.Resting(definition.Resting, Vector3I.Zero);
            var extent = resting.Extent(definition);
            if (x > 0 && x + extent.X > width)
            {
                z += depth + 1;
                x = 0;
                depth = 0;
            }

            var placement = Placement.Resting(definition.Resting, corner + new Vector3I(1 + x, 1, 1 + z));
            Place(fitting, definition, placement);
            x += extent.X + 1;
            widest = Math.Max(widest, x);
            depth = Math.Max(depth, extent.Z);
        }

        var far = corner + new Vector3I(widest + 1, 0, z + depth + 1);
        for (var fx = corner.X; fx <= far.X; fx++)
        for (var fz = corner.Z; fz <= far.Z; fz++)
            Floor(new Vector3I(fx, corner.Y, fz));

        return far;
    }

    /// <summary>The conveyor junction cells.</summary>
    public IReadOnlyList<Vector3I> Conveyors => _conveyors;

    /// <summary>Puts one conveyor junction at <paramref name="cell" />, joining what has a conveyor port on its faces.</summary>
    public void Conveyor(Vector3I cell)
    {
        Take(cell, ConveyorSubtype);
        _conveyors.Add(cell);
    }

    /// <summary>
    /// Fills the gap between <paramref name="near" /> and <paramref name="far" />, laid side by side in one row, with conveyor
    /// junctions over the full height and depth of both, so every conveyor port on the faces they turn to each other joins.
    /// </summary>
    public void ConveyorWall(Fitting near, Fitting far)
    {
        var a = _placed[near];
        var b = _placed[far];
        var aEnd = a.Min + a.Extent(Defined(near)) - Vector3I.One;
        var bEnd = b.Min + b.Extent(Defined(far)) - Vector3I.One;
        var x = aEnd.X + 1;
        for (var y = Math.Min(a.Min.Y, b.Min.Y); y <= Math.Max(aEnd.Y, bEnd.Y); y++)
        for (var z = Math.Min(a.Min.Z, b.Min.Z); z <= Math.Max(aEnd.Z, bEnd.Z); z++)
            Conveyor(new Vector3I(x, y, z));
    }

    /// <summary>Puts one armor block at <paramref name="cell" />.</summary>
    public void Floor(Vector3I cell)
    {
        Take(cell, ArmorSubtype);
        _armor.Add(cell);
    }

    /// <summary>The blocks of each part the build falls into with the cell <paramref name="gone" /> ground away.</summary>
    public List<List<Fitting>> Parts(Vector3I gone)
    {
        var parts = new List<List<Fitting>>();
        var seen = new HashSet<Vector3I> { gone };
        foreach (var start in _taken.Keys)
        {
            if (!seen.Add(start))
                continue;

            var part = new List<Fitting>();
            var waiting = new Queue<Vector3I>();
            waiting.Enqueue(start);
            while (waiting.Count > 0)
            {
                var cell = waiting.Dequeue();
                Fitting fitting;
                if (_at.TryGetValue(cell, out fitting) && !part.Contains(fitting))
                    part.Add(fitting);

                foreach (var next in new[]
                         {
                             cell + Vector3I.Right, cell + Vector3I.Left, cell + Vector3I.Up, cell + Vector3I.Down,
                             cell + Vector3I.Forward, cell + Vector3I.Backward
                         })
                {
                    if (_taken.ContainsKey(next) && seen.Add(next))
                        waiting.Enqueue(next);
                }
            }

            parts.Add(part);
        }

        return parts;
    }

    /// <summary>The definition of <paramref name="fitting" />, refusing a block the game does not define.</summary>
    public BlockDefinition Defined(Fitting fitting)
    {
        var id = fitting.Definition;
        var definition = _definitions.Find(id.TypeId.ToString(), id.SubtypeName);
        if (definition == null)
            throw new InvalidOperationException($"{fitting.Name} is a {id.TypeId}/{id.SubtypeName}, which the game does not define.");

        return definition;
    }

    void Place(Fitting fitting, BlockDefinition definition, Placement placement)
    {
        foreach (var cell in placement.Cells(definition))
        {
            Take(cell, fitting.Name);
            _at[cell] = fitting;
        }

        _placed[fitting] = placement;
        _order.Add(fitting);
        fitting.Cell = placement.Position(definition);
    }

    void Take(Vector3I cell, string what)
    {
        string there;
        if (_taken.TryGetValue(cell, out there))
            throw new InvalidOperationException($"{what} and {there} would both take the cell {cell}.");

        _taken[cell] = what;
    }
}
