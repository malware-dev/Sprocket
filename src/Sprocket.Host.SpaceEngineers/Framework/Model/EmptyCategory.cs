namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A category with nothing under it, on the installation panel or the Sequences tab, standing above a slot or sequence or under them all.</summary>
    /// <remarks>Kept with the logic system in memory only: the program says a category through the slots under it.</remarks>
    public sealed class EmptyCategory
    {
        /// <summary>
        /// The category <paramref name="name" />, standing above <paramref name="above" />, or under every one where that is null,
        /// what is put in it running one at a time where <paramref name="oneAtATime" />.
        /// </summary>
        public EmptyCategory(string name, ICategorized above, bool oneAtATime = false)
        {
            Name = name;
            Above = above;
            OneAtATime = oneAtATime;
        }

        /// <summary>What the category is called.</summary>
        public string Name { get; }

        /// <summary>What it stands above, or null where it stands under every one.</summary>
        public ICategorized Above { get; }

        /// <summary>Whether what is put in it runs one at a time, which it then says by naming the category as its <see cref="IExclusive.Exclusive" />.</summary>
        public bool OneAtATime { get; }
    }
}
