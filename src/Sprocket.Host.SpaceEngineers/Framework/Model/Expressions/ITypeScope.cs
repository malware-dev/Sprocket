using Sprocket.Host.SpaceEngineers.Framework.Game.Members;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>What kinds of value an expression's operands are, where that is known.</summary>
    public interface ITypeScope
    {
        /// <summary>The kind the setting <paramref name="name" /> holds, or null where it is not known.</summary>
        ValueKind? Setting(string name);

        /// <summary>The kind the state <paramref name="name" /> holds, or null where it is not known.</summary>
        ValueKind? State(string name);

        /// <summary>The kind the member <paramref name="reading" /> reads holds on its blocks, or null where they are not known.</summary>
        ValueKind? Reading(BlockReading reading);
    }
}
