
namespace Lumel.Layout
{
    /// <summary>A placement in a stack, which can ask for a share of the room the other children leave.</summary>
    public interface IStacked : ISized
    {
        /// <summary>This child's weight in sharing out the room left along the stack, or zero to take only its own size.</summary>
        float Grow { get; }

        /// <summary>Whether a child that grows asks for the room it grows into as the stack is measured, which makes the stack take all it is offered; otherwise it takes only what the stack is arranged in beyond what the others want.</summary>
        bool Claims { get; }
    }
}
