using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Blueprints
{
    /// <summary>
    /// A blueprint: the logic systems made from it, and its parts with the blocks the chosen one uses.
    /// </summary>
    /// <remarks>
    /// Two lists side by side rather than one under the other, because the question the screen answers
    /// is which blocks one of these uses - and pointing at another one on the left, with its blocks
    /// changing on the right while the parts stay put, is the answer seen rather than explained.
    /// </remarks>
    public sealed class BlueprintComponent : EditorComponent
    {
        StatusEntry _chosen;
        string _madeTitle = "";
        string _partsTitle = "";
        string _renamePrompt = "";

        public BlueprintComponent()
        {
            Members = new ObservableList<StatusEntry>();
            Parts = new ObservableList<PartEntry>();
        }

        /// <summary>The logic systems made from the blueprint, and the row that adds another.</summary>
        public ObservableList<StatusEntry> Members { get; private set; }

        /// <summary>
        /// The chosen logic system again, as the head of what is shown beside the list: its name, its
        /// switch, and the way into what it does.
        /// </summary>
        public StatusEntry Chosen
        {
            get { return _chosen; }
            set { Set(ref _chosen, value, nameof(Chosen)); }
        }

        /// <summary>The blueprint's parts, with the chosen logic system's blocks for each.</summary>
        public ObservableList<PartEntry> Parts { get; private set; }

        /// <summary>What the list of logic systems is headed.</summary>
        public string MadeTitle
        {
            get { return _madeTitle; }
            set { Set(ref _madeTitle, value ?? "", nameof(MadeTitle)); }
        }

        /// <summary>What renaming a part asks.</summary>
        public string RenamePrompt
        {
            get { return _renamePrompt; }
            set { Set(ref _renamePrompt, value ?? "", nameof(RenamePrompt)); }
        }

        /// <summary>What the list of parts is headed.</summary>
        public string PartsTitle
        {
            get { return _partsTitle; }
            set { Set(ref _partsTitle, value ?? "", nameof(PartsTitle)); }
        }
    }
}
