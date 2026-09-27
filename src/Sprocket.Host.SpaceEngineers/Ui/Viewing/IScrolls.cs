
namespace Sprocket.Host.SpaceEngineers.Ui.Viewing
{
    /// <summary>Something that shows part of what it holds and moves up and down over the rest, as a wheel moves it.</summary>
    public interface IScrolls
    {
        /// <summary>Whether any of what it holds is out of sight.</summary>
        bool CanScroll { get; }

        /// <summary>Moves by <paramref name="amount" />, down for more than nought, and stays inside what it holds.</summary>
        void ScrollBy(float amount);
    }
}
