namespace Reactor.Sequences
{
    /// <summary>
    /// A part a system needs filling in: the inner door, the vent, the warning light.
    /// </summary>
    /// <remarks>
    /// Roles are not declared. A role exists because a sequence acts on something, so the list
    /// of them cannot disagree with what the system actually does - a hand-kept list is a
    /// second thing to maintain and it goes stale the first time somebody adds a step. That is
    /// R23, and it is why this has no "add a role" anywhere.
    ///
    /// It is identified by the thing the system was built with. The airlock its author wrote
    /// used their own inner door, and that door's id is what every sequence in the system
    /// refers to; another one of the system says "where they used that, I use this". The same
    /// door mentioned in five places is one role, asked once.
    ///
    /// What is *not* derived is any of the wording. An author naming a role in their own words
    /// and writing a line about which door they mean is the difference between a system a
    /// stranger can set up and a list of block names that meant something to somebody else.
    /// That is authored, kept here, and deliberately outlives the derivation: delete the last
    /// step that used a door and the name written for it is still there when it comes back.
    /// </remarks>
    public sealed class SystemRole
    {
        public SystemRole(string id)
        {
            Id = id;
            Name = "";
            Instructions = "";
        }

        /// <summary>
        /// What the system's own sequences point at, which is whatever its author built with.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// What the author calls this part, or empty to fall back on what they built it with.
        /// </summary>
        /// <remarks>
        /// Empty is the ordinary state and reads as the block's own name. Somebody who never
        /// names a role has still made a system that can be set up, just one that says
        /// "Airlock Inner Door" where it could have said "the door onto the ship".
        /// </remarks>
        public string Name { get; set; }

        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// What the author wants said to whoever is filling this in.
        /// </summary>
        /// <remarks>
        /// The only chance an author gets to explain themselves. Which of two doors faces
        /// outward is obvious to the person who built the thing and invisible to everybody
        /// else, and there is nothing in the blocks that could tell them.
        /// </remarks>
        public string Instructions { get; set; }

        /// <summary>
        /// Whether a use of the system may leave this empty and still work.
        /// </summary>
        /// <remarks>
        /// R24. Not honoured anywhere yet - nothing consults it, and the parts of a sequence
        /// that depend on an unfilled role are supposed to drop out of that use, which is not
        /// built. Here so the model does not have to change shape when it is.
        /// </remarks>
        public bool Optional { get; set; }
    }
}
