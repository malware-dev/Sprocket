using System.Collections.Generic;

using Reactor.Sequences;

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
        /// Bumped whenever the set of entities changes, and never otherwise.
        /// </summary>
        /// <remarks>
        /// <b>Matching is expensive and the answer only changes when this does.</b> Resolving what a
        /// program's declarations point at walks keys against blocks; doing that on every tick, for
        /// every reference, inside the game's update loop, is the shape this number exists to
        /// prevent. A reader compares one integer and does nothing the rest of the time.
        /// <para>
        /// The host is the only thing that can know. Space Engineers says so through the grid's own
        /// block-added and block-removed events, which is the same signal that already decides when
        /// <see cref="Entities" /> is rebuilt.
        /// </para>
        /// </remarks>
        int Revision { get; }

        /// <summary>
        /// Writes down every way this entity could be found again, strongest first.
        /// </summary>
        /// <remarks>
        /// The host decides what a key is and what order they go in, because only the host can match
        /// one later. An id is the strongest and the most fragile: it survives a rename and does not
        /// survive a blueprint, where every id changes and every position is identical.
        /// <para>
        /// Called when a reference is first made and whenever it is re-pointed, never per tick.
        /// </para>
        /// </remarks>
        void Identify(IEntity entity, List<EntityKey> keys);

        /// <summary>
        /// Finds whatever these keys point at, and says which one found it.
        /// </summary>
        /// <remarks>
        /// Tried in order and the first hit wins. <paramref name="matched" /> is that key's index,
        /// which is how core tells a reference that still holds from one that only has a candidate:
        /// zero is the strongest key and anything else is a proposal rather than a binding.
        /// <para>
        /// Null with <paramref name="matched" /> at -1 when nothing matched, which includes a
        /// declaration with no keys at all.
        /// </para>
        /// </remarks>
        IEntity Match(IList<EntityKey> keys, out int matched);

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

        /// <summary>
        /// Whether anything could start this sequence at all, as things currently stand.
        /// </summary>
        /// <remarks>
        /// Not whether it <i>will</i> - whether it <i>can</i>. A host may have to arrange something
        /// outside the program for a trigger to reach Reactor at all: in Space Engineers a button
        /// panel raises nothing from a slot that no longer points at Reactor, so a sequence listening
        /// to that button is complete, correct and unreachable. Only the host can tell.
        /// <para>
        /// <b>Asked about the sequence rather than about each of its sources</b>, which is what makes
        /// it affordable and what makes it right. Affordable, because a host can keep the answer ready
        /// and hand back a lookup instead of reading something expensive once per source. Right,
        /// because "is there anything wired here" is a weaker question than "is there anything wired
        /// here that starts <i>this</i>" - a button pointed at a different sequence answers yes to the
        /// first and is no use to this one.
        /// </para>
        /// <para>
        /// A host with nothing to arrange answers true and is done.
        /// </para>
        /// </remarks>
        bool CanStart(Sequence sequence);


        /// <summary>Tells an entity to do something.</summary>
        void Invoke(IEntity entity, string actionId);
    }
}
