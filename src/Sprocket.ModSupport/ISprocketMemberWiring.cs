using System;
using Sandbox.ModAPI;

namespace Sprocket.ModSupport
{
    /// <summary>What the wrapper gives a member to make it a value, a function, an action or an awaitable action, by the type words of the wire.</summary>
    /// <remarks>The wrapper's own plumbing, not for mods: a mod reaches it only by casting.</remarks>
    public interface ISprocketMemberWiring
    {
        /// <summary>Gives the member its own reading <paramref name="read" />, writing <paramref name="write" />, and the type word <paramref name="type" />.</summary>
        SprocketMember Valuing(string type, Func<IMyTerminalBlock, object> read, Action<IMyTerminalBlock, object> write);

        /// <summary>Gives the function its options and its reading <paramref name="ask" />, of the type word <paramref name="type" />.</summary>
        SprocketMember Asking(string type, ISprocketOption[] options, Func<IMyTerminalBlock, object[], object> ask);

        /// <summary>Gives the action its options and what it does, <paramref name="run" />.</summary>
        SprocketMember Running(ISprocketOption[] options, Action<IMyTerminalBlock, object[]> run);

        /// <summary>Gives the awaitable action its options and the task <paramref name="start" /> starts.</summary>
        SprocketMember Starting(ISprocketOption[] options, Func<IMyTerminalBlock, object[], SprocketTask> start);

        /// <summary>Gives the awaitable action its options and <paramref name="kick" />, which starts it; <see cref="SprocketMember.PolledDone" /> says when it is done.</summary>
        SprocketMember Kicking(ISprocketOption[] options, Action<IMyTerminalBlock, object[]> kick);
    }
}
