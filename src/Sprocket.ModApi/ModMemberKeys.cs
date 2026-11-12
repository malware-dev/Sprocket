namespace Sprocket.ModApi
{
    /// <summary>The names both Sprocket and a mod using its API give to the parts of a member's registration.</summary>
    /// <remarks>
    /// A registration travels as a dictionary of these names to values, so a newer mod's extra parts are ignored by an older
    /// Sprocket rather than refused. A name, once used, keeps its meaning.
    /// </remarks>
    public static class ModMemberKeys
    {
        /// <summary>The member's id in a stored program, such as <c>hover.ride_height</c>: a string.</summary>
        public const string Id = "id";

        /// <summary>The block type the member is on, such as <c>MyObjectBuilder_FunctionalBlock</c>: a string.</summary>
        public const string BlockType = "blockType";

        /// <summary>The block subtypes the member is on, every subtype of the type where none is given: a string array.</summary>
        public const string Subtypes = "subtypes";

        /// <summary>What the member is, one of the kind words below: a string.</summary>
        public const string Kind = "kind";

        /// <summary>A value: a state of the block that can be read, and set where it has a writer.</summary>
        public const string KindValue = "value";

        /// <summary>An action: something the block does, which a step can wait on where it says when it is done.</summary>
        public const string KindAction = "action";

        /// <summary>A signal: a change of the block a trigger starts on.</summary>
        public const string KindSignal = "signal";

        /// <summary>What a value holds, one of the type words below: a string.</summary>
        public const string Type = "type";

        /// <summary>On or off: read and written as a bool.</summary>
        public const string TypeFlag = "flag";

        /// <summary>A number: read and written as a double.</summary>
        public const string TypeNumber = "number";

        /// <summary>A whole number: read and written as a long.</summary>
        public const string TypeWhole = "whole";

        /// <summary>One of named choices: read and written as the choice's place among them, an int.</summary>
        public const string TypeChoice = "choice";

        /// <summary>Text: read and written as a string.</summary>
        public const string TypeText = "text";

        /// <summary>A color: read and written as a VRageMath.Color.</summary>
        public const string TypeColor = "color";

        /// <summary>The member's name in the player's language, said now: a Func&lt;string&gt;.</summary>
        public const string Name = "name";

        /// <summary>What the member is, in the player's language, said now, markdown-lite as Sprocket's codex pages are: a Func&lt;string&gt;.</summary>
        public const string Description = "description";

        /// <summary>The group the member stands in: a Sprocket group's id, or the mod's own group's name, said now: a Func&lt;string&gt;.</summary>
        public const string Group = "group";

        /// <summary>Whether <see cref="Group" /> names one of Sprocket's own groups: a bool.</summary>
        public const string GroupIsSprockets = "groupIsSprockets";

        /// <summary>Reads the value on a block: a Func&lt;IMyTerminalBlock, object&gt; returning the type's .NET value.</summary>
        public const string Read = "read";

        /// <summary>Sets the value on a block: an Action&lt;IMyTerminalBlock, object&gt; taking the type's .NET value.</summary>
        public const string Write = "write";

        /// <summary>The least a number may be: a double.</summary>
        public const string Least = "least";

        /// <summary>The most a number may be: a double.</summary>
        public const string Most = "most";

        /// <summary>The unit a number is in, such as m or %: a string.</summary>
        public const string Unit = "unit";

        /// <summary>The ids of a choice's options, in order, as stored programs keep them and so never changed once released: a string array.</summary>
        public const string Choices = "choices";

        /// <summary>The names of a choice's options in the player's language, one for each id in order, said now: a Func&lt;string[]&gt;.</summary>
        public const string ChoiceNames = "choiceNames";

        /// <summary>Runs an action on a block with its options' values in order: an Action&lt;IMyTerminalBlock, object[]&gt;.</summary>
        public const string Run = "run";

        /// <summary>
        /// The options an action takes, in order, each a dictionary of <see cref="Name" />, <see cref="Type" />,
        /// <see cref="Least" />, <see cref="Most" />, <see cref="Unit" /> and <see cref="Choices" />: an object array.
        /// </summary>
        public const string Options = "options";

        /// <summary>Whether an action begun on a block is done, so a step waits on it: a Func&lt;IMyTerminalBlock, bool&gt;.</summary>
        public const string Done = "done";

        /// <summary>Whether the mod calls Changed for this member, so Sprocket listens instead of looking on a cadence: a bool.</summary>
        public const string Announced = "announced";

        /// <summary>For a signal: the id of the value registered before it whose change the signal is: a string.</summary>
        public const string Changes = "changes";

        /// <summary>For a signal: the state of its value the signal fires on reaching, in the value's .NET type: a bool, double, long, int choice or string.</summary>
        public const string Becomes = "becomes";

        /// <summary>For a signal: the words for entering and leaving its state, said now, such as gains grip and loses grip: a Func&lt;string[]&gt;.</summary>
        public const string States = "states";
    }
}
