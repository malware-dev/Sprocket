
namespace Reactor.Sim.Machine;
sealed class SimTank(long id, string name) : SimEntity(id, name, Kinds.Tank)
{
    float _level = 1f;

    public bool Draining { get; set; }

    public override string Status => $"{(int)(_level * 100)}%" + (Flag("low") ? " - low" : "");

    public override bool IsActive => Flag("low");

    public override void Tick()
    {
        _level += Draining ? -0.002f : 0.0008f;
        _level = Math.Clamp(_level, 0f, 1f);

        // The reading core gets is the level itself. "Low" is a relation the kind declares against it,
        // not something decided here and flattened to a bool on the way out, which would throw the number
        // away before anything could ask a different question of it.
        Set("fill", _level);
    }
}
