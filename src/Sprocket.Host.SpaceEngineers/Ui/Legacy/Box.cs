namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>What every legacy control is: a container whose children can be cut to its bounds by a switch.</summary>
    public abstract class Box : Container
    {
        /// <summary>Whether the children are cut to the bounds.</summary>
        public bool ClipsContent { get; set; }

        /// <inheritdoc />
        protected override bool Clips => ClipsContent;
    }
}
