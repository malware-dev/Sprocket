namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>Something listed under a category among others of its kind: a slot on the installation panel, a sequence on the Sequences tab.</summary>
    public interface ICategorized
    {
        /// <summary>The category it is listed under, or empty for none.</summary>
        string Category { get; set; }
    }
}
