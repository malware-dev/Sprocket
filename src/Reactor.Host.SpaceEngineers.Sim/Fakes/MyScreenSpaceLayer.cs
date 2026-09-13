
namespace Sandbox.ModAPI
{
    /// <summary>
    /// Which of the game's own layers a draw belongs to. Ordering between layers is the game's business and
    /// the simulator honours it by drawing them in this order.
    /// </summary>
    public enum MyScreenSpaceLayer
    {
        BelowHud,
        AboveHud,
        AboveEverything
    }
}
