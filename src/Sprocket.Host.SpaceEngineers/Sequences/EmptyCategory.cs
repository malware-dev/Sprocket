namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A category on the installation panel with no slot under it, standing above a slot or under them all.</summary>
    /// <remarks>Kept with the logic system in memory only: the program says a category through the slots under it.</remarks>
    public sealed class EmptyCategory
    {
        /// <summary>The category <paramref name="name" />, standing above <paramref name="above" />, or under every slot where that is null.</summary>
        public EmptyCategory(string name, Part above)
        {
            Name = name;
            Above = above;
        }

        /// <summary>What the category is called.</summary>
        public string Name { get; }

        /// <summary>The slot it stands above, or null where it stands under every slot.</summary>
        public Part Above { get; }
    }
}
