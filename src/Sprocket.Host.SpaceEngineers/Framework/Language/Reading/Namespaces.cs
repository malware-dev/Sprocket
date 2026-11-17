namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>The namespaces of what a logic system declares, which a name of it is qualified by where alone it could mean more than one thing.</summary>
    /// <remarks>Persisted in every program that qualifies one of these names.</remarks>
    public static class Namespaces
    {
        /// <summary>A system blueprint's settings: <c>settings.'Open angle'</c>.</summary>
        public const string Settings = "settings";

        /// <summary>A logic system's states: <c>states.open</c>.</summary>
        public const string States = "states";

        /// <summary>A logic system's types: <c>expects types.Door</c>.</summary>
        public const string Types = "types";
    }
}
