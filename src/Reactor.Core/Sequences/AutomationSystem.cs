using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// One machine's worth of automation: an airlock, a hangar, a drill rig.
    /// </summary>
    /// <remarks>
    /// The player's word for this is "system", and that is what the editor says. The type
    /// cannot be called that - a type named <c>System</c> shadows the namespace every file
    /// in the project depends on - so the name is longer here and nowhere else.
    ///
    /// A system is what a machine <em>is</em>; a <see cref="SystemInstance"/> is one somebody
    /// actually has. Every system has at least one, made with it, because a machine nobody has
    /// fitted anywhere is not a thing this editor can show.
    ///
    /// Nothing is declared before it is used. A system exists because somebody built
    /// something, never because they made an empty one first.
    /// </remarks>
    public sealed class AutomationSystem
    {
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<SystemInstance> _instances = new List<SystemInstance>();

        public AutomationSystem(int id)
        {
            Id = id;
            Name = "";
            Description = "";
        }

        public int Id { get; private set; }

        /// <summary>
        /// What the player called it, or empty if they have not said.
        /// </summary>
        /// <remarks>
        /// Empty is the ordinary state rather than an oversight. Naming happens when there
        /// is something worth naming, and until then <see cref="HasName"/> is false and the
        /// editor shows whatever it can work out instead.
        /// </remarks>
        public string Name { get; set; }

        public bool HasName { get { return !string.IsNullOrEmpty(Name); } }

        /// <summary>
        /// What this thing is, in the words of whoever built it.
        /// </summary>
        /// <remarks>
        /// A label to its author and the only explanation anybody who receives it will ever
        /// get, so it travels with the system when it is shared.
        /// </remarks>
        public string Description { get; set; }

        public IReadOnlyList<Sequence> Sequences { get { return _sequences; } }

        /// <summary>The ones on this ship. Never empty.</summary>
        public IReadOnlyList<SystemInstance> Instances { get { return _instances; } }

        /// <summary>
        /// Adds another of this machine, named after the system and its number.
        /// </summary>
        /// <remarks>
        /// It is not given a name. What it is called - Airlock 1, Airlock 2, the way the game
        /// says Interior Light 4 - is worked out from the system and the number, so a machine
        /// that only learns its own name later takes its children with it.
        /// </remarks>
        public SystemInstance AddInstance()
        {
            var instance = new SystemInstance(_instances.Count + 1);
            _instances.Add(instance);
            return instance;
        }

        internal void Add(Sequence sequence)
        {
            _sequences.Add(sequence);
        }

        internal bool Remove(Sequence sequence)
        {
            return _sequences.Remove(sequence);
        }
    }
}
