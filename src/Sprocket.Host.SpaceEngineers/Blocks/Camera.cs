using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A camera: whether somebody is looking through it, and how that view behaves.</summary>
    /// <remarks>
    /// The server knows whether anybody is looking through it; whether this machine's own player is looking is known only
    /// on that player's machine.
    /// </remarks>
    public sealed class Camera : Capability
    {
        /// <summary>Whether somebody is looking through it, which is Sprocket's own.</summary>
        public const string InUse = "in-use";

        const string Unheard = "A camera raises no event when somebody starts or stops looking through it.";

        static readonly OwnValue Watched = OwnValue.Flag(Reading.Of<IMyCameraBlock>(camera => camera.IsActive));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyCameraBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue(InUse, "in use", Watched).Looked(Unheard).In(Category.Usage)
                .Renaming("EnablePanning", true, "panning")
                .Renaming("EnablePanning", false, "toggle panning")
                .Renaming("ViewRotation", true, "view rotation");
        }
    }
}
