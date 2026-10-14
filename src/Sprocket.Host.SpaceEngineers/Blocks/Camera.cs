using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A camera: whether somebody is looking through it, and how that view behaves.</summary>
    /// <remarks>
    /// Looking through it is something a person does. Whether this machine's own player is looking is known only on that
    /// player's machine, never on the server where sequences run; whether anybody is, the server knows, since the number
    /// looking is shared with it.
    /// </remarks>
    public sealed class Camera : Capability
    {
        /// <summary>Whether somebody is looking through it, which is Sprocket's own.</summary>
        public const string InUse = "in-use";

        const string Unheard = "A camera raises no event when somebody starts or stops looking through it.";

        static readonly Being Watched = Being.Flag(Reading.Of<IMyCameraBlock>(camera => camera.IsActive));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyCameraBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue(InUse, "in use", Watched).Looked(Unheard)
                .Renaming("EnablePanning", true, "panning")
                .Renaming("EnablePanning", false, "toggle panning")
                .Renaming("ViewRotation", true, "view rotation");
        }
    }
}
