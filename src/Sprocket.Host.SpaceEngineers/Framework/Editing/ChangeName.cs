namespace Sprocket.Host.SpaceEngineers.Framework.Editing
{
    /// <summary>The things one change was made to, which the undo and redo arrows name it by.</summary>
    /// <remarks>Each is null or zero where the change has none of it.</remarks>
    public sealed class ChangeName
    {
        /// <summary>The thing changed: a step, a sequence, a slot, a trigger, a state, a type or its choice, an installation or a logic system.</summary>
        public object Thing { get; set; }

        /// <summary>Where the thing is: the sequence a step is in, the type a choice is of, the installation a slot is filled on.</summary>
        public object Place { get; set; }

        /// <summary>The number a step is shown with.</summary>
        public int Number { get; set; }

        /// <summary>What the thing was before the change: a name, a length of time, a step, a value.</summary>
        public object Was { get; set; }

        /// <summary>What the thing is after the change.</summary>
        public object Now { get; set; }

        /// <summary>How many things the change was made to, where it can be more than one.</summary>
        public int Count { get; set; }

        /// <summary>Forgets every thing named, for the next change.</summary>
        public void Clear()
        {
            Thing = null;
            Place = null;
            Number = 0;
            Was = null;
            Now = null;
            Count = 0;
        }
    }
}
