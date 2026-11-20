namespace Sprocket.ModSupport
{
    /// <summary>
    /// The words a mod and Sprocket share over the mod message channel: the channel, the question a mod asks, and the keys
    /// of the dictionaries that describe a mod, its blocks and its members.
    /// </summary>
    /// <remarks>
    /// Every mod built with this package and every Sprocket build read these, so a key once released is never renamed or
    /// given another meaning; a new one may be added, and a reader passes over a key it does not know.
    /// </remarks>
    public static class SprocketWire
    {
        /// <summary>The mod message channel Sprocket and every mod supporting it talk on.</summary>
        public const long Channel = 38072347710001;

        /// <summary>The version of these words this package speaks.</summary>
        public const int Version = 2;

        /// <summary>What a mod sends to ask Sprocket for its answer.</summary>
        public const string Asking = "sprocket.mod_support.ask";

        /// <summary>In Sprocket's answer: its register function, a <c>Func&lt;Dictionary&lt;string, object&gt;, Dictionary&lt;string, object&gt;&gt;</c>.</summary>
        public const string Register = "register";

        /// <summary>In Sprocket's answer, and in a mod's registration: the version spoken, an int.</summary>
        public const string Spoken = "version";

        /// <summary>In a registration: the mod's namespace, a string.</summary>
        public const string Namespace = "namespace";

        /// <summary>In a registration: the mod's name as players read it, a string.</summary>
        public const string Name = "name";

        /// <summary>In a registration: the mod's author as players read it, a string.</summary>
        public const string Author = "author";

        /// <summary>In a registration: who registers, a string unique to one registration, so a second mod claiming the namespace is told apart.</summary>
        public const string Owner = "owner";

        /// <summary>In a registration: the block scopes, a <c>Dictionary&lt;string, object&gt;[]</c>, each before any scope narrowing it.</summary>
        public const string Scopes = "scopes";

        /// <summary>In a registration: the members, a <c>Dictionary&lt;string, object&gt;[]</c>.</summary>
        public const string Members = "members";

        /// <summary>In a registration: the mod's types, a <c>Dictionary&lt;string, object&gt;[]</c>.</summary>
        public const string Types = "types";

        /// <summary>In Sprocket's reply to a registration: why it was refused, a string; missing where it was taken.</summary>
        public const string Refused = "refused";

        /// <summary>In Sprocket's reply to a registration: tells Sprocket a value may have changed, an <c>Action&lt;IMyTerminalBlock, string&gt;</c> of the block and the member's id.</summary>
        public const string Changed = "changed";

        /// <summary>In Sprocket's reply to a registration: raises a signal, an <c>Action&lt;IMyTerminalBlock, string&gt;</c> of the block and the signal's id.</summary>
        public const string Raise = "raise";

        /// <summary>In Sprocket's reply to a registration: withdraws it, an <c>Action</c>.</summary>
        public const string Withdraw = "withdraw";

        /// <summary>In a scope: its place among the scopes, an int.</summary>
        public const string Index = "index";

        /// <summary>In a scope: the place of the scope it narrows, an int, or -1 for a base.</summary>
        public const string Narrows = "narrows";

        /// <summary>In a scope: the block type's object builder name, a string.</summary>
        public const string BlockType = "block_type";

        /// <summary>In a scope: the subtypes it takes, a <c>string[]</c>; empty for every subtype.</summary>
        public const string Subtypes = "subtypes";

        /// <summary>In a scope or member: the group it stands in, a <c>Func&lt;string&gt;</c> of a group of the mod's own.</summary>
        public const string Group = "group";

        /// <summary>In a scope or member: the group of Sprocket's it stands in, the id of a <see cref="SprocketGroup" />, a string.</summary>
        public const string SprocketsGroup = "sprockets_group";

        /// <summary>In a member: the place of its scope, an int.</summary>
        public const string Scope = "scope";

        /// <summary>In a member: what it is, one of the kind words below.</summary>
        public const string Kind = "kind";

        /// <summary>A member's kind: a value, read and maybe set.</summary>
        public const string KindValue = "value";

        /// <summary>A member's kind: a function, a value taking options.</summary>
        public const string KindFunction = "function";

        /// <summary>A member's kind: an action, done at once.</summary>
        public const string KindAction = "action";

        /// <summary>A member's kind: an awaitable action, which a step waits for.</summary>
        public const string KindAwaitable = "awaitable";

        /// <summary>A member's kind: a signal the mod raises.</summary>
        public const string KindSignal = "signal";

        /// <summary>A member's kind: one of the game's terminal actions or properties, whitelisted for the organized list.</summary>
        public const string KindWhitelisted = "whitelisted";

        /// <summary>A member's kind: details of a member inherited from a wider scope, changed in a narrower one.</summary>
        public const string KindOverride = "override";

        /// <summary>In a member: its id, a string; for a whitelisted member, the game's id.</summary>
        public const string Id = "id";

        /// <summary>In a member, option or word: the name players read, a <c>Func&lt;string&gt;</c>.</summary>
        public const string Named = "named";

        /// <summary>In a member: its description, a <c>Func&lt;string&gt;</c>.</summary>
        public const string Described = "described";

        /// <summary>In a member or option: the type of value, one of the type words below.</summary>
        public const string Type = "type";

        /// <summary>A type of value: on or off.</summary>
        public const string TypeFlag = "flag";

        /// <summary>A type of value: a real number.</summary>
        public const string TypeReal = "real";

        /// <summary>A type of value: an integer.</summary>
        public const string TypeInteger = "integer";

        /// <summary>A type of value: text.</summary>
        public const string TypeText = "text";

        /// <summary>A type of value: a color.</summary>
        public const string TypeColor = "color";

        /// <summary>A type of value: a length of time.</summary>
        public const string TypeDuration = "duration";

        /// <summary>A type of value: one of a host type's words.</summary>
        public const string TypeChoice = "choice";

        /// <summary>In a member or option of a choice: the host type's full name, a string.</summary>
        public const string Of = "of";

        /// <summary>In a value: reads it, a <c>Func&lt;IMyTerminalBlock, object&gt;</c>.</summary>
        public const string Read = "read";

        /// <summary>In a value: sets it, an <c>Action&lt;IMyTerminalBlock, object&gt;</c>.</summary>
        public const string Write = "write";

        /// <summary>In a function: reads it, a <c>Func&lt;IMyTerminalBlock, object[], object&gt;</c> given the options' values.</summary>
        public const string Ask = "ask";

        /// <summary>In an action: does it, an <c>Action&lt;IMyTerminalBlock, object[]&gt;</c> given the options' values.</summary>
        public const string Run = "run";

        /// <summary>In an awaitable action: starts it, a <c>Func&lt;IMyTerminalBlock, object[], Dictionary&lt;string, object&gt;&gt;</c> returning the task.</summary>
        public const string Start = "start";

        /// <summary>In a function, action or whitelisted action: its options, a <c>Dictionary&lt;string, object&gt;[]</c> in order.</summary>
        public const string Options = "options";

        /// <summary>In a value or option: its unit as players read it, a string.</summary>
        public const string Unit = "unit";

        /// <summary>In a value or option: read and written as a share of a whole, from 0 to 1, a bool.</summary>
        public const string Percent = "percent";

        /// <summary>In a value or option: the least it may be, a <c>Func&lt;IMyTerminalBlock, double&gt;</c>.</summary>
        public const string Least = "least";

        /// <summary>In a value or option: the most it may be, a <c>Func&lt;IMyTerminalBlock, double&gt;</c>.</summary>
        public const string Most = "most";

        /// <summary>In a flag: how it reads when on, a <c>Func&lt;string&gt;</c>.</summary>
        public const string ReadsOn = "reads_on";

        /// <summary>In a flag: how it reads when off, a <c>Func&lt;string&gt;</c>.</summary>
        public const string ReadsOff = "reads_off";

        /// <summary>In a value: the word a sentence puts between it and its block, a string such as "on".</summary>
        public const string Joined = "joined";

        /// <summary>In a value: whether the mod tells Sprocket of every change, a bool.</summary>
        public const string Announced = "announced";

        /// <summary>In a type: its words, a <c>string[]</c>, as programs write them.</summary>
        public const string Words = "words";

        /// <summary>In a type: the names players read for its words, a <c>Func&lt;string&gt;[]</c>, one per word.</summary>
        public const string WordNames = "word_names";

        /// <summary>In a task: has Sprocket hear how it ends, an <c>Action&lt;Action&lt;string, string, string&gt;&gt;</c> of the outcome word, failure id and message.</summary>
        public const string Hear = "hear";

        /// <summary>In a task: tells it Sprocket wants it stopped, an <c>Action&lt;bool, string&gt;</c> of whether that is final and the cause.</summary>
        public const string Cancel = "cancel";

        /// <summary>In a task: lets it look at what it waits for, an <c>Action</c> Sprocket calls on its own cadence while a step waits on the task.</summary>
        public const string Poll = "poll";

        /// <summary>A task's outcome: it got there.</summary>
        public const string Completed = "completed";

        /// <summary>A task's outcome: it went wrong.</summary>
        public const string Failed = "failed";

        /// <summary>A task's outcome: something took it out of the mod's hands.</summary>
        public const string Conflicted = "conflicted";

        /// <summary>A task's outcome: the mod agreed to stop.</summary>
        public const string Cancelled = "cancelled";

        /// <summary>A task's state, not an end: it is not getting there now, with a message, or no longer, with none.</summary>
        public const string Stuck = "stuck";
    }
}
