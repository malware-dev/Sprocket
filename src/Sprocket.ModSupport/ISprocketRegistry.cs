using Sandbox.ModAPI;

namespace Sprocket.ModSupport
{
    /// <summary>What the wrapper's types do on a mod's registration, so that nothing but the approved path adds to it.</summary>
    /// <remarks>The wrapper's own plumbing, not for mods: a mod reaches it only by casting.</remarks>
    public interface ISprocketRegistry
    {
        /// <summary>The mod's namespace.</summary>
        string Namespace { get; }

        /// <summary>Starts listening for Sprocket and asks for it, in case it loaded first.</summary>
        void Listen();

        /// <summary>A new scope narrowing the scope numbered <paramref name="narrows" />, or a base where it is -1.</summary>
        SprocketBlock Scope(int narrows);

        /// <summary>A new member of the kind word <paramref name="kind" /> on the scope numbered <paramref name="scope" />; an id the mod makes is checked and must be new.</summary>
        SprocketMember Member(int scope, string kind, string id, bool made);

        /// <summary>Adds <paramref name="type" /> to the mod's types.</summary>
        T Define<T>(T type) where T : SprocketType;

        /// <summary>Tells Sprocket the member <paramref name="id" /> of <paramref name="block" /> may have changed.</summary>
        void Changed(IMyTerminalBlock block, string id);

        /// <summary>Raises the signal <paramref name="id" /> on <paramref name="block" />.</summary>
        void Raise(IMyTerminalBlock block, string id);
    }
}
