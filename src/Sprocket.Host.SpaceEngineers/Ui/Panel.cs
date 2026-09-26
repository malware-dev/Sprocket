namespace Sprocket.Host.SpaceEngineers.Ui
{
    /// <summary>A container holding any number of children, laid out by the kind of panel it is.</summary>
    public abstract class Panel : Container
    {
        /// <summary>An empty panel.</summary>
        protected Panel()
        {
            Children = Hold();
        }

        /// <summary>What the panel holds, in order.</summary>
        public Collection Children { get; }
    }
}
