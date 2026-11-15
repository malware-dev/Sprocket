namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>Something that may run one at a time with others of its kind that name the same id: a sequence.</summary>
    public interface IExclusive
    {
        /// <summary>The id of those it runs one at a time with, or empty for none.</summary>
        string Exclusive { get; set; }
    }
}
