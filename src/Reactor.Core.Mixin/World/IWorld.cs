using System.Collections.Generic;

namespace Reactor.World
{
    /// <summary>
    /// The machine Reactor is attached to, as seen from the Reactor unit.
    /// </summary>
    /// <remarks>
    /// Scope is the host's responsibility, and it is not negotiable at this level:
    /// <see cref="Entities"/> contains this construct only - the grid the Reactor unit
    /// sits on plus anything mechanically connected to it by rotors, pistons and
    /// hinges. Connectors, landing gear and magplates are docking, not construction,
    /// so whatever is on the other side of them is somebody else's machine and never
    /// appears here.
    ///
    /// That rule is what stops a rule's meaning from changing underneath the user
    /// when they dock, which in Space Engineers is a routine event rather than an
    /// edge case.
    /// </remarks>
    public interface IWorld
    {
        /// <summary>
        /// Everything on this construct, right now. Re-read rather than cached -
        /// blocks get welded on and shot off while the program is running.
        /// </summary>
        IReadOnlyList<IEntity> Entities { get; }

        /// <summary>
        /// What the people playing call these things.
        /// </summary>
        /// <remarks>
        /// Here rather than anywhere else because everything else about what an entity is comes
        /// from the host too - the words for its status, the display name of its kind. What it
        /// is called as a category is the same kind of fact, from the same authority.
        /// </remarks>
        EntityNoun Noun { get; }

        /// <summary>
        /// Looks up an entity by id, or returns null if it is no longer part of this
        /// construct.
        /// </summary>
        /// <remarks>
        /// Null here is a state the UI must surface loudly rather than skip over. A
        /// rule that quietly refers to a door that no longer exists is
        /// indistinguishable from a rule that is working, which is exactly the kind
        /// of silent failure that makes automation impossible to trust.
        /// </remarks>
        IEntity FindEntity(long entityId);

        /// <summary>
        /// Makes and reads the values this world deals in.
        /// </summary>
        /// <remarks>
        /// One store, because a comparison meets a reading its entities produced and an operand
        /// declared on one of their kinds, and a slot only means anything against the lists it was
        /// taken from. Anything that fits in eight bytes needs none of this - see
        /// <see cref="EntityValue.Of(bool)" /> - so a host only reaches for it to carry something
        /// wider, which today means text.
        /// </remarks>
        Values Values { get; }

        /// <summary>
        /// What this construct is called.
        /// </summary>
        /// <remarks>
        /// The host's answer, like <see cref="Noun"/>, and for the same reason - what a machine is called
        /// is a fact about the game and not about automation. Never empty: a game that names nothing can
        /// answer with whatever it puts on the thing itself, which in Space Engineers is a name the game
        /// made up and the player has simply not changed yet. There is no way to tell those apart and no
        /// reason to try.
        /// </remarks>
        string Name { get; }

        /// <summary>Whether <see cref="Rename"/> will do anything.</summary>
        /// <remarks>
        /// False is a real answer. A target drawing onto a text panel is looking at somebody else's
        /// machine and has no business renaming it, and a screen that offered to would be offering
        /// something it cannot do.
        /// </remarks>
        bool CanRename { get; }

        /// <summary>Changes what this construct is called.</summary>
        void Rename(string name);

        /// <summary>Tells an entity to do something.</summary>
        void Invoke(IEntity entity, string actionId);
    }
}
